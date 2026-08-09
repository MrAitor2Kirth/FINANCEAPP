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
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene una empresa por su ID
        /// </summary>
        public async Task<CompanyDetail?> GetByIdAsync(int id)
        {
            return await _context.CompanyDetails
                .FindAsync(id);
        }

        /// <summary>
        /// Obtiene una empresa por su ticker único
        /// </summary>
        public async Task<CompanyDetail?> GetByTickerAsync(string ticker)
        {
            return await _context.CompanyDetails
                .FirstOrDefaultAsync(c => c.Ticker == ticker);
        }

        /// <summary>
        /// Obtiene todas las empresas de un sector específico
        /// </summary>
        public async Task<List<CompanyDetail>> GetBySectorAsync(string sector)
        {
            return await _context.CompanyDetails
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
            try
            {
                company.UpdatedAt = DateTime.Now;
                _context.CompanyDetails.Update(company);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Verificar si la empresa aún existe
                if (!await _context.CompanyDetails.AnyAsync(c => c.Id == company.Id))
                {
                    return false;
                }
                throw;
            }
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
    }
}
