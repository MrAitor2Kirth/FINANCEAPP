using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Representa una operación sobre un fondo de inversión
    /// </summary>
    public class FundOperation
    {
        /// <summary>
        /// Identificador único de la operación
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Nombre del fondo al que pertenece la operación
        /// </summary>
        [Required(ErrorMessage = "El nombre del fondo es obligatorio")]
        [MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string FundName { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de operación:
        /// - "Aportación" (compra/entrada de dinero)
        /// - "Reembolso" (venta/salida de dinero)
        /// - "Traspaso Entrada" (desde otro fondo)
        /// - "Traspaso Salida" (hacia otro fondo)
        /// - "Plusvalía" (ajuste automático mensual - solo puede haber uno activo)
        /// </summary>
        [Required(ErrorMessage = "El tipo de operación es obligatorio")]
        [MaxLength(50, ErrorMessage = "El tipo no puede exceder 50 caracteres")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Importe de la operación
        /// Positivo para Aportación/Traspaso Entrada
        /// Negativo para Reembolso/Traspaso Salida
        /// Diferencia para Plusvalía
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
        /// Fecha de creación del registro
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Indica si es una operación de tipo Plusvalía
        /// </summary>
        [NotMapped]
        public bool IsPlusvaliaOperation => Type == "Plusvalía";

        /// <summary>
        /// Indica si es una operación de entrada de dinero
        /// </summary>
        [NotMapped]
        public bool IsIncome => Type == "Aportación" || Type == "Traspaso Entrada";

        /// <summary>
        /// Indica si es una operación de salida de dinero
        /// </summary>
        [NotMapped]
        public bool IsExpense => Type == "Reembolso" || Type == "Traspaso Salida";
    }
}
