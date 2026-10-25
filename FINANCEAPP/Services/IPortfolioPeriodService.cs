using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Indicadores de las tarjetas de /companies para un periodo (SPEC-01-09): las compras hechas en el periodo,
    /// valoradas hoy, o la cartera tal como estaba al cierre del periodo.
    /// </summary>
    public interface IPortfolioPeriodService
    {
        /// <summary>
        /// Compras y traspasos abiertos con fecha en el periodo, valorados con el precio y el tipo de cambio actuales
        /// (RF-3). Las compras del periodo ya vendidas no cuentan: su importe sale aparte para la nota.
        /// </summary>
        Task<PeriodSummary> GetPurchasesSummaryAsync(SalesPeriodFilter filter);

        /// <summary>
        /// Cartera al cierre del periodo (RF-5): posición, precio de cierre y tipo de cambio de la fecha de corte.
        /// Descarga el histórico de cotización y de tipos que falte.
        /// </summary>
        Task<PeriodSummary> GetClosingSummaryAsync(SalesPeriodFilter filter, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Vista del periodo en /companies (SPEC-01-09, RF-2)
    /// </summary>
    public enum PeriodSummaryView
    {
        /// <summary>Compras hechas en el periodo que sigues teniendo, valoradas hoy</summary>
        Purchases,
        /// <summary>La cartera tal como estaba al cierre del periodo</summary>
        Closing
    }

    /// <summary>
    /// Indicadores de un periodo para las tarjetas de /companies (SPEC-01-09)
    /// </summary>
    public class PeriodSummary
    {
        /// <summary>
        /// Totales convertidos a la divisa base (invertido, valor, dividendos e inversión por empresa), con la misma
        /// forma que los de hoy para usar las fórmulas de <see cref="InvestedCompaniesSummary"/>
        /// </summary>
        public ConvertedCompanyTotals Totals { get; set; } = new();

        /// <summary>
        /// Empresas del periodo (copias, no entidades del contexto) con sus cifras del periodo: sector y yield para
        /// «Empresas · sectores» y «Yield medio»
        /// </summary>
        public List<CompanyDetail> Companies { get; set; } = new();

        /// <summary>Compras del periodo ya vendidas, convertidas (solo en «Compras del periodo»)</summary>
        public decimal SoldPurchasesAmount { get; set; }

        /// <summary>Dividendos cobrados en el periodo, convertidos (solo en «Compras del periodo»)</summary>
        public decimal PeriodDividends { get; set; }

        /// <summary>true si la rentabilidad con dividendos tiene sentido en la vista</summary>
        public bool ShowReturnWithDividends { get; set; }

        /// <summary>Fecha de corte de «Cartera al cierre»</summary>
        public DateTime? CutoffDate { get; set; }

        /// <summary>true si el periodo empieza después de hoy</summary>
        public bool IsFuture { get; set; }

        /// <summary>Avisos de cifras aproximadas (sin histórico de cotización o de tipos, por ejemplo)</summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>true si hay alguna empresa en el periodo</summary>
        public bool HasData => Companies.Count > 0;
    }
}
