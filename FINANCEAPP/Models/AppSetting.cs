using System.ComponentModel.DataAnnotations;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Ajuste de la aplicación guardado como clave/valor (por ejemplo, el logo).
    /// </summary>
    public class AppSetting
    {
        /// <summary>
        /// Clave del ajuste (la fila del logo usa "AppLogo")
        /// </summary>
        [Key]
        [StringLength(100, ErrorMessage = "La clave no puede exceder 100 caracteres")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Valor de texto del ajuste; en el logo, el tipo de contenido (image/png)
        /// </summary>
        [StringLength(200, ErrorMessage = "El valor no puede exceder 200 caracteres")]
        public string? TextValue { get; set; }

        /// <summary>
        /// Valor binario del ajuste; en el logo, la imagen
        /// </summary>
        public byte[]? BinaryValue { get; set; }

        /// <summary>
        /// Fecha de la última modificación del ajuste
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
