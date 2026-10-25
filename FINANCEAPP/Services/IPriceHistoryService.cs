using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Servicio para obtener el histórico de cotización (OHLC) de una empresa,
    /// usando SQLite como caché y Yahoo Finance como origen de datos cuando la caché
    /// está vacía o desactualizada. La agregación semanal, mensual y anual se calcula en el servidor.
    /// El histórico diario se completa hacia atrás por bloques (SPEC-05-02); las barras de una hora
    /// se guardan solo en memoria.
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
        /// Con la caché vacía descarga los últimos 2 años; con la caché de más de 12 horas, solo los días nuevos;
        /// con <paramref name="forceRefresh"/>, de nuevo los últimos 2 años (SPEC-05-02, RF-1, RF-3 y RF-13).
        /// </summary>
        /// <param name="forceRefresh">true para ignorar la caché de 12 horas</param>
        Task<PriceHistoryResult> GetHistoryWithStatusAsync(int companyId, string ticker, ChartGranularity granularity, bool forceRefresh);

        /// <summary>
        /// Devuelve el histórico diario guardado en la granularidad pedida, sin consultar a Yahoo (SPEC-05-02).
        /// </summary>
        Task<PriceHistoryResult> GetCachedHistoryAsync(int companyId, ChartGranularity granularity);

        /// <summary>
        /// Indica si el histórico diario de la empresa ya está completo y cuál es su punto más antiguo (SPEC-05-02, RF-2).
        /// </summary>
        Task<HistoryBackfillState> GetBackfillStateAsync(int companyId);

        /// <summary>
        /// Descarga (sin tocar la base de datos) el bloque de 10 años anterior a <paramref name="beforeDate"/> (SPEC-05-02, RF-2).
        /// </summary>
        /// <param name="ticker">Ticker o símbolo ya resuelto</param>
        /// <param name="beforeDate">Fecha a partir de la cual se descarga hacia atrás (el bloque no la incluye)</param>
        /// <param name="cancellationToken">Cancela la descarga al cerrar el modal</param>
        Task<HistoryBlockResult> FetchOlderDailyBlockAsync(string ticker, DateTime beforeDate, CancellationToken cancellationToken);

        /// <summary>
        /// Guarda en la caché un bloque descargado con <see cref="FetchOlderDailyBlockAsync"/> y, si llegó a la
        /// primera cotización, marca el histórico de la empresa como completo (SPEC-05-02, RF-2).
        /// </summary>
        Task SaveDailyBlockAsync(int companyId, HistoryBlockResult block);

        /// <summary>
        /// Devuelve las barras de una hora del último mes (o las que haya en memoria si siguen vigentes, 15 minutos).
        /// No se guardan en la base de datos (SPEC-05-02, RF-9).
        /// </summary>
        Task<PriceHistoryResult> GetHourlyAsync(int companyId, string ticker, CancellationToken cancellationToken);

        /// <summary>
        /// Descarga el bloque de 6 meses anterior a las barras de una hora que hay en memoria y lo añade,
        /// hasta el límite de Yahoo (unos 730 días) (SPEC-05-02, RF-9).
        /// </summary>
        Task<HistoryBlockResult> FetchOlderHourlyBlockAsync(int companyId, string ticker, CancellationToken cancellationToken);

        /// <summary>
        /// Devuelve las barras de una hora que hay en memoria, sin descargar nada (vacío si no hay).
        /// </summary>
        PriceHistoryResult GetCachedHourly(int companyId);

        /// <summary>
        /// Vacía la memoria de barras de una hora de la empresa («Recargar histórico» en Horas).
        /// </summary>
        void ClearHourly(int companyId);

        /// <summary>
        /// Coloca cada marcador de operación en su periodo de la temporalidad: en Horas, la primera barra de su día
        /// (SPEC-05-02, RF-11); en Semanal, Mensual y Anual, el inicio del periodo (SPEC-05-03). Descarta los que no
        /// se pueden dibujar.
        /// </summary>
        List<ChartMarker> AlignMarkers(IEnumerable<ChartMarker> markers, IReadOnlyList<PriceHistoryPoint> points, ChartGranularity granularity);

        /// <summary>
        /// Coloca cada marca del gráfico en su periodo con las mismas reglas que <see cref="AlignMarkers"/> (SPEC-05-03, RF-10).
        /// </summary>
        List<ChartMarkPoint> AlignMarks(IEnumerable<ChartMark> marks, IReadOnlyList<PriceHistoryPoint> points, ChartGranularity granularity);

        /// <summary>
        /// Sesiones futuras vacías que prolongan el eje (SPEC-21-01, RF-10): desde el periodo siguiente al último punto hasta
        /// lo más lejano entre hoy + 1 año y el pago pendiente más lejano. Diario: lunes a viernes; Semanal: lunes;
        /// Mensual: día 1; Anual: 1 de enero; Horas: ninguna.
        /// </summary>
        List<DateTime> BuildFutureSessions(IReadOnlyList<PriceHistoryPoint> points, ChartGranularity granularity, IEnumerable<DateTime> pendingPaymentDates);

        /// <summary>
        /// Coloca las líneas de dividendo como marcas «Pago de dividendo» (SPEC-21-01, RF-9): las pasadas con las reglas de
        /// <see cref="AlignMarks"/> y las futuras en su periodo del tramo futuro (no en Horas).
        /// </summary>
        List<DividendMarkPoint> AlignDividendMarks(IEnumerable<DividendChartMark> marks, IReadOnlyList<PriceHistoryPoint> points, ChartGranularity granularity);

        // ---- Por serie (empresa o índice, SPEC-05-04): mismas reglas que los métodos de empresa

        /// <summary>
        /// Igual que <see cref="GetHistoryWithStatusAsync"/> para una empresa o un índice.
        /// </summary>
        Task<PriceHistoryResult> GetSeriesHistoryWithStatusAsync(ChartSeriesRef series, ChartGranularity granularity, bool forceRefresh);

        /// <summary>
        /// Igual que <see cref="GetCachedHistoryAsync"/> para una empresa o un índice.
        /// </summary>
        Task<PriceHistoryResult> GetCachedSeriesHistoryAsync(ChartSeriesRef series, ChartGranularity granularity);

        /// <summary>
        /// Igual que <see cref="GetBackfillStateAsync"/> para una empresa o un índice.
        /// </summary>
        Task<HistoryBackfillState> GetSeriesBackfillStateAsync(ChartSeriesRef series);

        /// <summary>
        /// Igual que <see cref="SaveDailyBlockAsync"/> para una empresa o un índice.
        /// </summary>
        Task SaveSeriesDailyBlockAsync(ChartSeriesRef series, HistoryBlockResult block);

        /// <summary>
        /// Igual que <see cref="GetHourlyAsync"/> para una empresa o un índice (barras solo en memoria).
        /// </summary>
        Task<PriceHistoryResult> GetSeriesHourlyAsync(ChartSeriesRef series, CancellationToken cancellationToken);

        /// <summary>
        /// Igual que <see cref="FetchOlderHourlyBlockAsync"/> para una empresa o un índice.
        /// </summary>
        Task<HistoryBlockResult> FetchOlderSeriesHourlyBlockAsync(ChartSeriesRef series, CancellationToken cancellationToken);

        /// <summary>
        /// Igual que <see cref="GetCachedHourly"/> para una empresa o un índice.
        /// </summary>
        PriceHistoryResult GetCachedSeriesHourly(ChartSeriesRef series);
    }

    /// <summary>
    /// Tipo de serie del gráfico (SPEC-05-04): su histórico diario se guarda en PriceHistoryPoints o en BenchmarkPricePoints
    /// </summary>
    public enum ChartSeriesKind
    {
        Company,
        Benchmark
    }

    /// <summary>
    /// Serie de cotización que se puede dibujar o comparar: una empresa o un índice (SPEC-05-04)
    /// </summary>
    /// <param name="Kind">Empresa o índice</param>
    /// <param name="Id">CompanyDetail.Id o BenchmarkIndex.Id</param>
    /// <param name="Symbol">Ticker o símbolo de Yahoo</param>
    /// <param name="Name">Nombre que se muestra en la leyenda</param>
    public record ChartSeriesRef(ChartSeriesKind Kind, int Id, string Symbol, string Name)
    {
        /// <summary>Serie de una empresa</summary>
        public static ChartSeriesRef ForCompany(int companyId, string ticker, string name = "")
            => new(ChartSeriesKind.Company, companyId, ticker, name);

        /// <summary>Serie de un índice</summary>
        public static ChartSeriesRef ForBenchmark(int benchmarkIndexId, string symbol, string name = "")
            => new(ChartSeriesKind.Benchmark, benchmarkIndexId, symbol, name);

        /// <summary>Clave de la serie (memoria de Horas, cola de relleno)</summary>
        public string Key => (Kind == ChartSeriesKind.Benchmark ? "I:" : "C:") + Id;
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

        /// <summary>Inicio del tramo visible al abrir la temporalidad; null = todo (SPEC-05-02, RF-8)</summary>
        public DateTime? ViewStart { get; set; }

        /// <summary>Fin del tramo visible al abrir (fecha del último punto)</summary>
        public DateTime? ViewEnd { get; set; }

        /// <summary>true si el histórico ya está completo (no queda nada anterior por descargar)</summary>
        public bool IsComplete { get; set; }

        /// <summary>Fecha del punto más antiguo disponible</summary>
        public DateTime? OldestDate { get; set; }
    }

    /// <summary>
    /// Estado del histórico diario guardado de una empresa (SPEC-05-02)
    /// </summary>
    public class HistoryBackfillState
    {
        /// <summary>true si ya se descargó hasta la primera cotización</summary>
        public bool IsComplete { get; set; }

        /// <summary>Fecha del punto diario más antiguo guardado; null si no hay caché</summary>
        public DateTime? OldestDate { get; set; }
    }

    /// <summary>
    /// Bloque de histórico antiguo descargado en segundo plano (SPEC-05-02)
    /// </summary>
    public class HistoryBlockResult
    {
        /// <summary>Barras del bloque anteriores a la fecha pedida</summary>
        public List<StockHistoryBar> Bars { get; set; } = new();

        /// <summary>Inicio del tramo pedido: el siguiente bloque se pide antes de esta fecha</summary>
        public DateTime BlockStart { get; set; }

        /// <summary>Fecha de la barra más antigua del bloque; null si vino vacío</summary>
        public DateTime? OldestDate { get; set; }

        /// <summary>true si el bloque llegó a la primera cotización o al límite de Yahoo: no hay que pedir más</summary>
        public bool ReachedStart { get; set; }

        /// <summary>true si la descarga falló (red o servidor); se reintenta en la siguiente apertura</summary>
        public bool Failed { get; set; }
    }
}
