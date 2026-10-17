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

        /// <summary>
        /// Igual que <see cref="GetHistoryAsync"/>, pero permite forzar la descarga aunque la caché sea reciente
        /// e indica si se intentó descargar y Yahoo no devolvió datos (SPEC-05-01, RF-22).
        /// </summary>
        /// <param name="forceRefresh">true para ignorar la caché de 12 horas</param>
        Task<PriceHistoryResult> GetHistoryWithStatusAsync(int companyId, string ticker, ChartGranularity granularity, bool forceRefresh);
    }

    /// <summary>
    /// Serie de cotización y estado de la última descarga
    /// </summary>
    public class PriceHistoryResult
    {
        /// <summary>Puntos en la granularidad pedida</summary>
        public List<PriceHistoryPoint> Points { get; set; } = new();

        /// <summary>true si se intentó descargar de Yahoo y no se obtuvo nada (se sirve la caché)</summary>
        public bool RefreshFailed { get; set; }
    }
}
