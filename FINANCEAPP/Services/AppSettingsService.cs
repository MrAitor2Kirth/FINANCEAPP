using Microsoft.EntityFrameworkCore;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Implementación de <see cref="IAppSettingsService"/> sobre la tabla AppSettings.
    /// Mantiene el logo en memoria para no leer la BD en cada pintado.
    /// </summary>
    public class AppSettingsService : IAppSettingsService
    {
        private const string LogoKey = "AppLogo";
        private const string BaseCountryKey = "BaseCountryCode";
        private const string DefaultBaseCountryCode = "ES";
        private const string AiModelKey = "AiModel";
        public const string DefaultAiModel = "claude-haiku-4-5-20251001";
        private const string ChartShowAverageKey = "CompanyChart.ShowAverage";
        private const string ChartShowCandlesKey = "CompanyChart.ShowCandles";
        private const string ChartShowVolumeKey = "CompanyChart.ShowVolume";

        private readonly AppDbContext _context;
        private AppLogoImage? _cachedLogo;
        private bool _loaded;

        public AppSettingsService(AppDbContext context)
        {
            _context = context;
        }

        public event Action? LogoChanged;

        public async Task<AppLogoImage?> GetLogoAsync()
        {
            if (_loaded)
            {
                return _cachedLogo;
            }

            try
            {
                var row = await _context.AppSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Key == LogoKey);

                // Un logo ilegible se trata como «sin logo»
                _cachedLogo = row?.BinaryValue is { Length: > 0 } data
                              && !string.IsNullOrEmpty(row.TextValue)
                              && HasValidSignature(data, row.TextValue)
                    ? BuildLogo(data, row.TextValue)
                    : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer el logo: {ex.Message}");
                _cachedLogo = null;
            }

            _loaded = true;
            return _cachedLogo;
        }

        public async Task<AppSettingResult> SaveLogoAsync(byte[] data, string contentType)
        {
            if (data == null || data.Length == 0)
            {
                return AppSettingResult.Fail("La imagen está vacía. Elige otra imagen.");
            }

            if (data.Length > AppLogoLimits.MaxUploadBytes)
            {
                return AppSettingResult.Fail("La imagen pesa más de 2 MB. Reduce su tamaño y vuelve a subirla.");
            }

            var type = (contentType ?? string.Empty).ToLowerInvariant();
            if (type != "image/png" && type != "image/jpeg" && type != "image/webp")
            {
                return AppSettingResult.Fail("Formato no admitido. Sube una imagen PNG, JPG o WebP.");
            }

            if (!HasValidSignature(data, type))
            {
                return AppSettingResult.Fail("El fichero no es una imagen válida. Elige otra imagen.");
            }

            try
            {
                var row = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == LogoKey);
                if (row == null)
                {
                    _context.AppSettings.Add(new AppSetting
                    {
                        Key = LogoKey,
                        TextValue = type,
                        BinaryValue = data,
                        UpdatedAt = DateTime.Now
                    });
                }
                else
                {
                    row.TextValue = type;
                    row.BinaryValue = data;
                    row.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar el logo: {ex.Message}");
                _context.ChangeTracker.Clear();
                return AppSettingResult.Fail("No se pudo guardar el logo. No se ha modificado ningún dato.");
            }

            _cachedLogo = BuildLogo(data, type);
            _loaded = true;
            LogoChanged?.Invoke();
            return AppSettingResult.Ok();
        }

        public async Task<AppSettingResult> RemoveLogoAsync()
        {
            try
            {
                var row = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == LogoKey);
                if (row != null)
                {
                    _context.AppSettings.Remove(row);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al quitar el logo: {ex.Message}");
                _context.ChangeTracker.Clear();
                return AppSettingResult.Fail("No se pudo quitar el logo. No se ha modificado ningún dato.");
            }

            _cachedLogo = null;
            _loaded = true;
            LogoChanged?.Invoke();
            return AppSettingResult.Ok();
        }

        public async Task<string> GetBaseCountryCodeAsync()
        {
            try
            {
                var row = await _context.AppSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Key == BaseCountryKey);

                return string.IsNullOrWhiteSpace(row?.TextValue)
                    ? DefaultBaseCountryCode
                    : row.TextValue.Trim().ToUpperInvariant();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer el país base: {ex.Message}");
                return DefaultBaseCountryCode;
            }
        }

        public async Task<AppSettingResult> SaveBaseCountryCodeAsync(string countryCode)
        {
            var code = (countryCode ?? string.Empty).Trim().ToUpperInvariant();
            if (code.Length < 2 || code.Length > 3)
            {
                return AppSettingResult.Fail("Elige un país base del catálogo.");
            }

            try
            {
                var row = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == BaseCountryKey);
                if (row == null)
                {
                    _context.AppSettings.Add(new AppSetting
                    {
                        Key = BaseCountryKey,
                        TextValue = code,
                        UpdatedAt = DateTime.Now
                    });
                }
                else
                {
                    row.TextValue = code;
                    row.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return AppSettingResult.Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar el país base: {ex.Message}");
                _context.ChangeTracker.Clear();
                return AppSettingResult.Fail("No se pudo guardar el país base. No se ha modificado ningún dato.");
            }
        }

        public async Task<string> GetAiModelAsync()
        {
            try
            {
                var row = await _context.AppSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Key == AiModelKey);

                return string.IsNullOrWhiteSpace(row?.TextValue) ? DefaultAiModel : row.TextValue.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer el modelo de IA: {ex.Message}");
                return DefaultAiModel;
            }
        }

        public async Task<AppSettingResult> SaveAiModelAsync(string model)
        {
            var value = (model ?? string.Empty).Trim();
            if (value.Length > 100 || value.Any(char.IsWhiteSpace))
            {
                return AppSettingResult.Fail("El nombre del modelo no es válido (sin espacios, máximo 100 caracteres).");
            }

            if (value.Length == 0)
            {
                value = DefaultAiModel;
            }

            try
            {
                var row = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == AiModelKey);
                if (row == null)
                {
                    _context.AppSettings.Add(new AppSetting
                    {
                        Key = AiModelKey,
                        TextValue = value,
                        UpdatedAt = DateTime.Now
                    });
                }
                else
                {
                    row.TextValue = value;
                    row.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return AppSettingResult.Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar el modelo de IA: {ex.Message}");
                _context.ChangeTracker.Clear();
                return AppSettingResult.Fail("No se pudo guardar el modelo. No se ha modificado ningún dato.");
            }
        }

        public async Task<ChartPreferences> GetChartPreferencesAsync()
        {
            try
            {
                var keys = new[] { ChartShowAverageKey, ChartShowCandlesKey, ChartShowVolumeKey };
                var rows = await _context.AppSettings
                    .AsNoTracking()
                    .Where(s => keys.Contains(s.Key))
                    .ToDictionaryAsync(s => s.Key, s => s.TextValue);

                bool Read(string key) => rows.TryGetValue(key, out var value) && value == "true";

                return new ChartPreferences
                {
                    ShowAverage = Read(ChartShowAverageKey),
                    ShowCandles = Read(ChartShowCandlesKey),
                    ShowVolume = Read(ChartShowVolumeKey)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer las preferencias del gráfico: {ex.Message}");
                return new ChartPreferences();
            }
        }

        public async Task<AppSettingResult> SaveChartPreferencesAsync(ChartPreferences preferences)
        {
            try
            {
                await UpsertTextAsync(ChartShowAverageKey, preferences.ShowAverage ? "true" : "false");
                await UpsertTextAsync(ChartShowCandlesKey, preferences.ShowCandles ? "true" : "false");
                await UpsertTextAsync(ChartShowVolumeKey, preferences.ShowVolume ? "true" : "false");
                await _context.SaveChangesAsync();
                return AppSettingResult.Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar las preferencias del gráfico: {ex.Message}");
                _context.ChangeTracker.Clear();
                return AppSettingResult.Fail("No se pudieron guardar las preferencias del gráfico.");
            }
        }

        /// <summary>
        /// Crea o actualiza una fila de texto de AppSettings (sin guardar: lo hace quien llama)
        /// </summary>
        private async Task UpsertTextAsync(string key, string value)
        {
            var row = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (row == null)
            {
                _context.AppSettings.Add(new AppSetting { Key = key, TextValue = value, UpdatedAt = DateTime.Now });
            }
            else
            {
                row.TextValue = value;
                row.UpdatedAt = DateTime.Now;
            }
        }

        private static AppLogoImage BuildLogo(byte[] data, string contentType) => new()
        {
            ContentType = contentType,
            Data = data,
            DataUri = $"data:{contentType};base64,{Convert.ToBase64String(data)}"
        };

        /// <summary>
        /// Comprueba que los primeros bytes corresponden al tipo declarado (PNG, JPEG o WebP)
        /// </summary>
        private static bool HasValidSignature(byte[] data, string contentType)
        {
            switch (contentType)
            {
                case "image/png":
                    return data.Length > 8
                           && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47;
                case "image/jpeg":
                    return data.Length > 3
                           && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF;
                case "image/webp":
                    return data.Length > 12
                           && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46
                           && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50;
                default:
                    return false;
            }
        }
    }
}
