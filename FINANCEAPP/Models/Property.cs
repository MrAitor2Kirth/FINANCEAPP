using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Representa una propiedad inmobiliaria
    /// </summary>
    public class Property
    {
        /// <summary>
        /// Identificador único de la propiedad
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Nombre identificativo (ej: "Piso Madrid Centro", "Casa Playa Alicante")
        /// </summary>
        [Required(ErrorMessage = "El nombre de la propiedad es obligatorio")]
        [MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de inmueble: "Vivienda", "Local Comercial", "Parking", "Terreno", "Oficina"
        /// </summary>
        [Required(ErrorMessage = "El tipo de propiedad es obligatorio")]
        [MaxLength(50, ErrorMessage = "El tipo no puede exceder 50 caracteres")]
        public string PropertyType { get; set; } = string.Empty;

        /// <summary>
        /// Dirección completa
        /// </summary>
        [MaxLength(500, ErrorMessage = "La dirección no puede exceder 500 caracteres")]
        public string? Address { get; set; }

        /// <summary>
        /// Precio de compra inicial
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99, ErrorMessage = "El precio de compra debe ser mayor que 0")]
        public decimal PurchasePrice { get; set; }

        /// <summary>
        /// Fecha de compra
        /// </summary>
        [Required(ErrorMessage = "La fecha de compra es obligatoria")]
        public DateTime PurchaseDate { get; set; }

        /// <summary>
        /// Valoración actual estimada (se actualiza manualmente)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CurrentValue { get; set; }

        /// <summary>
        /// Estado actual: "Alquilado", "Vacío", "Uso Propio", "En Venta", "Vendida"
        /// </summary>
        [Required(ErrorMessage = "El estado es obligatorio")]
        [MaxLength(50, ErrorMessage = "El estado no puede exceder 50 caracteres")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Alquiler mensual (si está alquilado)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MonthlyRent { get; set; }

        /// <summary>
        /// Fecha de venta (si Status = "Vendida")
        /// </summary>
        public DateTime? SaleDate { get; set; }

        /// <summary>
        /// Total invertido = Precio Compra + Suma de Gastos
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalInvested { get; set; }

        /// <summary>
        /// Total de ingresos = Suma de Alquileres + Venta (si aplica)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalIncome { get; set; }

        /// <summary>
        /// Fecha de creación del registro
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha de última actualización
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Resultado neto = TotalIncome - TotalInvested
        /// </summary>
        [NotMapped]
        public decimal NetResult => TotalIncome - TotalInvested;

        /// <summary>
        /// Resultado neto formateado para visualización
        /// </summary>
        [NotMapped]
        public string FormattedNetResult => NetResult >= 0
            ? $"+{NetResult:C}"
            : $"{NetResult:C}";

        /// <summary>
        /// Indica si la propiedad está vendida
        /// </summary>
        [NotMapped]
        public bool IsSold => Status == "Vendida" && SaleDate.HasValue;

        /// <summary>
        /// Indica si la propiedad está alquilada
        /// </summary>
        [NotMapped]
        public bool IsRented => Status == "Alquilado";
    }
}
