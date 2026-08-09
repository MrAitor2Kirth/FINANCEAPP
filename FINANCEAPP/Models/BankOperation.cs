using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Representa un movimiento en una cuenta bancaria
    /// </summary>
    public class BankOperation
    {
        /// <summary>
        /// Identificador único de la operación
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Nombre de la cuenta bancaria
        /// </summary>
        [Required(ErrorMessage = "El nombre de la cuenta es obligatorio")]
        [MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string AccountName { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de operación:
        /// - "Ingreso" (nómina, transferencia recibida)
        /// - "Gasto" (pago, transferencia enviada)
        /// - "Intereses" (intereses generados por la cuenta)
        /// - "Comisiones" (comisiones bancarias)
        /// - "Traspaso Entrada" (desde otra cuenta propia)
        /// - "Traspaso Salida" (hacia otra cuenta propia)
        /// </summary>
        [Required(ErrorMessage = "El tipo de operación es obligatorio")]
        [MaxLength(50, ErrorMessage = "El tipo no puede exceder 50 caracteres")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Importe del movimiento
        /// Positivo para Ingreso/Intereses/Traspaso Entrada
        /// Negativo para Gasto/Comisiones/Traspaso Salida
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(-999999999.99, 999999999.99, ErrorMessage = "El importe debe estar en un rango válido")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Fecha del movimiento
        /// </summary>
        [Required(ErrorMessage = "La fecha es obligatoria")]
        public DateTime Date { get; set; }

        /// <summary>
        /// Concepto/descripción del movimiento
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
        public bool IsIncome => Type == "Ingreso" || Type == "Intereses" || Type == "Traspaso Entrada";

        /// <summary>
        /// Indica si es un gasto
        /// </summary>
        [NotMapped]
        public bool IsExpense => Type == "Gasto" || Type == "Comisiones" || Type == "Traspaso Salida";
    }
}
