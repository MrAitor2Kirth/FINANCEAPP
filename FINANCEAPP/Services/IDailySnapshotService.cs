namespace FINANCEAPP.Services
{
    /// <summary>
    /// Capturas diarias de la cartera (SPEC-17-03): rehace los días pendientes desde la marca «recalcular desde X»
    /// (o desde la primera operación), calcula el día de hoy y escribe el JSON para la IA local.
    /// </summary>
    public interface IDailySnapshotService
    {
        /// <summary>
        /// Rehace los días pendientes (sin descargas, en memoria) y el de hoy, y escribe el JSON. No lanza si falla
        /// el JSON: el error queda en el resultado.
        /// </summary>
        Task<DailySnapshotRefreshResult> RefreshAsync(CancellationToken cancellationToken = default);

        /// <summary>Estado de la serie para /history</summary>
        Task<DailySnapshotStatus> GetStatusAsync();

        /// <summary>
        /// «Completar históricos»: descarga las cotizaciones de las empresas con operaciones, de los índices y los tipos
        /// de cambio que falten desde la primera operación, y rehace toda la serie. Devuelve cuántas series revisó.
        /// </summary>
        Task<int> CompleteHistoriesAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>Resultado de un recálculo de las capturas diarias</summary>
    public class DailySnapshotRefreshResult
    {
        /// <summary>Primer día rehecho (null si no había operaciones)</summary>
        public DateTime? From { get; set; }

        /// <summary>Días calculados, contando el de hoy</summary>
        public int Days { get; set; }

        /// <summary>Ruta del JSON escrito</summary>
        public string? JsonPath { get; set; }

        /// <summary>Error al escribir el JSON (los registros se guardan igual)</summary>
        public string? JsonError { get; set; }
    }

    /// <summary>Estado de la serie diaria (SPEC-17-03, /history)</summary>
    public class DailySnapshotStatus
    {
        public DateTime? FirstDate { get; set; }
        public DateTime? LastDate { get; set; }
        public int Days { get; set; }
        public int ReconstructedDays { get; set; }
        public int ApproximateDays { get; set; }

        /// <summary>Fecha pendiente de recalcular, si la hay</summary>
        public DateTime? DirtyFrom { get; set; }

        public string JsonPath { get; set; } = string.Empty;
        public DateTime? JsonWrittenAt { get; set; }

        /// <summary>Último error al escribir el JSON en esta sesión</summary>
        public string? JsonError { get; set; }
    }
}
