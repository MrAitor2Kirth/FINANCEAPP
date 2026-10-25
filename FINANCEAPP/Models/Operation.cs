using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Modelo que representa una operación financiera (compra/venta de acciones)
    /// </summary>
    public class Operation
    {
        /// <summary>
        /// Identificador único de la operación
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Nombre de la empresa asociada a la operación
        /// </summary>
        [Required(ErrorMessage = "El nombre de la empresa es obligatorio")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de apertura de la operación
        /// </summary>
        [Required(ErrorMessage = "La fecha es obligatoria")]
        [Display(Name = "Fecha de Apertura")]
        public DateTime Date { get; set; }

        /// <summary>
        /// Fecha de cierre de la operación (nullable - puede estar abierta)
        /// </summary>
        [Display(Name = "Fecha de Cierre")]
        public DateTime? CloseDate { get; set; }

        /// <summary>
        /// Precio por acción en el momento de la operación
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99, ErrorMessage = "El precio por acción debe ser mayor o igual a 0")]
        [Display(Name = "Precio por Acción")]
        public decimal PricePerShare { get; set; }

        /// <summary>
        /// Número de acciones compradas o vendidas
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(typeof(decimal), "0", "999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "El número de acciones no puede ser negativo")]
        [Display(Name = "Número de Acciones")]
        public decimal NumberOfShares { get; set; }

        /// <summary>
        /// Importe de comisiones asociadas a la operación
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99, ErrorMessage = "Las comisiones deben ser mayor o igual a 0")]
        [Display(Name = "Comisiones")]
        public decimal Commission { get; set; }

        /// <summary>
        /// Importe de la operación (PricePerShare × NumberOfShares + Commission)
        /// </summary>
        [Required(ErrorMessage = "El importe es obligatorio")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99, ErrorMessage = "El importe debe ser mayor que 0")]
        [Display(Name = "Importe")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Tipo de operación (ej: "Compra", "Venta", "Dividendo", etc.)
        /// </summary>
        [Required(ErrorMessage = "El tipo de operación es obligatorio")]
        [StringLength(50, ErrorMessage = "El tipo no puede exceder 50 caracteres")]
        [Display(Name = "Tipo")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de creación del registro
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Id del registro de SalesHistory que cerró esta compra o traspaso (sin clave foránea).
        /// Vacío si está abierta o se cerró a mano. Se limpia al deshacer la venta.
        /// </summary>
        public int? SaleHistoryId { get; set; }

        /// <summary>
        /// Id de la fila original de un lote partido por una venta parcial (sin clave foránea).
        /// Lo comparten todas las filas que salen del mismo lote; permite volver a unirlas al deshacer.
        /// </summary>
        public int? OriginOperationId { get; set; }

        /// <summary>
        /// Divisa de la operación (SPEC-01-02): la de la empresa cuando se registró. Nulo = EUR
        /// (operaciones anteriores a la feature o de empresas sin país).
        /// </summary>
        [StringLength(3)]
        [Display(Name = "Cód. divisa")]
        public string? CurrencyCode { get; set; }

        /// <summary>
        /// Tipo de cambio guardado en la operación (SPEC-01-03): unidades de ExchangeRateTo por 1 unidad
        /// de la divisa de la operación. Nulo en empresas de la divisa base y en operaciones sin tipo.
        /// Solo se usa en «Detalles»; los totales usan el tipo actual.
        /// </summary>
        [Column(TypeName = "decimal(18,6)")]
        [Display(Name = "Tipo de cambio")]
        public decimal? ExchangeRate { get; set; }

        /// <summary>
        /// Divisa destino del tipo de cambio guardado (la divisa base cuando se guardó). Nulo si no hay tipo.
        /// </summary>
        [StringLength(3)]
        public string? ExchangeRateTo { get; set; }

        /// <summary>
        /// Divisa efectiva de la operación (EUR si no tiene). No se mapea a la base de datos.
        /// </summary>
        [NotMapped]
        public string EffectiveCurrencyCode => FINANCEAPP.Services.CurrencyFormatter.Normalize(CurrencyCode);

        /// <summary>
        /// Propiedad calculada que indica si la operación está cerrada
        /// </summary>
        [NotMapped]
        public bool IsClosed => CloseDate.HasValue;

        /// <summary>
        /// Propiedad calculada que devuelve el estado de la operación
        /// </summary>
        [NotMapped]
        public string Status => IsClosed ? "Cerrada" : "Abierta";
    }
}
