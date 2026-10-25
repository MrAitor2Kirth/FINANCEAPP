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
        private readonly IExchangeRateService _exchangeRateService;

        public PortfolioPeriodService(
            AppDbContext context,
            ICountryService countryService,
            IPriceHistoryService priceHistoryService,
            ICurrencyHistoryService currencyHistoryService,
            IExchangeRateService exchangeRateService)
        {
            _context = context;
            _countryService = countryService;
            _priceHistoryService = priceHistoryService;
            _currencyHistoryService = currencyHistoryService;
            _exchangeRateService = exchangeRateService;
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
                    var close = await GetCloseAtAsync(copy, cutoff, cancellationToken);
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

        public async Task<SoldSummary> GetSoldAsync(SalesPeriodFilter filter, PeriodSummaryView view, CancellationToken cancellationToken = default)
        {
            var result = new SoldSummary();
            var isAll = filter.Mode == SalesPeriodMode.All;
            var (from, toExclusive) = filter.GetRange();
            if (!isAll && (from == null || toExclusive == null || from.Value > DateTime.Today))
            {
                return result;
            }

            // Fecha de corte de «Cartera al cierre»: la misma que GetClosingSummaryAsync
            DateTime? cutoff = null;
            if (!isAll && view == PeriodSummaryView.Closing)
            {
                cutoff = toExclusive!.Value.AddDays(-1).Date;
                if (cutoff > DateTime.Today)
                {
                    cutoff = DateTime.Today;
                }
            }
            var purchasesView = !isAll && view == PeriodSummaryView.Purchases;

            var companies = await LoadCompaniesAsync();
            var sales = await _context.SalesHistory.AsNoTracking().ToListAsync(cancellationToken);

            // Ventas de empresas ya borradas: sin empresa no hay divisa ni fila (RF-2)
            var deletedCompanySales = sales.Count(s => !companies.ContainsKey(s.CompanyName));
            sales = sales.Where(s => companies.ContainsKey(s.CompanyName)).ToList();
            if (cutoff.HasValue)
            {
                sales = sales.Where(s => s.SaleDate.Date <= cutoff.Value).ToList();
            }

            var currencyByCompany = companies.ToDictionary(kv => kv.Key, kv => kv.Value.CurrencyCode);
            var converted = await _exchangeRateService.GetConvertedSalesAsync(sales, currencyByCompany);

            int approximate = 0, missing = 0;
            var anyForeign = false;
            decimal cost = 0, proceeds = 0;
            foreach (var sale in converted)
            {
                if (sale.MissingRate)
                {
                    missing++;
                    continue;
                }

                decimal saleCost, saleProceeds, saleCostOriginal, saleProceedsOriginal;
                if (purchasesView)
                {
                    // Solo las compras del periodo que cerró esta venta, con su parte del cobrado (RF-5)
                    var lots = sale.Lots.Where(l => l.Date >= from!.Value && l.Date < toExclusive!.Value).ToList();
                    if (lots.Count == 0 || sale.Shares <= 0)
                    {
                        continue;
                    }

                    var share = lots.Sum(l => l.Shares) / sale.Shares;
                    saleCost = lots.Sum(l => l.Cost);
                    saleCostOriginal = lots.Sum(l => l.CostOriginal);
                    saleProceeds = sale.NetProceeds * share;
                    saleProceedsOriginal = sale.NetProceedsOriginal * share;
                }
                else
                {
                    saleCost = sale.Cost;
                    saleCostOriginal = sale.CostOriginal;
                    saleProceeds = sale.NetProceeds;
                    saleProceedsOriginal = sale.NetProceedsOriginal;
                }

                if (sale.Approximate)
                {
                    approximate++;
                }
                anyForeign |= sale.IsForeign;

                if (!result.ByCompany.TryGetValue(sale.CompanyName, out var sold))
                {
                    sold = new SoldCompany { CompanyId = companies[sale.CompanyName].Id };
                    result.ByCompany[sale.CompanyName] = sold;
                }

                sold.Cost += saleCost;
                sold.NetProceeds += saleProceeds;
                sold.CostOriginal += saleCostOriginal;
                sold.NetProceedsOriginal += saleProceedsOriginal;
                cost += saleCost;
                proceeds += saleProceeds;
            }

            if (result.ByCompany.Count > 0)
            {
                await ApplySoldStatusAsync(result, companies, isAll, purchasesView, from, toExclusive, cutoff, cancellationToken);
            }

            result.Cost = Round(cost);
            result.NetProceeds = Round(proceeds);
            foreach (var sold in result.ByCompany.Values)
            {
                sold.Cost = Round(sold.Cost);
                sold.NetProceeds = Round(sold.NetProceeds);
                sold.CostOriginal = Round(sold.CostOriginal);
                sold.NetProceedsOriginal = Round(sold.NetProceedsOriginal);
            }

            if (anyForeign)
            {
                result.Notes.Add("Lo vendido se convierte con el tipo de cambio guardado en cada venta y en sus compras.");
            }
            if (approximate > 0)
            {
                result.Notes.Add($"{approximate} {(approximate == 1 ? "venta usa" : "ventas usan")} el tipo de cambio actual porque faltaba el guardado (cifra aproximada).");
            }
            if (missing > 0)
            {
                result.Notes.Add($"{missing} {(missing == 1 ? "venta en otra divisa sin tipo de cambio no se incluye" : "ventas en otra divisa sin tipo de cambio no se incluyen")}.");
            }
            if (deletedCompanySales > 0)
            {
                result.Notes.Add($"{deletedCompanySales} {(deletedCompanySales == 1 ? "venta de una empresa borrada no cuenta" : "ventas de empresas borradas no cuentan")}.");
            }

            return result;
        }

        public async Task<CompanyBreakdown?> GetBreakdownAsync(string companyName, SalesPeriodFilter filter, PeriodSummaryView view, bool includeSold, CancellationToken cancellationToken = default)
        {
            var companies = await LoadCompaniesAsync();
            if (!companies.TryGetValue(companyName, out var company))
            {
                return null;
            }

            var result = new CompanyBreakdown { CompanyName = company.Name, CurrencyCode = company.CurrencyCode };
            var isAll = filter.Mode == SalesPeriodMode.All;
            var (from, toExclusive) = filter.GetRange();
            if (!isAll && (from == null || toExclusive == null || from.Value > DateTime.Today))
            {
                return result;
            }

            var purchasesView = !isAll && view == PeriodSummaryView.Purchases;
            DateTime? cutoff = null;
            if (!isAll && view == PeriodSummaryView.Closing)
            {
                cutoff = toExclusive!.Value.AddDays(-1).Date;
                if (cutoff > DateTime.Today)
                {
                    cutoff = DateTime.Today;
                }
            }
            result.ShowReturnWithDividends = !purchasesView;

            var operations = await _context.Operations
                .AsNoTracking()
                .Where(o => o.CompanyName == companyName)
                .ToListAsync(cancellationToken);

            // Mismas operaciones que la fila de la vista (SPEC-01-09, RF-3 y RF-5; SPEC-01-10)
            List<Operation> open;
            if (isAll)
            {
                open = operations.Where(o => o.Type != TypeDividend && o.CloseDate == null).ToList();
            }
            else if (purchasesView)
            {
                open = operations.Where(o => (o.Type == TypeBuy || o.Type == TypeTransfer) && o.CloseDate == null
                                             && o.Date >= from!.Value && o.Date < toExclusive!.Value).ToList();
            }
            else
            {
                open = operations.Where(o => o.Type != TypeDividend && o.Date.Date <= cutoff!.Value
                                             && (o.CloseDate == null || o.CloseDate.Value.Date > cutoff.Value)).ToList();
            }

            // Precio de la vista: el de hoy o el cierre de la fecha de corte (como la copia de GetClosingSummaryAsync)
            var price = company.LivePrice ?? company.CurrentPrice;
            if (cutoff.HasValue && cutoff.Value != DateTime.Today && open.Count > 0)
            {
                var close = await GetCloseAtAsync(company, cutoff.Value, cancellationToken);
                if (close is > 0)
                {
                    price = close.Value;
                }
                else
                {
                    result.Warnings.Add("Sin cotización guardada en la fecha de corte: se usa el precio actual (cifra aproximada).");
                }
            }

            decimal invested = 0, value = 0, dividends = 0;
            foreach (var operation in open)
            {
                var isOldSale = operation.Type == TypeSell;
                var shares = isOldSale ? -operation.NumberOfShares : operation.NumberOfShares;
                var cost = isOldSale ? -operation.Amount : operation.Amount;
                var lineValue = shares * price;
                invested += cost;
                value += lineValue;
                result.Lines.Add(new BreakdownLine
                {
                    Kind = isOldSale ? BreakdownLineKind.OldSale : BreakdownLineKind.OpenLot,
                    Date = operation.Date,
                    Label = isOldSale ? "Venta antigua" : operation.Type,
                    Shares = shares,
                    Price = operation.PricePerShare > 0 ? operation.PricePerShare : null,
                    Cost = Round(cost),
                    Value = Round(lineValue),
                    Result = Round(lineValue - cost),
                    ResultPercent = !isOldSale && cost > 0 ? Round((lineValue - cost) / cost * 100m) : null
                });
            }

            // Lo vendido de la vista (SPEC-01-11): mismas reglas que GetSoldAsync, en la divisa de la empresa
            if (includeSold)
            {
                var sales = await _context.SalesHistory
                    .AsNoTracking()
                    .Where(s => s.CompanyName == companyName)
                    .ToListAsync(cancellationToken);
                if (cutoff.HasValue)
                {
                    sales = sales.Where(s => s.SaleDate.Date <= cutoff.Value).ToList();
                }

                var converted = await _exchangeRateService.GetConvertedSalesAsync(
                    sales, new Dictionary<string, string> { [company.Name] = company.CurrencyCode });
                foreach (var sale in converted.Where(s => !s.MissingRate))
                {
                    decimal saleCost = sale.CostOriginal, saleProceeds = sale.NetProceedsOriginal, saleShares = sale.Shares;
                    if (purchasesView)
                    {
                        var lots = sale.Lots.Where(l => l.Date >= from!.Value && l.Date < toExclusive!.Value).ToList();
                        if (lots.Count == 0 || sale.Shares <= 0)
                        {
                            continue;
                        }

                        saleShares = lots.Sum(l => l.Shares);
                        saleCost = lots.Sum(l => l.CostOriginal);
                        saleProceeds = sale.NetProceedsOriginal * saleShares / sale.Shares;
                    }

                    if (sale.Approximate)
                    {
                        result.Warnings.Add($"La venta del {sale.SaleDate:dd/MM/yyyy} usa algún tipo de cambio actual porque faltaba el guardado.");
                    }

                    invested += saleCost;
                    value += saleProceeds;
                    result.Lines.Add(new BreakdownLine
                    {
                        Kind = BreakdownLineKind.Sale,
                        Date = sale.SaleDate,
                        Label = "Venta",
                        Shares = -saleShares,
                        Cost = Round(saleCost),
                        Value = Round(saleProceeds),
                        Result = Round(saleProceeds - saleCost),
                        ResultPercent = saleCost > 0 ? Round((saleProceeds - saleCost) / saleCost * 100m) : null
                    });
                }
            }

            // Dividendos que cuenta la fila: todos (Total histórico) o hasta el corte; ninguno en «Compras del periodo»
            if (!purchasesView)
            {
                foreach (var dividend in operations.Where(o => o.Type == TypeDividend && (!cutoff.HasValue || o.Date.Date <= cutoff.Value)))
                {
                    dividends += dividend.Amount;
                    result.Lines.Add(new BreakdownLine
                    {
                        Kind = BreakdownLineKind.Dividend,
                        Date = dividend.Date,
                        Label = "Dividendo",
                        Result = Round(dividend.Amount)
                    });
                }
            }

            result.Lines = result.Lines.OrderBy(l => l.Date).ThenBy(l => l.Kind).ToList();
            result.Invested = Round(invested);
            result.Value = Round(value);
            result.Dividends = Round(dividends);
            if (invested > 0)
            {
                result.ReturnPercent = Round((value - invested) / invested * 100m);
                result.ReturnWithDividendsPercent = Round((value + dividends - invested) / invested * 100m);
            }

            return result;
        }

        // ---------------------------------------------------------------- Auxiliares

        /// <summary>
        /// Precio de cierre de una empresa en una fecha (o del último día anterior); descarga el histórico que falte.
        /// Null si la empresa no tiene ticker o no hay cotización.
        /// </summary>
        private async Task<decimal?> GetCloseAtAsync(CompanyDetail company, DateTime date, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(company.Ticker))
            {
                return null;
            }

            var series = ChartSeriesRef.ForCompany(company.Id, company.Ticker, company.Name);
            await _priceHistoryService.EnsureDailyCoverageAsync(series, date, cancellationToken);
            return await _priceHistoryService.GetDailyCloseAtAsync(series, date);
        }

        /// <summary>
        /// Marca cada empresa con ventas como Vendida o Parcial según le queden acciones en la vista (RF-4) y suma los
        /// dividendos de las vendidas del todo (RF-3). En «Compras del periodo» los dividendos ya cuentan todos en
        /// PeriodDividends, así que aquí no se suman.
        /// </summary>
        private async Task ApplySoldStatusAsync(
            SoldSummary result,
            Dictionary<string, CompanyDetail> companies,
            bool isAll,
            bool purchasesView,
            DateTime? from,
            DateTime? toExclusive,
            DateTime? cutoff,
            CancellationToken cancellationToken)
        {
            var names = result.ByCompany.Keys.ToList();
            var operations = await _context.Operations
                .AsNoTracking()
                .Where(o => names.Contains(o.CompanyName))
                .ToListAsync(cancellationToken);

            var current = await GetCurrentFactorsAsync();
            decimal dividends = 0;
            foreach (var (name, sold) in result.ByCompany)
            {
                var company = companies[name];
                var own = operations.Where(o => o.CompanyName == name).ToList();
                bool hasShares;
                if (isAll)
                {
                    hasShares = company.NumberOfShares > 0;
                }
                else if (purchasesView)
                {
                    hasShares = own.Any(o => (o.Type == TypeBuy || o.Type == TypeTransfer) && o.CloseDate == null
                                             && o.Date >= from!.Value && o.Date < toExclusive!.Value);
                }
                else
                {
                    var open = own.Where(o => o.Type != TypeDividend && o.Date.Date <= cutoff!.Value
                                              && (o.CloseDate == null || o.CloseDate.Value.Date > cutoff.Value)).ToList();
                    hasShares = open.Where(o => o.Type == TypeBuy || o.Type == TypeTransfer).Sum(o => o.NumberOfShares)
                                - open.Where(o => o.Type == TypeSell).Sum(o => o.NumberOfShares) > 0;
                }

                sold.Status = hasShares ? SoldStatus.Partial : SoldStatus.Sold;
                if (sold.Status == SoldStatus.Partial || purchasesView)
                {
                    continue;
                }

                // Dividendos de la vendida: todos (Total histórico) o hasta el corte («Cartera al cierre»)
                sold.DividendsOriginal = isAll
                    ? company.TotalDividends
                    : own.Where(o => o.Type == TypeDividend && o.Date.Date <= cutoff!.Value).Sum(o => o.Amount);

                var currency = CurrencyFormatter.Normalize(company.CurrencyCode);
                decimal? factor = current.FactorFor(currency);
                if (cutoff.HasValue && cutoff.Value != DateTime.Today && currency != current.BaseCurrency)
                {
                    var rates = await _currencyHistoryService.GetRatesAtAsync(currency, current.BaseCurrency, new[] { cutoff.Value }, cancellationToken);
                    if (rates.TryGetValue(cutoff.Value, out var rate))
                    {
                        factor = rate;
                    }
                }

                sold.DividendsOriginal = Round(sold.DividendsOriginal);
                dividends += sold.DividendsOriginal * (factor ?? 0m);
            }

            result.Dividends = Round(dividends);
        }

        /// <summary>
        /// Empresas por nombre (las operaciones se enlazan por nombre), con la divisa de su país como en
        /// CompanyService (EUR sin país o con un país que ya no está en el catálogo)
        /// </summary>
        private async Task<Dictionary<string, CompanyDetail>> LoadCompaniesAsync()
        {
            var companies = await _context.CompanyDetails.AsNoTracking().ToListAsync();
            var currencies = await _context.Countries
                .AsNoTracking()
                .ToDictionaryAsync(c => c.Code, c => c.CurrencyCode);
            foreach (var company in companies)
            {
                company.CurrencyCode = company.CountryCode != null && currencies.TryGetValue(company.CountryCode, out var currency)
                    ? CurrencyFormatter.Normalize(currency)
                    : CurrencyFormatter.DefaultCurrency;
            }

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
