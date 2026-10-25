using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Registro histórico de una venta de acciones: guarda el beneficio generado
    /// respecto al precio medio de las compras que se han cerrado con esa venta.
    /// </summary>
    public class SaleHistory
    {
        /// <summary>
        /// Identificador único del registro (es el "número" que se muestra en pantalla)
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Id de la operación de tipo Venta que originó el registro (sin clave foránea)
        /// </summary>
        public int OperationId { get; set; }

        /// <summary>
        /// Nombre de la empresa (enlace por nombre, como el resto del proyecto)
        /// </summary>
        [Required(ErrorMessage = "El nombre de la empresa es obligatorio")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        [Display(Name = "Empresa")]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// Ticker de la empresa en el momento de la venta (puede estar vacío)
        /// </summary>
        [StringLength(20, ErrorMessage = "El ticker no puede exceder 20 caracteres")]
        [Display(Name = "Ticker")]
        public string Ticker { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de la venta
        /// </summary>
        [Required(ErrorMessage = "La fecha de la venta es obligatoria")]
        [Display(Name = "Fecha de venta")]
        public DateTime SaleDate { get; set; }

        /// <summary>
        /// Número de acciones vendidas
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(typeof(decimal), "0.01", "999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "El número de acciones debe ser mayor que 0")]
        [Display(Name = "Número de acciones")]
        public decimal NumberOfShares { get; set; }

        /// <summary>
        /// Precio medio ponderado de compra de las acciones vendidas
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Precio medio de compra")]
        public decimal AverageBuyPrice { get; set; }

        /// <summary>
        /// Precio de venta por acción (editable)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99, ErrorMessage = "El precio de venta debe ser mayor que 0")]
        [Display(Name = "Precio de venta")]
        public decimal SellPrice { get; set; }

        /// <summary>
        /// Importe cobrado: precio de venta × número de acciones
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Importe")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Comisiones descontadas del beneficio (la de la venta y la parte proporcional de las compras)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Comisiones")]
        public decimal Commission { get; set; }

        /// <summary>
        /// Beneficio generado. Se guarda; no se recalcula solo salvo al editar el precio de venta.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Beneficio")]
        public decimal Profit { get; set; }

        /// <summary>
        /// Fecha de creación del registro
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha de la última modificación del registro
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Rentabilidad de la venta en porcentaje (SPEC-02-04): beneficio ÷ (precio medio de compra × acciones) × 100,
        /// con 2 decimales. Null si el coste de compra es 0. Se calcula al leer.
        /// </summary>
        [NotMapped]
        public decimal? ReturnPercent
        {
            get
            {
                var cost = AverageBuyPrice * NumberOfShares;
                return cost > 0 ? Math.Round(Profit / cost * 100, 2) : null;
            }
        }
    }
}
