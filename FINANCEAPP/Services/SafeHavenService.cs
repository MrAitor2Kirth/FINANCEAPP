using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Servicio para la gestión de activos refugio (oro, plata, bitcoin, etc.)
    /// </summary>
    public class SafeHavenService : ISafeHavenService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SafeHavenService> _logger;

        public SafeHavenService(AppDbContext context, ILogger<SafeHavenService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Activos Refugio
        public async Task<List<SafeHavenAsset>> GetAllAssetsAsync()
        {
            try
            {
                return await _context.SafeHavenAssets
                    .OrderBy(sa => sa.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los activos refugio");
                throw;
            }
        }

        public async Task<SafeHavenAsset?> GetAssetByIdAsync(int id)
        {
            try
            {
                return await _context.SafeHavenAssets
                    .FirstOrDefaultAsync(sa => sa.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener activo refugio con ID {id}");
                throw;
            }
        }

        public async Task<SafeHavenAsset?> GetAssetByNameAsync(string name)
        {
            try
            {
                return await _context.SafeHavenAssets
                    .FirstOrDefaultAsync(sa => sa.Name == name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener activo refugio '{name}'");
                throw;
            }
        }

        public async Task<SafeHavenAsset> CreateAssetAsync(SafeHavenAsset asset)
        {
            try
            {
                // Validar que no exista un activo con el mismo nombre
                var existingAsset = await GetAssetByNameAsync(asset.Name);
                if (existingAsset != null)
                {
                    throw new InvalidOperationException($"Ya existe un activo refugio llamado '{asset.Name}'");
                }

                _context.SafeHavenAssets.Add(asset);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Activo refugio '{asset.Name}' creado exitosamente");
                return asset;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear activo refugio '{asset.Name}'");
                throw;
            }
        }

        public async Task<SafeHavenAsset> UpdateAssetAsync(SafeHavenAsset asset)
        {
            try
            {
                var existingAsset = await GetAssetByIdAsync(asset.Id);
                if (existingAsset == null)
                {
                    throw new InvalidOperationException($"No existe activo refugio con ID {asset.Id}");
                }

                // Si cambió el nombre, validar que no exista otro activo con ese nombre
                if (existingAsset.Name != asset.Name)
                {
                    var duplicateAsset = await GetAssetByNameAsync(asset.Name);
                    if (duplicateAsset != null)
                    {
                        throw new InvalidOperationException($"Ya existe un activo refugio llamado '{asset.Name}'");
                    }
                }

                existingAsset.Name = asset.Name;
                existingAsset.AssetType = asset.AssetType;
                existingAsset.TotalInvested = asset.TotalInvested;
                existingAsset.CurrentValue = asset.CurrentValue;
                existingAsset.UpdatedAt = DateTime.Now;

                _context.SafeHavenAssets.Update(existingAsset);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Activo refugio '{existingAsset.Name}' actualizado exitosamente");
                return existingAsset;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar activo refugio con ID {asset.Id}");
                throw;
            }
        }

        public async Task<bool> DeleteAssetAsync(int id)
        {
            try
            {
                var asset = await GetAssetByIdAsync(id);
                if (asset == null)
                {
                    return false;
                }

                // Eliminar todas las operaciones del activo
                var operations = await GetOperationsByAssetNameAsync(asset.Name);
                _context.SafeHavenOperations.RemoveRange(operations);

                // Eliminar el activo
                _context.SafeHavenAssets.Remove(asset);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Activo refugio con ID {id} eliminado exitosamente");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar activo refugio con ID {id}");
                throw;
            }
        }

        public async Task<decimal> GetTotalInvestedAsync()
        {
            try
            {
                return await _context.SafeHavenAssets
                    .SumAsync(sa => sa.TotalInvested);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular total invertido en activos refugio");
                throw;
            }
        }

        public async Task<decimal> GetTotalCurrentValueAsync()
        {
            try
            {
                return await _context.SafeHavenAssets
                    .SumAsync(sa => sa.CurrentValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular total valor actual en activos refugio");
                throw;
            }
        }

        public async Task<Dictionary<string, decimal>> GetInvestmentByAssetTypeAsync()
        {
            try
            {
                var result = await _context.SafeHavenAssets
                    .GroupBy(sa => sa.AssetType)
                    .Select(g => new { AssetType = g.Key, Total = g.Sum(sa => sa.TotalInvested) })
                    .ToListAsync();

                return result.ToDictionary(r => r.AssetType, r => r.Total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener inversión por tipo de activo");
                throw;
            }
        }

        public async Task<decimal> RecalculateAssetTotalAsync(string assetName)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var total = await RecalculateAssetTotalInternalAsync(assetName);
                    await transaction.CommitAsync();
                    return total;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al recalcular total de activo '{assetName}'");
                throw;
            }
        }

        private async Task<decimal> RecalculateAssetTotalInternalAsync(string assetName)
        {
            var asset = await GetAssetByNameAsync(assetName);
            if (asset == null)
                throw new InvalidOperationException($"No existe activo refugio llamado '{assetName}'");

            var operations = await GetOperationsByAssetNameAsync(assetName);

            decimal totalInvested = 0;
            foreach (var op in operations)
            {
                if (op.Type == "Compra")
                    totalInvested += op.Amount;
                else if (op.Type == "Venta")
                    totalInvested -= Math.Abs(op.Amount);
            }

            asset.TotalInvested = totalInvested;
            asset.UpdatedAt = DateTime.Now;
            _context.SafeHavenAssets.Update(asset);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Total invertido en '{assetName}' recalculado: {totalInvested}");
            return totalInvested;
        }
        #endregion

        #region Operaciones de Activos Refugio
        public async Task<List<SafeHavenOperation>> GetOperationsByAssetNameAsync(string assetName)
        {
            try
            {
                return await _context.SafeHavenOperations
                    .Where(so => so.AssetName == assetName)
                    .OrderBy(so => so.Date)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener operaciones de activo '{assetName}'");
                throw;
            }
        }

        public async Task<SafeHavenOperation?> GetOperationByIdAsync(int id)
        {
            try
            {
                return await _context.SafeHavenOperations
                    .FirstOrDefaultAsync(so => so.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener operación con ID {id}");
                throw;
            }
        }

        public async Task<SafeHavenOperation> CreateOperationAsync(SafeHavenOperation operation)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Validar que el activo exista
                    var asset = await GetAssetByNameAsync(operation.AssetName);
                    if (asset == null)
                        throw new InvalidOperationException($"No existe activo refugio llamado '{operation.AssetName}'");

                    // 2. Crear la operación
                    _context.SafeHavenOperations.Add(operation);
                    await _context.SaveChangesAsync();

                    // 3. Recalcular el total del activo (sin nueva transacción)
                    await RecalculateAssetTotalInternalAsync(operation.AssetName);

                    await transaction.CommitAsync();

                    _logger.LogInformation($"Operación de activo '{operation.AssetName}' ({operation.Type}) creada exitosamente");
                    return operation;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear operación de activo refugio");
                throw;
            }
        }

        public async Task<SafeHavenOperation> UpdateOperationAsync(SafeHavenOperation operation)
        {
            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var existingOperation = await GetOperationByIdAsync(operation.Id);
                        if (existingOperation == null)
                        {
                            throw new InvalidOperationException($"No existe operación con ID {operation.Id}");
                        }

                        string oldAssetName = existingOperation.AssetName;

                        existingOperation.AssetName = operation.AssetName;
                        existingOperation.Type = operation.Type;
                        existingOperation.Amount = operation.Amount;
                        existingOperation.Date = operation.Date;

                        _context.SafeHavenOperations.Update(existingOperation);
                        await _context.SaveChangesAsync();

                        // Recalcular total del activo anterior (sin nueva transacción)
                        await RecalculateAssetTotalInternalAsync(oldAssetName);

                        // Recalcular total del nuevo activo (si cambió)
                        if (oldAssetName != operation.AssetName)
                        {
                            await RecalculateAssetTotalInternalAsync(operation.AssetName);
                        }

                        await transaction.CommitAsync();

                        _logger.LogInformation($"Operación con ID {operation.Id} actualizada exitosamente");
                        return existingOperation;
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
                _logger.LogError(ex, $"Error al actualizar operación con ID {operation.Id}");
                throw;
            }
        }

        public async Task<bool> DeleteOperationAsync(int id)
        {
            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var operation = await GetOperationByIdAsync(id);
                        if (operation == null)
                        {
                            return false;
                        }

                        string assetName = operation.AssetName;
                        _context.SafeHavenOperations.Remove(operation);
                        await _context.SaveChangesAsync();

                        // Recalcular el total del activo (sin nueva transacción)
                        await RecalculateAssetTotalInternalAsync(assetName);

                        await transaction.CommitAsync();

                        _logger.LogInformation($"Operación con ID {id} eliminada exitosamente");
                        return true;
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
                _logger.LogError(ex, $"Error al eliminar operación con ID {id}");
                throw;
            }
        }

        public async Task<decimal> GetTotalPurchasesAsync(string assetName)
        {
            try
            {
                return await _context.SafeHavenOperations
                    .Where(so => so.AssetName == assetName && so.Type == "Compra")
                    .SumAsync(so => so.Amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al calcular compras de '{assetName}'");
                throw;
            }
        }

        public async Task<decimal> GetTotalSalesAsync(string assetName)
        {
            try
            {
                var sales = await _context.SafeHavenOperations
                    .Where(so => so.AssetName == assetName && so.Type == "Venta")
                    .SumAsync(so => Math.Abs(so.Amount));

                return sales;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al calcular ventas de '{assetName}'");
                throw;
            }
        }

        public async Task<List<SafeHavenOperation>> GetOperationsByTypeAsync(string assetName, string type)
        {
            try
            {
                return await _context.SafeHavenOperations
                    .Where(so => so.AssetName == assetName && so.Type == type)
                    .OrderBy(so => so.Date)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener operaciones de '{assetName}' tipo '{type}'");
                throw;
            }
        }
        #endregion
    }
}
