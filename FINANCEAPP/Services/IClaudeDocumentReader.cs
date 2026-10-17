namespace FINANCEAPP.Services
{
    /// <summary>Datos que la IA ha leído de una factura o recibo</summary>
    public class DocumentReadResult
    {
        public bool Success { get; set; }

        /// <summary>Mensaje en español cuando Success es false</summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>Aviso (p. ej. el documento no parece una factura)</summary>
        public string? Warning { get; set; }

        public string? Company { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? Date { get; set; }
        public string? Type { get; set; }
    }

    /// <summary>
    /// Lee facturas y recibos con la API de Claude (SPEC-14-01). Solo se llama tras la confirmación del usuario.
    /// </summary>
    public interface IClaudeDocumentReader
    {
        /// <summary>Indica si el tipo de archivo se puede leer (imagen jpeg/png/gif/webp o PDF)</summary>
        bool CanRead(string contentType);

        Task<DocumentReadResult> ReadAsync(byte[] content, string contentType, CancellationToken ct = default);
    }
}
