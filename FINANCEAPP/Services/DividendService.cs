using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Implementación de <see cref="IDividendService"/> sobre la tabla DividendPayments (SPEC-21-01).
    /// La operación «Dividendo» se crea siempre con <see cref="IOperationService.AddAsync"/>, que marca la línea
    /// Cobrada en su propia transacción; este servicio no abre transacciones (cada alta, edición o borrado es un solo guardado).
    /// </summary>
    public class DividendService : IDividendService
    {
        private const int UpcomingDays = 90;

        private readonly AppDbContext _context;
        private readonly ICompanyService _companyService;
        private readonly IOperationService _operationService;
        private readonly IExchangeRateService _exchangeRateService;
        private readonly ICountryService _countryService;
        private readonly IStockPriceService _stockPriceService;
        private readonly ILogger<DividendService> _logger;

        public DividendService(
            AppDbContext context,
            ICompanyService companyService,
            IOperationService operationService,
            IExchangeRateService exchangeRateService,
            ICountryService countryService,
            IStockPriceService stockPriceService,
            ILogger<DividendService> logger)
        {
            _context = context;
            _companyService = companyService;
            _operationService = operationService;
            _exchangeRateService = exchangeRateService;
            _countryService = countryService;
            _stockPriceService = stockPriceService;
            _logger = logger;
        }

        // ---------------------------------------------------------------- Lectura

        public async Task<List<DividendPayment>> GetAsync(DividendFilter filter, int? companyId = null)
        {
            var query = _context.DividendPayments.AsNoTracking().AsQueryable();
            if (companyId.HasValue)
            {
                query = query.Where(d => d.CompanyId == companyId.Value);
            }

            query = filter switch
            {
                DividendFilter.Pending => query.Where(d => d.Status == DividendPaymentRules.StatusPending),
                DividendFilter.Collected => query.Where(d => d.Status == DividendPaymentRules.StatusCollected),
                _ => query
            };

            var lines = await query.ToListAsync();
            await FillCompanyDataAsync(lines);

            return filter == DividendFilter.Pending
                ? lines.OrderBy(d => d.PaymentDate).ThenBy(d => d.CompanyName).ToList()
                : lines.OrderByDescending(d => d.PaymentDate).ThenBy(d => d.CompanyName).ToList();
        }

        public async Task<DividendSummary> GetSummaryAsync()
        {
            var baseCurrency = CurrencyFormatter.Normalize((await _countryService.GetBaseCurrencyAsync()).CurrencyCode);
            var summary = new DividendSummary { BaseCurrencyCode = baseCurrency };
            var rates = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);
            var missing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            async Task<decimal?> ConvertAsync(decimal amount, string currency, string companyKey)
            {
                currency = CurrencyFormatter.Normalize(currency);
                if (currency == baseCurrency)
                {
                    return amount;
                }

                if (!rates.TryGetValue(currency, out var rate))
                {
                    rate = (await _exchangeRateService.GetCurrentRateAsync(currency))?.Rate;
                    rates[currency] = rate;
                }

                if (!rate.HasValue)
                {
                    missing.Add(companyKey);
                    return null;
                }

                return Math.Round(amount * rate.Value, 2);
            }

            // Pendiente a 90 días: líneas Pendientes con importe y pago en [hoy, hoy + 90]
            var today = DateTime.Today;
            var limit = today.AddDays(UpcomingDays);
            var pending = (await GetAsync(DividendFilter.Pending))
                .Where(d => d.TotalAmount.HasValue && d.PaymentDate.Date >= today && d.PaymentDate.Date <= limit);
            foreach (var line in pending)
            {
                summary.PendingNext90Days += await ConvertAsync(line.TotalAmount!.Value, line.CurrencyCode, line.CompanyName) ?? 0m;
            }

            // Cobrado del año: todas las operaciones «Dividendo» del año natural, se generaran o no desde una línea
            var yearStart = new DateTime(today.Year, 1, 1);
            var nextYear = yearStart.AddYears(1);
            var dividends = await _context.Operations
                .AsNoTracking()
                .Where(o => o.Type == "Dividendo" && o.Date >= yearStart && o.Date < nextYear)
                .ToListAsync();
            var currencyByCompany = (await _companyService.GetAllAsync())
                .ToDictionary(c => c.Name, c => c.CurrencyCode, StringComparer.OrdinalIgnoreCase);
            foreach (var operation in dividends)
            {
                var currency = operation.CurrencyCode
                    ?? (currencyByCompany.TryGetValue(operation.CompanyName, out var companyCurrency) ? companyCurrency : CurrencyFormatter.DefaultCurrency);
                summary.CollectedThisYear += await ConvertAsync(operation.Amount, currency, operation.CompanyName) ?? 0m;
            }

            summary.MissingRateCompanies = missing.Count;
            return summary;
        }

        public async Task<decimal> GetDefaultSharesAsync(int companyId)
        {
            return await _context.CompanyDetails
                .AsNoTracking()
                .Where(c => c.Id == companyId)
                .Select(c => c.NumberOfShares)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Rellena nombre y divisa de la empresa e importe total de cada línea
        /// </summary>
        private async Task FillCompanyDataAsync(IReadOnlyCollection<DividendPayment> lines)
        {
            if (lines.Count == 0)
            {
                return;
            }

            var companies = (await _companyService.GetAllAsync()).ToDictionary(c => c.Id);
            foreach (var line in lines)
            {
                if (companies.TryGetValue(line.CompanyId, out var company))
                {
                    line.CompanyName = company.Name;
                    line.CurrencyCode = CurrencyFormatter.Normalize(company.CurrencyCode);
                }
                else
                {
                    line.CompanyName = "(empresa eliminada)";
                }

                line.TotalAmount = DividendPaymentRules.CalculateTotal(line.NumberOfShares, line.AmountPerShare);
            }
        }

        // ---------------------------------------------------------------- Alta, edición y borrado

        public async Task<DividendResult> CreateAsync(DividendPaymentInput input)
        {
            var error = await ValidateAsync(input, null);
            if (error != null)
            {
                return DividendResult.Fail(error);
            }

            var now = DateTime.Now;
            var line = new DividendPayment
            {
                Status = DividendPaymentRules.StatusPending,
                Source = DividendPaymentRules.SourceManual,
                CreatedAt = now
            };
            Apply(line, input, now);

            _context.DividendPayments.Add(line);
            await _context.SaveChangesAsync();
            return DividendResult.Ok(1);
        }

        public async Task<DividendResult> UpdateAsync(int id, DividendPaymentInput input)
        {
            var line = await _context.DividendPayments.FindAsync(id);
            if (line == null)
            {
                return DividendResult.Fail("La línea de dividendo ya no existe.");
            }

            if (line.Status == DividendPaymentRules.StatusCollected)
            {
                return DividendResult.Fail("Cobrado: para cambiarlo, borra la operación en Operaciones.");
            }

            var error = await ValidateAsync(input, id);
            if (error != null)
            {
                return DividendResult.Fail(error);
            }

            Apply(line, input, DateTime.Now);
            await _context.SaveChangesAsync();
            return DividendResult.Ok();
        }

        public async Task<DividendResult> DeleteAsync(int id)
        {
            var line = await _context.DividendPayments.FindAsync(id);
            if (line == null)
            {
                return DividendResult.Fail("La línea de dividendo ya no existe.");
            }

            if (line.Status == DividendPaymentRules.StatusCollected)
            {
                return DividendResult.Fail("Cobrado: para cambiarlo, borra la operación en Operaciones.");
            }

            _context.DividendPayments.Remove(line);
            await _context.SaveChangesAsync();
            return DividendResult.Ok();
        }

        private static void Apply(DividendPayment line, DividendPaymentInput input, DateTime now)
        {
            line.CompanyId = input.CompanyId;
            line.PaymentDate = input.PaymentDate.Date;
            line.ExDividendDate = input.ExDividendDate?.Date;
            line.AmountPerShare = input.AmountPerShare;
            line.NumberOfShares = input.NumberOfShares;
            line.DividendPercent = input.DividendPercent;
            line.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
            line.UpdatedAt = now;
        }

        /// <summary>
        /// Reglas de la línea más empresa existente y duplicado empresa + fecha de pago (RF-3)
        /// </summary>
        private async Task<string?> ValidateAsync(DividendPaymentInput input, int? currentId)
        {
            var error = DividendPaymentRules.Validate(input);
            if (error != null)
            {
                return error;
            }

            if (!await _context.CompanyDetails.AnyAsync(c => c.Id == input.CompanyId))
            {
                return "La empresa ya no existe.";
            }

            var paymentDate = input.PaymentDate.Date;
            var duplicate = await _context.DividendPayments
                .AnyAsync(d => d.CompanyId == input.CompanyId && d.PaymentDate == paymentDate && d.Id != (currentId ?? 0));
            return duplicate ? "Ya existe un dividendo de esta empresa con esa fecha de pago." : null;
        }

        // ---------------------------------------------------------------- Generar la operación

        public async Task<DividendResult> GenerateOperationAsync(int id, decimal amount, string? bankAccountName, decimal? exchangeRate)
        {
            var line = await _context.DividendPayments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
            if (line == null)
            {
                return DividendResult.Fail("La línea de dividendo ya no existe.");
            }

            var blockReason = DividendPaymentRules.GetGenerateBlockReason(line, DateTime.Today);
            if (blockReason != null)
            {
                return DividendResult.Fail(blockReason);
            }

            if (amount <= 0)
            {
                return DividendResult.Fail("El importe debe ser mayor que 0.");
            }

            var companyName = await _context.CompanyDetails
                .AsNoTracking()
                .Where(c => c.Id == line.CompanyId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();
            if (companyName == null)
            {
                return DividendResult.Fail("La empresa ya no existe.");
            }

            var operation = new Operation
            {
                CompanyName = companyName,
                Type = "Dividendo",
                Date = line.PaymentDate.Date,
                CloseDate = null,
                PricePerShare = 0m,
                NumberOfShares = line.NumberOfShares,
                Commission = 0m,
                Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero),
                ExchangeRate = exchangeRate
            };

            try
            {
                // AddAsync crea el Ingreso del banco, recalcula la empresa y marca la línea Cobrada en su transacción
                await _operationService.AddAsync(operation, bankAccountName, line.Id);
                return DividendResult.Ok();
            }
            catch (InvalidOperationException ex)
            {
                return DividendResult.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar la operación del dividendo {Id}", id);
                return DividendResult.Fail("No se pudo generar la operación. No se ha guardado nada.");
            }
        }

        public async Task<int> RepairOrphanLinksAsync()
        {
            try
            {
                var collected = await _context.DividendPayments
                    .Where(d => d.Status == DividendPaymentRules.StatusCollected)
                    .ToListAsync();
                if (collected.Count == 0)
                {
                    return 0;
                }

                var operationIds = collected.Where(d => d.OperationId.HasValue).Select(d => d.OperationId!.Value).Distinct().ToList();
                var existing = (await _context.Operations
                        .AsNoTracking()
                        .Where(o => operationIds.Contains(o.Id))
                        .Select(o => o.Id)
                        .ToListAsync())
                    .ToHashSet();

                var repaired = 0;
                foreach (var line in collected.Where(d => !d.OperationId.HasValue || !existing.Contains(d.OperationId.Value)))
                {
                    line.Status = DividendPaymentRules.StatusPending;
                    line.OperationId = null;
                    line.UpdatedAt = DateTime.Now;
                    repaired++;
                }

                if (repaired > 0)
                {
                    await _context.SaveChangesAsync();
                }

                return repaired;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reparar los enlaces de las líneas de dividendo");
                return 0;
            }
        }

        // ---------------------------------------------------------------- Inicio

        public async Task<List<UpcomingDividend>> GetUpcomingAsync(int days = UpcomingDays)
        {
            var today = DateTime.Today;
            var limit = today.AddDays(days);
            var companies = (await _companyService.GetAllAsync())
                .Where(c => c.NumberOfShares > 0)
                .ToDictionary(c => c.Id);
            if (companies.Count == 0)
            {
                return new List<UpcomingDividend>();
            }

            var ids = companies.Keys.ToList();
            var lines = await _context.DividendPayments
                .AsNoTracking()
                .Where(d => d.Status == DividendPaymentRules.StatusPending
                    && ids.Contains(d.CompanyId)
                    && d.PaymentDate >= today && d.PaymentDate <= limit)
                .OrderBy(d => d.PaymentDate)
                .ToListAsync();

            return lines.Select(d => new UpcomingDividend
            {
                Id = d.Id,
                CompanyName = companies[d.CompanyId].Name,
                PaymentDate = d.PaymentDate,
                TotalAmount = DividendPaymentRules.CalculateTotal(d.NumberOfShares, d.AmountPerShare),
                CurrencyCode = CurrencyFormatter.Normalize(companies[d.CompanyId].CurrencyCode)
            }).ToList();
        }

        // ---------------------------------------------------------------- Propuestas

        public async Task<List<DividendProposalView>> GetProposalsAsync()
        {
            if (DividendProposalsData.Proposals.Count == 0)
            {
                return new List<DividendProposalView>();
            }

            var companies = await _companyService.GetAllAsync();
            var byTicker = companies
                .Where(c => !string.IsNullOrWhiteSpace(c.Ticker))
                .GroupBy(c => DividendPaymentRules.NormalizeTicker(c.Ticker))
                .ToDictionary(g => g.Key, g => g.First());
            var existing = (await _context.DividendPayments
                    .AsNoTracking()
                    .Select(d => new { d.CompanyId, d.PaymentDate })
                    .ToListAsync())
                .Select(d => (d.CompanyId, d.PaymentDate.Date))
                .ToHashSet();

            var views = new List<DividendProposalView>();
            foreach (var proposal in DividendProposalsData.Proposals)
            {
                // Las propuestas de tickers que no están entre las empresas no se muestran
                if (!byTicker.TryGetValue(DividendPaymentRules.NormalizeTicker(proposal.Ticker), out var company))
                {
                    continue;
                }

                views.Add(new DividendProposalView
                {
                    Proposal = proposal,
                    CompanyId = company.Id,
                    CompanyName = company.Name,
                    CurrencyCode = CurrencyFormatter.Normalize(company.CurrencyCode),
                    Shares = company.NumberOfShares,
                    AlreadyExists = existing.Contains((company.Id, proposal.PaymentDate.Date))
                });
            }

            return views.OrderBy(v => v.Proposal.PaymentDate).ThenBy(v => v.CompanyName).ToList();
        }

        public async Task<DividendResult> CreateFromProposalsAsync(IEnumerable<DividendProposalView> selected)
        {
            var now = DateTime.Now;
            var created = 0;
            foreach (var view in selected.Where(v => !v.AlreadyExists))
            {
                var paymentDate = view.Proposal.PaymentDate.Date;
                var exists = await _context.DividendPayments
                    .AnyAsync(d => d.CompanyId == view.CompanyId && d.PaymentDate == paymentDate)
                    || _context.DividendPayments.Local.Any(d => d.CompanyId == view.CompanyId && d.PaymentDate == paymentDate);
                if (exists)
                {
                    continue;
                }

                // Solo líneas Pendientes: las operaciones las genera el usuario con «Generar operación»
                _context.DividendPayments.Add(new DividendPayment
                {
                    CompanyId = view.CompanyId,
                    PaymentDate = paymentDate,
                    ExDividendDate = view.Proposal.ExDividendDate?.Date,
                    AmountPerShare = view.Proposal.AmountPerShare,
                    NumberOfShares = await GetDefaultSharesAsync(view.CompanyId),
                    DividendPercent = view.Proposal.DividendPercent,
                    Status = DividendPaymentRules.StatusPending,
                    Source = DividendPaymentRules.SourceSearch,
                    Notes = Truncate(view.Proposal.Source, DividendPaymentRules.MaxNotesLength),
                    CreatedAt = now,
                    UpdatedAt = now
                });
                created++;
            }

            if (created > 0)
            {
                await _context.SaveChangesAsync();
            }

            return DividendResult.Ok(created);
        }

        private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];

        // ---------------------------------------------------------------- Yahoo y gráfico

        public async Task<LastDividendResult> GetLastDividendAsync(int companyId)
        {
            var ticker = await _context.CompanyDetails
                .AsNoTracking()
                .Where(c => c.Id == companyId)
                .Select(c => c.Ticker)
                .FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(ticker))
            {
                return new LastDividendResult { Message = "La empresa no tiene ticker." };
            }

            var lookup = await _stockPriceService.GetLastDividendAsync(ticker);
            if (lookup.Failed)
            {
                return new LastDividendResult { Message = "No se pudo consultar Yahoo Finance. Comprueba la conexión a Internet." };
            }

            return lookup.Last == null
                ? new LastDividendResult { Message = "Yahoo no tiene dividendos de esta empresa." }
                : new LastDividendResult { Dividend = lookup.Last };
        }

        public async Task<List<DividendChartMark>> GetChartMarksAsync(int companyId)
        {
            var lines = await GetAsync(DividendFilter.All, companyId);
            return lines
                .OrderBy(d => d.PaymentDate)
                .Select(d => new DividendChartMark
                {
                    Id = d.Id,
                    PaymentDate = d.PaymentDate,
                    ExDividendDate = d.ExDividendDate,
                    Status = d.Status,
                    TotalAmount = d.TotalAmount,
                    CurrencyCode = d.CurrencyCode
                })
                .ToList();
        }
    }
}
