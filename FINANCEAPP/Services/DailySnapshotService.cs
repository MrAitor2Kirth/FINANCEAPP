using System.Globalization;
using Microsoft.EntityFrameworkCore;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Implementación de <see cref="IDailySnapshotService"/> (SPEC-17-03). Los días pasados se reconstruyen en memoria
    /// con los datos guardados (cotizaciones y tipos del histórico; sin descargas) y se guardan en un único SaveChanges,
    /// sin un segundo contexto de BD. El día de hoy se calcula con los mismos servicios que el Dashboard y /companies,
    /// así que coincide con ellos por construcción.
    /// </summary>
    public class DailySnapshotService : IDailySnapshotService
    {
        private const string TypeBuy = "Compra";
        private const string TypeTransfer = "Traspaso";
        private const string TypeSell = "Venta";
        private const string TypeDividend = "Dividendo";

        // Último error al escribir el JSON (solo en memoria: se enseña en /history)
        private static string? _lastJsonError;

        private readonly AppDbContext _context;
        private readonly ICountryService _countryService;
        private readonly ICompanyService _companyService;
        private readonly IExchangeRateService _exchangeRateService;
        private readonly IDashboardService _dashboardService;
        private readonly IPortfolioPeriodService _portfolioPeriodService;
        private readonly IPriceHistoryService _priceHistoryService;
        private readonly ICurrencyHistoryService _currencyHistoryService;
        private readonly IAppSettingsService _appSettingsService;
        private readonly IAiPortfolioExportService _aiExportService;

        public DailySnapshotService(
            AppDbContext context,
            ICountryService countryService,
            ICompanyService companyService,
            IExchangeRateService exchangeRateService,
            IDashboardService dashboardService,
            IPortfolioPeriodService portfolioPeriodService,
            IPriceHistoryService priceHistoryService,
            ICurrencyHistoryService currencyHistoryService,
            IAppSettingsService appSettingsService,
            IAiPortfolioExportService aiExportService)
        {
            _context = context;
            _countryService = countryService;
            _companyService = companyService;
            _exchangeRateService = exchangeRateService;
            _dashboardService = dashboardService;
            _portfolioPeriodService = portfolioPeriodService;
            _priceHistoryService = priceHistoryService;
            _currencyHistoryService = currencyHistoryService;
            _appSettingsService = appSettingsService;
            _aiExportService = aiExportService;
        }

        public async Task<DailySnapshotRefreshResult> RefreshAsync(CancellationToken cancellationToken = default)
        {
            var result = new DailySnapshotRefreshResult();
            var today = DateTime.Today;
            var baseCurrency = CurrencyFormatter.Normalize((await _countryService.GetBaseCurrencyAsync()).CurrencyCode);

            var operations = await _context.Operations.AsNoTracking().ToListAsync(cancellationToken);
            if (operations.Count > 0)
            {
                var firstDay = operations.Min(o => o.Date).Date;
                var from = await GetFromAsync(firstDay, baseCurrency, today);
                result.From = from;

                var data = await LoadDataAsync(operations, baseCurrency, from, cancellationToken);
                var days = new List<(DailyPortfolioSnapshot Day, List<DailyCompanySnapshot> Companies)>();
                for (var day = from; day < today; day = day.AddDays(1))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    days.Add(BuildPastDay(day, data));
                }
                days.Add(await BuildTodayAsync(today, data));

                await SaveAsync(from, days, cancellationToken);
                result.Days = days.Count;
            }

            // JSON para la IA: un fallo aquí no deshace los registros
            try
            {
                result.JsonPath = await _aiExportService.WriteAsync(cancellationToken);
                _lastJsonError = null;
            }
            catch (Exception ex)
            {
                _lastJsonError = $"No se pudo escribir el JSON para la IA: {ex.Message}";
                result.JsonError = _lastJsonError;
            }

            return result;
        }

        public async Task<DailySnapshotStatus> GetStatusAsync()
        {
            var settings = await _appSettingsService.GetAiExportSettingsAsync();
            var status = new DailySnapshotStatus { JsonPath = settings.FilePath, JsonError = _lastJsonError };

            var rows = _context.DailyPortfolioSnapshots.AsNoTracking();
            status.Days = await rows.CountAsync();
            if (status.Days > 0)
            {
                status.FirstDate = await rows.MinAsync(r => r.Date);
                status.LastDate = await rows.MaxAsync(r => r.Date);
                status.ReconstructedDays = await rows.CountAsync(r => r.IsReconstructed);
                status.ApproximateDays = await rows.CountAsync(r => r.IsApproximate);
            }

            status.DirtyFrom = await GetDirtyFromAsync();
            if (File.Exists(settings.FilePath))
            {
                status.JsonWrittenAt = File.GetLastWriteTime(settings.FilePath);
            }

            return status;
        }

        public async Task<int> CompleteHistoriesAsync(CancellationToken cancellationToken = default)
        {
            var operations = await _context.Operations.AsNoTracking().ToListAsync(cancellationToken);
            if (operations.Count == 0)
            {
                return 0;
            }

            var firstDay = operations.Min(o => o.Date).Date;
            var names = operations.Select(o => o.CompanyName).Distinct().ToList();
            var companies = await _companyService.GetAllAsync();
            var baseCurrency = CurrencyFormatter.Normalize((await _countryService.GetBaseCurrencyAsync()).CurrencyCode);
            var series = 0;

            foreach (var company in companies.Where(c => names.Contains(c.Name) && !string.IsNullOrWhiteSpace(c.Ticker)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _priceHistoryService.EnsureDailyCoverageAsync(ChartSeriesRef.ForCompany(company.Id, company.Ticker, company.Name), firstDay, cancellationToken);
                series++;
            }

            foreach (var index in await _context.BenchmarkIndices.AsNoTracking().ToListAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _priceHistoryService.EnsureDailyCoverageAsync(ChartSeriesRef.ForBenchmark(index.Id, index.Symbol, index.Name), firstDay, cancellationToken);
                series++;
            }

            foreach (var currency in companies.Where(c => names.Contains(c.Name)).Select(c => CurrencyFormatter.Normalize(c.CurrencyCode)).Distinct().Where(c => c != baseCurrency))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _currencyHistoryService.GetRatesAtAsync(currency, baseCurrency, new[] { firstDay }, cancellationToken);
                series++;
            }

            // Con los históricos completos, toda la serie se rehace
            await SetDirtyFromAsync(DateTime.MinValue);
            await RefreshAsync(cancellationToken);
            return series;
        }

        // ---------------------------------------------------------------- Rango

        /// <summary>
        /// Primer día que hay que rehacer: la marca «recalcular desde X», el día siguiente al último guardado o la primera
        /// operación (tabla vacía o divisa base distinta). Nunca antes de la primera operación ni después de hoy.
        /// </summary>
        private async Task<DateTime> GetFromAsync(DateTime firstDay, string baseCurrency, DateTime today)
        {
            var saved = await _context.DailyPortfolioSnapshots
                .AsNoTracking()
                .Where(s => s.Date < today)
                .OrderByDescending(s => s.Date)
                .Select(s => new { s.Date, s.CurrencyCode })
                .FirstOrDefaultAsync();
            var earliestSaved = await _context.DailyPortfolioSnapshots.AsNoTracking().MinAsync(s => (DateTime?)s.Date);

            DateTime from;
            if (saved == null || saved.CurrencyCode != baseCurrency || earliestSaved == null || earliestSaved.Value.Date > firstDay)
            {
                from = firstDay;
            }
            else
            {
                from = saved.Date.Date.AddDays(1);
                var dirty = await GetDirtyFromAsync();
                if (dirty.HasValue && dirty.Value < from)
                {
                    from = dirty.Value;
                }
            }

            if (from < firstDay)
            {
                from = firstDay;
            }

            return from > today ? today : from;
        }

        private async Task<DateTime?> GetDirtyFromAsync()
        {
            var value = await _context.AppSettings
                .AsNoTracking()
                .Where(s => s.Key == AppDbContext.DailySnapshotsDirtyKey)
                .Select(s => s.TextValue)
                .FirstOrDefaultAsync();
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date.Date : null;
        }

        private async Task SetDirtyFromAsync(DateTime date)
        {
            var row = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == AppDbContext.DailySnapshotsDirtyKey);
            var text = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (row == null)
            {
                _context.AppSettings.Add(new AppSetting { Key = AppDbContext.DailySnapshotsDirtyKey, TextValue = text, UpdatedAt = DateTime.Now });
            }
            else
            {
                row.TextValue = text;
                row.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        // ---------------------------------------------------------------- Datos en memoria

        private sealed class SnapshotData
        {
            public string BaseCurrency = CurrencyFormatter.DefaultCurrency;
            public Dictionary<string, CompanyDetail> Companies = new();
            public Dictionary<string, List<Operation>> OperationsByCompany = new();
            public Dictionary<int, List<(DateTime Date, decimal Close)>> Closes = new();
            public Dictionary<string, List<(DateTime Date, decimal Rate)>> Rates = new();
            public Dictionary<string, decimal> CurrentRates = new();
            public List<ConvertedSale> Sales = new();
            public List<BankAccount> BankAccounts = new();
            public Dictionary<string, List<BankOperation>> BankOperations = new();
            public List<Fund> Funds = new();
            public Dictionary<string, List<FundValueHistory>> FundValues = new();
            public List<SafeHavenAsset> SafeHaven = new();
            public List<Property> Properties = new();
        }

        private async Task<SnapshotData> LoadDataAsync(List<Operation> operations, string baseCurrency, DateTime from, CancellationToken cancellationToken)
        {
            var data = new SnapshotData { BaseCurrency = baseCurrency };

            // Empresas con su divisa (como CompanyService); las operaciones de empresas borradas no cuentan
            var companies = await _companyService.GetAllAsync();
            data.Companies = companies.GroupBy(c => c.Name).ToDictionary(g => g.Key, g => g.First());
            data.OperationsByCompany = operations
                .Where(o => data.Companies.ContainsKey(o.CompanyName))
                .GroupBy(o => o.CompanyName)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Cierres y tipos desde un poco antes del primer día (para tener el último cierre anterior)
            var since = from.AddDays(-15);
            var companyIds = data.Companies.Values.Where(c => data.OperationsByCompany.ContainsKey(c.Name)).Select(c => c.Id).ToList();
            data.Closes = (await _context.PriceHistoryPoints
                    .AsNoTracking()
                    .Where(p => companyIds.Contains(p.CompanyId) && p.Date >= since)
                    .Select(p => new { p.CompanyId, p.Date, p.Close })
                    .ToListAsync(cancellationToken))
                .GroupBy(p => p.CompanyId)
                .ToDictionary(g => g.Key, g => g.Select(p => (p.Date.Date, p.Close)).OrderBy(p => p.Item1).ToList());

            var pairs = await _context.CurrencyPairs.AsNoTracking().Where(p => p.BaseCurrencyCode == baseCurrency).ToListAsync(cancellationToken);
            foreach (var pair in pairs)
            {
                var points = await _context.CurrencyRatePoints
                    .AsNoTracking()
                    .Where(p => p.CurrencyPairId == pair.Id && p.Date >= since)
                    .Select(p => new { p.Date, p.Close })
                    .ToListAsync(cancellationToken);
                data.Rates[CurrencyFormatter.Normalize(pair.CurrencyCode)] = points.Select(p => (p.Date.Date, p.Close)).OrderBy(p => p.Item1).ToList();
            }

            data.CurrentRates = await _context.ExchangeRates
                .AsNoTracking()
                .Where(r => r.BaseCurrencyCode == baseCurrency && r.Rate > 0)
                .ToDictionaryAsync(r => CurrencyFormatter.Normalize(r.CurrencyCode), r => r.Rate, cancellationToken);

            // Ventas con sus tipos guardados (SPEC-02-05); las de empresas borradas no cuentan (SPEC-01-11)
            var sales = (await _context.SalesHistory.AsNoTracking().ToListAsync(cancellationToken))
                .Where(s => data.Companies.ContainsKey(s.CompanyName))
                .ToList();
            data.Sales = (await _exchangeRateService.GetConvertedSalesAsync(sales, data.Companies.ToDictionary(kv => kv.Key, kv => kv.Value.CurrencyCode)))
                .Where(s => !s.MissingRate)
                .ToList();

            data.BankAccounts = await _context.BankAccounts.AsNoTracking().ToListAsync(cancellationToken);
            data.BankOperations = (await _context.BankOperations.AsNoTracking().ToListAsync(cancellationToken))
                .GroupBy(o => o.AccountName)
                .ToDictionary(g => g.Key, g => g.ToList());
            data.Funds = await _context.Funds.AsNoTracking().ToListAsync(cancellationToken);
            data.FundValues = (await _context.FundValueHistories.AsNoTracking().ToListAsync(cancellationToken))
                .GroupBy(v => v.FundName)
                .ToDictionary(g => g.Key, g => g.OrderBy(v => v.Date).ToList());
            data.SafeHaven = await _context.SafeHavenAssets.AsNoTracking().ToListAsync(cancellationToken);
            data.Properties = await _context.Properties.AsNoTracking().ToListAsync(cancellationToken);
            return data;
        }

        // ---------------------------------------------------------------- Días pasados

        /// <summary>
        /// Reconstruye un día pasado (SPEC-17-03, RF-3) con las reglas de «Cartera al cierre» (SPEC-01-09) y de
        /// «Incluir vendidas» (SPEC-01-11). Lo que no tiene histórico diario usa el mejor dato y marca el día aproximado.
        /// </summary>
        private (DailyPortfolioSnapshot, List<DailyCompanySnapshot>) BuildPastDay(DateTime day, SnapshotData data)
        {
            var notes = new SortedSet<string>(StringComparer.Ordinal);
            var row = new DailyPortfolioSnapshot { Date = day, CurrencyCode = data.BaseCurrency, IsReconstructed = true, CalculatedAt = DateTime.Now };
            var companies = new List<DailyCompanySnapshot>();

            decimal? Factor(string currency)
            {
                var code = CurrencyFormatter.Normalize(currency);
                if (code == data.BaseCurrency)
                {
                    return 1m;
                }
                var rate = LastOnOrBefore(data.Rates.GetValueOrDefault(code), day);
                if (rate.HasValue)
                {
                    return rate.Value;
                }
                if (data.CurrentRates.TryGetValue(code, out var current))
                {
                    notes.Add($"tipo de cambio actual para {code}");
                    return current;
                }
                notes.Add($"sin tipo de cambio para {code}");
                return null;
            }

            decimal openInvested = 0, openValue = 0, openDividends = 0, allExtraDividends = 0;
            decimal dayBought = 0, dayDividends = 0, dayCommissions = 0;
            var dayBuyCount = 0;
            var soldNames = data.Sales.Where(s => s.SaleDate.Date <= day).Select(s => s.CompanyName).ToHashSet();

            foreach (var (name, ops) in data.OperationsByCompany)
            {
                var company = data.Companies[name];
                var factor = Factor(company.CurrencyCode);
                if (factor == null)
                {
                    continue;
                }

                // Flujos del día (compras, dividendos y comisiones)
                foreach (var op in ops.Where(o => o.Date.Date == day))
                {
                    if (op.Type is TypeBuy or TypeTransfer)
                    {
                        dayBought += op.Amount * factor.Value;
                        dayBuyCount++;
                    }
                    else if (op.Type == TypeDividend)
                    {
                        dayDividends += op.Amount * factor.Value;
                    }
                    if (op.Type != TypeDividend)
                    {
                        dayCommissions += op.Commission * factor.Value;
                    }
                }

                var dividends = ops.Where(o => o.Type == TypeDividend && o.Date.Date <= day).Sum(o => o.Amount) * factor.Value;
                var open = ops.Where(o => o.Type != TypeDividend && o.Date.Date <= day && (o.CloseDate == null || o.CloseDate.Value.Date > day)).ToList();
                var buys = open.Where(o => o.Type is TypeBuy or TypeTransfer).ToList();
                var oldSells = open.Where(o => o.Type == TypeSell).ToList();
                var shares = ShareQuantity.Round(buys.Sum(o => o.NumberOfShares) - oldSells.Sum(o => o.NumberOfShares));
                if (shares <= 0)
                {
                    // Sin acciones ese día: sus dividendos cuentan en «con vendidas» si ya tenía ventas
                    if (soldNames.Contains(name))
                    {
                        allExtraDividends += dividends;
                    }
                    continue;
                }

                var price = LastOnOrBefore(data.Closes.GetValueOrDefault(company.Id), day);
                if (price == null)
                {
                    price = company.LivePrice ?? company.CurrentPrice;
                    notes.Add($"sin cotización de {name}");
                }

                var cost = (buys.Sum(o => o.Amount) - oldSells.Sum(o => o.Amount)) * factor.Value;
                var value = shares * price.Value * factor.Value;
                openInvested += cost;
                openValue += value;
                openDividends += dividends;
                companies.Add(new DailyCompanySnapshot
                {
                    Date = day,
                    CompanyId = company.Id,
                    CompanyName = company.Name,
                    Ticker = company.Ticker,
                    Sector = company.Sector,
                    CurrencyCode = CurrencyFormatter.Normalize(company.CurrencyCode),
                    Shares = shares,
                    Price = Math.Round(price.Value, 4, MidpointRounding.AwayFromZero),
                    CostBase = Round(cost),
                    ValueBase = Round(value),
                    DividendsBase = Round(dividends),
                    ReturnPct = Pct(value - cost, cost)
                });
            }

            var salesToDay = data.Sales.Where(s => s.SaleDate.Date <= day).ToList();
            if (salesToDay.Any(s => s.Approximate))
            {
                notes.Add("ventas con tipo de cambio actual");
            }

            FillStocks(row, openInvested, openValue, openDividends, salesToDay.Sum(s => s.Cost), salesToDay.Sum(s => s.NetProceeds), allExtraDividends);
            row.DayBought = Round(dayBought);
            row.DayBuyCount = dayBuyCount;
            row.DaySoldNet = Round(data.Sales.Where(s => s.SaleDate.Date == day).Sum(s => s.NetProceeds));
            row.DayNetContribution = Round(dayBought) - row.DaySoldNet;
            row.DayDividends = Round(dayDividends);
            row.DayCommissions = Round(dayCommissions);

            // Patrimonio: mejor dato disponible para lo que no tiene histórico diario (SPEC-17-03, decisión 4)
            decimal fundsValue = 0, fundsInvested = 0;
            foreach (var fund in data.Funds.Where(f => f.CreatedAt.Date <= day))
            {
                var history = data.FundValues.GetValueOrDefault(fund.Name)?.LastOrDefault(v => v.Date.Date <= day);
                fundsValue += history?.Value ?? fund.TotalInvested;
                fundsInvested += fund.TotalInvested;
                notes.Add(history == null ? "fondos sin historial de valores" : "fondos con el último valor del historial");
            }

            decimal safeValue = 0, safeInvested = 0;
            foreach (var asset in data.SafeHaven.Where(a => a.CreatedAt.Date <= day))
            {
                safeValue += asset.CurrentValue;
                safeInvested += asset.TotalInvested;
                notes.Add("refugio con su valor actual");
            }

            decimal realValue = 0, realInvested = 0;
            foreach (var property in data.Properties.Where(p => p.PurchaseDate.Date <= day && (p.SaleDate == null || p.SaleDate.Value.Date > day)))
            {
                realValue += property.CurrentValue ?? property.TotalInvested;
                realInvested += property.TotalInvested;
                notes.Add("inmuebles con su valor actual");
            }

            decimal bank = 0;
            foreach (var account in data.BankAccounts)
            {
                var movements = data.BankOperations.GetValueOrDefault(account.Name) ?? new List<BankOperation>();
                bank += account.OpeningBalance + movements.Where(m => m.Date.Date <= day).Sum(BankEffect);
                if (movements.Count == 0 || movements.Min(m => m.Date).Date > day)
                {
                    notes.Add("banco antes de su primer movimiento");
                }
            }

            FillPatrimony(row, row.OpenValue, row.OpenInvested, fundsValue, fundsInvested, safeValue, safeInvested, realValue, realInvested, bank);

            var weightBase = companies.Sum(c => c.ValueBase);
            foreach (var company in companies)
            {
                company.WeightPct = Pct(company.ValueBase, weightBase);
            }

            row.IsApproximate = notes.Count > 0;
            row.ApproximateNote = notes.Count > 0 ? Truncate(string.Join("; ", notes), 500) : null;
            return (row, companies);
        }

        // ---------------------------------------------------------------- Hoy

        /// <summary>
        /// El día de hoy con los mismos servicios que las pantallas (SPEC-17-03, RF-1 y RF-5): Dashboard para el
        /// patrimonio, GetConvertedTotalsAsync para la cartera actual y GetSoldAsync + Combine para lo vendido.
        /// </summary>
        private async Task<(DailyPortfolioSnapshot, List<DailyCompanySnapshot>)> BuildTodayAsync(DateTime today, SnapshotData data)
        {
            var notes = new SortedSet<string>(StringComparer.Ordinal);
            var row = new DailyPortfolioSnapshot { Date = today, CurrencyCode = data.BaseCurrency, IsReconstructed = false, CalculatedAt = DateTime.Now };

            var invested = InvestedCompaniesSummary.GetInvested(data.Companies.Values);
            var openTotals = await _exchangeRateService.GetConvertedTotalsAsync(invested);
            var sold = await _portfolioPeriodService.GetSoldAsync(SalesPeriodFilter.All, PeriodSummaryView.Purchases);
            var allTotals = SoldPositionsSummary.Combine(openTotals, sold);
            if (openTotals.MissingRateCompanies > 0)
            {
                notes.Add($"{openTotals.MissingRateCompanies} empresas sin tipo de cambio");
            }

            row.OpenInvested = Round(openTotals.InvestedInitial);
            row.OpenValue = Round(openTotals.CurrentValue);
            row.OpenDividends = Round(openTotals.Dividends);
            row.OpenReturnPct = Pct(openTotals.CurrentValue - openTotals.InvestedInitial, openTotals.InvestedInitial);
            row.OpenReturnWithDivPct = Pct(openTotals.CurrentValue + openTotals.Dividends - openTotals.InvestedInitial, openTotals.InvestedInitial);
            row.AllInvested = Round(allTotals.InvestedInitial);
            row.AllValue = Round(allTotals.CurrentValue);
            row.AllDividends = Round(allTotals.Dividends);
            row.AllReturnPct = Pct(allTotals.CurrentValue - allTotals.InvestedInitial, allTotals.InvestedInitial);
            row.AllReturnWithDivPct = Pct(allTotals.CurrentValue + allTotals.Dividends - allTotals.InvestedInitial, allTotals.InvestedInitial);
            row.SoldCost = Round(sold.Cost);
            row.SoldProceeds = Round(sold.NetProceeds);
            row.SoldResult = Round(sold.NetProceeds - sold.Cost);
            row.SoldReturnPct = Pct(sold.NetProceeds - sold.Cost, sold.Cost);

            // Flujos de hoy con el tipo actual
            decimal bought = 0, dividends = 0, commissions = 0;
            var buyCount = 0;
            foreach (var (name, ops) in data.OperationsByCompany)
            {
                var factor = CurrentFactor(data, data.Companies[name].CurrencyCode);
                if (factor == null)
                {
                    continue;
                }
                foreach (var op in ops.Where(o => o.Date.Date == today))
                {
                    if (op.Type is TypeBuy or TypeTransfer)
                    {
                        bought += op.Amount * factor.Value;
                        buyCount++;
                    }
                    else if (op.Type == TypeDividend)
                    {
                        dividends += op.Amount * factor.Value;
                    }
                    if (op.Type != TypeDividend)
                    {
                        commissions += op.Commission * factor.Value;
                    }
                }
            }
            row.DayBought = Round(bought);
            row.DayBuyCount = buyCount;
            row.DaySoldNet = Round(data.Sales.Where(s => s.SaleDate.Date == today).Sum(s => s.NetProceeds));
            row.DayNetContribution = row.DayBought - row.DaySoldNet;
            row.DayDividends = Round(dividends);
            row.DayCommissions = Round(commissions);

            var dashboard = await _dashboardService.GetDashboardSummaryAsync();
            FillPatrimony(row, dashboard.AccionesTotal, dashboard.AccionesInitialTotal, dashboard.FundsTotal, dashboard.FundsInitialTotal,
                dashboard.SafeHavenTotal, dashboard.SafeHavenInitialTotal, dashboard.RealEstateTotal, dashboard.RealEstateInvestedTotal, dashboard.BankTotal);

            var companies = new List<DailyCompanySnapshot>();
            foreach (var company in openTotals.Included)
            {
                var factor = CurrentFactor(data, company.CurrencyCode) ?? 0m;
                var value = company.TotalInvestedValue * factor;
                var cost = company.TotalInvestedInitial * factor;
                companies.Add(new DailyCompanySnapshot
                {
                    Date = today,
                    CompanyId = company.Id,
                    CompanyName = company.Name,
                    Ticker = company.Ticker,
                    Sector = company.Sector,
                    CurrencyCode = CurrencyFormatter.Normalize(company.CurrencyCode),
                    Shares = company.NumberOfShares,
                    Price = Math.Round(company.LivePrice ?? company.CurrentPrice, 4, MidpointRounding.AwayFromZero),
                    CostBase = Round(cost),
                    ValueBase = Round(value),
                    DividendsBase = Round(company.TotalDividends * factor),
                    ReturnPct = Pct(value - cost, cost)
                });
            }

            var weightBase = companies.Sum(c => c.ValueBase);
            foreach (var company in companies)
            {
                company.WeightPct = Pct(company.ValueBase, weightBase);
            }

            row.IsApproximate = notes.Count > 0;
            row.ApproximateNote = notes.Count > 0 ? Truncate(string.Join("; ", notes), 500) : null;
            return (row, companies);
        }

        // ---------------------------------------------------------------- Guardado

        /// <summary>
        /// Sustituye los días desde «from» (incluido hoy) en una transacción y borra la marca «recalcular desde X».
        /// Las filas guardadas se sueltan del contexto (es el de toda la app) para no acumularlas en memoria.
        /// </summary>
        private async Task SaveAsync(DateTime from, List<(DailyPortfolioSnapshot Day, List<DailyCompanySnapshot> Companies)> days, CancellationToken cancellationToken)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _context.DailyCompanySnapshots.Where(s => s.Date >= from).ExecuteDeleteAsync(cancellationToken);
                await _context.DailyPortfolioSnapshots.Where(s => s.Date >= from).ExecuteDeleteAsync(cancellationToken);

                _context.DailyPortfolioSnapshots.AddRange(days.Select(d => d.Day));
                _context.DailyCompanySnapshots.AddRange(days.SelectMany(d => d.Companies));

                var mark = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == AppDbContext.DailySnapshotsDirtyKey, cancellationToken);
                if (mark != null)
                {
                    _context.AppSettings.Remove(mark);
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                foreach (var entry in _context.ChangeTracker.Entries()
                             .Where(e => e.Entity is DailyPortfolioSnapshot or DailyCompanySnapshot)
                             .ToList())
                {
                    entry.State = EntityState.Detached;
                }
            }
        }

        // ---------------------------------------------------------------- Auxiliares

        private static void FillStocks(DailyPortfolioSnapshot row, decimal openInvested, decimal openValue, decimal openDividends,
            decimal soldCost, decimal soldProceeds, decimal extraDividends)
        {
            row.OpenInvested = Round(openInvested);
            row.OpenValue = Round(openValue);
            row.OpenDividends = Round(openDividends);
            row.OpenReturnPct = Pct(openValue - openInvested, openInvested);
            row.OpenReturnWithDivPct = Pct(openValue + openDividends - openInvested, openInvested);

            var allInvested = openInvested + soldCost;
            var allValue = openValue + soldProceeds;
            var allDividends = openDividends + extraDividends;
            row.AllInvested = Round(allInvested);
            row.AllValue = Round(allValue);
            row.AllDividends = Round(allDividends);
            row.AllReturnPct = Pct(allValue - allInvested, allInvested);
            row.AllReturnWithDivPct = Pct(allValue + allDividends - allInvested, allInvested);

            row.SoldCost = Round(soldCost);
            row.SoldProceeds = Round(soldProceeds);
            row.SoldResult = Round(soldProceeds - soldCost);
            row.SoldReturnPct = Pct(soldProceeds - soldCost, soldCost);
        }

        // El banco no cuenta como invertido (como SPEC-17-01)
        private static void FillPatrimony(DailyPortfolioSnapshot row, decimal stocksValue, decimal stocksInvested, decimal fundsValue, decimal fundsInvested,
            decimal safeValue, decimal safeInvested, decimal realValue, decimal realInvested, decimal bank)
        {
            row.StocksValue = Round(stocksValue);
            row.StocksInvested = Round(stocksInvested);
            row.FundsValue = Round(fundsValue);
            row.FundsInvested = Round(fundsInvested);
            row.SafeHavenValue = Round(safeValue);
            row.SafeHavenInvested = Round(safeInvested);
            row.RealEstateValue = Round(realValue);
            row.RealEstateInvested = Round(realInvested);
            row.BankBalance = Round(bank);
            row.TotalValue = Round(stocksValue + fundsValue + safeValue + realValue + bank);
            row.TotalInvested = Round(stocksInvested + fundsInvested + safeInvested + realInvested);
            row.LiquidityPct = Pct(bank, stocksValue + fundsValue + safeValue + realValue + bank);
        }

        // Efecto de un movimiento en el saldo, como BankService (importes en valor absoluto con signo por tipo)
        private static decimal BankEffect(BankOperation operation) =>
            operation.IsIncome ? Math.Abs(operation.Amount) : operation.IsExpense ? -Math.Abs(operation.Amount) : 0m;

        private static decimal? CurrentFactor(SnapshotData data, string currency)
        {
            var code = CurrencyFormatter.Normalize(currency);
            return code == data.BaseCurrency ? 1m : data.CurrentRates.TryGetValue(code, out var rate) ? rate : null;
        }

        /// <summary>Último valor con fecha ≤ día (las listas van ordenadas por fecha)</summary>
        private static decimal? LastOnOrBefore(List<(DateTime Date, decimal Value)>? points, DateTime day)
        {
            if (points == null || points.Count == 0)
            {
                return null;
            }

            int low = 0, high = points.Count - 1, found = -1;
            while (low <= high)
            {
                var mid = (low + high) / 2;
                if (points[mid].Date <= day)
                {
                    found = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return found >= 0 && points[found].Value > 0 ? points[found].Value : null;
        }

        private static decimal Pct(decimal part, decimal total) =>
            total > 0 ? Round(part / total * 100m) : 0m;

        private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private static string Truncate(string text, int length) => text.Length <= length ? text : text[..length];
    }
}
