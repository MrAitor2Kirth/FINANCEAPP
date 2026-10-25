using System.ComponentModel.DataAnnotations;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Par de divisas con histórico diario de tipos de cambio (SPEC-01-09): 1 unidad de CurrencyCode equivale al cierre
    /// en unidades de BaseCurrencyCode (mismo sentido que ExchangeRate). Sus cierres están en CurrencyRatePoints.
    /// </summary>
    public class CurrencyPair
    {
        /// <summary>Identificador único del par</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Divisa de origen (por ejemplo USD)</summary>
        [Required]
        [StringLength(3)]
        public string CurrencyCode { get; set; } = string.Empty;

        /// <summary>Divisa base (por ejemplo EUR)</summary>
        [Required]
        [StringLength(3)]
        public string BaseCurrencyCode { get; set; } = string.Empty;

        /// <summary>Símbolo de Yahoo del par (por ejemplo USDEUR=X)</summary>
        [Required]
        [StringLength(20)]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>Fecha de creación</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
