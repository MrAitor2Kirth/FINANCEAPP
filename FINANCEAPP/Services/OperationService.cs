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
        /// Registra una Venta de acciones cerrando por FIFO las compras abiertas y guardando el histórico
        /// </summary>
        public async Task<SaleRegistrationResult> RegisterSaleAsync(Operation sale, decimal sellPrice)
        {
            // 1. Validación
            if (!string.Equals(sale.Type, "Venta", StringComparison.Ordinal))
            {
                return SaleRegistrationResult.Fail("La operación debe ser de tipo Venta.");
            }

            if (string.IsNullOrWhiteSpace(sale.CompanyName))
            {
                return SaleRegistrationResult.Fail("El nombre de la empresa es obligatorio.");
            }

            if (sale.NumberOfShares <= 0)
            {
                return SaleRegistrationResult.Fail("El número de acciones de la venta debe ser mayor que 0.");
            }

            if (sale.PricePerShare <= 0)
            {
                return SaleRegistrationResult.Fail("El precio por acción de la venta debe ser mayor que 0.");
            }

            if (sellPrice <= 0)
            {
                return SaleRegistrationResult.Fail("El precio de venta debe ser mayor que 0.");
            }

            var company = await _context.CompanyDetails.FirstOrDefaultAsync(c => c.Name == sale.CompanyName);
            if (company == null)
            {
                return SaleRegistrationResult.Fail($"La empresa «{sale.CompanyName}» no existe.");
            }

            // 2. Transacción (SQLite no admite transacciones anidadas: el recálculo se llama dentro de esta)
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 3. Compras abiertas por FIFO (fecha e Id)
                var openOperations = await _context.Operations
                    .Where(o => o.CompanyName == sale.CompanyName && o.CloseDate == null)
                    .ToListAsync();

                var lots = openOperations
                    .Where(o => o.Type == "Compra" || o.Type == "Traspaso")
                    .OrderBy(o => o.Date)
                    .ThenBy(o => o.Id)
                    .ToList();

                // Las ventas antiguas que siguen abiertas ya restan acciones en la empresa
                var legacyOpenSoldShares = openOperations
                    .Where(o => o.Type == "Venta")
                    .Sum(o => o.NumberOfShares);

                var availableShares = lots.Sum(o => o.NumberOfShares) - legacyOpenSoldShares;

                // 4. Acciones insuficientes: no se cambia nada
                if (availableShares < sale.NumberOfShares)
                {
                    await transaction.RollbackAsync();
                    return SaleRegistrationResult.Fail(
                        $"No hay acciones suficientes: se venden {sale.NumberOfShares} y solo hay {Math.Max(availableShares, 0)} abiertas de «{sale.CompanyName}».");
                }

                // 5. Consumir lotes hasta cubrir las acciones vendidas
                var remaining = sale.NumberOfShares;
                decimal totalCost = 0m;
                decimal closedCommissions = 0m;
                var pendingParts = new List<Operation>();

                foreach (var lot in lots)
                {
                    if (remaining <= 0)
                    {
                        break;
                    }

                    if (lot.NumberOfShares <= remaining)
                    {
                        // Lote completo
                        totalCost += lot.PricePerShare * lot.NumberOfShares;
                        closedCommissions += lot.Commission;
                        remaining -= lot.NumberOfShares;
                        lot.CloseDate = sale.Date;
                    }
                    else
                    {
                        // Lote parcial: se divide en una fila cerrada y otra abierta duplicada.
                        // La parte pendiente se queda con el resto para no perder céntimos.
                        var closedShares = remaining;
                        var pendingShares = lot.NumberOfShares - closedShares;
                        var ratio = (decimal)closedShares / lot.NumberOfShares;

                        var closedCommission = Math.Round(lot.Commission * ratio, 2);
                        var closedAmount = Math.Round(lot.Amount * ratio, 2);

                        pendingParts.Add(new Operation
                        {
                            CompanyName = lot.CompanyName,
                            Type = lot.Type,
                            Date = lot.Date,
                            CloseDate = null,
                            PricePerShare = lot.PricePerShare,
                            NumberOfShares = pendingShares,
                            Commission = lot.Commission - closedCommission,
                            Amount = lot.Amount - closedAmount,
                            CreatedAt = lot.CreatedAt
                        });

                        totalCost += lot.PricePerShare * closedShares;
                        closedCommissions += closedCommission;

                        lot.NumberOfShares = closedShares;
                        lot.Commission = closedCommission;
                        lot.Amount = closedAmount;
                        lot.CloseDate = sale.Date;
                        remaining = 0;
                    }
                }

                _context.Operations.AddRange(pendingParts);

                // 6. Precio medio ponderado de compra
                var averageBuyPrice = Math.Round(totalCost / sale.NumberOfShares, 4);

                // 7. La Venta se guarda cerrada
                sale.CloseDate = sale.Date;
                sale.CreatedAt = DateTime.Now;
                _context.Operations.Add(sale);

                // 8. Importe, comisiones y beneficio (mismo cálculo que al editar el precio de venta)
                var roundedSellPrice = Math.Round(sellPrice, 2);
                var amount = Math.Round(roundedSellPrice * sale.NumberOfShares, 2);
                var commission = Math.Round(sale.Commission + closedCommissions, 2);
                var profit = Math.Round(amount - (averageBuyPrice * sale.NumberOfShares) - commission, 2);

                var record = new SaleHistory
                {
                    CompanyName = sale.CompanyName,
                    Ticker = company.Ticker ?? string.Empty,
                    SaleDate = sale.Date,
                    NumberOfShares = sale.NumberOfShares,
                    AverageBuyPrice = averageBuyPrice,
                    SellPrice = roundedSellPrice,
                    Amount = amount,
                    Commission = commission,
                    Profit = profit,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // Primero se guarda la Venta para conocer su Id y enlazarla en el histórico
                await _context.SaveChangesAsync();
                record.OperationId = sale.Id;
                _context.SalesHistory.Add(record);
                await _context.SaveChangesAsync();

                // 9. Recalcular la empresa dentro de la transacción y confirmar
                await _companyService.RecalculateTotalInvestedAsync(sale.CompanyName);
                await transaction.CommitAsync();

                return new SaleRegistrationResult { Success = true, SaleHistoryId = record.Id };
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
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
