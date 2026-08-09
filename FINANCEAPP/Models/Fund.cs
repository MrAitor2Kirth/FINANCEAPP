using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Representa un fondo de inversión en el portafolio
    /// </summary>
    public class Fund
    {
        /// <summary>
        /// Identificador único del fondo
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Nombre del fondo (ej: "Vanguard S&amp;P 500", "MyInvestor Cartera Permanente")
        /// </summary>
        [Required(ErrorMessage = "El nombre del fondo es obligatorio")]
        [MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Tipología del fondo: "Indexado", "Renta Fija", "Renta Variable", "Mixto"
        /// </summary>
        [Required(ErrorMessage = "El tipo de fondo es obligatorio")]
        [MaxLength(50, ErrorMessage = "El tipo no puede exceder 50 caracteres")]
        public string FundType { get; set; } = string.Empty;

        /// <summary>
        /// Gestora del fondo (ej: "Vanguard", "BlackRock", "MyInvestor")
        /// </summary>
        [MaxLength(100, ErrorMessage = "La gestora no puede exceder 100 caracteres")]
        public string? Manager { get; set; }

        /// <summary>
        /// TER (Total Expense Ratio) - Comisión anual en porcentaje
        /// Ejemplo: 0.15 para 0.15%
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 99.99, ErrorMessage = "El TER debe estar entre 0 y 99.99%")]
        public decimal TER { get; set; }

        /// <summary>
        /// ISIN - Código identificador internacional del fondo
        /// Ejemplo: "IE00B4L5Y983"
        /// </summary>
        [MaxLength(12, ErrorMessage = "El ISIN debe tener máximo 12 caracteres")]
        public string? ISIN { get; set; }

        /// <summary>
        /// Rentabilidad histórica anual en porcentaje
        /// Ejemplo: 8.5 para 8.5% anual
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        [Range(-99.99, 99.99, ErrorMessage = "La rentabilidad debe estar entre -99.99% y 99.99%")]
        public decimal HistoricalReturn { get; set; }

        /// <summary>
        /// Total invertido actualmente en el fondo
        /// Se calcula desde FundOperations
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalInvested { get; set; }

        /// <summary>
        /// Valor actual de la inversión (incluye plusvalías/minusvalías)
        /// Se actualiza mediante operación especial mensual O mediante método directo
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentValue { get; set; }

        /// <summary>
        /// Fecha de creación del registro
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha de última actualización
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Rentabilidad actual calculada: ((CurrentValue - TotalInvested) / TotalInvested) * 100
        /// </summary>
        [NotMapped]
        public decimal CurrentReturn => TotalInvested > 0
            ? ((CurrentValue - TotalInvested) / TotalInvested) * 100
            : 0;

        /// <summary>
        /// Rentabilidad formateada para visualización
        /// </summary>
        [NotMapped]
        public string FormattedCurrentReturn => $"{CurrentReturn:F2}%";

        /// <summary>
        /// TER formateado para visualización
        /// </summary>
        [NotMapped]
        public string FormattedTER => $"{TER:F2}%";

        /// <summary>
        /// Rentabilidad histórica formateada
        /// </summary>
        [NotMapped]
        public string FormattedHistoricalReturn => $"{HistoricalReturn:F2}%";
    }
}
