using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Calendario de dividendos (SPEC-21-01): líneas de dividendo por empresa, resumen, generación de la operación
    /// «Dividendo», próximos dividendos de Inicio, propuestas confirmadas, último dividendo de Yahoo y marcas del gráfico.
    /// </summary>
    public interface IDividendService
    {
        /// <summary>Líneas con empresa, divisa e importe total; Pendientes por fecha ascendente, el resto descendente (RF-2)</summary>
        Task<List<DividendPayment>> GetAsync(DividendFilter filter, int? companyId = null);

        /// <summary>Pendiente a 90 días y cobrado del año, en divisa base con el tipo actual (RF-2)</summary>
        Task<DividendSummary> GetSummaryAsync();

        /// <summary>Crea una línea Manual y Pendiente (RF-3)</summary>
        Task<DividendResult> CreateAsync(DividendPaymentInput input);

        /// <summary>Edita una línea Pendiente (RF-4); las Cobradas no se editan</summary>
        Task<DividendResult> UpdateAsync(int id, DividendPaymentInput input);

        /// <summary>Borra una línea Pendiente (RF-4); las Cobradas no se borran</summary>
        Task<DividendResult> DeleteAsync(int id);

        /// <summary>Acciones en cartera de la empresa, para proponerlas en la línea</summary>
        Task<decimal> GetDefaultSharesAsync(int companyId);

        /// <summary>
        /// Genera la operación «Dividendo» de la línea (RF-6) con <paramref name="amount"/> en la divisa de la empresa,
        /// por <see cref="IOperationService.AddAsync"/>, que en la misma transacción marca la línea Cobrada.
        /// </summary>
        Task<DividendResult> GenerateOperationAsync(int id, decimal amount, string? bankAccountName, decimal? exchangeRate);

        /// <summary>Devuelve a Pendiente las líneas Cobradas cuya operación ya no existe (RF-4); devuelve cuántas</summary>
        Task<int> RepairOrphanLinksAsync();

        /// <summary>Líneas Pendientes de empresas con acciones y pago de hoy a <paramref name="days"/> días (RF-8)</summary>
        Task<List<UpcomingDividend>> GetUpcomingAsync(int days = 90);

        /// <summary>Propuestas de dividendos anunciados enlazadas con las empresas por ticker (RF-11)</summary>
        Task<List<DividendProposalView>> GetProposalsAsync();

        /// <summary>Crea como líneas Pendientes (origen Búsqueda) las propuestas elegidas; nunca crea operaciones (RF-11)</summary>
        Task<DividendResult> CreateFromProposalsAsync(IEnumerable<DividendProposalView> selected);

        /// <summary>Último dividendo pagado según Yahoo, de referencia para el importe por acción (RF-12)</summary>
        Task<LastDividendResult> GetLastDividendAsync(int companyId);

        /// <summary>Líneas de la empresa para dibujarlas como «Pago de dividendo» en el gráfico (RF-9)</summary>
        Task<List<DividendChartMark>> GetChartMarksAsync(int companyId);
    }

    /// <summary>Filtro de la vista de dividendos</summary>
    public enum DividendFilter
    {
        Pending,
        Collected,
        All
    }

    /// <summary>
    /// Datos del formulario de una línea (copia: el formulario no enlaza la entidad del contexto)
    /// </summary>
    public class DividendPaymentInput
    {
        public int CompanyId { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Today;
        public DateTime? ExDividendDate { get; set; }
        public decimal? AmountPerShare { get; set; }
        public decimal NumberOfShares { get; set; }
        public decimal? DividendPercent { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>Resultado de una operación sobre las líneas de dividendo</summary>
    public class DividendResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>Cuántas líneas se crearon (propuestas)</summary>
        public int Created { get; set; }

        public static DividendResult Fail(string message) => new() { Success = false, ErrorMessage = message };

        public static DividendResult Ok(int created = 0) => new() { Success = true, Created = created };
    }

    /// <summary>Tarjetas de resumen de /dividends, en divisa base</summary>
    public class DividendSummary
    {
        public string BaseCurrencyCode { get; set; } = CurrencyFormatter.DefaultCurrency;

        /// <summary>Líneas Pendientes con importe y pago de hoy a 90 días</summary>
        public decimal PendingNext90Days { get; set; }

        /// <summary>Todas las operaciones «Dividendo» del año natural</summary>
        public decimal CollectedThisYear { get; set; }

        /// <summary>Empresas en otra divisa sin tipo de cambio que se quedan fuera de los totales</summary>
        public int MissingRateCompanies { get; set; }
    }

    /// <summary>Propuesta de dividendo enlazada con una empresa (RF-11)</summary>
    public class DividendProposalView
    {
        public DividendProposal Proposal { get; set; } = null!;
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = CurrencyFormatter.DefaultCurrency;

        /// <summary>Ya hay una línea de la empresa con esa fecha de pago</summary>
        public bool AlreadyExists { get; set; }

        /// <summary>Acciones en cartera que tendrá la línea</summary>
        public decimal Shares { get; set; }

        /// <summary>Marcada en la interfaz para crearla</summary>
        public bool Selected { get; set; }
    }

    /// <summary>Línea del bloque «Próximos dividendos» de Inicio (RF-8)</summary>
    public class UpcomingDividend
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public decimal? TotalAmount { get; set; }
        public string CurrencyCode { get; set; } = CurrencyFormatter.DefaultCurrency;
    }

    /// <summary>Resultado de «Último dividendo (Yahoo)» (RF-12)</summary>
    public class LastDividendResult
    {
        public StockDividend? Dividend { get; set; }

        /// <summary>Mensaje en español si no hay dividendo o falló la consulta</summary>
        public string? Message { get; set; }
    }

    /// <summary>Línea de dividendo colocada en un periodo del gráfico, pasado o futuro (RF-9)</summary>
    public class DividendMarkPoint
    {
        public int Id { get; set; }

        /// <summary>Fecha del periodo donde se dibuja</summary>
        public DateTime Date { get; set; }

        public DateTime PaymentDate { get; set; }
        public DateTime? ExDividendDate { get; set; }
        public string Status { get; set; } = DividendPaymentRules.StatusPending;
        public decimal? TotalAmount { get; set; }
        public string CurrencyCode { get; set; } = CurrencyFormatter.DefaultCurrency;

        /// <summary>true si cae después del último precio (se dibuja a la altura del último precio)</summary>
        public bool IsFuture { get; set; }
    }

    /// <summary>Línea de dividendo lista para dibujarla en el gráfico (RF-9)</summary>
    public class DividendChartMark
    {
        public int Id { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime? ExDividendDate { get; set; }
        public string Status { get; set; } = DividendPaymentRules.StatusPending;
        public decimal? TotalAmount { get; set; }
        public string CurrencyCode { get; set; } = CurrencyFormatter.DefaultCurrency;
    }
}
