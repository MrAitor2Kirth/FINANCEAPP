using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Historial de valores de un fondo (para Opción B de gestión de plusvalías)
    /// Mantiene un registro histórico de los valores que ha tenido un fondo
    /// </summary>
    public class FundValueHistory
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Nombre del fondo
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string FundName { get; set; } = string.Empty;

        /// <summary>
        /// Valor de la inversión en la fecha del registro
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Value { get; set; }

        /// <summary>
        /// Fecha en que se registró este valor
        /// </summary>
        [Required]
        public DateTime Date { get; set; }

        /// <summary>
        /// Rentabilidad calculada en esa fecha: ((Value - TotalInvested) / TotalInvested) * 100
        /// Se almacena para referencia histórica
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal ReturnAtDate { get; set; }

        /// <summary>
        /// Fecha de creación del registro
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Descripción opcional del cambio de valor (ej: "Actualización mensual", "Revisión trimestral")
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
