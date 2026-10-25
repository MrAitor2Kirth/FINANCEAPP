using System.Globalization;
using FINANCEAPP.Data;
using FINANCEAPP.Models;
using Microsoft.EntityFrameworkCore;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Implementación del servicio de tipos de cambio (SPEC-01-03)
    /// </summary>
    public class ExchangeRateService : IExchangeRateService
    {
        private readonly AppDbContext _context;
        private readonly IStockPriceService _stockPriceService;
        private readonly ICountryService _countryService;
        private readonly ICompanyService _companyService;

        public ExchangeRateService(
            AppDbContext context,
            IStockPriceService stockPriceService,
            ICountryService countryService,
            ICompanyService companyService)
        {
            _context = context;
            _stockPriceService = stockPriceService;
            _countryService = countryService;
            _companyService = companyService;
        }

        /// <summary>Convierte un importe con un tipo de cambio, redondeado a 2 decimales</summary>
        public static decimal Convert(decimal amount, decimal rate) =>
            Math.Round(amount * rate, 2, MidpointRounding.AwayFromZero);

        private async Task<string> GetBaseCurrencyCodeAsync() =>
            CurrencyFormatter.Normalize((await _countryService.GetBaseCurrencyAsync()).CurrencyCode);

        public async Task<List<ExchangeRateRow>> GetRowsAsync()
        {
            var baseCurrency = await GetBaseCurrencyCodeAsync();
            var companies = await _companyService.GetAllAsync();
            var groups = companies
                .Select(c => CurrencyFormatter.Normalize(c.CurrencyCode))
                .Where(code => code != baseCurrency)
                .GroupBy(code => code)
                .OrderBy(g => g.Key)
                .ToList();

            var rates = await _context.ExchangeRates
                .AsNoTracking()
                .Where(r => r.BaseCurrencyCode == baseCurrency)
                .ToListAsync();

            return groups.Select(g => new ExchangeRateRow
            {
                CurrencyCode = g.Key,
                BaseCurrencyCode = baseCurrency,
                CompaniesCount = g.Count(),
                Rate = rates.FirstOrDefault(r => r.CurrencyCode == g.Key)
            }).ToList();
        }

        public async Task<ExchangeRate?> GetCurrentRateAsync(string? currencyCode)
        {
            var currency = CurrencyFormatter.Normalize(currencyCode);
            var baseCurrency = await GetBaseCurrencyCodeAsync();
            if (currency == baseCurrency)
            {
                return null;
            }

            return await _context.ExchangeRates
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.CurrencyCode == currency && r.BaseCurrencyCode == baseCurrency);
        }

        public async Task<ExchangeRateResult> RefreshAsync(string currencyCode)
        {
            var currency = CurrencyFormatter.Normalize(currencyCode);
            var baseCurrency = await GetBaseCurrencyCodeAsync();
            if (currency == baseCurrency)
            {
                return ExchangeRateResult.Fail("La divisa es la divisa base: no necesita tipo de cambio.");
            }

            decimal? rate = null;
            try
            {
                // Par directo (por ejemplo USDEUR=X: euros por dólar)
                var direct = await _stockPriceService.GetLatestPriceAsync($"{currency}{baseCurrency}=X");
                if (direct.HasValue && direct.Value > 0)
                {
                    rate = direct.Value;
                }
                else
                {
                    // Par inverso (por ejemplo EURUSD=X) y se invierte
                    var inverse = await _stockPriceService.GetLatestPriceAsync($"{baseCurrency}{currency}=X");
                    if (inverse.HasValue && inverse.Value > 0)
                    {
                        rate = 1m / inverse.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                return ExchangeRateResult.Fail($"No se pudo consultar Yahoo Finance: {ex.Message}");
            }

            if (!rate.HasValue || rate.Value <= 0)
            {
                return ExchangeRateResult.Fail(
                    $"Yahoo Finance no devolvió el tipo {currency}/{baseCurrency}. Se conserva el anterior; puedes escribirlo a mano.");
            }

            return await SaveAsync(currency, baseCurrency, Math.Round(rate.Value, 6), ExchangeRate.SourceYahoo);
        }

        public async Task<int> RefreshAllAsync(Func<string, ExchangeRateResult, Task>? onResult = null)
        {
            var rows = await GetRowsAsync();
            var ok = 0;
            foreach (var row in rows)
            {
                var result = await RefreshAsync(row.CurrencyCode);
                if (result.Success)
                {
                    ok++;
                }
                if (onResult != null)
                {
                    await onResult(row.CurrencyCode, result);
                }
            }
            return ok;
        }

        public async Task<ExchangeRateResult> SaveManualAsync(string currencyCode, decimal rate)
        {
            if (rate <= 0)
            {
                return ExchangeRateResult.Fail("El tipo de cambio debe ser mayor que 0.");
            }

            var currency = CurrencyFormatter.Normalize(currencyCode);
            var baseCurrency = await GetBaseCurrencyCodeAsync();
            if (currency == baseCurrency)
            {
                return ExchangeRateResult.Fail("La divisa es la divisa base: no necesita tipo de cambio.");
            }

            return await SaveAsync(currency, baseCurrency, Math.Round(rate, 6), ExchangeRate.SourceManual);
        }

        private async Task<ExchangeRateResult> SaveAsync(string currency, string baseCurrency, decimal rate, string source)
        {
            if (rate <= 0)
            {
                return ExchangeRateResult.Fail("El tipo de cambio debe ser mayor que 0.");
            }

            var existing = await _context.ExchangeRates
                .FirstOrDefaultAsync(r => r.CurrencyCode == currency && r.BaseCurrencyCode == baseCurrency);
            if (existing == null)
            {
                existing = new ExchangeRate { CurrencyCode = currency, BaseCurrencyCode = baseCurrency };
                _context.ExchangeRates.Add(existing);
            }

            existing.Rate = rate;
            existing.Source = source;
            existing.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return ExchangeRateResult.Ok(existing);
        }

        public async Task<ConvertedCompanyTotals> GetConvertedTotalsAsync(IEnumerable<CompanyDetail> companies)
        {
            var baseCurrency = await GetBaseCurrencyCodeAsync();
            var rates = await _context.ExchangeRates
                .AsNoTracking()
                .Where(r => r.BaseCurrencyCode == baseCurrency)
                .ToDictionaryAsync(r => r.CurrencyCode, r => r.Rate);

            var totals = new ConvertedCompanyTotals { BaseCurrencyCode = baseCurrency };
            decimal initial = 0, value = 0, dividends = 0;

            foreach (var company in companies)
            {
                var currency = CurrencyFormatter.Normalize(company.CurrencyCode);
                decimal factor;
                if (currency == baseCurrency)
                {
                    factor = 1m;
                }
                else if (rates.TryGetValue(currency, out var rate) && rate > 0)
                {
                    factor = rate;
                }
                else
                {
                    totals.MissingRateCompanies++;
                    continue;
                }

                initial += company.TotalInvestedInitial * factor;
                totals.InitialByCompanyId[company.Id] = company.TotalInvestedInitial * factor;
                value += company.TotalInvestedValue * factor;
                dividends += company.TotalDividends * factor;
                totals.Included.Add(company);
            }

            totals.InvestedInitial = Math.Round(initial, 2, MidpointRounding.AwayFromZero);
            totals.CurrentValue = Math.Round(value, 2, MidpointRounding.AwayFromZero);
            totals.Dividends = Math.Round(dividends, 2, MidpointRounding.AwayFromZero);
            return totals;
        }

        public async Task<ConvertedSaleTotals> GetConvertedSaleTotalsAsync(IEnumerable<SaleHistory> sales, IReadOnlyDictionary<string, string> currencyByCompany)
        {
            var baseCurrency = await GetBaseCurrencyCodeAsync();
            var currentRates = await _context.ExchangeRates
                .AsNoTracking()
                .Where(r => r.BaseCurrencyCode == baseCurrency)
                .ToDictionaryAsync(r => r.CurrencyCode, r => r.Rate);

            var salesList = sales.ToList();

            // SPEC-02-05 (RF-10): cada venta y cada compra cerrada se convierten con su tipo guardado.
            // Solo hacen falta las operaciones de las ventas en otra divisa.
            var foreignSales = salesList
                .Where(s => CurrencyOf(s, currencyByCompany) != baseCurrency)
                .ToList();
            var saleIds = foreignSales.Select(s => s.Id).ToList();
            var operationIds = foreignSales.Select(s => s.OperationId).ToList();

            var saleOperations = saleIds.Count == 0
                ? new Dictionary<int, Operation>()
                : (await _context.Operations
                    .AsNoTracking()
                    .Where(o => operationIds.Contains(o.Id))
                    .ToListAsync())
                    .ToDictionary(o => o.Id);
            var closedLots = saleIds.Count == 0
                ? new Dictionary<int, List<Operation>>()
                : (await _context.Operations
                    .AsNoTracking()
                    .Where(o => o.SaleHistoryId != null && saleIds.Contains(o.SaleHistoryId.Value))
                    .ToListAsync())
                    .GroupBy(o => o.SaleHistoryId!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

            var totals = new ConvertedSaleTotals { BaseCurrencyCode = baseCurrency };
            decimal amount = 0, commission = 0, profit = 0;

            foreach (var sale in salesList)
            {
                var currency = CurrencyOf(sale, currencyByCompany);
                if (currency == baseCurrency)
                {
                    amount += sale.Amount;
                    commission += sale.Commission;
                    profit += sale.Profit;
                    continue;
                }

                var approximate = false;

                // Tipo de la Venta: el guardado en la operación; si no vale, el actual
                saleOperations.TryGetValue(sale.OperationId, out var saleOperation);
                var saleRate = StoredRate(saleOperation, baseCurrency);
                if (saleRate == null)
                {
                    saleRate = CurrentRate(currentRates, currency);
                    approximate = true;
                }
                if (saleRate == null)
                {
                    totals.MissingRateSales++;
                    continue;
                }

                // Coste y comisiones de las compras cerradas, cada una con su tipo
                decimal cost = 0, buyCommission = 0;
                var missingBuyRate = false;
                if (closedLots.TryGetValue(sale.Id, out var lots) && lots.Count > 0)
                {
                    foreach (var lot in lots)
                    {
                        var buyRate = StoredRate(lot, baseCurrency);
                        if (buyRate == null)
                        {
                            buyRate = CurrentRate(currentRates, currency);
                            approximate = true;
                        }
                        if (buyRate == null)
                        {
                            missingBuyRate = true;
                            break;
                        }

                        cost += lot.PricePerShare * lot.NumberOfShares * buyRate.Value;
                        buyCommission += lot.Commission * buyRate.Value;
                    }
                }
                else
                {
                    // Venta anterior a SPEC-02-03: sin compras enlazadas se usa el coste medio con el tipo actual
                    var currentRate = CurrentRate(currentRates, currency);
                    if (currentRate == null)
                    {
                        missingBuyRate = true;
                    }
                    else
                    {
                        cost = sale.AverageBuyPrice * sale.NumberOfShares * currentRate.Value;
                        buyCommission = Math.Max(sale.Commission - (saleOperation?.Commission ?? 0m), 0m) * currentRate.Value;
                        approximate = true;
                    }
                }

                if (missingBuyRate)
                {
                    totals.MissingRateSales++;
                    continue;
                }

                var saleAmount = sale.Amount * saleRate.Value;
                var saleCommissionTotal = (saleOperation?.Commission ?? sale.Commission) * saleRate.Value + buyCommission;

                amount += saleAmount;
                commission += saleCommissionTotal;
                profit += saleAmount - cost - saleCommissionTotal;

                if (approximate)
                {
                    totals.ApproximateSales++;
                }
                else
                {
                    totals.ConvertedSales++;
                }
            }

            totals.Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
            totals.Commission = Math.Round(commission, 2, MidpointRounding.AwayFromZero);
            totals.Profit = Math.Round(profit, 2, MidpointRounding.AwayFromZero);
            return totals;
        }

        private static string CurrencyOf(SaleHistory sale, IReadOnlyDictionary<string, string> currencyByCompany) =>
            CurrencyFormatter.Normalize(currencyByCompany.TryGetValue(sale.CompanyName, out var code) ? code : null);

        /// <summary>Tipo guardado en la operación; solo vale si es mayor que 0 y su destino es la divisa base actual</summary>
        private static decimal? StoredRate(Operation? operation, string baseCurrency) =>
            operation?.ExchangeRate is > 0
                && string.Equals(operation.ExchangeRateTo, baseCurrency, StringComparison.OrdinalIgnoreCase)
                ? operation.ExchangeRate
                : null;

        private static decimal? CurrentRate(IReadOnlyDictionary<string, decimal> rates, string currency) =>
            rates.TryGetValue(currency, out var rate) && rate > 0 ? rate : null;

        private static string RateText(string from, string to, decimal rate) =>
            $"1 {from} = {rate.ToString("0.######", CultureInfo.CurrentCulture)} {to}";

        private static string CurrentRateNote(ExchangeRate rate) =>
            $"Tipo actual de Configuración ({rate.SourceLabel}, {rate.UpdatedAt:dd/MM/yyyy HH:mm}).";

        private async Task<string> GetCompanyCurrencyAsync(string companyName)
        {
            var countryCode = await _context.CompanyDetails
                .AsNoTracking()
                .Where(c => c.Name == companyName)
                .Select(c => c.CountryCode)
                .FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                return CurrencyFormatter.DefaultCurrency;
            }

            var currency = await _context.Countries
                .AsNoTracking()
                .Where(c => c.Code == countryCode)
                .Select(c => c.CurrencyCode)
                .FirstOrDefaultAsync();
            return CurrencyFormatter.Normalize(currency);
        }

        /// <summary>
        /// Rellena el tipo, su destino y la nota de procedencia: el guardado en la operación
        /// o, si no tiene, el actual.
        /// </summary>
        private async Task<decimal?> ApplyRateAsync(ExchangeDetails details, string currency, decimal? storedRate, string? storedTo)
        {
            details.OriginalCurrencyCode = currency;

            // Divisa base: no hay nada que convertir
            var baseCurrency = await GetBaseCurrencyCodeAsync();
            if (currency == baseCurrency)
            {
                details.IsBaseCurrency = true;
                details.TargetCurrencyCode = baseCurrency;
                details.HasRate = false;
                return null;
            }

            if (storedRate.HasValue && storedRate.Value > 0 && !string.IsNullOrWhiteSpace(storedTo))
            {
                details.TargetCurrencyCode = CurrencyFormatter.Normalize(storedTo);
                details.HasRate = true;
                details.RateText = RateText(currency, details.TargetCurrencyCode, storedRate.Value);
                details.RateNote = "Tipo guardado en la operación.";
                return storedRate.Value;
            }

            var current = await GetCurrentRateAsync(currency);
            if (current != null)
            {
                details.TargetCurrencyCode = current.BaseCurrencyCode;
                details.HasRate = true;
                details.RateText = RateText(currency, current.BaseCurrencyCode, current.Rate);
                details.RateNote = "Sin tipo guardado en la operación: se usa el tipo actual, así que el resultado es orientativo. " + CurrentRateNote(current);
                return current.Rate;
            }

            details.TargetCurrencyCode = await GetBaseCurrencyCodeAsync();
            details.HasRate = false;
            details.RateText = null;
            details.RateNote = "Sin tipo de cambio: ni la operación ni Configuración tienen uno para esta divisa.";
            return null;
        }

        private static ExchangeDetailLine Line(string label, decimal amount, ExchangeDetails details, decimal? rate) => new()
        {
            Label = label,
            Original = CurrencyFormatter.Format(amount, details.OriginalCurrencyCode),
            Converted = details.HasRate && rate.HasValue
                ? CurrencyFormatter.Format(Convert(amount, rate.Value), details.TargetCurrencyCode)
                : null
        };

        public async Task<ExchangeDetails> GetOperationDetailsAsync(Operation operation)
        {
            var details = new ExchangeDetails
            {
                Title = $"{operation.Type} · {operation.CompanyName}",
                Subtitle = $"Fecha: {operation.Date:dd/MM/yyyy}"
            };
            var rate = await ApplyRateAsync(details, operation.EffectiveCurrencyCode, operation.ExchangeRate, operation.ExchangeRateTo);
            details.Lines.Add(Line("Importe", operation.Amount, details, rate));
            details.Lines.Add(Line("Comisión", operation.Commission, details, rate));
            return details;
        }

        public async Task<ExchangeDetails> GetSaleDetailsAsync(SaleHistory sale)
        {
            var details = new ExchangeDetails
            {
                Title = $"Venta · {sale.CompanyName}",
                Subtitle = $"Fecha: {sale.SaleDate:dd/MM/yyyy}"
            };

            var currency = await GetCompanyCurrencyAsync(sale.CompanyName);
            var saleOperation = await _context.Operations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == sale.OperationId);
            var rate = await ApplyRateAsync(details, currency, saleOperation?.ExchangeRate, saleOperation?.ExchangeRateTo);
            details.Lines.Add(Line("Importe", sale.Amount, details, rate));
            details.Lines.Add(Line("Comisión", sale.Commission, details, rate));
            details.Lines.Add(Line("Beneficio", sale.Profit, details, rate));
            return details;
        }

        public async Task<ExchangeDetails> GetCompanyDetailsAsync(CompanyDetail company)
        {
            var details = new ExchangeDetails
            {
                Title = company.Name,
                Subtitle = string.IsNullOrWhiteSpace(company.Ticker) ? null : $"Ticker: {company.Ticker}"
            };
            // Las empresas siempre se convierten con el tipo actual
            var rate = await ApplyRateAsync(details, CurrencyFormatter.Normalize(company.CurrencyCode), null, null);
            if (details.HasRate)
            {
                details.RateNote = CurrentRateNote((await GetCurrentRateAsync(company.CurrencyCode))!);
            }
            details.Lines.Add(Line("Total invertido", company.TotalInvestedInitial, details, rate));
            details.Lines.Add(Line("Valor actual", company.TotalInvestedValue, details, rate));
            details.Lines.Add(Line("Dividendos", company.TotalDividends, details, rate));
            return details;
        }
    }
}
