using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Interfaz para la gestión de propiedades inmobiliarias e inversiones
    /// </summary>
    public interface IPropertyService
    {
        #region Propiedades
        /// <summary>
        /// Obtiene todas las propiedades
        /// </summary>
        Task<List<Property>> GetAllPropertiesAsync();

        /// <summary>
        /// Obtiene propiedades activas (no vendidas)
        /// </summary>
        Task<List<Property>> GetActivePropertiesAsync();

        /// <summary>
        /// Obtiene propiedades vendidas
        /// </summary>
        Task<List<Property>> GetSoldPropertiesAsync();

        /// <summary>
        /// Obtiene una propiedad por ID
        /// </summary>
        Task<Property?> GetPropertyByIdAsync(int id);

        /// <summary>
        /// Obtiene una propiedad por nombre
        /// </summary>
        Task<Property?> GetPropertyByNameAsync(string name);

        /// <summary>
        /// Crea una nueva propiedad
        /// Nota: La compra inicial se registra solo en Property.PurchasePrice (Opción B del diseño)
        /// </summary>
        Task<Property> CreatePropertyAsync(Property property);

        /// <summary>
        /// Actualiza una propiedad existente
        /// </summary>
        Task<Property> UpdatePropertyAsync(Property property);

        /// <summary>
        /// Vende una propiedad (Opción C: crear operación + cambiar Status a Vendida)
        /// </summary>
        Task<Property> SellPropertyAsync(int propertyId, decimal salePrice, string notes = "");

        /// <summary>
        /// Elimina una propiedad y todas sus operaciones
        /// </summary>
        Task<bool> DeletePropertyAsync(int id);

        /// <summary>
        /// Obtiene el total invertido en propiedades activas
        /// </summary>
        Task<decimal> GetTotalInvestedInActivePropertiesAsync();

        /// <summary>
        /// Obtiene el total generado en ingresos por alquiler
        /// </summary>
        Task<decimal> GetTotalRentalIncomeAsync();

        /// <summary>
        /// Obtiene el resultado neto (ingresos - gastos) de una propiedad
        /// </summary>
        Task<decimal> GetPropertyNetResultAsync(string propertyName);

        /// <summary>
        /// Obtiene propiedades filtradas por status
        /// </summary>
        Task<List<Property>> GetPropertiesByStatusAsync(string status);

        /// <summary>
        /// Obtiene propiedades con ingresos por alquiler
        /// </summary>
        Task<List<Property>> GetRentedPropertiesAsync();

        /// <summary>
        /// Recalcula los totales de inversión e ingresos de una propiedad
        /// </summary>
        Task RecalculatePropertyTotalsAsync(string propertyName);
        #endregion

        #region Operaciones Inmobiliarias
        /// <summary>
        /// Obtiene todas las operaciones de una propiedad
        /// </summary>
        Task<List<PropertyOperation>> GetOperationsByPropertyNameAsync(string propertyName);

        /// <summary>
        /// Obtiene una operación por ID
        /// </summary>
        Task<PropertyOperation?> GetOperationByIdAsync(int id);

        /// <summary>
        /// Crea una nueva operación en una propiedad (gasto o ingreso)
        /// </summary>
        Task<PropertyOperation> CreateOperationAsync(PropertyOperation operation);

        /// <summary>
        /// Actualiza una operación existente
        /// </summary>
        Task<PropertyOperation> UpdateOperationAsync(PropertyOperation operation);

        /// <summary>
        /// Elimina una operación y recalcula totales
        /// </summary>
        Task<bool> DeleteOperationAsync(int id);

        /// <summary>
        /// Obtiene el total de ingresos de una propiedad
        /// </summary>
        Task<decimal> GetTotalIncomeAsync(string propertyName);

        /// <summary>
        /// Obtiene el total de gastos de una propiedad
        /// </summary>
        Task<decimal> GetTotalExpensesAsync(string propertyName);

        /// <summary>
        /// Obtiene operaciones filtradas por categoría (Ingreso/Gasto)
        /// </summary>
        Task<List<PropertyOperation>> GetOperationsByCategoryAsync(string propertyName, string category);

        /// <summary>
        /// Obtiene operaciones filtradas por tipo (Alquiler, IBI, Reforma, etc.)
        /// </summary>
        Task<List<PropertyOperation>> GetOperationsByTypeAsync(string propertyName, string type);

        /// <summary>
        /// Obtiene operaciones en un rango de fechas
        /// </summary>
        Task<List<PropertyOperation>> GetOperationsByDateRangeAsync(string propertyName, DateTime startDate, DateTime endDate);
        #endregion
    }
}
