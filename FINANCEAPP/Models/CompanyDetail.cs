using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Modelo que representa los detalles de una empresa
    /// Contiene informaci├│n relevante para an├ílisis de inversi├│n
    /// </summary>
    public class CompanyDetail
    {
        /// <summary>
        /// Identificador ├║nico de la empresa
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
        /// S├¡mbolo o ticker de cotizaci├│n en bolsa
        /// </summary>
        [Required(ErrorMessage = "El ticker es obligatorio")]
        [StringLength(20, ErrorMessage = "El ticker no puede exceder 20 caracteres")]
        [Display(Name = "Ticker")]
        public string Ticker { get; set; } = string.Empty;

        /// <summary>
        /// Sector econ├│mico al que pertenece la empresa
        /// </summary>
        [Required(ErrorMessage = "El sector es obligatorio")]
        [StringLength(100, ErrorMessage = "El sector no puede exceder 100 caracteres")]
        [Display(Name = "Sector")]
        public string Sector { get; set; } = string.Empty;

        /// <summary>
        /// Precio actual de la acci├│n
        /// </summary>
        [Required(ErrorMessage = "El precio actual es obligatorio")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99, ErrorMessage = "El precio debe ser mayor que 0")]
        [Display(Name = "Precio Actual")]
        public decimal CurrentPrice { get; set; }

        /// <summary>
        /// Precio en tiempo real recuperado desde Yahoo Finance.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Precio en Vivo")]
        public decimal? LivePrice { get; set; }

        /// <summary>
        /// Fecha y hora de la última actualización correcta del LivePrice.
        /// </summary>
        [Display(Name = "Última actualización precio vivo")]
        public DateTime? LivePriceUpdatedAt { get; set; }

        /// <summary>
        /// Error de la última consulta de precio, solo para UI, no se persiste en BD.
        /// </summary>
        [NotMapped]
        [Display(Name = "Error de precio en vivo")]
        public string? LivePriceError { get; set; }

        /// <summary>
        /// Rentabilidad por dividendo (dividend yield) expresada en porcentaje
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 100, ErrorMessage = "El dividend yield debe estar entre 0 y 100")]
        [Display(Name = "Dividend Yield (%)")]
        public decimal DividendYield { get; set; }

        /// <summary>
        /// Fecha de creaci├│n del registro
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha de ├║ltima actualizaci├│n del registro
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
        /// Rentabilidad por plusval├¡a expresada en porcentaje.
        /// F├│rmula: ((TotalValorInvertido - TotalInvertidoInicial) / TotalInvertidoInicial) * 100
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Rentabilidad Plusval├¡as (%)")]
        public decimal PlusvaliaReturnPercent { get; set; }

        /// <summary>
        /// Rentabilidad con dividendos expresada en porcentaje.
        /// F├│rmula: ((TotalValorInvertido + TotalDividendos - TotalInvertidoInicial) / TotalInvertidoInicial) * 100
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Rentabilidad con Dividendos (%)")]
        public decimal DividendPlusvaliaReturnPercent { get; set; }

        /// <summary>
        /// N├║mero de acciones actualmente en cartera
        /// Se calcula sumando el n├║mero de acciones de las operaciones abiertas (no cerradas) de la empresa
        /// </summary>
        [Display(Name = "N├║mero de Acciones")]
        public int NumberOfShares { get; set; }

        /// <summary>
        /// Propiedad calculada que multiplica el precio actual por el n├║mero de acciones
        /// No se mapea a la base de datos
        /// </summary>
        [NotMapped]
        [Display(Name = "Total Valor Invertido")]
        public decimal TotalInvestedValue => (LivePrice ?? CurrentPrice) * NumberOfShares;

        /// <summary>
        /// Precio medio de compra: total invertido inicial dividido entre el numero de acciones.
        /// Es null cuando no hay acciones en cartera o el total invertido es cero o negativo.
        /// No se mapea a la base de datos
        /// </summary>
        [NotMapped]
        [Display(Name = "Precio Medio de Compra")]
        public decimal? AveragePurchasePrice =>
            NumberOfShares > 0 && TotalInvestedInitial > 0
                ? TotalInvestedInitial / NumberOfShares
                : null;

        /// <summary>
        /// Código del país de la empresa (SPEC-01-02). Nulo en empresas antiguas sin país.
        /// </summary>
        [StringLength(3)]
        [Display(Name = "País")]
        public string? CountryCode { get; set; }

        /// <summary>
        /// Divisa de la empresa, la de su país (EUR si no tiene país). Solo para la interfaz,
        /// la rellena CompanyService; no se persiste.
        /// </summary>
        [NotMapped]
        public string CurrencyCode { get; set; } = FINANCEAPP.Services.CurrencyFormatter.DefaultCurrency;

        /// <summary>
        /// Propiedad calculada que devuelve la rentabilidad por plusval├¡a con formato de porcentaje.
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
        public string FormattedTotalDividends => FINANCEAPP.Services.CurrencyFormatter.Format(TotalDividends, CurrencyCode);

        /// <summary>
        /// Calcula la rentabilidad por plusval├¡a para la empresa.
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
        /// Propiedad calculada que formatea el dividend yield con s├¡mbolo de porcentaje
        /// </summary>
        [NotMapped]
        public string FormattedDividendYield => $"{DividendYield:N2}%";
    }
}
