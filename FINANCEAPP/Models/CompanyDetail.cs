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
        /// Total invertido inicialmente en la empresa
        /// Se calcula sumando el importe de las operaciones de acciones abiertas (no cerradas) de la empresa
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Invertido Inicial")]
        public decimal TotalInvestedInitial { get; set; }

        /// <summary>
        /// Total acumulado de dividendos recibidos para la empresa.
        /// Suma los importes de las operaciones de tipo Dividendo.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Dividendos")]
        public decimal TotalDividends { get; set; }

        /// <summary>
        /// Rentabilidad por plusvalía expresada en porcentaje.
        /// Fórmula: ((TotalValorInvertido - TotalInvertidoInicial) / TotalInvertidoInicial) * 100
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Rentabilidad Plusvalías (%)")]
        public decimal PlusvaliaReturnPercent { get; set; }

        /// <summary>
        /// Rentabilidad con dividendos expresada en porcentaje.
        /// Fórmula: ((TotalValorInvertido + TotalDividendos - TotalInvertidoInicial) / TotalInvertidoInicial) * 100
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Rentabilidad con Dividendos (%)")]
        public decimal DividendPlusvaliaReturnPercent { get; set; }

        /// <summary>
        /// Número de acciones actualmente en cartera
        /// Se calcula sumando el número de acciones de las operaciones abiertas (no cerradas) de la empresa
        /// </summary>
        [Display(Name = "Número de Acciones")]
        public int NumberOfShares { get; set; }

        /// <summary>
        /// Propiedad calculada que multiplica el precio actual por el número de acciones
        /// No se mapea a la base de datos
        /// </summary>
        [NotMapped]
        [Display(Name = "Total Valor Invertido")]
        public decimal TotalInvestedValue => CurrentPrice * NumberOfShares;

        /// <summary>
        /// Propiedad calculada que devuelve la rentabilidad por plusvalía con formato de porcentaje.
        /// </summary>
        [NotMapped]
        public string FormattedPlusvaliaReturn => $"{PlusvaliaReturnPercent:N2}%";

        /// <summary>
        /// Propiedad calculada que devuelve la rentabilidad con dividendos con formato de porcentaje.
        /// </summary>
        [NotMapped]
        public string FormattedDividendPlusvaliaReturn => $"{DividendPlusvaliaReturnPercent:N2}%";

        /// <summary>
        /// Propiedad calculada que devuelve el importe total de dividendos con formato monetario.
        /// </summary>
        [NotMapped]
        public string FormattedTotalDividends => $"{TotalDividends:C2}";

        /// <summary>
        /// Calcula la rentabilidad por plusvalía para la empresa.
        /// </summary>
        public decimal CalculatePlusvaliaReturnPercent()
        {
            if (TotalInvestedInitial <= 0)
            {
                return 0m;
            }

            return ((TotalInvestedValue - TotalInvestedInitial) / TotalInvestedInitial) * 100m;
        }

        /// <summary>
        /// Calcula la rentabilidad total con dividendos.
        /// </summary>
        public decimal CalculateDividendPlusvaliaReturnPercent()
        {
            if (TotalInvestedInitial <= 0)
            {
                return 0m;
            }

            return (((TotalInvestedValue + TotalDividends) - TotalInvestedInitial) / TotalInvestedInitial) * 100m;
        }

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
