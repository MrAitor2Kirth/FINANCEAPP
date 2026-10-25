using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Marcas temporales del gráfico de cotización (SPEC-05-03): de una empresa o globales (todas las empresas).
    /// </summary>
    public interface IChartMarkService
    {
        /// <summary>
        /// Marcas de la empresa y marcas globales, activas e inactivas, de la más reciente a la más antigua.
        /// </summary>
        Task<List<ChartMark>> GetForCompanyAsync(int companyId);

        /// <summary>
        /// Crea una marca desde el gráfico de la empresa indicada (de Empresa o Global según <see cref="ChartMarkInput.IsGlobal"/>).
        /// </summary>
        Task<ChartMarkResult> CreateAsync(int companyId, ChartMarkInput input);

        /// <summary>
        /// Edita fecha, descripción y tipo. Una marca que pasa a ser de Empresa queda ligada a <paramref name="companyId"/>
        /// (la empresa del gráfico desde el que se edita).
        /// </summary>
        Task<ChartMarkResult> UpdateAsync(int markId, int companyId, ChartMarkInput input);

        /// <summary>
        /// Activa o desactiva una marca (una marca inactiva no se dibuja).
        /// </summary>
        Task<ChartMarkResult> SetActiveAsync(int markId, bool isActive);

        /// <summary>
        /// Borra una marca. Una global desaparece del gráfico de todas las empresas.
        /// </summary>
        Task<ChartMarkResult> DeleteAsync(int markId);
    }

    /// <summary>
    /// Datos del formulario de una marca (copia: el formulario no enlaza la entidad del contexto)
    /// </summary>
    public class ChartMarkInput
    {
        /// <summary>Fecha de la marca</summary>
        public DateTime Date { get; set; } = DateTime.Today;

        /// <summary>Descripción</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>true = Global (todas las empresas); false = solo esta empresa</summary>
        public bool IsGlobal { get; set; }
    }

    /// <summary>
    /// Resultado de una operación sobre las marcas
    /// </summary>
    public class ChartMarkResult
    {
        /// <summary>true si se guardó</summary>
        public bool Success { get; set; }

        /// <summary>Mensaje de error en español si no se guardó</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>Marca guardada (en altas y ediciones)</summary>
        public ChartMark? Mark { get; set; }

        public static ChartMarkResult Fail(string message) => new() { Success = false, ErrorMessage = message };

        public static ChartMarkResult Ok(ChartMark? mark = null) => new() { Success = true, Mark = mark };
    }

    /// <summary>
    /// Marca colocada en un periodo de la temporalidad, lista para dibujar (SPEC-05-03)
    /// </summary>
    public class ChartMarkPoint
    {
        /// <summary>Identificador de la marca</summary>
        public int Id { get; set; }

        /// <summary>Fecha del periodo donde se dibuja</summary>
        public DateTime Date { get; set; }

        /// <summary>Fecha original de la marca (para el tooltip)</summary>
        public DateTime MarkDate { get; set; }

        /// <summary>Descripción</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>true si es global (se dibuja hueca)</summary>
        public bool IsGlobal { get; set; }
    }
}
