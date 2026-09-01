using Microsoft.EntityFrameworkCore;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Servicio que implementa la lógica de negocio para gestionar operaciones financieras
    /// Utiliza Entity Framework Core para el acceso a datos
    /// </summary>
    public class OperationService : IOperationService
    {
        private readonly AppDbContext _context;
        private readonly ICompanyService _companyService;

        /// <summary>
        /// Constructor que inyecta el contexto de base de datos
        /// </summary>
        /// <param name="context">Contexto de Entity Framework</param>
        /// <param name="companyService">Servicio de empresas para recalcular importes</param>
        public OperationService(AppDbContext context, ICompanyService companyService)
        {
            _context = context;
            _companyService = companyService;
        }

        /// <summary>
        /// Obtiene todas las operaciones ordenadas por fecha descendente
        /// </summary>
        public async Task<List<Operation>> GetAllAsync()
        {
            return await _context.Operations
                .OrderByDescending(o => o.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene una operación por su ID
        /// </summary>
        public async Task<Operation?> GetByIdAsync(int id)
        {
            return await _context.Operations
                .FindAsync(id);
        }

        /// <summary>
        /// Obtiene todas las operaciones de una empresa específica
        /// </summary>
        public async Task<List<Operation>> GetByCompanyAsync(string companyName)
        {
            return await _context.Operations
                .Where(o => o.CompanyName.Contains(companyName))
                .OrderByDescending(o => o.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene operaciones filtradas por tipo
        /// </summary>
        public async Task<List<Operation>> GetByTypeAsync(string type)
        {
            return await _context.Operations
                .Where(o => o.Type == type)
                .OrderByDescending(o => o.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene las operaciones que no tienen fecha de cierre (operaciones abiertas)
        /// </summary>
        public async Task<List<Operation>> GetOpenOperationsAsync()
        {
            return await _context.Operations
                .Where(o => o.CloseDate == null)
                .OrderByDescending(o => o.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene las operaciones que tienen fecha de cierre (operaciones cerradas)
        /// </summary>
        public async Task<List<Operation>> GetClosedOperationsAsync()
        {
            return await _context.Operations
                .Where(o => o.CloseDate != null)
                .OrderByDescending(o => o.CloseDate)
                .ToListAsync();
        }

        /// <summary>
        /// Añade una nueva operación a la base de datos
        /// </summary>
        public async Task<Operation> AddAsync(Operation operation)
        {
            operation.CreatedAt = DateTime.Now;
            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(operation.CompanyName))
            {
                await _companyService.RecalculateTotalInvestedAsync(operation.CompanyName);
            }

            return operation;
        }

        /// <summary>
        /// Actualiza una operación existente
        /// </summary>
        public async Task<bool> UpdateAsync(Operation operation)
        {
            try
            {
                var existing = await _context.Operations.FindAsync(operation.Id);
                if (existing == null)
                {
                    return false;
                }

                var previousCompanyName = existing.CompanyName;

                existing.CompanyName = operation.CompanyName;
                existing.Type = operation.Type;
                existing.Date = operation.Date;
                existing.CloseDate = operation.CloseDate;
                existing.Amount = operation.Amount;
                existing.PricePerShare = operation.PricePerShare;
                existing.NumberOfShares = operation.NumberOfShares;
                existing.Commission = operation.Commission;

                await _context.SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(previousCompanyName))
                {
                    await _companyService.RecalculateTotalInvestedAsync(previousCompanyName);
                }

                if (!string.IsNullOrWhiteSpace(operation.CompanyName) && operation.CompanyName != previousCompanyName)
                {
                    await _companyService.RecalculateTotalInvestedAsync(operation.CompanyName);
                }

                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Operations.AnyAsync(o => o.Id == operation.Id))
                {
                    return false;
                }
                throw;
            }
        }

        /// <summary>
        /// Elimina una operación de la base de datos
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var operation = await _context.Operations.FindAsync(id);
            if (operation == null)
            {
                return false;
            }

            var companyName = operation.CompanyName;
            _context.Operations.Remove(operation);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(companyName))
            {
                await _companyService.RecalculateTotalInvestedAsync(companyName);
            }

            return true;
        }

        /// <summary>
        /// Calcula el total invertido en operaciones abiertas
        /// </summary>
        public async Task<decimal> GetTotalOpenAmountAsync()
        {
            return await _context.Operations
                .Where(o => o.CloseDate == null)
                .SumAsync(o => o.Amount);
        }

        /// <summary>
        /// Obtiene un diccionario con el conteo de operaciones agrupadas por tipo
        /// </summary>
        public async Task<Dictionary<string, int>> GetOperationCountByTypeAsync()
        {
            return await _context.Operations
                .GroupBy(o => o.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);
        }
    }
}
