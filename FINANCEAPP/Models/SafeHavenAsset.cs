using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Representa un activo refugio (Oro, Plata, Bitcoin, etc.)
    /// </summary>
    public class SafeHavenAsset
    {
        /// <summary>
        /// Identificador único del activo
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Nombre del activo (ej: "Oro Físico", "Bitcoin Core")
        /// </summary>
        [Required(ErrorMessage = "El nombre del activo es obligatorio")]
        [MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de activo: "Oro", "Plata", "Bitcoin", "Ethereum", "Otros"
        /// </summary>
        [Required(ErrorMessage = "El tipo de activo es obligatorio")]
        [MaxLength(50, ErrorMessage = "El tipo no puede exceder 50 caracteres")]
        public string AssetType { get; set; } = string.Empty;

        /// <summary>
        /// Total invertido actualmente en el activo
        /// Se calcula desde operaciones (Compras - Ventas)
        /// </summary>
        [Display(Name = "Total Invertido")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalInvested { get; set; }

        /// <summary>
        /// Valor actual del activo
        /// Se calcula desde operaciones (Compras - Ventas)
        /// </summary>
        [Display(Name = "Valor Actual")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentValue { get; set; }

        /// <summary>
        /// Fecha de creación del registro
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha de última actualización
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
