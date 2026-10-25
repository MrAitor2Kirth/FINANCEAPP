using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Punto diario de cotización (OHLC) de un índice (SPEC-05-04), con las mismas reglas de caché que
    /// PriceHistoryPoint. Sin clave foránea: la baja del índice borra sus puntos desde BenchmarkIndexService.
    /// </summary>
    public class BenchmarkPricePoint
    {
        /// <summary>Identificador único del punto</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Índice al que pertenece (BenchmarkIndex.Id)</summary>
        [Required]
        public int BenchmarkIndexId { get; set; }

        /// <summary>Fecha de la sesión (sin hora)</summary>
        [Required]
        public DateTime Date { get; set; }

        /// <summary>Valor de apertura</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal Open { get; set; }

        /// <summary>Valor máximo de la sesión</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal High { get; set; }

        /// <summary>Valor mínimo de la sesión</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal Low { get; set; }

        /// <summary>Valor de cierre</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal Close { get; set; }

        /// <summary>Volumen de la sesión (Yahoo no lo da en todos los índices)</summary>
        public long Volume { get; set; }

        /// <summary>Fecha y hora en la que se guardó o actualizó en caché</summary>
        public DateTime FetchedAt { get; set; } = DateTime.Now;
    }
}
