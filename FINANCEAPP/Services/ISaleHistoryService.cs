using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Servicio de consulta y mantenimiento del histórico de ventas de acciones.
    /// Los registros se crean desde <see cref="IOperationService"/> al registrar una Venta.
    /// </summary>
    public interface ISaleHistoryService
    {
        /// <summary>
        /// Devuelve todos los registros, de la venta más reciente a la más antigua.
        /// </summary>
        Task<List<SaleHistory>> GetAllAsync();

        /// <summary>
        /// Devuelve un registro por su Id, o null si no existe.
        /// </summary>
        Task<SaleHistory?> GetByIdAsync(int id);

        /// <summary>
        /// Cambia el precio de venta y recalcula importe y beneficio.
        /// Devuelve false si el registro no existe o el precio no es mayor que 0.
        /// </summary>
        Task<bool> UpdateSellPriceAsync(int id, decimal sellPrice);

        /// <summary>
        /// Elimina un registro del histórico. No reabre las compras cerradas por esa venta.
        /// </summary>
        Task<bool> DeleteAsync(int id);
    }
}
