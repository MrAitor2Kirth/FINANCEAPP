using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Interfaz para la gestión de fondos de inversión
    /// </summary>
    public interface IFundService
    {
        #region Fondos
        /// <summary>
        /// Obtiene todos los fondos
        /// </summary>
        Task<List<Fund>> GetAllFundsAsync();

        /// <summary>
        /// Obtiene un fondo por ID
        /// </summary>
        Task<Fund?> GetFundByIdAsync(int id);

        /// <summary>
        /// Obtiene un fondo por nombre
        /// </summary>
        Task<Fund?> GetFundByNameAsync(string name);

        /// <summary>
        /// Crea un nuevo fondo
        /// </summary>
        Task<Fund> CreateFundAsync(Fund fund);

        /// <summary>
        /// Actualiza un fondo existente
        /// </summary>
        Task<Fund> UpdateFundAsync(Fund fund);

        /// <summary>
        /// Elimina un fondo
        /// </summary>
        Task<bool> DeleteFundAsync(int id);

        /// <summary>
        /// Obtiene el total invertido en todos los fondos
        /// </summary>
        Task<decimal> GetTotalInvestedAsync();

        /// <summary>
        /// Obtiene el valor total actual de todos los fondos
        /// </summary>
        Task<decimal> GetTotalCurrentValueAsync();

        /// <summary>
        /// Obtiene la rentabilidad agregada de todos los fondos
        /// </summary>
        Task<decimal> GetAggregateReturnAsync();
        #endregion

        #region Operaciones
        /// <summary>
        /// Obtiene todas las operaciones de un fondo
        /// </summary>
        Task<List<FundOperation>> GetOperationsByFundNameAsync(string fundName);

        /// <summary>
        /// Obtiene una operación por ID
        /// </summary>
        Task<FundOperation?> GetOperationByIdAsync(int id);

        /// <summary>
        /// Crea una nueva operación de fondo
        /// </summary>
        Task<FundOperation> CreateOperationAsync(FundOperation operation);

        /// <summary>
        /// Actualiza una operación existente
        /// </summary>
        Task<FundOperation> UpdateOperationAsync(FundOperation operation);

        /// <summary>
        /// Elimina una operación de fondo
        /// </summary>
        Task<bool> DeleteOperationAsync(int id);

        /// <summary>
        /// Obtiene el total de aportaciones de un fondo (solo operaciones tipo Aportación)
        /// </summary>
        Task<decimal> GetTotalContributionsAsync(string fundName);

        /// <summary>
        /// Obtiene el total de reembolsos de un fondo (solo operaciones tipo Reembolso)
        /// </summary>
        Task<decimal> GetTotalRedemptionsAsync(string fundName);

        /// <summary>
        /// Obtiene la última operación de plusvalía de un fondo
        /// </summary>
        Task<FundOperation?> GetLatestPlusvaliOperationAsync(string fundName);
        #endregion

        #region Plusvalías - Opción A: Operación de Plusvalía
        /// <summary>
        /// OPCIÓN A - Actualiza el valor del fondo creando/actualizando operación de plusvalía
        /// Crea una nueva operación de plusvalía eliminando la anterior si existe
        /// </summary>
        Task UpdateFundValueWithPlusvaliaOperationAsync(string fundName, decimal newValue);
        #endregion

        #region Plusvalías - Opción B: Historial de Valores
        /// <summary>
        /// OPCIÓN B - Actualiza el valor del fondo registrando en historial
        /// Solo actualiza Fund.CurrentValue y mantiene historial en tabla separada
        /// </summary>
        Task UpdateFundValueWithHistoryAsync(string fundName, decimal newValue);

        /// <summary>
        /// OPCIÓN B - Obtiene el historial de valores de un fondo
        /// </summary>
        Task<List<FundValueHistory>> GetFundValueHistoryAsync(string fundName);
        #endregion
    }
}
