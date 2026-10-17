namespace FINANCEAPP.Services
{
    /// <summary>
    /// Implementación sobre SecureStorage (Keychain en iOS, Keystore en Android, DPAPI en Windows)
    /// </summary>
    public class ClaudeApiKeyStore : IClaudeApiKeyStore
    {
        private const string StorageKey = "claude_api_key";

        public async Task<bool> HasKeyAsync() => !string.IsNullOrEmpty(await GetKeyAsync());

        public async Task<string?> GetKeyAsync()
        {
            try
            {
                var key = await SecureStorage.Default.GetAsync(StorageKey);
                return string.IsNullOrWhiteSpace(key) ? null : key;
            }
            catch (Exception)
            {
                // Almacén no disponible: se trata como «sin clave». Nunca se muestra la clave en el mensaje.
                return null;
            }
        }

        public async Task<string?> GetMaskedKeyAsync()
        {
            var key = await GetKeyAsync();
            if (key == null)
            {
                return null;
            }

            return key.Length <= 4 ? "••••" : "••••" + key[^4..];
        }

        public async Task<AppSettingResult> SaveKeyAsync(string key)
        {
            var value = (key ?? string.Empty).Trim();
            if (value.Length == 0)
            {
                return AppSettingResult.Fail("Escribe la clave de API.");
            }

            try
            {
                await SecureStorage.Default.SetAsync(StorageKey, value);
                return AppSettingResult.Ok();
            }
            catch (Exception)
            {
                return AppSettingResult.Fail("No se pudo guardar la clave en el almacén seguro de este dispositivo. No se ha guardado en ningún otro sitio.");
            }
        }

        public Task<AppSettingResult> RemoveKeyAsync()
        {
            try
            {
                SecureStorage.Default.Remove(StorageKey);
                return Task.FromResult(AppSettingResult.Ok());
            }
            catch (Exception)
            {
                return Task.FromResult(AppSettingResult.Fail("No se pudo eliminar la clave del almacén seguro."));
            }
        }
    }
}
