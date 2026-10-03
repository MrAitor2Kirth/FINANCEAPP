namespace FINANCEAPP.Services
{
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
    }
}
