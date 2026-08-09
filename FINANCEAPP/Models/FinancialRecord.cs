using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Modelo de registro financiero que almacena todas las transacciones
    /// y balances del usuario
    /// </summary>
    public class FinancialRecord
    {
        /// <summary>
        /// Identificador único del registro
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Fecha del registro financiero
        /// </summary>
        [Required]
        public DateTime Date { get; set; }

        /// <summary>
        /// Valor de acciones en el portafolio
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Stocks { get; set; }

        /// <summary>
        /// Dividendos recibidos
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Dividends { get; set; }

        /// <summary>
        /// Plusvalías obtenidas (capital gains)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal CapitalGains { get; set; }

        /// <summary>
        /// Beneficios totales generados
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Profits { get; set; }

        /// <summary>
        /// Fondos de inversión
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Funds { get; set; }

        /// <summary>
        /// Activos refugio (oro, bonos, etc.)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SafeHaven { get; set; }

        /// <summary>
        /// Capital inicial invertido
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal InitialCapital { get; set; }

        /// <summary>
        /// Saldo en cuenta corriente/bancaria
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal BankAccount { get; set; }

        /// <summary>
        /// Total general de todos los activos
        /// Calculado automáticamente sumando todos los componentes
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total => Stocks + Dividends + CapitalGains + Profits + Funds + SafeHaven + InitialCapital + BankAccount;

        /// <summary>
        /// Fecha de creación del registro en la base de datos
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Notas o comentarios adicionales sobre el registro
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
