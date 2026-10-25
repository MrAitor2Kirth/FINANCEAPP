using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Marca temporal del gráfico de cotización (SPEC-05-03): una fecha con una descripción que se dibuja
    /// como punto marrón. Es de una empresa (solo se ve en su gráfico) o global (CompanyId nulo: se ve en todas).
    /// </summary>
    public class ChartMark
    {
        /// <summary>Identificador único de la marca</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Empresa de la marca (CompanyDetail.Id); null en las marcas globales</summary>
        public int? CompanyId { get; set; }

        /// <summary>Fecha de la marca (sin hora; no puede ser futura)</summary>
        [Required(ErrorMessage = "La fecha es obligatoria")]
        public DateTime Date { get; set; }

        /// <summary>Descripción que se muestra al pasar el ratón</summary>
        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(200, ErrorMessage = "La descripción no puede superar los 200 caracteres")]
        public string Description { get; set; } = string.Empty;

        /// <summary>Si es false la marca no se dibuja, pero sigue en el panel de marcas</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Fecha de creación</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>Fecha de la última modificación</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>true si la marca es global (se ve en el gráfico de todas las empresas)</summary>
        [NotMapped]
        public bool IsGlobal => CompanyId == null;
    }
}
