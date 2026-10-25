namespace FINANCEAPP.Services
{
    /// <summary>Dividendo pagado: fecha (sin hora) e importe por acción en la divisa de cotización</summary>
    public record StockDividend(DateTime Date, decimal Amount);

    /// <summary>Resultado de la consulta del último dividendo (SPEC-21-01, RF-12)</summary>
    public class StockDividendLookup
    {
        /// <summary>Último dividendo, o null si Yahoo no tiene dividendos de la empresa</summary>
        public StockDividend? Last { get; set; }

        /// <summary>true si la consulta falló por red o servidor (distinto de «sin dividendos»)</summary>
        public bool Failed { get; set; }
    }

    /// <summary>
    /// Servicio para obtener precios en vivo por ticker desde una fuente de mercado.
    /// </summary>
    public interface IStockPriceService
    {
        Task<decimal?> GetLatestPriceAsync(string ticker);
        Task<Dictionary<string, decimal?>> GetLatestPricesAsync(IEnumerable<string> tickers);

        /// <summary>
        /// Obtiene el histórico diario de cotización (OHLC) de un ticker para el rango indicado.
        /// </summary>
        /// <param name="ticker">Símbolo ticker de la empresa</param>
        /// <param name="range">Rango soportado por Yahoo Finance, p.ej. "5y", "1y", "max"</param>
        /// <returns>Lista de barras diarias ordenadas por fecha ascendente, o lista vacía si no se pudo obtener</returns>
        Task<List<StockHistoryBar>> GetDailyHistoryAsync(string ticker, string range = "5y");

        /// <summary>
        /// Obtiene el histórico de un ticker entre dos fechas (period1/period2 de Yahoo) en barras diarias o
        /// de una hora, probando los sufijos de bolsa si el símbolo no existe (SPEC-05-02).
        /// </summary>
        /// <param name="ticker">Ticker de la empresa o símbolo ya resuelto (p. ej. ACS.MC)</param>
        /// <param name="interval">"1d" para barras diarias (fecha sin hora) o "1h" para barras de una hora (hora local)</param>
        /// <param name="fromUtc">Inicio del tramo (UTC)</param>
        /// <param name="toUtc">Fin del tramo (UTC)</param>
        /// <param name="cancellationToken">Cancela la descarga (lanza OperationCanceledException)</param>
        /// <returns>El tramo descargado; <see cref="StockHistoryChunk.Failed"/> indica error de red o del servidor</returns>
        Task<StockHistoryChunk> GetHistoryRangeAsync(string ticker, string interval, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

        /// <summary>
        /// Último dividendo pagado según Yahoo (v8/finance/chart con events=div, 5 años), probando los sufijos de bolsa
        /// si el símbolo no existe (SPEC-21-01, RF-12). Yahoo no da dividendos anunciados: solo los ya pagados.
        /// </summary>
        Task<StockDividendLookup> GetLastDividendAsync(string ticker);

        /// <summary>
        /// Obtiene la cotización en vivo de un ticker (último precio, cierre anterior, máximo y mínimo del día,
        /// hora del precio y sesión regular) probando los sufijos de bolsa si hace falta (SPEC-05-01).
        /// </summary>
        /// <param name="ticker">Ticker de la empresa o símbolo ya resuelto (p. ej. ACS.MC)</param>
        /// <returns>La cotización con el símbolo resuelto, o null si no se obtuvo precio</returns>
        Task<StockQuote?> GetQuoteAsync(string ticker);
    }
}
