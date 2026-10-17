using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Resultado de preparar el alta de una empresa a partir del catálogo de mercado (SPEC-01-04)
    /// </summary>
    public class MarketCompanyPrepareResult
    {
        /// <summary>True si el ticker está en el catálogo y se puede rellenar la ficha</summary>
        public bool Found { get; set; }

        /// <summary>Nombre para la ficha (solo si Found)</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Ticker para la ficha (solo si Found)</summary>
        public string Ticker { get; set; } = string.Empty;

        /// <summary>País para la ficha; null si el país del mercado no está en el catálogo de países</summary>
        public string? CountryCode { get; set; }

        /// <summary>Error en español: el ticker o el nombre ya existen en la lista del usuario. No se rellena la ficha.</summary>
        public string? Error { get; set; }

        /// <summary>Aviso en español (por ejemplo, país desconocido). La ficha se rellena igualmente.</summary>
        public string? Warning { get; set; }
    }

    /// <summary>
    /// Servicio del catálogo de empresas de mercado, de solo lectura (SPEC-01-04)
    /// </summary>
    public interface IMarketCatalogService
    {
        /// <summary>Máximo de tickers que se comprueban en una sola pasada</summary>
        const int MaxCheckBatch = 50;

        /// <summary>Todo el catálogo ordenado por nombre</summary>
        Task<List<MarketCompany>> GetAllAsync();

        /// <summary>
        /// Busca por nombre o ticker (sin distinguir mayúsculas ni tildes). Las coincidencias que empiezan
        /// por el texto salen primero.
        /// </summary>
        Task<List<MarketCompany>> SearchAsync(string text, int take = 10);

        /// <summary>Nombre visible de cada código de índice</summary>
        IReadOnlyDictionary<string, string> GetIndexLabels();

        /// <summary>Versión de la revisión del catálogo cargada en la aplicación</summary>
        string GetCatalogVersion();

        /// <summary>Tickers que el usuario ya tiene dados de alta como empresa (en mayúsculas)</summary>
        Task<HashSet<string>> GetOwnedTickersAsync();

        /// <summary>
        /// Prepara el alta de una empresa del catálogo: comprueba que el ticker y el nombre no estén ya en la
        /// lista del usuario y que el país del mercado exista en el catálogo de países.
        /// </summary>
        Task<MarketCompanyPrepareResult> PrepareCompanyAsync(string ticker);

        /// <summary>Pide a Yahoo el precio de un ticker; null si no responde</summary>
        Task<decimal?> CheckTickerAsync(string ticker);

        /// <summary>
        /// Comprueba los tickers de uno en uno (máximo <see cref="MaxCheckBatch"/>) y avisa de cada resultado
        /// según llega. Devuelve cuántos ha comprobado.
        /// </summary>
        Task<int> CheckManyAsync(IEnumerable<string> tickers, Func<string, decimal?, Task> onResult);
    }
}
