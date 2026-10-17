namespace FINANCEAPP.Services
{
    /// <summary>
    /// Estado del mercado de un valor según la sesión regular que informa Yahoo Finance (SPEC-05-01)
    /// </summary>
    public enum MarketState
    {
        /// <summary>La hora actual está dentro de la sesión regular</summary>
        Open,

        /// <summary>La hora actual está fuera de la sesión regular</summary>
        Closed,

        /// <summary>Yahoo no informó la sesión regular</summary>
        Unknown
    }

    /// <summary>
    /// Cotización en vivo de un valor (último precio y datos del día). No se guarda en la BD.
    /// </summary>
    public class StockQuote
    {
        /// <summary>Ticker resuelto en Yahoo (p. ej. ACS.MC aunque la empresa tenga ACS)</summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>Último precio (regularMarketPrice)</summary>
        public decimal Price { get; set; }

        /// <summary>Cierre de la sesión anterior; null si Yahoo no lo da</summary>
        public decimal? PreviousClose { get; set; }

        /// <summary>Máximo del día; null si Yahoo no lo da</summary>
        public decimal? DayHigh { get; set; }

        /// <summary>Mínimo del día; null si Yahoo no lo da</summary>
        public decimal? DayLow { get; set; }

        /// <summary>Hora local del precio (regularMarketTime); null si Yahoo no la da</summary>
        public DateTime? PriceTime { get; set; }

        /// <summary>Divisa en la que cotiza según Yahoo (informativa)</summary>
        public string? Currency { get; set; }

        /// <summary>Inicio de la sesión regular en UTC; null si Yahoo no la da</summary>
        public DateTime? RegularSessionStartUtc { get; set; }

        /// <summary>Fin de la sesión regular en UTC; null si Yahoo no la da</summary>
        public DateTime? RegularSessionEndUtc { get; set; }

        /// <summary>Variación absoluta frente al cierre anterior; null si no hay cierre anterior</summary>
        public decimal? Change => PreviousClose.HasValue ? Price - PreviousClose.Value : null;

        /// <summary>Variación porcentual frente al cierre anterior; null si no hay cierre anterior válido</summary>
        public decimal? ChangePercent =>
            PreviousClose.HasValue && PreviousClose.Value != 0
                ? (Price - PreviousClose.Value) / PreviousClose.Value * 100m
                : null;

        /// <summary>
        /// Estado del mercado en el instante indicado (UTC): Abierto si está dentro de la sesión regular,
        /// Cerrado si está fuera y Desconocido si Yahoo no informó la sesión.
        /// </summary>
        public MarketState GetMarketState(DateTime utcNow)
        {
            if (!RegularSessionStartUtc.HasValue || !RegularSessionEndUtc.HasValue)
            {
                return MarketState.Unknown;
            }

            return utcNow >= RegularSessionStartUtc.Value && utcNow < RegularSessionEndUtc.Value
                ? MarketState.Open
                : MarketState.Closed;
        }
    }
}
