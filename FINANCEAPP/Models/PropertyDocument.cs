using System.ComponentModel.DataAnnotations;

namespace FINANCEAPP.Models
{
    /// <summary>
    /// Documento (recibo, factura...) adjunto a un inmueble (SPEC-14-01).
    /// Se vincula por PropertyId (sin clave foránea) y se borra con el inmueble desde PropertyService.
    /// </summary>
    public class PropertyDocument
    {
        public int Id { get; set; }

        /// <summary>Id del inmueble al que pertenece</summary>
        public int PropertyId { get; set; }

        [Required]
        [MaxLength(200)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = "application/octet-stream";

        public long SizeBytes { get; set; }

        /// <summary>Contenido del archivo. No se carga en los listados</summary>
        public byte[] Content { get; set; } = Array.Empty<byte>();

        [MaxLength(200)]
        public string? Description { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // Datos leídos por la IA (o corregidos a mano)
        [MaxLength(200)]
        public string? ExtractedCompany { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "decimal(18,2)")]
        public decimal? ExtractedAmount { get; set; }

        public DateTime? ExtractedDate { get; set; }

        [MaxLength(50)]
        public string? ExtractedType { get; set; }

        /// <summary>Fecha de la última lectura con IA (null = sin leer)</summary>
        public DateTime? AiReadAt { get; set; }

        /// <summary>Gasto creado a partir del documento (null = ninguno)</summary>
        public int? PropertyOperationId { get; set; }
    }
}
