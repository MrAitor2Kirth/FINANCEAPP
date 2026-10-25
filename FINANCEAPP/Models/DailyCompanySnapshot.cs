using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Posición de una empresa en un día (SPEC-17-03): solo empresas con acciones ese día. Dato derivado que se rehace
    /// junto con <see cref="DailyPortfolioSnapshot"/>. Sin clave foránea (como el resto de enlaces del proyecto).
    /// </summary>
    public class DailyCompanySnapshot
    {
        [Key]
        public int Id { get; set; }

        public DateTime Date { get; set; }

        public int CompanyId { get; set; }

        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(20)]
        public string Ticker { get; set; } = string.Empty;

        [StringLength(100)]
        public string Sector { get; set; } = string.Empty;

        /// <summary>Divisa de la empresa (la del precio)</summary>
        [StringLength(3)]
        public string CurrencyCode { get; set; } = FINANCEAPP.Services.CurrencyFormatter.DefaultCurrency;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Shares { get; set; }

        /// <summary>Precio de ese día en la divisa de la empresa</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal Price { get; set; }

        /// <summary>Coste de las acciones abiertas, en la divisa base</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostBase { get; set; }

        /// <summary>Valor de las acciones abiertas, en la divisa base</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal ValueBase { get; set; }

        /// <summary>Dividendos cobrados hasta ese día, en la divisa base</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal DividendsBase { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ReturnPct { get; set; }

        /// <summary>Peso sobre el valor de la cartera de acciones de ese día</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal WeightPct { get; set; }
    }
}
