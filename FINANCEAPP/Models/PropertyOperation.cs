using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Representa una operación/movimiento sobre una propiedad inmobiliaria
    /// </summary>
    public class PropertyOperation
    {
        /// <summary>
        /// Identificador único de la operación
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Nombre de la propiedad
        /// </summary>
        [Required(ErrorMessage = "El nombre de la propiedad es obligatorio")]
        [MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Categoría de la operación: "Gasto" o "Ingreso"
        /// </summary>
        [Required(ErrorMessage = "La categoría es obligatoria")]
        [MaxLength(20, ErrorMessage = "La categoría no puede exceder 20 caracteres")]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Tipo específico de operación:
        /// 
        /// GASTOS:
        /// - "Compra Inicial"
        /// - "Reforma Baño"
        /// - "Reforma Cocina"
        /// - "Reforma Integral"
        /// - "IBI"
        /// - "Comunidad"
        /// - "Seguro Hogar"
        /// - "Mantenimiento"
        /// - "Reparación"
        /// - "Hipoteca"
        /// - "Notaría/Registro"
        /// - "Otro Gasto"
        /// 
        /// INGRESOS:
        /// - "Alquiler"
        /// - "Venta"
        /// - "Indemnización Seguro"
        /// - "Otro Ingreso"
        /// </summary>
        [Required(ErrorMessage = "El tipo de operación es obligatorio")]
        [MaxLength(50, ErrorMessage = "El tipo no puede exceder 50 caracteres")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Importe de la operación
        /// Positivo para ingresos, negativo para gastos
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(-999999999.99, 999999999.99, ErrorMessage = "El importe debe estar en un rango válido")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Fecha de la operación
        /// </summary>
        [Required(ErrorMessage = "La fecha es obligatoria")]
        public DateTime Date { get; set; }

        /// <summary>
        /// Descripción adicional (ej: "Arreglo fuga baño", "Alquiler Diciembre 2024")
        /// </summary>
        [MaxLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? Description { get; set; }

        /// <summary>
        /// Fecha de creación del registro
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Indica si es un ingreso
        /// </summary>
        [NotMapped]
        public bool IsIncome => Category == "Ingreso";

        /// <summary>
        /// Indica si es un gasto
        /// </summary>
        [NotMapped]
        public bool IsExpense => Category == "Gasto";
    }
}
