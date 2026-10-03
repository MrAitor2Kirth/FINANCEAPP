using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Servicio para obtener el histórico de cotización (OHLC) de una empresa,
    /// usando SQLite como caché y Yahoo Finance como origen de datos cuando la caché
    /// está vacía o desactualizada. La agregación mensual/anual se calcula en el servidor.
    /// </summary>
    public interface IPriceHistoryService
    {
        /// <summary>
        /// Obtiene la serie de precios de la empresa en la granularidad solicitada,
        /// refrescando la caché local desde Yahoo Finance si está desactualizada.
        /// </summary>
        Task<List<PriceHistoryPoint>> GetHistoryAsync(int companyId, string ticker, ChartGranularity granularity);
    }
}
