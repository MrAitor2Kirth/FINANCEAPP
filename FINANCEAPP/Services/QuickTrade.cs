using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Datos de una compra o venta registrada desde el gráfico de cotización (SPEC-05-01)
    /// </summary>
    public class QuickTradeRequest
    {
        /// <summary>Id de la empresa</summary>
        public int CompanyId { get; set; }

        /// <summary>Tipo de operación: "Compra" o "Venta" (literal exacto)</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Fecha de la operación (no futura)</summary>
        public DateTime Date { get; set; }

        /// <summary>Número de acciones (entero mayor o igual que 1)</summary>
        public int NumberOfShares { get; set; }

        /// <summary>Precio por acción (mayor que 0)</summary>
        public decimal PricePerShare { get; set; }

        /// <summary>Comisión (mayor o igual que 0)</summary>
        public decimal Commission { get; set; }

        /// <summary>Tipo de cambio a la divisa base; null si la empresa es de la divisa base o no se informa</summary>
        public decimal? ExchangeRate { get; set; }

        /// <summary>Estado del mercado en el momento de confirmar</summary>
        public MarketState MarketState { get; set; } = MarketState.Unknown;
    }

    /// <summary>
    /// Resultado de <see cref="IOperationService.RegisterQuickTradeAsync"/>
    /// </summary>
    public class QuickTradeResult
    {
        /// <summary>Indica si la operación se registró</summary>
        public bool Success { get; set; }

        /// <summary>Mensaje de error en español cuando Success es false</summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>Operación creada (null si hubo error)</summary>
        public Operation? Operation { get; set; }

        public static QuickTradeResult Fail(string message) =>
            new() { Success = false, ErrorMessage = message };
    }

    /// <summary>
    /// Tipo de marcador del gráfico de cotización
    /// </summary>
    public enum ChartMarkerKind
    {
        Buy,
        Transfer,
        Sell,
        Dividend
    }

    /// <summary>
    /// Marcador de una operación sobre el gráfico de cotización
    /// </summary>
    public class ChartMarker
    {
        /// <summary>Tipo de marcador</summary>
        public ChartMarkerKind Kind { get; set; }

        /// <summary>Fecha de la operación</summary>
        public DateTime Date { get; set; }

        /// <summary>Precio por acción; null en dividendos (el gráfico lo sitúa sobre la cotización)</summary>
        public decimal? Price { get; set; }

        /// <summary>Número de acciones (suma de las filas de un lote partido)</summary>
        public int Shares { get; set; }

        /// <summary>Importe de la operación (suma de las filas de un lote partido)</summary>
        public decimal Amount { get; set; }
    }
}
