namespace FINANCEAPP.Services
{
    /// <summary>
    /// Tramo de histórico de cotización descargado de Yahoo Finance entre dos fechas (SPEC-05-02).
    /// </summary>
    public class StockHistoryChunk
    {
        /// <summary>Barras del tramo ordenadas por fecha ascendente (vacía si no hay datos)</summary>
        public List<StockHistoryBar> Bars { get; set; } = new();

        /// <summary>Símbolo con el que respondió Yahoo (p. ej. ACS.MC), o null si no se encontró</summary>
        public string? Symbol { get; set; }

        /// <summary>Fecha de la primera cotización de la empresa según Yahoo (meta.firstTradeDate), si la da</summary>
        public DateTime? FirstTradeDateUtc { get; set; }

        /// <summary>true si Yahoo reconoció el símbolo, aunque el tramo no tenga barras</summary>
        public bool SymbolFound { get; set; }

        /// <summary>true si la consulta falló por red, tiempo de espera o error del servidor (distinto de «sin datos»)</summary>
        public bool Failed { get; set; }
    }
}
