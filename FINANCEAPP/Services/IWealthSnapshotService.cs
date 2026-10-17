using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Capturas mensuales del patrimonio y su histórico (SPEC-17-01).
    /// Una captura es una foto de los importes del Dashboard: no se recalcula nunca.
    /// </summary>
    public interface IWealthSnapshotService
    {
        /// <summary>Periodos que se pueden capturar: el mes en curso y el anterior</summary>
        IReadOnlyList<SnapshotPeriod> GetCapturePeriods(DateTime today);

        /// <summary>Importes que se guardarían, captura existente del periodo y avisos. No guarda nada.</summary>
        Task<WealthSnapshotPreview> PreviewAsync(int year, int month);

        /// <summary>Guarda (o sustituye) la captura del periodo con los importes actuales</summary>
        Task<WealthSnapshot> CaptureAsync(int year, int month, string? note);

        /// <summary>Filas del listado con sus variaciones, de la más reciente a la más antigua</summary>
        Task<List<WealthSnapshotRow>> GetRowsAsync(WealthView view, int? year);

        /// <summary>Series del gráfico de evolución (solo capturas en la divisa base actual)</summary>
        Task<WealthChartData> GetChartDataAsync(WealthView view, int? year);

        /// <summary>Años que tienen capturas, del más reciente al más antiguo</summary>
        Task<List<int>> GetYearsAsync();

        /// <summary>Última captura por periodo, o null si no hay ninguna</summary>
        Task<WealthSnapshot?> GetLatestAsync();

        /// <summary>
        /// Variación del patrimonio actual frente a la última captura de un mes anterior al actual,
        /// solo si esa captura está en la divisa base actual
        /// </summary>
        Task<WealthVariation?> GetDashboardVariationAsync(DateTime today, decimal currentTotal, string currencyCode);

        /// <summary>Cambia solo la nota de una captura</summary>
        Task UpdateNoteAsync(int id, string? note);

        /// <summary>Elimina una captura</summary>
        Task DeleteAsync(int id);

        /// <summary>Genera el CSV de las filas indicadas (separador ;, coma decimal)</summary>
        string BuildCsv(IEnumerable<WealthSnapshotRow> rows);

        /// <summary>Escribe el CSV de las capturas visibles y abre la hoja de compartir del sistema</summary>
        Task ExportCsvAsync(WealthView view, int? year);
    }

    /// <summary>Vista del histórico: una fila por mes o una por año</summary>
    public enum WealthView
    {
        Monthly,
        Annual
    }

    /// <summary>Periodo (año y mes) de una captura</summary>
    public record SnapshotPeriod(int Year, int Month)
    {
        public string Label => WealthSnapshot.FormatPeriod(Year, Month);
        public int Key => Year * 100 + Month;
    }

    /// <summary>Vista previa de una captura antes de guardarla</summary>
    public class WealthSnapshotPreview
    {
        /// <summary>Importes calculados ahora (sin guardar)</summary>
        public WealthSnapshot Snapshot { get; set; } = new();

        /// <summary>Fecha de la captura que ya existe para ese periodo, si hay una</summary>
        public DateTime? ExistingCapturedAt { get; set; }

        /// <summary>Nota de la captura existente</summary>
        public string? ExistingNote { get; set; }

        /// <summary>Empresas con acciones cuyo precio en vivo falta o tiene más de 24 horas</summary>
        public int StalePriceCompanies { get; set; }

        /// <summary>Empresas en otra divisa sin tipo de cambio (quedan fuera del total)</summary>
        public int MissingRateCompanies { get; set; }

        public bool HasWarnings => StalePriceCompanies > 0 || MissingRateCompanies > 0;
    }

    /// <summary>Diferencia entre dos capturas</summary>
    public class WealthVariation
    {
        public decimal Amount { get; set; }

        /// <summary>Porcentaje, o null si la captura comparada vale 0</summary>
        public decimal? Percent { get; set; }

        /// <summary>Periodo con el que se compara, p. ej. «septiembre 2026»</summary>
        public string ComparedToLabel { get; set; } = string.Empty;
    }

    /// <summary>Fila del listado del histórico del patrimonio</summary>
    public class WealthSnapshotRow
    {
        public WealthSnapshot Snapshot { get; set; } = new();

        /// <summary>Variación frente a la captura anterior existente (o el año anterior en vista anual)</summary>
        public WealthVariation? VsPrevious { get; set; }

        /// <summary>True si la captura anterior no es el mes (o año) justo anterior</summary>
        public bool PreviousIsGap { get; set; }

        /// <summary>True si la anterior existe pero tiene otra divisa base</summary>
        public bool PreviousCurrencyMismatch { get; set; }

        /// <summary>Divisa de la captura anterior cuando no coincide</summary>
        public string? PreviousCurrencyCode { get; set; }

        /// <summary>Variación frente al mismo mes del año anterior (solo vista mensual)</summary>
        public WealthVariation? VsLastYear { get; set; }

        /// <summary>True si la del año anterior existe pero tiene otra divisa base</summary>
        public bool LastYearCurrencyMismatch { get; set; }

        /// <summary>Vista anual: el año es el actual</summary>
        public bool IsCurrentYear { get; set; }
    }

    /// <summary>Datos del gráfico de evolución</summary>
    public class WealthChartData
    {
        public string CurrencyCode { get; set; } = CurrencyFormatter.DefaultCurrency;
        public List<WealthChartPoint> Points { get; set; } = new();

        /// <summary>Capturas ocultas por estar en otra divisa base</summary>
        public int HiddenOtherCurrency { get; set; }
    }

    /// <summary>Un punto del gráfico (una captura)</summary>
    public class WealthChartPoint
    {
        public string Label { get; set; } = string.Empty;
        public decimal Stocks { get; set; }
        public decimal Funds { get; set; }
        public decimal Bank { get; set; }
        public decimal SafeHaven { get; set; }
        public decimal RealEstate { get; set; }
        public decimal Total { get; set; }
        public decimal Invested { get; set; }
        public string? Note { get; set; }
    }
}
