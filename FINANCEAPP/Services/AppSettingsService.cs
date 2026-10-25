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
        private const string ChartShowMarksKey = "CompanyChart.ShowMarks";
        // Selector «Cartera actual / Incluir vendidas» de /companies (SPEC-01-11)
        private const string CompaniesIncludeSoldKey = "Companies.IncludeSold";
        // JSON para la IA local y alertas (SPEC-17-03)
        private const string AiFolderKey = "AiExport.Folder";
        private const string AiFullDaysKey = "AiExport.FullDays";
        private const string AiCompactDaysKey = "AiExport.CompactDays";
        private const string AlertCompanyKey = "Alerts.MaxCompanyPct";
        private const string AlertSectorKey = "Alerts.MaxSectorPct";
        private const string AlertForeignKey = "Alerts.MaxForeignPct";
        private const string AlertDropKey = "Alerts.MaxDrop7dPct";
        private const string AlertLiquidityKey = "Alerts.MinLiquidityPct";
        private const string AlertStaleKey = "Alerts.StalePriceDays";

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
                var keys = new[] { ChartShowAverageKey, ChartShowCandlesKey, ChartShowVolumeKey, ChartShowMarksKey };
                var rows = await _context.AppSettings
                    .AsNoTracking()
                    .Where(s => keys.Contains(s.Key))
                    .ToDictionaryAsync(s => s.Key, s => s.TextValue);

                bool Read(string key) => rows.TryGetValue(key, out var value) && value == "true";

                return new ChartPreferences
                {
                    ShowAverage = Read(ChartShowAverageKey),
                    ShowCandles = Read(ChartShowCandlesKey),
                    ShowVolume = Read(ChartShowVolumeKey),
                    // Las marcas se ven salvo que el usuario las haya ocultado (sin fila = activado)
                    ShowMarks = !rows.TryGetValue(ChartShowMarksKey, out var marks) || marks != "false"
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
                await UpsertTextAsync(ChartShowMarksKey, preferences.ShowMarks ? "true" : "false");
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

        public async Task<bool> GetCompaniesIncludeSoldAsync()
        {
            try
            {
                var value = await _context.AppSettings
                    .AsNoTracking()
                    .Where(s => s.Key == CompaniesIncludeSoldKey)
                    .Select(s => s.TextValue)
                    .FirstOrDefaultAsync();
                return value == "true";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer la preferencia de empresas vendidas: {ex.Message}");
                return false;
            }
        }

        public async Task<AppSettingResult> SaveCompaniesIncludeSoldAsync(bool includeSold)
        {
            try
            {
                await UpsertTextAsync(CompaniesIncludeSoldKey, includeSold ? "true" : "false");
                await _context.SaveChangesAsync();
                return AppSettingResult.Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar la preferencia de empresas vendidas: {ex.Message}");
                _context.ChangeTracker.Clear();
                return AppSettingResult.Fail("No se pudo guardar la preferencia.");
            }
        }

        public async Task<AiExportSettings> GetAiExportSettingsAsync()
        {
            var settings = new AiExportSettings();
            try
            {
                var keys = new[] { AiFolderKey, AiFullDaysKey, AiCompactDaysKey, AlertCompanyKey, AlertSectorKey, AlertForeignKey, AlertDropKey, AlertLiquidityKey, AlertStaleKey };
                var rows = await _context.AppSettings
                    .AsNoTracking()
                    .Where(s => keys.Contains(s.Key))
                    .ToDictionaryAsync(s => s.Key, s => s.TextValue);

                decimal Dec(string key, decimal fallback) =>
                    rows.TryGetValue(key, out var v) && decimal.TryParse(v, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : fallback;
                int Int(string key, int fallback) =>
                    rows.TryGetValue(key, out var v) && int.TryParse(v, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var i) ? i : fallback;

                settings.Folder = rows.TryGetValue(AiFolderKey, out var folder) ? folder ?? string.Empty : string.Empty;
                settings.FullDays = Int(AiFullDaysKey, settings.FullDays);
                settings.CompactDays = Int(AiCompactDaysKey, settings.CompactDays);
                settings.MaxCompanyPct = Dec(AlertCompanyKey, settings.MaxCompanyPct);
                settings.MaxSectorPct = Dec(AlertSectorKey, settings.MaxSectorPct);
                settings.MaxForeignPct = Dec(AlertForeignKey, settings.MaxForeignPct);
                settings.MaxDrop7dPct = Dec(AlertDropKey, settings.MaxDrop7dPct);
                settings.MinLiquidityPct = Dec(AlertLiquidityKey, settings.MinLiquidityPct);
                settings.StalePriceDays = Int(AlertStaleKey, settings.StalePriceDays);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer los ajustes de la IA: {ex.Message}");
            }

            return settings;
        }

        public async Task<AppSettingResult> SaveAiExportSettingsAsync(AiExportSettings settings)
        {
            if (settings.FullDays < 1 || settings.FullDays > 366)
            {
                return AppSettingResult.Fail("Los días de la serie completa deben estar entre 1 y 366.");
            }
            if (settings.CompactDays < settings.FullDays || settings.CompactDays > 3650)
            {
                return AppSettingResult.Fail("Los días de la serie compacta deben ser al menos los de la completa y como mucho 3.650.");
            }
            if (settings.MaxCompanyPct is <= 0 or > 100 || settings.MaxSectorPct is <= 0 or > 100
                || settings.MaxForeignPct is <= 0 or > 100 || settings.MinLiquidityPct is < 0 or > 100)
            {
                return AppSettingResult.Fail("Los porcentajes de las alertas deben estar entre 0 y 100.");
            }
            if (settings.MaxDrop7dPct is >= 0 or < -100)
            {
                return AppSettingResult.Fail("La caída de 7 días debe ser un porcentaje negativo (por ejemplo, −8).");
            }
            if (settings.StalePriceDays is < 1 or > 365)
            {
                return AppSettingResult.Fail("Los días sin actualizar el precio deben estar entre 1 y 365.");
            }

            try
            {
                string D(decimal v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
                await UpsertTextAsync(AiFolderKey, settings.Folder?.Trim() ?? string.Empty);
                await UpsertTextAsync(AiFullDaysKey, settings.FullDays.ToString(System.Globalization.CultureInfo.InvariantCulture));
                await UpsertTextAsync(AiCompactDaysKey, settings.CompactDays.ToString(System.Globalization.CultureInfo.InvariantCulture));
                await UpsertTextAsync(AlertCompanyKey, D(settings.MaxCompanyPct));
                await UpsertTextAsync(AlertSectorKey, D(settings.MaxSectorPct));
                await UpsertTextAsync(AlertForeignKey, D(settings.MaxForeignPct));
                await UpsertTextAsync(AlertDropKey, D(settings.MaxDrop7dPct));
                await UpsertTextAsync(AlertLiquidityKey, D(settings.MinLiquidityPct));
                await UpsertTextAsync(AlertStaleKey, settings.StalePriceDays.ToString(System.Globalization.CultureInfo.InvariantCulture));
                await _context.SaveChangesAsync();
                return AppSettingResult.Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar los ajustes de la IA: {ex.Message}");
                _context.ChangeTracker.Clear();
                return AppSettingResult.Fail("No se pudieron guardar los ajustes de la IA.");
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
