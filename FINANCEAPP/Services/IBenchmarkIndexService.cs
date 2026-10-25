using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Índices (o cualquier símbolo de Yahoo) para comparar con las empresas en el gráfico (SPEC-05-04, RF-1).
    /// </summary>
    public interface IBenchmarkIndexService
    {
        /// <summary>
        /// Índices dados de alta, por nombre, con el último cierre guardado en su histórico.
        /// </summary>
        Task<List<BenchmarkIndexSummary>> GetAllAsync();

        /// <summary>
        /// Da de alta un índice. Nombre y símbolo obligatorios; el símbolo se guarda en mayúsculas y es único.
        /// </summary>
        Task<BenchmarkIndexResult> CreateAsync(BenchmarkIndexInput input);

        /// <summary>
        /// Cambia nombre y símbolo. Si cambia el símbolo, el histórico guardado del anterior se borra.
        /// </summary>
        Task<BenchmarkIndexResult> UpdateAsync(int id, BenchmarkIndexInput input);

        /// <summary>
        /// Borra el índice, su histórico y su marca de histórico completo, en una transacción.
        /// </summary>
        Task<BenchmarkIndexResult> DeleteAsync(int id);

        /// <summary>
        /// Consulta a Yahoo el último valor del símbolo (sin probar sufijos de bolsa).
        /// </summary>
        Task<BenchmarkSymbolCheck> CheckSymbolAsync(string symbol);

        /// <summary>
        /// Índices del catálogo de mercado que aún no están dados de alta.
        /// </summary>
        Task<List<BenchmarkIndexInput>> GetSuggestionsAsync();
    }

    /// <summary>
    /// Fila del listado de índices
    /// </summary>
    public class BenchmarkIndexSummary
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Symbol { get; set; } = string.Empty;

        /// <summary>Último cierre guardado; null si aún no tiene histórico</summary>
        public decimal? LastClose { get; set; }

        /// <summary>Fecha del último cierre guardado</summary>
        public DateTime? LastDate { get; set; }
    }

    /// <summary>
    /// Datos del formulario de un índice (copia: el formulario no enlaza la entidad del contexto)
    /// </summary>
    public class BenchmarkIndexInput
    {
        public string Name { get; set; } = string.Empty;

        public string Symbol { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resultado de una operación sobre los índices
    /// </summary>
    public class BenchmarkIndexResult
    {
        /// <summary>true si se guardó</summary>
        public bool Success { get; set; }

        /// <summary>Mensaje de error en español si no se guardó</summary>
        public string? ErrorMessage { get; set; }

        public static BenchmarkIndexResult Fail(string message) => new() { Success = false, ErrorMessage = message };

        public static BenchmarkIndexResult Ok() => new() { Success = true };
    }

    /// <summary>
    /// Resultado de «Comprobar» un símbolo en Yahoo
    /// </summary>
    public class BenchmarkSymbolCheck
    {
        /// <summary>true si Yahoo devolvió un valor</summary>
        public bool Found { get; set; }

        /// <summary>Último valor</summary>
        public decimal? Price { get; set; }

        /// <summary>Divisa según Yahoo (informativa)</summary>
        public string? Currency { get; set; }

        /// <summary>Mensaje para el usuario</summary>
        public string Message { get; set; } = string.Empty;
    }
}
