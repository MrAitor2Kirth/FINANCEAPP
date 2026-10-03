using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Interfaz para la gestión de activos refugio (oro, plata, bitcoin, etc.)
    /// </summary>
    public interface ISafeHavenService
    {
        #region Activos Refugio
        /// <summary>
        /// Obtiene todos los activos refugio
        /// </summary>
        Task<List<SafeHavenAsset>> GetAllAssetsAsync();

        /// <summary>
        /// Obtiene un activo por ID
        /// </summary>
        Task<SafeHavenAsset?> GetAssetByIdAsync(int id);

        /// <summary>
        /// Obtiene un activo por nombre
        /// </summary>
        Task<SafeHavenAsset?> GetAssetByNameAsync(string name);

        /// <summary>
        /// Crea un nuevo activo refugio
        /// </summary>
        Task<SafeHavenAsset> CreateAssetAsync(SafeHavenAsset asset);

        /// <summary>
        /// Actualiza un activo existente
        /// </summary>
        Task<SafeHavenAsset> UpdateAssetAsync(SafeHavenAsset asset);

        /// <summary>
        /// Elimina un activo refugio y todas sus operaciones
        /// </summary>
        Task<bool> DeleteAssetAsync(int id);

        /// <summary>
        /// Obtiene el total invertido en todos los activos refugio
        /// </summary>
        Task<decimal> GetTotalInvestedAsync();

        /// <summary>
        /// Obtiene el valor actual total de todos los activos refugio
        /// </summary>
        Task<decimal> GetTotalCurrentValueAsync();

        /// <summary>
        /// Obtiene el desglose de inversión por tipo de activo
        /// </summary>
        Task<Dictionary<string, decimal>> GetInvestmentByAssetTypeAsync();




        /// <summary>
        /// Recalcula el total invertido de un activo desde sus operaciones
        /// </summary>
        Task<decimal> RecalculateAssetTotalAsync(string assetName);
        #endregion

        #region Operaciones de Activos Refugio
        /// <summary>
        /// Obtiene todas las operaciones de un activo
        /// </summary>
        Task<List<SafeHavenOperation>> GetOperationsByAssetNameAsync(string assetName);

        /// <summary>
        /// Obtiene una operación por ID
        /// </summary>
        Task<SafeHavenOperation?> GetOperationByIdAsync(int id);

        /// <summary>
        /// Crea una nueva operación de activo refugio
        /// </summary>
        Task<SafeHavenOperation> CreateOperationAsync(SafeHavenOperation operation);

        /// <summary>
        /// Actualiza una operación existente
        /// </summary>
        Task<SafeHavenOperation> UpdateOperationAsync(SafeHavenOperation operation);

        /// <summary>
        /// Elimina una operación y recalcula totales
        /// </summary>
        Task<bool> DeleteOperationAsync(int id);

        /// <summary>
        /// Obtiene el total de compras de un activo
        /// </summary>
        Task<decimal> GetTotalPurchasesAsync(string assetName);

        /// <summary>
        /// Obtiene el total de ventas de un activo
        /// </summary>
        Task<decimal> GetTotalSalesAsync(string assetName);

        /// <summary>
        /// Obtiene operaciones filtradas por tipo
        /// </summary>
        Task<List<SafeHavenOperation>> GetOperationsByTypeAsync(string assetName, string type);
        #endregion
    }
}
