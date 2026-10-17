namespace FINANCEAPP.Services
{
    /// <summary>
    /// Guarda la clave de API de Claude solo en el almacén seguro del sistema (SPEC-14-01).
    /// Nunca va a la base de datos, a las copias de seguridad ni a los registros.
    /// </summary>
    public interface IClaudeApiKeyStore
    {
        /// <summary>Indica si hay una clave guardada</summary>
        Task<bool> HasKeyAsync();

        /// <summary>Devuelve la clave guardada o null. Solo para enviarla a la API</summary>
        Task<string?> GetKeyAsync();

        /// <summary>Clave enmascarada para mostrar (•••• y los 4 últimos caracteres), o null</summary>
        Task<string?> GetMaskedKeyAsync();

        /// <summary>Guarda la clave. Rechaza vacíos</summary>
        Task<AppSettingResult> SaveKeyAsync(string key);

        /// <summary>Elimina la clave guardada</summary>
        Task<AppSettingResult> RemoveKeyAsync();
    }
}
