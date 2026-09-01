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
        [Display(Name = "Número de Acciones")]
        public int NumberOfShares { get; set; }

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
