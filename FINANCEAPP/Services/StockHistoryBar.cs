namespace FINANCEAPP.Services
{
    /// <summary>
    /// Representa un punto diario de cotización (OHLC) devuelto por el proveedor de mercado,
    /// antes de ser persistido como <see cref="FINANCEAPP.Models.PriceHistoryPoint"/>.
    /// </summary>
    public record StockHistoryBar(DateTime Date, decimal Open, decimal High, decimal Low, decimal Close, long Volume);
}
