using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Representa una operación sobre un activo refugio
    /// </summary>
    public class SafeHavenOperation
    {
        /// <summary>
        /// Identificador único de la operación
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Nombre del activo
        /// </summary>
        [Required(ErrorMessage = "El nombre del activo es obligatorio")]
        [MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string AssetName { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de operación:
        /// - "Compra" (adquisición)
        /// - "Venta" (liquidación)
        /// </summary>
        [Required(ErrorMessage = "El tipo de operación es obligatorio")]
        [MaxLength(50, ErrorMessage = "El tipo no puede exceder 50 caracteres")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Importe de la operación
        /// Positivo para Compra
        /// Negativo para Venta
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99, ErrorMessage = "El importe debe ser mayor que 0")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Fecha de la operación
        /// </summary>
        [Required(ErrorMessage = "La fecha es obligatoria")]
        public DateTime Date { get; set; }

        /// <summary>
        /// Fecha de creación del registro
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Indica si es una compra
        /// </summary>
        [NotMapped]
        public bool IsPurchase => Type == "Compra";

        /// <summary>
        /// Indica si es una venta
        /// </summary>
        [NotMapped]
        public bool IsSale => Type == "Venta";
    }
}
