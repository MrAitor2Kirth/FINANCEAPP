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

        /// <summary>
        /// Constructor que inyecta el contexto de base de datos
        /// </summary>
        /// <param name="context">Contexto de Entity Framework</param>
        public CompanyService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las empresas ordenadas alfabéticamente por nombre
        /// </summary>
        public async Task<List<CompanyDetail>> GetAllAsync()
        {
            return await _context.CompanyDetails
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene una empresa por su ID
        /// </summary>
        public async Task<CompanyDetail?> GetByIdAsync(int id)
        {
            return await _context.CompanyDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <summary>
        /// Obtiene una empresa por su ticker único
        /// </summary>
        public async Task<CompanyDetail?> GetByTickerAsync(string ticker)
        {
            return await _context.CompanyDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Ticker == ticker);
        }

        /// <summary>
        /// Obtiene todas las empresas de un sector específico
        /// </summary>
        public async Task<List<CompanyDetail>> GetBySectorAsync(string sector)
        {
            return await _context.CompanyDetails
                .AsNoTracking()
                .Where(c => c.Sector == sector)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Busca empresas cuyo nombre contenga el término de búsqueda
        /// </summary>
        public async Task<List<CompanyDetail>> SearchByNameAsync(string searchTerm)
        {
            return await _context.CompanyDetails
                .AsNoTracking()
                .Where(c => c.Name.Contains(searchTerm))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene empresas con dividend yield igual o superior al valor mínimo
        /// </summary>
        public async Task<List<CompanyDetail>> GetByMinDividendYieldAsync(decimal minYield)
        {
            return await _context.CompanyDetails
                .AsNoTracking()
                .Where(c => c.DividendYield >= minYield)
                .OrderByDescending(c => c.DividendYield)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene las empresas con mayor dividend yield
        /// </summary>
        public async Task<List<CompanyDetail>> GetTopByDividendYieldAsync(int topCount = 10)
        {
            return await _context.CompanyDetails
                .AsNoTracking()
                .OrderByDescending(c => c.DividendYield)
                .Take(topCount)
                .ToListAsync();
        }

        /// <summary>
        /// Añade una nueva empresa a la base de datos
        /// </summary>
        public async Task<CompanyDetail> AddAsync(CompanyDetail company)
        {
            company.CreatedAt = DateTime.Now;
            company.UpdatedAt = DateTime.Now;
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

            existing.Name = company.Name;
            existing.Ticker = company.Ticker;
            existing.Sector = company.Sector;
            existing.CurrentPrice = company.CurrentPrice;
            existing.DividendYield = company.DividendYield;
            existing.PlusvaliaReturnPercent = existing.CalculatePlusvaliaReturnPercent();
            existing.DividendPlusvaliaReturnPercent = existing.CalculateDividendPlusvaliaReturnPercent();
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

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

            _context.CompanyDetails.Remove(company);
            await _context.SaveChangesAsync();
            return true;
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

            var total = openOperations.Sum(o => o.Amount);
            var shares = openOperations.Sum(o => o.NumberOfShares);
            var dividends = await _context.Operations
                .Where(o => o.CompanyName == companyName && o.Type == "Dividendo")
                .SumAsync(o => (decimal?)o.Amount) ?? 0m;

            company.TotalInvestedInitial = total;
            company.NumberOfShares = shares;
            company.TotalDividends = dividends;
            company.PlusvaliaReturnPercent = company.CalculatePlusvaliaReturnPercent();
            company.DividendPlusvaliaReturnPercent = company.CalculateDividendPlusvaliaReturnPercent();
            company.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return total;
        }
    }
}
