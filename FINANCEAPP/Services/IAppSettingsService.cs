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
