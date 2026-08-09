using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Servicio para la gestión de fondos de inversión
    /// Implementa las dos opciones de gestión de plusvalías
    /// </summary>
    public class FundService : IFundService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FundService> _logger;

        public FundService(AppDbContext context, ILogger<FundService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Fondos
        public async Task<List<Fund>> GetAllFundsAsync()
        {
            try
            {
                return await _context.Funds
                    .OrderBy(f => f.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los fondos");
                throw;
            }
        }

        public async Task<Fund?> GetFundByIdAsync(int id)
        {
            try
            {
                return await _context.Funds
                    .FirstOrDefaultAsync(f => f.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener fondo con ID {id}");
                throw;
            }
        }

        public async Task<Fund?> GetFundByNameAsync(string name)
        {
            try
            {
                return await _context.Funds
                    .FirstOrDefaultAsync(f => f.Name == name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener fondo con nombre {name}");
                throw;
            }
        }

        public async Task<Fund> CreateFundAsync(Fund fund)
        {
            try
            {
                // Validar que no exista un fondo con el mismo nombre
                var existingFund = await GetFundByNameAsync(fund.Name);
                if (existingFund != null)
                {
                    throw new InvalidOperationException($"Ya existe un fondo llamado '{fund.Name}'");
                }

                _context.Funds.Add(fund);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Fondo '{fund.Name}' creado exitosamente");
                return fund;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear fondo '{fund.Name}'");
                throw;
            }
        }

        public async Task<Fund> UpdateFundAsync(Fund fund)
        {
            try
            {
                var existingFund = await GetFundByIdAsync(fund.Id);
                if (existingFund == null)
                {
                    throw new InvalidOperationException($"No existe fondo con ID {fund.Id}");
                }

                // Si cambió el nombre, validar que no exista otro fondo con ese nombre
                if (existingFund.Name != fund.Name)
                {
                    var duplicateFund = await GetFundByNameAsync(fund.Name);
                    if (duplicateFund != null)
                    {
                        throw new InvalidOperationException($"Ya existe un fondo llamado '{fund.Name}'");
                    }
                }

                existingFund.Name = fund.Name;
                existingFund.FundType = fund.FundType;
                existingFund.Manager = fund.Manager;
                existingFund.TER = fund.TER;
                existingFund.ISIN = fund.ISIN;
                existingFund.HistoricalReturn = fund.HistoricalReturn;
                existingFund.TotalInvested = fund.TotalInvested;
                existingFund.CurrentValue = fund.CurrentValue;
                existingFund.UpdatedAt = DateTime.Now;

                _context.Funds.Update(existingFund);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Fondo '{existingFund.Name}' actualizado exitosamente");
                return existingFund;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar fondo con ID {fund.Id}");
                throw;
            }
        }

        public async Task<bool> DeleteFundAsync(int id)
        {
            try
            {
                var fund = await GetFundByIdAsync(id);
                if (fund == null)
                {
                    return false;
                }

                // Eliminar todas las operaciones del fondo
                var operations = await GetOperationsByFundNameAsync(fund.Name);
                _context.FundOperations.RemoveRange(operations);

                // Eliminar historial de valores si existe (Opción B)
                var valueHistory = await _context.FundValueHistories
                    .Where(fvh => fvh.FundName == fund.Name)
                    .ToListAsync();
                _context.FundValueHistories.RemoveRange(valueHistory);

                // Eliminar el fondo
                _context.Funds.Remove(fund);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Fondo con ID {id} eliminado exitosamente");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar fondo con ID {id}");
                throw;
            }
        }

        public async Task<decimal> GetTotalInvestedAsync()
        {
            try
            {
                return await _context.Funds
                    .SumAsync(f => f.TotalInvested);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular total invertido");
                throw;
            }
        }

        public async Task<decimal> GetTotalCurrentValueAsync()
        {
            try
            {
                return await _context.Funds
                    .SumAsync(f => f.CurrentValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular valor total actual");
                throw;
            }
        }

        public async Task<decimal> GetAggregateReturnAsync()
        {
            try
            {
                var totalInvested = await GetTotalInvestedAsync();
                if (totalInvested == 0)
                    return 0;

                var totalCurrentValue = await GetTotalCurrentValueAsync();
                return ((totalCurrentValue - totalInvested) / totalInvested) * 100;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular rentabilidad agregada");
                throw;
            }
        }
        #endregion

        #region Operaciones
        public async Task<List<FundOperation>> GetOperationsByFundNameAsync(string fundName)
        {
            try
            {
                return await _context.FundOperations
                    .Where(fo => fo.FundName == fundName)
                    .OrderBy(fo => fo.Date)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener operaciones del fondo '{fundName}'");
                throw;
            }
        }

        public async Task<FundOperation?> GetOperationByIdAsync(int id)
        {
            try
            {
                return await _context.FundOperations
                    .FirstOrDefaultAsync(fo => fo.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener operación con ID {id}");
                throw;
            }
        }

        public async Task<FundOperation> CreateOperationAsync(FundOperation operation)
        {
            try
            {
                // Validar que el fondo exista
                var fund = await GetFundByNameAsync(operation.FundName);
                if (fund == null)
                {
                    throw new InvalidOperationException($"No existe fondo llamado '{operation.FundName}'");
                }

                _context.FundOperations.Add(operation);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Operación de fondo '{operation.FundName}' ({operation.Type}) creada exitosamente");
                return operation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear operación de fondo");
                throw;
            }
        }

        public async Task<FundOperation> UpdateOperationAsync(FundOperation operation)
        {
            try
            {
                var existingOperation = await GetOperationByIdAsync(operation.Id);
                if (existingOperation == null)
                {
                    throw new InvalidOperationException($"No existe operación con ID {operation.Id}");
                }

                existingOperation.FundName = operation.FundName;
                existingOperation.Type = operation.Type;
                existingOperation.Amount = operation.Amount;
                existingOperation.Date = operation.Date;

                _context.FundOperations.Update(existingOperation);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Operación con ID {operation.Id} actualizada exitosamente");
                return existingOperation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar operación con ID {operation.Id}");
                throw;
            }
        }

        public async Task<bool> DeleteOperationAsync(int id)
        {
            try
            {
                var operation = await GetOperationByIdAsync(id);
                if (operation == null)
                {
                    return false;
                }

                _context.FundOperations.Remove(operation);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Operación con ID {id} eliminada exitosamente");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar operación con ID {id}");
                throw;
            }
        }

        public async Task<decimal> GetTotalContributionsAsync(string fundName)
        {
            try
            {
                return await _context.FundOperations
                    .Where(fo => fo.FundName == fundName && fo.Type == "Aportación")
                    .SumAsync(fo => fo.Amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al calcular aportaciones del fondo '{fundName}'");
                throw;
            }
        }

        public async Task<decimal> GetTotalRedemptionsAsync(string fundName)
        {
            try
            {
                return await _context.FundOperations
                    .Where(fo => fo.FundName == fundName && fo.Type == "Reembolso")
                    .SumAsync(fo => Math.Abs(fo.Amount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al calcular reembolsos del fondo '{fundName}'");
                throw;
            }
        }

        public async Task<FundOperation?> GetLatestPlusvaliOperationAsync(string fundName)
        {
            try
            {
                return await _context.FundOperations
                    .Where(fo => fo.FundName == fundName && fo.Type == "Plusvalía")
                    .OrderByDescending(fo => fo.Date)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener última operación de plusvalía del fondo '{fundName}'");
                throw;
            }
        }
        #endregion

        #region Plusvalías - Opción A: Operación de Plusvalía
        public async Task UpdateFundValueWithPlusvaliaOperationAsync(string fundName, decimal newValue)
        {
            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. Obtener el fondo
                        var fund = await GetFundByNameAsync(fundName);
                        if (fund == null)
                        {
                            throw new InvalidOperationException($"No existe fondo llamado '{fundName}'");
                        }

                        // 2. Buscar la última operación de plusvalía
                        var lastPlusvaliaOperation = await GetLatestPlusvaliOperationAsync(fundName);

                        // 3. Si existe, eliminarla
                        if (lastPlusvaliaOperation != null)
                        {
                            await DeleteOperationAsync(lastPlusvaliaOperation.Id);
                            _logger.LogInformation($"Operación de plusvalía anterior del fondo '{fundName}' eliminada");
                        }

                        // 4. Calcular la diferencia
                        decimal difference = newValue - fund.TotalInvested;

                        // 5. Crear NUEVA operación de plusvalía
                        var newOperation = new FundOperation
                        {
                            FundName = fundName,
                            Type = "Plusvalía",
                            Amount = difference,
                            Date = DateTime.Now,
                            CreatedAt = DateTime.Now
                        };

                        _context.FundOperations.Add(newOperation);

                        // 6. Actualizar el valor actual del fondo
                        fund.CurrentValue = newValue;
                        fund.UpdatedAt = DateTime.Now;
                        _context.Funds.Update(fund);

                        // 7. Guardar cambios
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        _logger.LogInformation($"Valor del fondo '{fundName}' actualizado a {newValue} (diferencia: {difference}) - OPCIÓN A");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en UpdateFundValueWithPlusvaliaOperationAsync para fondo '{fundName}'");
                throw;
            }
        }
        #endregion

        #region Plusvalías - Opción B: Historial de Valores
        public async Task UpdateFundValueWithHistoryAsync(string fundName, decimal newValue)
        {
            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. Obtener el fondo
                        var fund = await GetFundByNameAsync(fundName);
                        if (fund == null)
                        {
                            throw new InvalidOperationException($"No existe fondo llamado '{fundName}'");
                        }

                        // 2. Calcular la rentabilidad en esta fecha
                        decimal returnAtDate = fund.TotalInvested > 0
                            ? ((newValue - fund.TotalInvested) / fund.TotalInvested) * 100
                            : 0;

                        // 3. Registrar en el historial
                        var valueHistory = new FundValueHistory
                        {
                            FundName = fundName,
                            Value = newValue,
                            Date = DateTime.Now,
                            ReturnAtDate = returnAtDate,
                            CreatedAt = DateTime.Now,
                            Notes = $"Actualización automática del {DateTime.Now:dd/MM/yyyy}"
                        };

                        _context.FundValueHistories.Add(valueHistory);

                        // 4. Actualizar el valor actual del fondo
                        fund.CurrentValue = newValue;
                        fund.UpdatedAt = DateTime.Now;
                        _context.Funds.Update(fund);

                        // 5. Guardar cambios
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        _logger.LogInformation($"Valor del fondo '{fundName}' actualizado a {newValue} con rentabilidad {returnAtDate:F2}% - OPCIÓN B");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en UpdateFundValueWithHistoryAsync para fondo '{fundName}'");
                throw;
            }
        }

        public async Task<List<FundValueHistory>> GetFundValueHistoryAsync(string fundName)
        {
            try
            {
                return await _context.FundValueHistories
                    .Where(fvh => fvh.FundName == fundName)
                    .OrderBy(fvh => fvh.Date)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener historial de valores del fondo '{fundName}'");
                throw;
            }
        }
        #endregion
    }
}
