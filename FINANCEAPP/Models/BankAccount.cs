using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Representa una cuenta bancaria
    /// </summary>
    public class BankAccount
    {
        /// <summary>
        /// Identificador único de la cuenta
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Nombre identificativo de la cuenta (ej: "Cuenta Santander", "Cuenta BBVA Nómina")
        /// </summary>
        [Required(ErrorMessage = "El nombre de la cuenta es obligatorio")]
        [MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Entidad bancaria (ej: "Santander", "BBVA", "ING")
        /// </summary>
        [Required(ErrorMessage = "El nombre del banco es obligatorio")]
        [MaxLength(100, ErrorMessage = "El nombre del banco no puede exceder 100 caracteres")]
        public string BankName { get; set; } = string.Empty;

        /// <summary>
        /// IBAN de la cuenta (opcional)
        /// </summary>
        [MaxLength(34, ErrorMessage = "El IBAN no puede exceder 34 caracteres")]
        public string? IBAN { get; set; }

        /// <summary>
        /// Tipo de cuenta: "Corriente", "Ahorro", "Depósito", "Cuenta Remunerada"
        /// </summary>
        [Required(ErrorMessage = "El tipo de cuenta es obligatorio")]
        [MaxLength(50, ErrorMessage = "El tipo no puede exceder 50 caracteres")]
        public string AccountType { get; set; } = string.Empty;

        /// <summary>
        /// Saldo actual de la cuenta
        /// Se calcula desde operaciones
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentBalance { get; set; }

        /// <summary>
        /// Fecha de creación del registro
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha de última actualización
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Saldo formateado para visualización
        /// </summary>
        [NotMapped]
        public string FormattedBalance => CurrentBalance >= 0
            ? $"{CurrentBalance:C}"
            : $"-{Math.Abs(CurrentBalance):C}";
    }
}
