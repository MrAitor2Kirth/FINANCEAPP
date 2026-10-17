using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Empresa del catálogo de mercado (SPEC-01-04): una fila por ticker con su nombre, el país del
    /// mercado donde cotiza y los índices a los que pertenece. Es de solo lectura: la carga la hace
    /// <c>DatabaseSchemaUpgrader.EnsureMarketCatalogTable</c> a partir de <c>MarketCatalogData</c>.
    /// </summary>
    public class MarketCompany
    {
        /// <summary>Ticker en formato Yahoo Finance (por ejemplo ACS.MC). Es la clave.</summary>
        [Key]
        [StringLength(20)]
        public string Ticker { get; set; } = string.Empty;

        /// <summary>Nombre de la empresa</summary>
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Código del país del mercado donde cotiza el ticker (de él depende la divisa). Sin clave foránea.
        /// </summary>
        [Required]
        [StringLength(3)]
        public string CountryCode { get; set; } = string.Empty;

        /// <summary>Códigos de los índices a los que pertenece, separados por comas (por ejemplo IBEX35,EUROSTOXX50)</summary>
        [Required]
        [StringLength(100)]
        public string Indices { get; set; } = string.Empty;

        /// <summary>Códigos de índice ya separados. No se mapea a la base de datos.</summary>
        [NotMapped]
        public IEnumerable<string> IndexCodes =>
            Indices.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
