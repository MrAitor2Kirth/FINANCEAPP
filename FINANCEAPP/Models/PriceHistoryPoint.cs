using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Representa un punto diario de cotización histórica (OHLC) de una empresa,
    /// usado como caché local para no depender de peticiones repetidas a Yahoo Finance.
    /// </summary>
    public class PriceHistoryPoint
    {
        /// <summary>
        /// Identificador único del punto histórico
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Identificador de la empresa (CompanyDetail.Id) a la que pertenece este punto
        /// </summary>
        [Required]
        public int CompanyId { get; set; }

        /// <summary>
        /// Fecha de la sesión de mercado (sin componente horario)
        /// </summary>
        [Required]
        public DateTime Date { get; set; }

        /// <summary>
        /// Precio de apertura
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal Open { get; set; }

        /// <summary>
        /// Precio máximo de la sesión
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal High { get; set; }

        /// <summary>
        /// Precio mínimo de la sesión
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal Low { get; set; }

        /// <summary>
        /// Precio de cierre
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal Close { get; set; }

        /// <summary>
        /// Volumen negociado en la sesión
        /// </summary>
        public long Volume { get; set; }

        /// <summary>
        /// Fecha y hora en la que este punto fue guardado/actualizado en caché
        /// </summary>
        public DateTime FetchedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Dinero negociado aproximado del periodo (acciones × cierre) en la divisa de la empresa.
        /// Se calcula al leer y no se guarda (SPEC-05-03)
        /// </summary>
        [NotMapped]
        public decimal TradedValue { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public CompanyDetail? Company { get; set; }
    }
}
