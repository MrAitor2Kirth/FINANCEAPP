using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Cierre diario de un par de divisas (SPEC-01-09), con las mismas reglas de caché que PriceHistoryPoint.
    /// Sin clave foránea con CurrencyPair.
    /// </summary>
    public class CurrencyRatePoint
    {
        /// <summary>Identificador único del punto</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Par al que pertenece (CurrencyPair.Id)</summary>
        [Required]
        public int CurrencyPairId { get; set; }

        /// <summary>Fecha de la sesión (sin hora)</summary>
        [Required]
        public DateTime Date { get; set; }

        /// <summary>Tipo de apertura</summary>
        [Column(TypeName = "decimal(18,6)")]
        public decimal Open { get; set; }

        /// <summary>Tipo máximo de la sesión</summary>
        [Column(TypeName = "decimal(18,6)")]
        public decimal High { get; set; }

        /// <summary>Tipo mínimo de la sesión</summary>
        [Column(TypeName = "decimal(18,6)")]
        public decimal Low { get; set; }

        /// <summary>Tipo de cierre: el que se usa para convertir en esa fecha</summary>
        [Column(TypeName = "decimal(18,6)")]
        public decimal Close { get; set; }

        /// <summary>Volumen (Yahoo da 0 en las divisas)</summary>
        public long Volume { get; set; }

        /// <summary>Fecha y hora en la que se guardó o actualizó en caché</summary>
        public DateTime FetchedAt { get; set; } = DateTime.Now;
    }
}
