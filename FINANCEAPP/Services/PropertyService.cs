using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Servicio para la gestión de propiedades inmobiliarias
    /// Implementa la lógica de compra (Opción B) y venta (Opción C)
    /// </summary>
    public class PropertyService : IPropertyService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PropertyService> _logger;

        public PropertyService(AppDbContext context, ILogger<PropertyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Propiedades
        public async Task<List<Property>> GetAllPropertiesAsync()
        {
            try
            {
                return await _context.Properties
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todas las propiedades");
                throw;
            }
        }

        public async Task<List<Property>> GetActivePropertiesAsync()
        {
            try
            {
                return await _context.Properties
                    .Where(p => p.Status != "Vendida")
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener propiedades activas");
                throw;
            }
        }

        public async Task<List<Property>> GetSoldPropertiesAsync()
        {
            try
            {
                return await _context.Properties
                    .Where(p => p.Status == "Vendida")
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener propiedades vendidas");
                throw;
            }
        }

        public async Task<Property?> GetPropertyByIdAsync(int id)
        {
            try
            {
                return await _context.Properties
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener propiedad con ID {id}");
                throw;
            }
        }

        public async Task<Property?> GetPropertyByNameAsync(string name)
        {
            try
            {
                return await _context.Properties
                    .FirstOrDefaultAsync(p => p.Name == name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener propiedad '{name}'");
                throw;
            }
        }

        public async Task<Property> CreatePropertyAsync(Property property)
        {
            try
            {
                // Validar que no exista una propiedad con el mismo nombre
                var existingProperty = await GetPropertyByNameAsync(property.Name);
                if (existingProperty != null)
                {
                    throw new InvalidOperationException($"Ya existe una propiedad llamada '{property.Name}'");
                }

                // OPCIÓN B: La compra inicial se registra SOLO en Property.PurchasePrice
                // No creamos una operación "Compra Inicial"
                property.Status = property.Status ?? "Vacío";

                // Una propiedad nueva no tiene operaciones: lo invertido es el precio de compra.
                // Antes quedaba a 0 y el patrimonio no la sumaba si no tenía valor actual.
                property.TotalInvested = property.PurchasePrice;
                property.TotalIncome = 0m;

                _context.Properties.Add(property);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Propiedad '{property.Name}' creada exitosamente (Compra Inicial: {property.PurchasePrice})");
                return property;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear propiedad '{property.Name}'");
                throw;
            }
        }

        public async Task<Property> UpdatePropertyAsync(Property property)
        {
            try
            {
                var existingProperty = await GetPropertyByIdAsync(property.Id);
                if (existingProperty == null)
                {
                    throw new InvalidOperationException($"No existe propiedad con ID {property.Id}");
                }

                // Si cambió el nombre, validar que no exista otra propiedad con ese nombre
                if (existingProperty.Name != property.Name)
                {
                    var duplicateProperty = await GetPropertyByNameAsync(property.Name);
                    if (duplicateProperty != null)
                    {
                        throw new InvalidOperationException($"Ya existe una propiedad llamada '{property.Name}'");
                    }
                }

                var nameChanged = existingProperty.Name != property.Name;
                var previousPurchasePrice = existingProperty.PurchasePrice;

                existingProperty.Name = property.Name;
                existingProperty.PropertyType = property.PropertyType;
                existingProperty.Address = property.Address;
                existingProperty.PurchasePrice = property.PurchasePrice;
                existingProperty.PurchaseDate = property.PurchaseDate;
                existingProperty.CurrentValue = property.CurrentValue;
                existingProperty.Status = property.Status;
                existingProperty.MonthlyRent = property.MonthlyRent;
                existingProperty.UpdatedAt = DateTime.Now;

                // Los totales no se copian del formulario: se calculan (precio de compra + gastos; ingresos).
                // Si cambia el nombre, las operaciones siguen con el nombre anterior (se enlazan por nombre),
                // así que solo se ajusta la diferencia del precio de compra.
                if (nameChanged)
                {
                    existingProperty.TotalInvested += property.PurchasePrice - previousPurchasePrice;
                }

                _context.Properties.Update(existingProperty);
                await _context.SaveChangesAsync();

                if (!nameChanged)
                {
                    await RecalculatePropertyTotalsAsync(existingProperty.Name);
                }

                _logger.LogInformation($"Propiedad '{existingProperty.Name}' actualizada exitosamente");
                return existingProperty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar propiedad con ID {property.Id}");
                throw;
            }
        }

        public async Task<Property> SellPropertyAsync(int propertyId, decimal salePrice, string notes = "", DateTime? saleDate = null)
        {
            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. Obtener la propiedad
                        var property = await GetPropertyByIdAsync(propertyId);
                        if (property == null)
                        {
                            throw new InvalidOperationException($"No existe propiedad con ID {propertyId}");
                        }

                        // 2. Si ya estaba vendida, no permitir vender de nuevo
                        if (property.Status == "Vendida")
                        {
                            throw new InvalidOperationException($"La propiedad '{property.Name}' ya ha sido vendida");
                        }

                        // OPCIÓN C: Crear operación de venta + cambiar Status a Vendida
                        var saleOperation = new PropertyOperation
                        {
                            PropertyName = property.Name,
                            Category = "Ingreso",
                            Type = "Venta",
                            Amount = salePrice,
                            Date = saleDate ?? DateTime.Now,
                            Description = notes != "" ? $"Venta de propiedad. {notes}" : "Venta de propiedad",
                            CreatedAt = DateTime.Now
                        };

                        _context.PropertyOperations.Add(saleOperation);

                        // 3. Calcular beneficio/pérdida
                        decimal totalExpenses = await GetTotalExpensesAsync(property.Name);
                        decimal totalIncome = (await GetTotalIncomeAsync(property.Name)) + salePrice;
                        decimal totalInvested = property.PurchasePrice + Math.Abs(totalExpenses);

                        // 4. Actualizar la propiedad
                        property.Status = "Vendida";
                        property.SaleDate = saleDate ?? DateTime.Now;
                        property.CurrentValue = salePrice;
                        property.TotalIncome = totalIncome;
                        property.TotalInvested = totalInvested;
                        property.UpdatedAt = DateTime.Now;

                        _context.Properties.Update(property);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        _logger.LogInformation($"Propiedad '{property.Name}' vendida exitosamente por {salePrice}");
                        return property;
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
                _logger.LogError(ex, $"Error al vender propiedad con ID {propertyId}");
                throw;
            }
        }

        public async Task<bool> DeletePropertyAsync(int id)
        {
            try
            {
                var property = await GetPropertyByIdAsync(id);
                if (property == null)
                {
                    return false;
                }

                // Eliminar todas las operaciones de la propiedad
                var operations = await GetOperationsByPropertyNameAsync(property.Name);
                _context.PropertyOperations.RemoveRange(operations);

                // Eliminar los documentos adjuntos (SPEC-14-01) en el mismo guardado, sin cargar sus archivos
                var documentIds = await _context.PropertyDocuments
                    .Where(d => d.PropertyId == id)
                    .Select(d => d.Id)
                    .ToListAsync();
                // Si un documento ya está cargado en el contexto (p. ej. se abrió el panel de documentos),
                // se usa esa instancia: adjuntar otra con el mismo Id hacía fallar el borrado
                var documentsToRemove = documentIds
                    .Select(docId => _context.PropertyDocuments.Local.FirstOrDefault(d => d.Id == docId)
                                     ?? new PropertyDocument { Id = docId })
                    .ToList();
                _context.PropertyDocuments.RemoveRange(documentsToRemove);

                // Eliminar la propiedad
                _context.Properties.Remove(property);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Propiedad con ID {id} eliminada exitosamente");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar propiedad con ID {id}");
                throw;
            }
        }

        public async Task<decimal> GetTotalInvestedInActivePropertiesAsync()
        {
            try
            {
                return await _context.Properties
                    .Where(p => p.Status != "Vendida")
                    .SumAsync(p => p.TotalInvested);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular total invertido en propiedades activas");
                throw;
            }
        }

        public async Task<decimal> GetTotalRentalIncomeAsync()
        {
            try
            {
                return await _context.PropertyOperations
                    .Where(po => po.Type == "Alquiler" && po.Category == "Ingreso")
                    .SumAsync(po => po.Amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular ingresos totales por alquiler");
                throw;
            }
        }

        public async Task<decimal> GetPropertyNetResultAsync(string propertyName)
        {
            try
            {
                var property = await GetPropertyByNameAsync(propertyName);
                if (property == null)
                {
                    throw new InvalidOperationException($"No existe propiedad llamada '{propertyName}'");
                }

                // Net Result = TotalIncome - TotalInvested
                return property.TotalIncome - property.TotalInvested;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al calcular resultado neto de '{propertyName}'");
                throw;
            }
        }

        public async Task<List<Property>> GetPropertiesByStatusAsync(string status)
        {
            try
            {
                return await _context.Properties
                    .Where(p => p.Status == status)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener propiedades con status '{status}'");
                throw;
            }
        }

        public async Task<List<Property>> GetRentedPropertiesAsync()
        {
            try
            {
                return await _context.Properties
                    .Where(p => p.Status == "Alquilado")
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener propiedades alquiladas");
                throw;
            }
        }

        public async Task RecalculatePropertyTotalsAsync(string propertyName)
        {
            try
            {
                // Nota: este método se invoca desde dentro de transacciones ya abiertas
                // (p.ej. CreateOperationAsync/UpdateOperationAsync/DeleteOperationAsync).
                // SQLite no soporta transacciones anidadas, así que aquí NO se abre una nueva.
                var property = await GetPropertyByNameAsync(propertyName);
                if (property == null)
                {
                    throw new InvalidOperationException($"No existe propiedad llamada '{propertyName}'");
                }

                var totalIncome = await GetTotalIncomeAsync(propertyName);
                var totalExpenses = await GetTotalExpensesAsync(propertyName);

                decimal totalInvested = property.PurchasePrice + Math.Abs(totalExpenses);

                property.TotalIncome = totalIncome;
                property.TotalInvested = totalInvested;
                property.UpdatedAt = DateTime.Now;

                _context.Properties.Update(property);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Totales de '{propertyName}' recalculados - Ingresos: {totalIncome}, Inversión: {totalInvested}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al recalcular totales de '{propertyName}'");
                throw;
            }
        }
        #endregion

        #region Operaciones Inmobiliarias
        public async Task<List<PropertyOperation>> GetOperationsByPropertyNameAsync(string propertyName)
        {
            try
            {
                return await _context.PropertyOperations
                    .Where(po => po.PropertyName == propertyName)
                    .OrderByDescending(po => po.Date)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener operaciones de propiedad '{propertyName}'");
                throw;
            }
        }

        public async Task<PropertyOperation?> GetOperationByIdAsync(int id)
        {
            try
            {
                return await _context.PropertyOperations
                    .FirstOrDefaultAsync(po => po.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener operación con ID {id}");
                throw;
            }
        }

        public async Task<PropertyOperation> CreateOperationAsync(PropertyOperation operation)
        {
            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. Validar que la propiedad exista
                        var property = await GetPropertyByNameAsync(operation.PropertyName);
                        if (property == null)
                        {
                            throw new InvalidOperationException($"No existe propiedad llamada '{operation.PropertyName}'");
                        }

                        // 2. Crear la operación
                        _context.PropertyOperations.Add(operation);
                        await _context.SaveChangesAsync();

                        // 3. Recalcular los totales de la propiedad
                        await RecalculatePropertyTotalsAsync(operation.PropertyName);

                        await transaction.CommitAsync();

                        _logger.LogInformation($"Operación en propiedad '{operation.PropertyName}' ({operation.Type}) creada exitosamente");
                        return operation;
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
                _logger.LogError(ex, $"Error al crear operación de propiedad");
                throw;
            }
        }

        public async Task<PropertyOperation> UpdateOperationAsync(PropertyOperation operation)
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

                        string oldPropertyName = existingOperation.PropertyName;

                        existingOperation.PropertyName = operation.PropertyName;
                        existingOperation.Category = operation.Category;
                        existingOperation.Type = operation.Type;
                        existingOperation.Amount = operation.Amount;
                        existingOperation.Date = operation.Date;
                        existingOperation.Description = operation.Description;

                        _context.PropertyOperations.Update(existingOperation);
                        await _context.SaveChangesAsync();

                        // Recalcular totales de la propiedad anterior
                        await RecalculatePropertyTotalsAsync(oldPropertyName);

                        // Recalcular totales de la nueva propiedad (si cambió)
                        if (oldPropertyName != operation.PropertyName)
                        {
                            await RecalculatePropertyTotalsAsync(operation.PropertyName);
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

                        string propertyName = operation.PropertyName;
                        _context.PropertyOperations.Remove(operation);
                        await _context.SaveChangesAsync();

                        // Los documentos que apuntaban a este gasto quedan sin gasto enlazado (SPEC-14-01)
                        await _context.PropertyDocuments
                            .Where(d => d.PropertyOperationId == id)
                            .ExecuteUpdateAsync(s => s.SetProperty(d => d.PropertyOperationId, (int?)null));

                        // Recalcular los totales de la propiedad
                        await RecalculatePropertyTotalsAsync(propertyName);

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

        public async Task<decimal> GetTotalIncomeAsync(string propertyName)
        {
            try
            {
                return await _context.PropertyOperations
                    .Where(po => po.PropertyName == propertyName && po.Category == "Ingreso")
                    .SumAsync(po => po.Amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al calcular ingresos de '{propertyName}'");
                throw;
            }
        }

        public async Task<decimal> GetTotalExpensesAsync(string propertyName)
        {
            try
            {
                // SQLite no traduce Math.Abs dentro de Sum: se leen los importes y se suman en memoria
                var amounts = await _context.PropertyOperations
                    .Where(po => po.PropertyName == propertyName && po.Category == "Gasto")
                    .Select(po => po.Amount)
                    .ToListAsync();

                return amounts.Sum(amount => Math.Abs(amount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al calcular gastos de '{propertyName}'");
                throw;
            }
        }

        public async Task<List<PropertyOperation>> GetOperationsByCategoryAsync(string propertyName, string category)
        {
            try
            {
                return await _context.PropertyOperations
                    .Where(po => po.PropertyName == propertyName && po.Category == category)
                    .OrderByDescending(po => po.Date)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener operaciones de '{propertyName}' categoría '{category}'");
                throw;
            }
        }

        public async Task<List<PropertyOperation>> GetOperationsByTypeAsync(string propertyName, string type)
        {
            try
            {
                return await _context.PropertyOperations
                    .Where(po => po.PropertyName == propertyName && po.Type == type)
                    .OrderByDescending(po => po.Date)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener operaciones de '{propertyName}' tipo '{type}'");
                throw;
            }
        }

        public async Task<List<PropertyOperation>> GetOperationsByDateRangeAsync(string propertyName, DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.PropertyOperations
                    .Where(po => po.PropertyName == propertyName && 
                           po.Date >= startDate && 
                           po.Date <= endDate)
                    .OrderByDescending(po => po.Date)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener operaciones de '{propertyName}' en rango de fechas");
                throw;
            }
        }
        #endregion
    }
}
