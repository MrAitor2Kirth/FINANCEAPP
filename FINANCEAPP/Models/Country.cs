using System.ComponentModel.DataAnnotations;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// País del catálogo de Configuración (SPEC-01-02). Lleva la región y la divisa
    /// que heredan las empresas y sus operaciones.
    /// </summary>
    public class Country
    {
        /// <summary>Código del país (2 o 3 letras, mayúsculas). Es la clave.</summary>
        [Key]
        [StringLength(3, MinimumLength = 2, ErrorMessage = "El código debe tener 2 o 3 letras")]
        [Display(Name = "Código")]
        public string Code { get; set; } = string.Empty;

        /// <summary>Nombre del país (único, sin distinguir mayúsculas)</summary>
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Display(Name = "Nombre")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Región: EU, ASIA, AMERICA, SUDAMERICA u OTRAS</summary>
        [Required(ErrorMessage = "La región es obligatoria")]
        [StringLength(20)]
        [Display(Name = "Región")]
        public string Region { get; set; } = "EU";

        /// <summary>Código ISO de la divisa del país (3 letras, p. ej. EUR, USD)</summary>
        [Required(ErrorMessage = "La divisa es obligatoria")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "La divisa debe tener 3 letras")]
        [Display(Name = "Divisa")]
        public string CurrencyCode { get; set; } = "EUR";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>Regiones admitidas</summary>
        public static readonly string[] Regions = { "EU", "ASIA", "AMERICA", "SUDAMERICA", "OTRAS" };
    }
}
