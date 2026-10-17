using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>Resumen de un documento para los listados (sin el contenido del archivo)</summary>
    public class PropertyDocumentInfo
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string? Description { get; set; }
        public DateTime UploadedAt { get; set; }
        public string? ExtractedCompany { get; set; }
        public decimal? ExtractedAmount { get; set; }
        public DateTime? ExtractedDate { get; set; }
        public string? ExtractedType { get; set; }
        public DateTime? AiReadAt { get; set; }
        public int? PropertyOperationId { get; set; }
    }

    /// <summary>Resultado de una operación sobre documentos</summary>
    public class PropertyDocumentResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int Id { get; set; }

        public static PropertyDocumentResult Ok(int id = 0) => new() { Success = true, Id = id };
        public static PropertyDocumentResult Fail(string message) => new() { Success = false, ErrorMessage = message };
    }

    /// <summary>
    /// Documentos adjuntos a inmuebles (SPEC-14-01)
    /// </summary>
    public interface IPropertyDocumentService
    {
        /// <summary>Tamaño máximo de un documento en bytes (10 MB)</summary>
        const long MaxFileSize = 10 * 1024 * 1024;

        Task<PropertyDocumentResult> AddAsync(int propertyId, string fileName, string? contentType, byte[] content, string? description);

        /// <summary>Lista los documentos de un inmueble, del más reciente al más antiguo, sin cargar el contenido</summary>
        Task<List<PropertyDocumentInfo>> GetByPropertyAsync(int propertyId);

        Task<long> GetTotalSizeAsync(int propertyId);

        Task<int> CountByPropertyAsync(int propertyId);

        Task<byte[]?> GetContentAsync(int documentId);

        /// <summary>Guarda los datos leídos (o corregidos a mano) de un documento</summary>
        Task<PropertyDocumentResult> UpdateExtractedDataAsync(int documentId, string? company, decimal? amount, DateTime? date, string? type, bool markAiRead);

        Task<PropertyDocumentResult> LinkOperationAsync(int documentId, int operationId);

        Task<PropertyDocumentResult> DeleteAsync(int documentId);

        /// <summary>Guarda una copia temporal y la abre con la aplicación del sistema</summary>
        Task<PropertyDocumentResult> OpenAsync(int documentId);
    }
}
