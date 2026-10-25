using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Línea del calendario de dividendos de una empresa (SPEC-21-01): un pago de dividendo pasado o futuro.
    /// Se enlaza a la empresa por CompanyId y, cuando se cobra, a la operación «Dividendo» generada por OperationId.
    /// Estados y orígenes son literales en español (ver DividendPaymentRules).
    /// </summary>
    public class DividendPayment
    {
        /// <summary>Identificador único de la línea</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Empresa (CompanyDetail.Id)</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Elige la empresa")]
        public int CompanyId { get; set; }

        /// <summary>Fecha de pago (sin hora); puede ser pasada o futura</summary>
        [Required(ErrorMessage = "La fecha de pago es obligatoria")]
        [Display(Name = "Fecha de pago")]
        public DateTime PaymentDate { get; set; }

        /// <summary>Fecha ex-dividendo; si se informa, no puede ser posterior a la de pago</summary>
        [Display(Name = "Fecha ex-dividendo")]
        public DateTime? ExDividendDate { get; set; }

        /// <summary>Importe por acción en la divisa de la empresa, con hasta 4 decimales (excepción a decimal(18,2))</summary>
        [Column(TypeName = "decimal(18,4)")]
        [Range(typeof(decimal), "0.0001", "1000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "El importe por acción debe ser mayor que 0")]
        [Display(Name = "Importe por acción")]
        public decimal? AmountPerShare { get; set; }

        /// <summary>Número de acciones (se propone con las de la cartera y es editable)</summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(typeof(decimal), "0", "999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "El número de acciones no puede ser negativo")]
        [Display(Name = "Acciones")]
        public decimal NumberOfShares { get; set; }

        /// <summary>% dividendo; en blanco si no se conoce</summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(typeof(decimal), "0", "100", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "El % dividendo debe estar entre 0 y 100")]
        [Display(Name = "% dividendo")]
        public decimal? DividendPercent { get; set; }

        /// <summary>"Pendiente" o "Cobrado"</summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pendiente";

        /// <summary>Operación «Dividendo» generada desde la línea (Operations.Id)</summary>
        public int? OperationId { get; set; }

        /// <summary>"Manual", "Ficha" o "Búsqueda"</summary>
        [Required]
        [StringLength(20)]
        public string Source { get; set; } = "Manual";

        /// <summary>Notas (p. ej. la fuente de la fecha)</summary>
        [StringLength(200, ErrorMessage = "Las notas no pueden superar los 200 caracteres")]
        public string? Notes { get; set; }

        /// <summary>Fecha de creación</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>Fecha de la última modificación</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>Importe total = acciones × importe por acción, redondeado a 2 decimales (lo rellena el servicio al leer)</summary>
        [NotMapped]
        public decimal? TotalAmount { get; set; }

        /// <summary>Nombre de la empresa (para la interfaz)</summary>
        [NotMapped]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>Divisa de la empresa (para la interfaz)</summary>
        [NotMapped]
        public string CurrencyCode { get; set; } = FINANCEAPP.Services.CurrencyFormatter.DefaultCurrency;
    }
}
