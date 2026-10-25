using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Suma lo vendido a las tarjetas y al listado de /companies con «Incluir vendidas» (SPEC-01-11).
    /// Clase estática sin estado ni acceso a datos: la página solo la invoca. Las filas son copias (nunca entidades
    /// del contexto) para no tocar las cifras guardadas.
    /// </summary>
    public static class SoldPositionsSummary
    {
        /// <summary>
        /// Totales de la vista con lo vendido (RF-3): invertido + coste de lo vendido, valor + cobrado neto y
        /// dividendos + los de las vendidas del todo. El peso (InitialByCompanyId) sigue siendo el de la cartera abierta.
        /// Las rentabilidades y el resultado salen de <see cref="InvestedCompaniesSummary"/> con estos totales.
        /// </summary>
        public static ConvertedCompanyTotals Combine(ConvertedCompanyTotals open, SoldSummary sold) => new()
        {
            BaseCurrencyCode = open.BaseCurrencyCode,
            InvestedInitial = Round(open.InvestedInitial + sold.Cost),
            CurrentValue = Round(open.CurrentValue + sold.NetProceeds),
            Dividends = Round(open.Dividends + sold.Dividends),
            Included = open.Included,
            MissingRateCompanies = open.MissingRateCompanies,
            InitialByCompanyId = open.InitialByCompanyId
        };

        /// <summary>
        /// Filas del listado con lo vendido (RF-4). Las empresas abiertas con ventas pasan a copias «Parcial» con su
        /// rentabilidad total; las vendidas del todo se añaden al final como copias «Vendida», en el orden del catálogo.
        /// </summary>
        /// <param name="openRows">Filas del listado corto de la vista (empresas con acciones)</param>
        /// <param name="catalog">Todas las empresas, en el orden en que se muestran</param>
        /// <param name="sold">Lo vendido de la vista</param>
        public static SoldRows BuildRows(IReadOnlyList<CompanyDetail> openRows, IReadOnlyList<CompanyDetail> catalog, SoldSummary sold)
        {
            var result = new SoldRows();
            var byId = sold.ByCompany.Values.ToDictionary(s => s.CompanyId);
            var openIds = new HashSet<int>();

            foreach (var row in openRows)
            {
                openIds.Add(row.Id);
                if (byId.TryGetValue(row.Id, out var info))
                {
                    result.Info[row.Id] = info;
                    result.ShortRows.Add(PartialCopy(row, info));
                }
                else
                {
                    result.ShortRows.Add(row);
                }
            }

            // Vendidas del todo (o con ventas pero sin fila abierta en la vista): al final, en el orden del catálogo
            foreach (var company in catalog)
            {
                if (openIds.Contains(company.Id) || !byId.TryGetValue(company.Id, out var info))
                {
                    continue;
                }

                result.Info[company.Id] = info;
                result.ShortRows.Add(SoldCopy(company, info));
            }

            // Lista extendible: todo el catálogo en su orden, con las mismas copias para las empresas con ventas
            foreach (var company in catalog)
            {
                if (!byId.TryGetValue(company.Id, out var info))
                {
                    result.ExtendedRows.Add(company);
                }
                else
                {
                    result.ExtendedRows.Add(info.Status == SoldStatus.Partial && company.NumberOfShares > 0
                        ? PartialCopy(company, info)
                        : SoldCopy(company, info));
                }
            }

            return result;
        }

        /// <summary>Inversión de la fila: lo abierto más el coste de lo vendido, en la divisa de la empresa</summary>
        public static decimal Invested(CompanyDetail row, SoldCompany? info) =>
            row.TotalInvestedInitial + (info?.CostOriginal ?? 0m);

        /// <summary>Valor de la fila: lo abierto a precio actual más el cobrado neto, en la divisa de la empresa</summary>
        public static decimal Value(CompanyDetail row, SoldCompany? info) =>
            row.TotalInvestedValue + (info?.NetProceedsOriginal ?? 0m);

        /// <summary>
        /// Media simple del yield de las filas con acciones (RF-3: una vendida del todo no tiene yield propio)
        /// </summary>
        public static decimal? AverageDividendYield(IEnumerable<CompanyDetail> rows, IReadOnlyDictionary<int, SoldCompany> info) =>
            InvestedCompaniesSummary.AverageDividendYield(rows
                .Where(r => !info.TryGetValue(r.Id, out var s) || s.Status != SoldStatus.Sold)
                .ToList());

        // Copia «Parcial»: las cifras abiertas de la fila y la rentabilidad sobre lo abierto + lo vendido
        private static CompanyDetail PartialCopy(CompanyDetail row, SoldCompany info)
        {
            var copy = CopyOf(row);
            copy.TotalInvestedInitial = row.TotalInvestedInitial;
            copy.NumberOfShares = row.NumberOfShares;
            copy.TotalDividends = row.TotalDividends;
            SetReturns(copy, Invested(row, info), Value(row, info), row.TotalDividends);
            return copy;
        }

        // Copia «Vendida»: sin acciones ni inversión abierta; el coste y el cobrado van en SoldCompany
        private static CompanyDetail SoldCopy(CompanyDetail company, SoldCompany info)
        {
            var copy = CopyOf(company);
            copy.TotalInvestedInitial = 0m;
            copy.NumberOfShares = 0m;
            copy.TotalDividends = info.DividendsOriginal;
            SetReturns(copy, info.CostOriginal, info.NetProceedsOriginal, info.DividendsOriginal);
            return copy;
        }

        private static void SetReturns(CompanyDetail copy, decimal invested, decimal value, decimal dividends)
        {
            copy.PlusvaliaReturnPercent = invested > 0 ? Round((value - invested) / invested * 100m) : 0m;
            copy.DividendPlusvaliaReturnPercent = invested > 0 ? Round((value + dividends - invested) / invested * 100m) : 0m;
        }

        private static CompanyDetail CopyOf(CompanyDetail company) => new()
        {
            Id = company.Id,
            Name = company.Name,
            Ticker = company.Ticker,
            Sector = company.Sector,
            CountryCode = company.CountryCode,
            CurrencyCode = company.CurrencyCode,
            DividendYield = company.DividendYield,
            CurrentPrice = company.CurrentPrice,
            LivePrice = company.LivePrice,
            LivePriceUpdatedAt = company.LivePriceUpdatedAt,
            NextDividendDate = company.NextDividendDate
        };

        private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Filas del listado de /companies con «Incluir vendidas» (SPEC-01-11)
    /// </summary>
    public class SoldRows
    {
        /// <summary>Listado corto: filas abiertas (las parciales como copias) y, al final, las vendidas del todo</summary>
        public List<CompanyDetail> ShortRows { get; } = new();

        /// <summary>Lista extendible: todo el catálogo, con copias para las empresas con ventas</summary>
        public List<CompanyDetail> ExtendedRows { get; } = new();

        /// <summary>Lo vendido de cada empresa con ventas, por Id (estado, coste y cobrado en su divisa)</summary>
        public Dictionary<int, SoldCompany> Info { get; } = new();
    }
}
