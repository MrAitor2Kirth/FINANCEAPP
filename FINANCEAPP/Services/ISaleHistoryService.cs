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
        /// Devuelve los registros cuya fecha de venta cae en el periodo (SPEC-02-05), de la venta más reciente
        /// a la más antigua. Con Total histórico equivale a <see cref="GetAllAsync"/>.
        /// </summary>
        Task<List<SaleHistory>> GetByPeriodAsync(SalesPeriodFilter filter);

        /// <summary>
        /// Indica si hay alguna venta en el histórico, sea cual sea el periodo (SPEC-02-05).
        /// </summary>
        Task<bool> HasAnyAsync();

        /// <summary>
        /// Devuelve un registro por su Id, o null si no existe.
        /// </summary>
        Task<SaleHistory?> GetByIdAsync(int id);

        /// <summary>
        /// Cambia el precio de venta y recalcula importe y beneficio, y el importe y la descripción de su
        /// ingreso bancario si lo tiene (SPEC-02-04), en una transacción. Devuelve false si el registro no existe
        /// o el precio no es mayor que 0; lanza InvalidOperationException si la liquidez neta no es positiva.
        /// </summary>
        Task<bool> UpdateSellPriceAsync(int id, decimal sellPrice);

        /// <summary>
        /// Elimina un registro del histórico sin más. No reabre las compras cerradas por esa venta:
        /// para deshacer una venta usa <see cref="IOperationService.UndoSaleAsync"/> (la página ya no llama a este método).
        /// </summary>
        Task<bool> DeleteAsync(int id);
    }
}
