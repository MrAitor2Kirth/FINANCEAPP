using Microsoft.EntityFrameworkCore;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Servicio que implementa la lógica de negocio para gestionar detalles de empresas
    /// Utiliza Entity Framework Core para el acceso a datos
    /// </summary>
    public class CompanyService : ICompanyService
    {
        private readonly AppDbContext _context;
        private readonly IStockPriceService _stockPriceService;

        /// <summary>
        /// Constructor que inyecta el contexto de base de datos
        /// </summary>
        /// <param name="context">Contexto de Entity Framework</param>
        public CompanyService(AppDbContext context, IStockPriceService stockPriceService)
        {
            _context = context;
            _stockPriceService = stockPriceService;
        }

        /// <summary>
        /// Obtiene todas las empresas ordenadas alfabéticamente por nombre
        /// </summary>
        public async Task<List<CompanyDetail>> GetAllAsync()
        {
            return await ApplyCurrencyAsync(await _context.CompanyDetails
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync());
        }

        /// <summary>
        /// Rellena la divisa (CurrencyCode, solo interfaz) de las empresas a partir de su país.
        /// Las empresas sin país, o con un país que ya no está en el catálogo, quedan en EUR.
        /// </summary>
        private async Task<List<CompanyDetail>> ApplyCurrencyAsync(List<CompanyDetail> companies)
        {
            if (companies.Count == 0)
            {
                return companies;
            }

            var currencies = await GetCurrenciesByCountryAsync();
            foreach (var company in companies)
            {
                ApplyCurrency(company, currencies);
            }

            return companies;
        }

        private async Task<CompanyDetail?> ApplyCurrencyAsync(CompanyDetail? company)
        {
            if (company != null)
            {
                ApplyCurrency(company, await GetCurrenciesByCountryAsync());
            }

            return company;
        }

        private async Task<Dictionary<string, string>> GetCurrenciesByCountryAsync()
        {
            return await _context.Countries
                .AsNoTracking()
                .ToDictionaryAsync(c => c.Code, c => c.CurrencyCode);
        }

        private static void ApplyCurrency(CompanyDetail company, Dictionary<string, string> currencies)
        {
            company.CurrencyCode = company.CountryCode != null
                                   && currencies.TryGetValue(company.CountryCode, out var currency)
                ? CurrencyFormatter.Normalize(currency)
                : CurrencyFormatter.DefaultCurrency;
        }

        /// <summary>
        /// Obtiene una empresa por su ID
        /// </summary>
        public async Task<CompanyDetail?> GetByIdAsync(int id)
        {
            return await ApplyCurrencyAsync(await _context.CompanyDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id));
        }

        /// <summary>
        /// Obtiene una empresa por su ticker único
        /// </summary>
        public async Task<CompanyDetail?> GetByTickerAsync(string ticker)
        {
            return await ApplyCurrencyAsync(await _context.CompanyDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Ticker == ticker));
        }

        /// <summary>
        /// Obtiene todas las empresas de un sector específico
        /// </summary>
        public async Task<List<CompanyDetail>> GetBySectorAsync(string sector)
        {
            return await ApplyCurrencyAsync(await _context.CompanyDetails
                .AsNoTracking()
                .Where(c => c.Sector == sector)
                .OrderBy(c => c.Name)
                .ToListAsync());
        }

        /// <summary>
        /// Busca empresas cuyo nombre contenga el término de búsqueda
        /// </summary>
        public async Task<List<CompanyDetail>> SearchByNameAsync(string searchTerm)
        {
            return await ApplyCurrencyAsync(await _context.CompanyDetails
                .AsNoTracking()
                .Where(c => c.Name.Contains(searchTerm))
                .OrderBy(c => c.Name)
                .ToListAsync());
        }

        /// <summary>
        /// Obtiene empresas con dividend yield igual o superior al valor mínimo
        /// </summary>
        public async Task<List<CompanyDetail>> GetByMinDividendYieldAsync(decimal minYield)
        {
            return await ApplyCurrencyAsync(await _context.CompanyDetails
                .AsNoTracking()
                .Where(c => c.DividendYield >= minYield)
                .OrderByDescending(c => c.DividendYield)
                .ToListAsync());
        }

        /// <summary>
        /// Obtiene las empresas con mayor dividend yield
        /// </summary>
        public async Task<List<CompanyDetail>> GetTopByDividendYieldAsync(int topCount = 10)
        {
            return await ApplyCurrencyAsync(await _context.CompanyDetails
                .AsNoTracking()
                .OrderByDescending(c => c.DividendYield)
                .Take(topCount)
                .ToListAsync());
        }

        /// <summary>
        /// Añade una nueva empresa a la base de datos
        /// </summary>
        public async Task<CompanyDetail> AddAsync(CompanyDetail company)
        {
            company.CreatedAt = DateTime.Now;
            company.UpdatedAt = DateTime.Now;
            company.CountryCode = NormalizeCountryCode(company.CountryCode);
            _context.CompanyDetails.Add(company);
            await _context.SaveChangesAsync();
            return company;
        }

        /// <summary>
        /// Actualiza una empresa existente
        /// </summary>
        public async Task<bool> UpdateAsync(CompanyDetail company)
        {
            var existing = await _context.CompanyDetails.FindAsync(company.Id);
            if (existing == null) return false;

            // País (SPEC-01-02): se valida el cambio y, si la empresa no tenía país, sus operaciones
            // sin divisa reciben la del nuevo país. Todo se guarda en el mismo SaveChanges.
            var newCountryCode = NormalizeCountryCode(company.CountryCode);
            var oldCountryCode = NormalizeCountryCode(existing.CountryCode);
            if (newCountryCode != oldCountryCode)
            {
                if (await GetCountryChangeErrorAsync(company) != null)
                {
                    return false;
                }

                if (newCountryCode != null)
                {
                    var country = await _context.Countries.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Code == newCountryCode);
                    if (country == null)
                    {
                        return false;
                    }

                    var oldCountryExists = oldCountryCode != null
                        && await _context.Countries.AnyAsync(c => c.Code == oldCountryCode);
                    if (!oldCountryExists)
                    {
                        var undated = await _context.Operations
                            .Where(o => o.CompanyName == existing.Name && o.CurrencyCode == null)
                            .ToListAsync();
                        foreach (var operation in undated)
                        {
                            operation.CurrencyCode = CurrencyFormatter.Normalize(country.CurrencyCode);
                        }
                    }
                }

                existing.CountryCode = newCountryCode;
            }

            existing.Name = company.Name;
            existing.Ticker = company.Ticker;
            existing.Sector = company.Sector;
            existing.CurrentPrice = company.CurrentPrice;
            existing.LivePrice = company.LivePrice;
            existing.DividendYield = company.DividendYield;
            existing.PlusvaliaReturnPercent = existing.CalculatePlusvaliaReturnPercent();
            existing.DividendPlusvaliaReturnPercent = existing.CalculateDividendPlusvaliaReturnPercent();
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Devuelve el mensaje de error si el cambio de país de la empresa no está permitido
        /// (país inexistente, o cambio a otra divisa con operaciones), o null si se puede guardar.
        /// </summary>
        public async Task<string?> GetCountryChangeErrorAsync(CompanyDetail company)
        {
            var newCode = NormalizeCountryCode(company.CountryCode);
            if (newCode == null)
            {
                return "Elige el país de la empresa.";
            }

            var newCountry = await _context.Countries.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Code == newCode);
            if (newCountry == null)
            {
                return "El país elegido ya no existe en el catálogo.";
            }

            if (company.Id == 0)
            {
                return null;
            }

            var existing = await _context.CompanyDetails.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == company.Id);
            if (existing == null || NormalizeCountryCode(existing.CountryCode) == newCode)
            {
                return null;
            }

            var oldCountry = existing.CountryCode == null
                ? null
                : await _context.Countries.AsNoTracking().FirstOrDefaultAsync(c => c.Code == existing.CountryCode);
            if (oldCountry == null)
            {
                // Sin país (o país desaparecido): se asigna y las operaciones sin divisa heredan la nueva
                return null;
            }

            var oldCurrency = CurrencyFormatter.Normalize(oldCountry.CurrencyCode);
            var newCurrency = CurrencyFormatter.Normalize(newCountry.CurrencyCode);
            if (oldCurrency == newCurrency)
            {
                return null;
            }

            var operations = await _context.Operations.CountAsync(o => o.CompanyName == existing.Name);
            if (operations == 0)
            {
                return null;
            }

            var many = operations == 1 ? "1 operación" : $"{operations} operaciones";
            return $"No se puede cambiar a {newCountry.Name}: la empresa tiene {many} en {oldCurrency} y ese país usa {newCurrency}. " +
                   "Elige un país con la misma divisa o elimina antes sus operaciones.";
        }

        private static string? NormalizeCountryCode(string? code) =>
            string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();

        public async Task<bool> UpdateCurrentPriceAsync(string companyName, decimal price)
        {
            var company = await _context.CompanyDetails
                .FirstOrDefaultAsync(c => c.Name == companyName);
            if (company == null) return false;

            company.CurrentPrice = price;
            company.PlusvaliaReturnPercent = company.CalculatePlusvaliaReturnPercent();
            company.DividendPlusvaliaReturnPercent = company.CalculateDividendPlusvaliaReturnPercent();
            company.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Elimina una empresa de la base de datos
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var company = await _context.CompanyDetails.FindAsync(id);
            if (company == null)
            {
                return false;
            }

            // Eliminación en cascada: operaciones de la empresa (vinculadas por nombre) y su
            // histórico de cotización en caché, todo en una única transacción.
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var operations = await _context.Operations
                    .Where(o => o.CompanyName == company.Name)
                    .ToListAsync();
                _context.Operations.RemoveRange(operations);

                var priceHistory = await _context.PriceHistoryPoints
                    .Where(p => p.CompanyId == company.Id)
                    .ToListAsync();
                _context.PriceHistoryPoints.RemoveRange(priceHistory);

                _context.CompanyDetails.Remove(company);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<int> CountOperationsAsync(string companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName))
            {
                return 0;
            }

            return await _context.Operations.CountAsync(o => o.CompanyName == companyName);
        }

        /// <summary>
        /// Obtiene una lista de todos los sectores únicos
        /// </summary>
        public async Task<List<string>> GetAllSectorsAsync()
        {
            return await _context.CompanyDetails
                .Select(c => c.Sector)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();
        }

        /// <summary>
        /// Verifica si existe una empresa con el ticker especificado
        /// Puede excluir un ID específico (útil para validación en actualizaciones)
        /// </summary>
        public async Task<bool> TickerExistsAsync(string ticker, int? excludeId = null)
        {
            var query = _context.CompanyDetails.Where(c => c.Ticker == ticker);

            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<decimal> RecalculateTotalInvestedAsync(string companyName)
        {
            var company = await _context.CompanyDetails.FirstOrDefaultAsync(c => c.Name == companyName);
            if (company == null)
            {
                return 0m;
            }

            var openOperations = await _context.Operations
                .Where(o => o.CompanyName == companyName && o.CloseDate == null)
                .ToListAsync();

            var total = openOperations
                .Where(o => o.Type == "Compra" || o.Type == "Traspaso")
                .Sum(o => o.Amount);

            var soldAmount = openOperations
                .Where(o => o.Type == "Venta")
                .Sum(o => o.Amount);

            var shares = openOperations
                .Where(o => o.Type == "Compra" || o.Type == "Traspaso")
                .Sum(o => o.NumberOfShares)
                - openOperations
                    .Where(o => o.Type == "Venta")
                    .Sum(o => o.NumberOfShares);

            var dividends = await _context.Operations
                .Where(o => o.CompanyName == companyName && o.Type == "Dividendo")
                .SumAsync(o => (decimal?)o.Amount) ?? 0m;

            var adjustedTotal = total - soldAmount;

            company.TotalInvestedInitial = adjustedTotal;
            company.NumberOfShares = shares;
            company.TotalDividends = dividends;
            company.PlusvaliaReturnPercent = company.CalculatePlusvaliaReturnPercent();
            company.DividendPlusvaliaReturnPercent = company.CalculateDividendPlusvaliaReturnPercent();
            company.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return adjustedTotal;
        }

        public async Task<bool> RefreshLivePriceAsync(int companyId)
        {
            var company = await _context.CompanyDetails.FirstOrDefaultAsync(c => c.Id == companyId);
            if (company == null || string.IsNullOrWhiteSpace(company.Ticker))
            {
                return false;
            }

            var livePrice = await _stockPriceService.GetLatestPriceAsync(company.Ticker);
            if (livePrice.HasValue)
            {
                company.LivePrice = livePrice.Value;
                company.LivePriceUpdatedAt = DateTime.Now;
                company.LivePriceError = null;
                await _context.SaveChangesAsync();
                return true;
            }

            company.LivePriceError = "No se pudo obtener el precio en vivo";
            company.LivePriceUpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return false;
        }

        public async Task<StockQuote?> RefreshLiveQuoteAsync(int companyId, string? resolvedSymbol = null)
        {
            var company = await _context.CompanyDetails.FirstOrDefaultAsync(c => c.Id == companyId);
            if (company == null || string.IsNullOrWhiteSpace(company.Ticker))
            {
                return null;
            }

            var quote = await _stockPriceService.GetQuoteAsync(
                string.IsNullOrWhiteSpace(resolvedSymbol) ? company.Ticker : resolvedSymbol);

            // Sin precio no se toca lo guardado: se conserva el último precio válido y su hora
            if (quote == null)
            {
                return null;
            }

            company.LivePrice = quote.Price;
            company.LivePriceUpdatedAt = DateTime.Now;
            company.LivePriceError = null;
            await _context.SaveChangesAsync();
            return quote;
        }

        public async Task RefreshAllLivePricesAsync()
        {
            var companies = await _context.CompanyDetails
                .Where(c => !string.IsNullOrWhiteSpace(c.Ticker))
                .ToListAsync();

            if (companies.Count == 0)
            {
                return;
            }

            var prices = await _stockPriceService.GetLatestPricesAsync(companies.Select(c => c.Ticker).Distinct());

            foreach (var company in companies)
            {
                if (prices.TryGetValue(company.Ticker, out var newPrice) && newPrice.HasValue)
                {
                    company.LivePrice = newPrice.Value;
                    company.LivePriceUpdatedAt = DateTime.Now;
                    company.LivePriceError = null;
                }
                else
                {
                    company.LivePriceError = "No se pudo obtener el precio en vivo";
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}

















