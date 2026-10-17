using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Captura mensual del patrimonio (SPEC-17-01): foto de los importes del Dashboard en un mes.
    /// Una sola captura por año y mes. Los importes no se recalculan nunca después de guardarse.
    /// </summary>
    public class WealthSnapshot
    {
        /// <summary>Identificador único de la captura</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Año del periodo capturado</summary>
        [Range(2000, 2200, ErrorMessage = "El año no es válido")]
        public int Year { get; set; }

        /// <summary>Mes del periodo capturado (1-12)</summary>
        [Range(1, 12, ErrorMessage = "El mes debe estar entre 1 y 12")]
        public int Month { get; set; }

        /// <summary>Fecha y hora local en que se tomó la captura</summary>
        [Display(Name = "Fecha de captura")]
        public DateTime CapturedAt { get; set; }

        /// <summary>Divisa base en el momento de la captura</summary>
        [Required]
        [StringLength(3)]
        public string CurrencyCode { get; set; } = FINANCEAPP.Services.CurrencyFormatter.DefaultCurrency;

        /// <summary>Patrimonio total (mismo valor que el Dashboard)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPatrimony { get; set; }

        /// <summary>Total invertido: acciones + fondos + refugio + inmuebles (sin banco)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalInvested { get; set; }

        /// <summary>Acciones: valor actual convertido a la divisa base</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal StocksValue { get; set; }

        /// <summary>Acciones: inversión inicial</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal StocksInvested { get; set; }

        /// <summary>Fondos: valor actual</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal FundsValue { get; set; }

        /// <summary>Fondos: total invertido</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal FundsInvested { get; set; }

        /// <summary>Saldo bancario total (el efectivo no tiene invertido)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal BankBalance { get; set; }

        /// <summary>Activos refugio: valor actual</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SafeHavenValue { get; set; }

        /// <summary>Activos refugio: total invertido</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SafeHavenInvested { get; set; }

        /// <summary>Inmuebles no vendidos: valor actual (o invertido si no hay)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal RealEstateValue { get; set; }

        /// <summary>Inmuebles no vendidos: total invertido</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal RealEstateInvested { get; set; }

        /// <summary>Empresas en otra divisa sin tipo de cambio que quedaron fuera del total</summary>
        public int ExcludedCompanies { get; set; }

        /// <summary>Nota opcional de la captura</summary>
        [StringLength(200, ErrorMessage = "La nota no puede exceder 200 caracteres")]
        public string? Note { get; set; }

        /// <summary>Fecha de creación del registro</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>Fecha de la última modificación</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>Plusvalía latente: patrimonio sin efectivo menos lo invertido. No se guarda.</summary>
        [NotMapped]
        public decimal LatentGain => TotalPatrimony - BankBalance - TotalInvested;

        /// <summary>Clave ordenable del periodo (AAAAMM). No se guarda.</summary>
        [NotMapped]
        public int PeriodKey => Year * 100 + Month;

        /// <summary>Periodo legible, p. ej. «octubre 2026». No se guarda.</summary>
        [NotMapped]
        public string PeriodLabel => FormatPeriod(Year, Month);

        /// <summary>Nombre del mes en español y año (cultura fija es-ES)</summary>
        public static string FormatPeriod(int year, int month) =>
            $"{CultureInfo.GetCultureInfo("es-ES").DateTimeFormat.GetMonthName(month)} {year}";
    }
}
