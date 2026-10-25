using System.ComponentModel.DataAnnotations;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Índice (o cualquier símbolo de Yahoo) dado de alta para comparar con las empresas en el gráfico (SPEC-05-04).
    /// Su histórico diario se guarda en BenchmarkPricePoints.
    /// </summary>
    public class BenchmarkIndex
    {
        /// <summary>Identificador único del índice</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Nombre que se muestra (p. ej. «IBEX 35»)</summary>
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Símbolo de Yahoo Finance (p. ej. «^IBEX»); único</summary>
        [Required(ErrorMessage = "El símbolo de Yahoo es obligatorio")]
        [StringLength(20, ErrorMessage = "El símbolo no puede superar los 20 caracteres")]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>Fecha de creación</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>Fecha de la última modificación</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
