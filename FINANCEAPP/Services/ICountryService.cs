using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Divisa base de la aplicación (la del país base de Configuración)
    /// </summary>
    public class BaseCurrencyInfo
    {
        /// <summary>Código del país base guardado (ES si no se eligió ninguno)</summary>
        public string CountryCode { get; set; } = "ES";

        /// <summary>Nombre del país base si existe en el catálogo</summary>
        public string? CountryName { get; set; }

        /// <summary>Divisa base (EUR si el país no existe en el catálogo)</summary>
        public string CurrencyCode { get; set; } = CurrencyFormatter.DefaultCurrency;

        /// <summary>True si el país base guardado no existe en el catálogo y se usa EUR por defecto</summary>
        public bool IsFallback { get; set; }
    }

    /// <summary>
    /// Resultado de crear, editar o eliminar un país
    /// </summary>
    public class CountryResult
    {
        public bool Success { get; set; }

        /// <summary>Mensaje de error en español cuando Success es false</summary>
        public string ErrorMessage { get; set; } = string.Empty;

        public static CountryResult Ok() => new() { Success = true };

        public static CountryResult Fail(string message) => new() { Success = false, ErrorMessage = message };
    }

    /// <summary>
    /// Servicio del catálogo de países (SPEC-01-02)
    /// </summary>
    public interface ICountryService
    {
        /// <summary>Todos los países ordenados por nombre</summary>
        Task<List<Country>> GetAllAsync();

        /// <summary>Un país por su código o null</summary>
        Task<Country?> GetByCodeAsync(string code);

        /// <summary>Crea un país (valida código, nombre, región, divisa y unicidad)</summary>
        Task<CountryResult> CreateAsync(Country country);

        /// <summary>Edita nombre, región y divisa de un país. No cambia el código.</summary>
        Task<CountryResult> UpdateAsync(Country country);

        /// <summary>Elimina un país que no esté en uso ni sea el país base</summary>
        Task<CountryResult> DeleteAsync(string code);

        /// <summary>Número de empresas que usan el país</summary>
        Task<int> CountCompaniesAsync(string code);

        /// <summary>Divisa base de la aplicación según el país base</summary>
        Task<BaseCurrencyInfo> GetBaseCurrencyAsync();
    }
}
