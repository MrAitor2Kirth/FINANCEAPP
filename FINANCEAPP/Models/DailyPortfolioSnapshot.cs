using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Captura diaria de la cartera (SPEC-17-03): una fila por día con los cinco registros (patrimonio, cartera actual,
    /// compras del día, con vendidas y solo vendidas) y los flujos del día, en la divisa base. Son datos derivados:
    /// se rehacen desde la fecha más antigua que cambie (a diferencia de las capturas mensuales de SPEC-17-01).
    /// </summary>
    public class DailyPortfolioSnapshot
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Día de la captura (sin hora); único</summary>
        public DateTime Date { get; set; }

        /// <summary>Divisa base de los importes</summary>
        [StringLength(3)]
        public string CurrencyCode { get; set; } = FINANCEAPP.Services.CurrencyFormatter.DefaultCurrency;

        /// <summary>Momento en que se calculó</summary>
        public DateTime CalculatedAt { get; set; } = DateTime.Now;

        /// <summary>true si se reconstruyó después (no se calculó ese mismo día)</summary>
        public bool IsReconstructed { get; set; }

        /// <summary>true si algún dato es aproximado (sin cotización, sin tipo o sin histórico)</summary>
        public bool IsApproximate { get; set; }

        /// <summary>Motivos de la aproximación, separados por «; »</summary>
        [StringLength(500)]
        public string? ApproximateNote { get; set; }

        // ---- 1 Patrimonio total (el banco no cuenta como invertido, como SPEC-17-01)
        [Column(TypeName = "decimal(18,2)")] public decimal TotalValue { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal TotalInvested { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal StocksValue { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal StocksInvested { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal FundsValue { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal FundsInvested { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal SafeHavenValue { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal SafeHavenInvested { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal RealEstateValue { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal RealEstateInvested { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal BankBalance { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal LiquidityPct { get; set; }

        // ---- 2 Cartera actual (acciones abiertas)
        [Column(TypeName = "decimal(18,2)")] public decimal OpenInvested { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal OpenValue { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal OpenDividends { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal OpenReturnPct { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal OpenReturnWithDivPct { get; set; }

        // ---- 3 Compras del día
        [Column(TypeName = "decimal(18,2)")] public decimal DayBought { get; set; }
        public int DayBuyCount { get; set; }

        // ---- 4 Empresas con vendidas (SPEC-01-11)
        [Column(TypeName = "decimal(18,2)")] public decimal AllInvested { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal AllValue { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal AllDividends { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal AllReturnPct { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal AllReturnWithDivPct { get; set; }

        // ---- 5 Solo vendidas (ventas hasta ese día)
        [Column(TypeName = "decimal(18,2)")] public decimal SoldCost { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal SoldProceeds { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal SoldResult { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal SoldReturnPct { get; set; }

        // ---- Flujos del día
        [Column(TypeName = "decimal(18,2)")] public decimal DaySoldNet { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal DayNetContribution { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal DayDividends { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal DayCommissions { get; set; }
    }
}
