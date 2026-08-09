using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Modelo que representa los detalles de una empresa
    /// Contiene información relevante para análisis de inversión
    /// </summary>
    public class CompanyDetail
    {
        /// <summary>
        /// Identificador único de la empresa
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Nombre completo de la empresa
        /// </summary>
        [Required(ErrorMessage = "El nombre de la empresa es obligatorio")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        [Display(Name = "Nombre")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Símbolo o ticker de cotización en bolsa
        /// </summary>
        [Required(ErrorMessage = "El ticker es obligatorio")]
        [StringLength(20, ErrorMessage = "El ticker no puede exceder 20 caracteres")]
        [Display(Name = "Ticker")]
        public string Ticker { get; set; } = string.Empty;

        /// <summary>
        /// Sector económico al que pertenece la empresa
        /// </summary>
        [Required(ErrorMessage = "El sector es obligatorio")]
        [StringLength(100, ErrorMessage = "El sector no puede exceder 100 caracteres")]
        [Display(Name = "Sector")]
        public string Sector { get; set; } = string.Empty;

        /// <summary>
        /// Precio actual de la acción
        /// </summary>
        [Required(ErrorMessage = "El precio actual es obligatorio")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99, ErrorMessage = "El precio debe ser mayor que 0")]
        [Display(Name = "Precio Actual")]
        public decimal CurrentPrice { get; set; }

        /// <summary>
        /// Rentabilidad por dividendo (dividend yield) expresada en porcentaje
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 100, ErrorMessage = "El dividend yield debe estar entre 0 y 100")]
        [Display(Name = "Dividend Yield (%)")]
        public decimal DividendYield { get; set; }

        /// <summary>
        /// Fecha de creación del registro
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha de última actualización del registro
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Total invertido actualmente en la empresa
        /// Se calcula desde Operations (Compras - Ventas)
        /// Si hay operaciones cerradas, solo suma las posteriores a la última fecha de cierre
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Invertido")]
        public decimal TotalInvested { get; set; }

        /// <summary>
        /// Propiedad calculada que genera la URL de Investing.com para esta empresa
        /// No se mapea a la base de datos
        /// </summary>
        [NotMapped]
        public string InvestingUrl => $"https://www.investing.com/search/?q={Ticker}";

        /// <summary>
        /// Propiedad calculada que formatea el dividend yield con símbolo de porcentaje
        /// </summary>
        [NotMapped]
        public string FormattedDividendYield => $"{DividendYield:N2}%";
    }
}
