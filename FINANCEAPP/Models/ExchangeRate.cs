using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Tipo de cambio actual entre una divisa y la divisa base (SPEC-01-03).
    /// 1 unidad de CurrencyCode equivale a Rate unidades de BaseCurrencyCode.
    /// La clave es compuesta (CurrencyCode + BaseCurrencyCode): si cambia el país base, los tipos
    /// antiguos no se mezclan con los nuevos.
    /// </summary>
    public class ExchangeRate
    {
        /// <summary>Divisa de origen (por ejemplo USD)</summary>
        [Required]
        [StringLength(3)]
        public string CurrencyCode { get; set; } = string.Empty;

        /// <summary>Divisa destino, la divisa base en el momento de guardar el tipo (por ejemplo EUR)</summary>
        [Required]
        [StringLength(3)]
        public string BaseCurrencyCode { get; set; } = string.Empty;

        /// <summary>Unidades de la divisa base por 1 unidad de la divisa de origen (6 decimales)</summary>
        [Column(TypeName = "decimal(18,6)")]
        public decimal Rate { get; set; }

        /// <summary>Origen del tipo: YAHOO o MANUAL</summary>
        [Required]
        [StringLength(10)]
        public string Source { get; set; } = SourceManual;

        /// <summary>Fecha y hora de la consulta a Yahoo o del guardado manual</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>Valor de Source para los tipos traídos de Yahoo</summary>
        public const string SourceYahoo = "YAHOO";

        /// <summary>Valor de Source para los tipos escritos a mano</summary>
        public const string SourceManual = "MANUAL";

        /// <summary>Texto del origen para pantalla (Yahoo o manual)</summary>
        [NotMapped]
        public string SourceLabel => Source == SourceYahoo ? "Yahoo" : "Manual";
    }
}
