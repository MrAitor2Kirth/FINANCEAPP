using Microsoft.EntityFrameworkCore;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Implementación de <see cref="IPortfolioPeriodService"/> (SPEC-01-09). Solo lee: no guarda nada en las empresas
    /// ni en las operaciones. Las cifras de cada empresa se calculan en copias de <see cref="CompanyDetail"/> para
    /// reutilizar sus fórmulas (valor = precio × acciones) y las de <see cref="InvestedCompaniesSummary"/>.
    /// </summary>
    public class PortfolioPeriodService : IPortfolioPeriodService
    {
        // Tipos de operación (literales, SPEC-02): las compras y traspasos suman posición; las ventas antiguas abiertas restan
        private const string TypeBuy = "Compra";
        private const string TypeTransfer = "Traspaso";
        private const string TypeSell = "Venta";
        private const string TypeDividend = "Dividendo";

        private readonly AppDbContext _context;
        private readonly ICountryService _countryService;
        private readonly IPriceHistoryService _priceHistoryService;
        private readonly ICurrencyHistoryService _currencyHistoryService;

        public PortfolioPeriodService(
            AppDbContext context,
            ICountryService countryService,
            IPriceHistoryService priceHistoryService,
            ICurrencyHistoryService currencyHistoryService)
        {
            _context = context;
            _countryService = countryService;
            _priceHistoryService = priceHistoryService;
            _currencyHistoryService = currencyHistoryService;
        }

        public async Task<PeriodSummary> GetPurchasesSummaryAsync(SalesPeriodFilter filter)
        {
            var (from, toExclusive) = filter.GetRange();
            var summary = new PeriodSummary { ShowReturnWithDividends = false };
            if (from == null || toExclusive == null)
            {
                return summary;
            }

            if (from.Value > DateTime.Today)
            {
                summary.IsFuture = true;
                return summary;
            }

            var companies = await LoadCompaniesAsync();
            var operations = await _context.Operations
                .AsNoTracking()
                .Where(o => o.Date >= from.Value && o.Date < toExclusive.Value)
                .Where(o => o.Type == TypeBuy || o.Type == TypeTransfer || o.Type == TypeDividend)
                .ToListAsync();

            var factors = await GetCurrentFactorsAsync();
            summary.Totals.BaseCurrencyCode = factors.BaseCurrency;

            // Compras del periodo que sigues teniendo, valoradas hoy (RF-3)
            var copies = new List<CompanyDetail>();
            foreach (var group in operations.Where(o => o.Type != TypeDividend && o.CloseDate == null).GroupBy(o => o.CompanyName))
            {
                if (!companies.TryGetValue(group.Key, out var company))
                {
                    continue;
                }

                var copy = CopyOf(company);
                copy.TotalInvestedInitial = group.Sum(o => o.Amount);
                copy.NumberOfShares = ShareQuantity.Round(group.Sum(o => o.NumberOfShares));
                copy.TotalDividends = 0m;
                copies.Add(copy);
            }

            Convert(summary, copies, factors.FactorFor);

            // Compras del periodo ya vendidas: fuera de las tarjetas, en la nota
            summary.SoldPurchasesAmount = Round(operations
                .Where(o => o.Type != TypeDividend && o.CloseDate != null && companies.ContainsKey(o.CompanyName))
                .Sum(o => o.Amount * (factors.FactorFor(companies[o.CompanyName].CurrencyCode) ?? 0m)));

            // Dividendos cobrados en el periodo, de todas las empresas (no van por compra)
            summary.PeriodDividends = Round(operations
                .Where(o => o.Type == TypeDividend && companies.ContainsKey(o.CompanyName))
                .Sum(o => o.Amount * (factors.FactorFor(companies[o.CompanyName].CurrencyCode) ?? 0m)));

            return summary;
        }

        public async Task<PeriodSummary> GetClosingSummaryAsync(SalesPeriodFilter filter, CancellationToken cancellationToken = default)
        {
            var (from, toExclusive) = filter.GetRange();
            var summary = new PeriodSummary { ShowReturnWithDividends = true };
            if (from == null || toExclusive == null)
            {
                return summary;
            }

            if (from.Value > DateTime.Today)
            {
                summary.IsFuture = true;
                return summary;
            }

            // Último día del periodo, o hoy si el periodo no ha terminado (RF-5)
            var cutoff = toExclusive.Value.AddDays(-1).Date;
            if (cutoff > DateTime.Today)
            {
                cutoff = DateTime.Today;
            }

            var isToday = cutoff == DateTime.Today;
            summary.CutoffDate = cutoff;

            var companies = await LoadCompaniesAsync();
            var endOfCutoff = cutoff.AddDays(1);
            var operations = await _context.Operations
                .AsNoTracking()
                .Where(o => o.Date < endOfCutoff)
                .ToListAsync();

            // Posición en la fecha de corte: abiertas entonces (sin cerrar o cerradas después), con las reglas del cálculo
            // de hoy (RecalculateTotalInvestedAsync). Las filas de un lote partido suman lo mismo que el lote original.
            var openAtCutoff = operations
                .Where(o => o.Type != TypeDividend && (o.CloseDate == null || o.CloseDate.Value.Date > cutoff))
                .ToList();

            var copies = new List<CompanyDetail>();
            foreach (var group in openAtCutoff.GroupBy(o => o.CompanyName))
            {
                if (!companies.TryGetValue(group.Key, out var company))
                {
                    continue;
                }

                var buys = group.Where(o => o.Type == TypeBuy || o.Type == TypeTransfer).ToList();
                var sells = group.Where(o => o.Type == TypeSell).ToList();
                var shares = ShareQuantity.Round(buys.Sum(o => o.NumberOfShares) - sells.Sum(o => o.NumberOfShares));
                if (shares <= 0)
                {
                    continue;
                }

                var copy = CopyOf(company);
                copy.TotalInvestedInitial = buys.Sum(o => o.Amount) - sells.Sum(o => o.Amount);
                copy.NumberOfShares = shares;
                copy.TotalDividends = operations
                    .Where(o => o.Type == TypeDividend && o.CompanyName == group.Key)
                    .Sum(o => o.Amount);
                copies.Add(copy);
            }

            var withoutHistory = new List<string>();
            if (!isToday)
            {
                // Precio de cierre de esa fecha (o del último día anterior); descarga el histórico que falte
                foreach (var copy in copies)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    decimal? close = null;
                    if (!string.IsNullOrWhiteSpace(copy.Ticker))
                    {
                        var series = ChartSeriesRef.ForCompany(copy.Id, copy.Ticker, copy.Name);
                        await _priceHistoryService.EnsureDailyCoverageAsync(series, cutoff, cancellationToken);
                        close = await _priceHistoryService.GetDailyCloseAtAsync(series, cutoff);
                    }

                    if (close is > 0)
                    {
                        copy.LivePrice = close.Value;
                    }
                    else
                    {
                        withoutHistory.Add(copy.Name);
                    }
                }
            }

            // Tipo de cambio de la fecha de corte (hoy: el actual, como las tarjetas de hoy)
            var current = await GetCurrentFactorsAsync();
            summary.Totals.BaseCurrencyCode = current.BaseCurrency;
            var factors = new Dictionary<string, decimal?>();
            var withoutRateHistory = new List<string>();
            foreach (var currency in copies.Select(c => CurrencyFormatter.Normalize(c.CurrencyCode)).Distinct())
            {
                if (currency == current.BaseCurrency || isToday)
                {
                    factors[currency] = current.FactorFor(currency);
                    continue;
                }

                var rates = await _currencyHistoryService.GetRatesAtAsync(currency, current.BaseCurrency, new[] { cutoff }, cancellationToken);
                if (rates.TryGetValue(cutoff, out var rate))
                {
                    factors[currency] = rate;
                }
                else
                {
                    factors[currency] = current.FactorFor(currency);
                    withoutRateHistory.Add(currency);
                }
            }

            Convert(summary, copies, currency => factors.TryGetValue(CurrencyFormatter.Normalize(currency), out var factor) ? factor : null);

            if (withoutHistory.Count > 0)
            {
                summary.Warnings.Add($"Sin cotización guardada en esa fecha para {string.Join(", ", withoutHistory)}: se usa su precio actual (cifra aproximada).");
            }

            if (withoutRateHistory.Count > 0)
            {
                summary.Warnings.Add($"Sin histórico de tipos de cambio para {string.Join(", ", withoutRateHistory)}: se usa el tipo actual (cifra aproximada).");
            }

            return summary;
        }

        // ---------------------------------------------------------------- Auxiliares

        /// <summary>Empresas por nombre (las operaciones se enlazan por nombre)</summary>
        private async Task<Dictionary<string, CompanyDetail>> LoadCompaniesAsync()
        {
            var companies = await _context.CompanyDetails.AsNoTracking().ToListAsync();
            return companies
                .GroupBy(c => c.Name)
                .ToDictionary(g => g.Key, g => g.First());
        }

        /// <summary>Copia sin seguimiento con los datos que usan las tarjetas</summary>
        private static CompanyDetail CopyOf(CompanyDetail company) => new()
        {
            Id = company.Id,
            Name = company.Name,
            Ticker = company.Ticker,
            Sector = company.Sector,
            CurrencyCode = company.CurrencyCode,
            DividendYield = company.DividendYield,
            CurrentPrice = company.CurrentPrice,
            LivePrice = company.LivePrice,
            LivePriceUpdatedAt = company.LivePriceUpdatedAt
        };

        /// <summary>
        /// Suma las copias convertidas a la divisa base con el factor de cada divisa (null = sin tipo: queda fuera y se
        /// cuenta). Mismas reglas que <see cref="IExchangeRateService.GetConvertedTotalsAsync"/>, con el factor que toque.
        /// </summary>
        private static void Convert(PeriodSummary summary, IEnumerable<CompanyDetail> copies, Func<string?, decimal?> factorFor)
        {
            var totals = summary.Totals;
            decimal initial = 0, value = 0, dividends = 0;
            foreach (var copy in copies)
            {
                var factor = factorFor(copy.CurrencyCode);
                if (!factor.HasValue)
                {
                    totals.MissingRateCompanies++;
                    continue;
                }

                // Rentabilidad de la fila con las cifras del periodo (las columnas guardadas son las de hoy; SPEC-01-10)
                copy.PlusvaliaReturnPercent = Round(copy.CalculatePlusvaliaReturnPercent());
                copy.DividendPlusvaliaReturnPercent = Round(copy.CalculateDividendPlusvaliaReturnPercent());

                initial += copy.TotalInvestedInitial * factor.Value;
                totals.InitialByCompanyId[copy.Id] = copy.TotalInvestedInitial * factor.Value;
                value += copy.TotalInvestedValue * factor.Value;
                dividends += copy.TotalDividends * factor.Value;
                totals.Included.Add(copy);
                summary.Companies.Add(copy);
            }

            totals.InvestedInitial = Round(initial);
            totals.CurrentValue = Round(value);
            totals.Dividends = Round(dividends);
        }

        /// <summary>Divisa base y factor actual de cada divisa (1 en la base; null sin tipo guardado)</summary>
        private async Task<CurrencyFactors> GetCurrentFactorsAsync()
        {
            var baseCurrency = CurrencyFormatter.Normalize((await _countryService.GetBaseCurrencyAsync()).CurrencyCode);
            var rates = await _context.ExchangeRates
                .AsNoTracking()
                .Where(r => r.BaseCurrencyCode == baseCurrency)
                .ToDictionaryAsync(r => r.CurrencyCode, r => r.Rate);

            return new CurrencyFactors(baseCurrency, currency =>
            {
                var code = CurrencyFormatter.Normalize(currency);
                if (code == baseCurrency)
                {
                    return 1m;
                }

                return rates.TryGetValue(code, out var rate) && rate > 0 ? rate : null;
            });
        }

        private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private sealed record CurrencyFactors(string BaseCurrency, Func<string?, decimal?> FactorFor);
    }
}
