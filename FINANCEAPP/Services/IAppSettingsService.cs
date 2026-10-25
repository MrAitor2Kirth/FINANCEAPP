namespace FINANCEAPP.Services
{
    /// <summary>
    /// Límites de la imagen del logo (compartidos por la página y el servicio)
    /// </summary>
    public static class AppLogoLimits
    {
        /// <summary>Tamaño máximo del fichero que sube el usuario y de la imagen guardada (2 MB)</summary>
        public const int MaxUploadBytes = 2 * 1024 * 1024;

        /// <summary>Lado mayor máximo, en píxeles, de la imagen procesada</summary>
        public const int MaxSide = 640;

        /// <summary>Lado mayor máximo, en píxeles, de la imagen original que se acepta</summary>
        public const int MaxInputSide = 6000;
    }

    /// <summary>
    /// Servicio de ajustes de la aplicación: logo, país base, modelo de IA y preferencias del gráfico.
    /// </summary>
    public interface IAppSettingsService
    {
        /// <summary>
        /// Se lanza cuando el logo se guarda o se quita
        /// </summary>
        event Action? LogoChanged;

        /// <summary>
        /// Devuelve el logo actual (se lee de la BD una vez y se mantiene en memoria) o null si no hay
        /// </summary>
        Task<AppLogoImage?> GetLogoAsync();

        /// <summary>
        /// Valida y guarda el logo, sustituyendo al anterior
        /// </summary>
        /// <param name="data">Bytes de la imagen (PNG, JPEG o WebP)</param>
        /// <param name="contentType">Tipo de contenido: image/png, image/jpeg o image/webp</param>
        Task<AppSettingResult> SaveLogoAsync(byte[] data, string contentType);

        /// <summary>
        /// Elimina el logo guardado
        /// </summary>
        Task<AppSettingResult> RemoveLogoAsync();

        /// <summary>
        /// Devuelve el código del país base guardado (clave BaseCountryCode) o "ES" si no hay ninguno
        /// </summary>
        Task<string> GetBaseCountryCodeAsync();

        /// <summary>
        /// Guarda el código del país base (SPEC-01-02). La existencia del país la valida quien llama.
        /// </summary>
        Task<AppSettingResult> SaveBaseCountryCodeAsync(string countryCode);

        /// <summary>
        /// Devuelve el modelo de IA para leer facturas (clave AiModel) o el modelo por defecto (SPEC-14-01)
        /// </summary>
        Task<string> GetAiModelAsync();

        /// <summary>
        /// Guarda el modelo de IA (SPEC-14-01). Vacío = volver al modelo por defecto.
        /// </summary>
        Task<AppSettingResult> SaveAiModelAsync(string model);

        /// <summary>
        /// Devuelve las capas opcionales del gráfico de cotización (SPEC-05-01). Sin ajuste guardado, todo apagado.
        /// </summary>
        Task<ChartPreferences> GetChartPreferencesAsync();

        /// <summary>
        /// Guarda las capas opcionales del gráfico de cotización (claves CompanyChart.*)
        /// </summary>
        Task<AppSettingResult> SaveChartPreferencesAsync(ChartPreferences preferences);

        /// <summary>
        /// Devuelve si /companies incluye las empresas vendidas (SPEC-01-11, clave Companies.IncludeSold).
        /// Sin ajuste guardado, false («Cartera actual»).
        /// </summary>
        Task<bool> GetCompaniesIncludeSoldAsync();

        /// <summary>
        /// Guarda la elección «Cartera actual / Incluir vendidas» de /companies (SPEC-01-11)
        /// </summary>
        Task<AppSettingResult> SaveCompaniesIncludeSoldAsync(bool includeSold);

        /// <summary>
        /// Ajustes del JSON para la IA local y de las alertas (SPEC-17-03). Sin ajuste guardado, los valores por defecto.
        /// </summary>
        Task<AiExportSettings> GetAiExportSettingsAsync();

        /// <summary>
        /// Guarda los ajustes del JSON para la IA local y de las alertas (claves AiExport.* y Alerts.*), validados
        /// </summary>
        Task<AppSettingResult> SaveAiExportSettingsAsync(AiExportSettings settings);
    }

    /// <summary>
    /// Ajustes del JSON para la IA local y límites de las alertas (SPEC-17-03, RF-6 y RF-7)
    /// </summary>
    public class AiExportSettings
    {
        /// <summary>Carpeta del JSON; vacía = Documentos\FINANCEAPP\IA</summary>
        public string Folder { get; set; } = string.Empty;

        /// <summary>Días de la serie con todas las columnas</summary>
        public int FullDays { get; set; } = 30;

        /// <summary>Días de la serie compacta (4 columnas), contando los de la serie completa</summary>
        public int CompactDays { get; set; } = 365;

        public decimal MaxCompanyPct { get; set; } = 15m;
        public decimal MaxSectorPct { get; set; } = 35m;
        public decimal MaxForeignPct { get; set; } = 40m;

        /// <summary>Caída máxima de una empresa en 7 días (negativa)</summary>
        public decimal MaxDrop7dPct { get; set; } = -8m;

        public decimal MinLiquidityPct { get; set; } = 5m;
        public int StalePriceDays { get; set; } = 3;

        /// <summary>Carpeta por defecto: Documentos\FINANCEAPP\IA</summary>
        public static string DefaultFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FINANCEAPP", "IA");

        /// <summary>Carpeta que se usa: la elegida o la de por defecto</summary>
        public string EffectiveFolder => string.IsNullOrWhiteSpace(Folder) ? DefaultFolder : Folder.Trim();

        /// <summary>Ruta completa del JSON</summary>
        public string FilePath => Path.Combine(EffectiveFolder, "cartera-ia.json");
    }

    /// <summary>
    /// Capas opcionales del gráfico de cotización que se recuerdan entre sesiones (SPEC-05-01)
    /// </summary>
    public class ChartPreferences
    {
        /// <summary>Línea de precio medio de compra y resumen de resultado</summary>
        public bool ShowAverage { get; set; }

        /// <summary>Velas japonesas en lugar de línea de cierre</summary>
        public bool ShowCandles { get; set; }

        /// <summary>Barras de volumen bajo el gráfico</summary>
        public bool ShowVolume { get; set; }

        /// <summary>Marcas temporales del usuario (SPEC-05-03); activada por defecto</summary>
        public bool ShowMarks { get; set; } = true;
    }

    /// <summary>
    /// Imagen del logo en memoria
    /// </summary>
    public class AppLogoImage
    {
        /// <summary>Tipo de contenido de la imagen</summary>
        public string ContentType { get; init; } = string.Empty;

        /// <summary>Bytes de la imagen</summary>
        public byte[] Data { get; init; } = Array.Empty<byte>();

        /// <summary>Imagen como URI de datos, lista para usar en un src</summary>
        public string DataUri { get; init; } = string.Empty;
    }

    /// <summary>
    /// Resultado de guardar o quitar un ajuste
    /// </summary>
    public class AppSettingResult
    {
        /// <summary>Indica si la operación se realizó correctamente</summary>
        public bool Success { get; set; }

        /// <summary>Mensaje de error en español cuando Success es false</summary>
        public string ErrorMessage { get; set; } = string.Empty;

        public static AppSettingResult Ok() => new() { Success = true };

        public static AppSettingResult Fail(string message) =>
            new() { Success = false, ErrorMessage = message };
    }
}
