using FINANCEAPP.Data;
using FINANCEAPP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Implementación del servicio de documentos de inmuebles (SPEC-14-01)
    /// </summary>
    public class PropertyDocumentService : IPropertyDocumentService
    {
        private static readonly string[] BlockedExtensions =
        {
            ".exe", ".bat", ".cmd", ".com", ".msi", ".ps1", ".vbs", ".vbe", ".js", ".jse", ".wsf", ".scr", ".lnk", ".jar", ".reg", ".dll"
        };

        private static readonly string[] ExpenseTypes = { "Comunidad", "IBI", "Hipoteca", "Reforma", "Seguro", "Servicios" };

        private readonly AppDbContext _context;
        private readonly ILogger<PropertyDocumentService> _logger;

        public PropertyDocumentService(AppDbContext context, ILogger<PropertyDocumentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PropertyDocumentResult> AddAsync(int propertyId, string fileName, string? contentType, byte[] content, string? description)
        {
            var name = Path.GetFileName((fileName ?? string.Empty).Trim());
            if (string.IsNullOrEmpty(name))
            {
                return PropertyDocumentResult.Fail("El archivo no tiene nombre.");
            }

            if (name.Length > 200)
            {
                name = name[^200..];
            }

            if (content == null || content.Length == 0)
            {
                return PropertyDocumentResult.Fail("El archivo está vacío.");
            }

            if (content.Length > IPropertyDocumentService.MaxFileSize)
            {
                return PropertyDocumentResult.Fail("El archivo supera el tamaño máximo de 10 MB.");
            }

            var extension = Path.GetExtension(name).ToLowerInvariant();
            if (BlockedExtensions.Contains(extension))
            {
                return PropertyDocumentResult.Fail($"No se pueden adjuntar archivos ejecutables ({extension}).");
            }

            var desc = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            if (desc != null && desc.Length > 200)
            {
                return PropertyDocumentResult.Fail("La descripción no puede superar 200 caracteres.");
            }

            try
            {
                if (!await _context.Properties.AnyAsync(p => p.Id == propertyId))
                {
                    return PropertyDocumentResult.Fail("El inmueble ya no existe.");
                }

                var document = new PropertyDocument
                {
                    PropertyId = propertyId,
                    FileName = name,
                    ContentType = ResolveContentType(contentType, extension),
                    SizeBytes = content.Length,
                    Content = content,
                    Description = desc,
                    UploadedAt = DateTime.Now
                };

                _context.PropertyDocuments.Add(document);
                await _context.SaveChangesAsync();
                _context.Entry(document).State = EntityState.Detached;

                return PropertyDocumentResult.Ok(document.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al adjuntar documento");
                _context.ChangeTracker.Clear();
                return PropertyDocumentResult.Fail("No se pudo guardar el documento. No se ha modificado ningún dato.");
            }
        }

        public async Task<List<PropertyDocumentInfo>> GetByPropertyAsync(int propertyId)
        {
            try
            {
                return await _context.PropertyDocuments
                    .AsNoTracking()
                    .Where(d => d.PropertyId == propertyId)
                    .OrderByDescending(d => d.UploadedAt)
                    .ThenByDescending(d => d.Id)
                    .Select(d => new PropertyDocumentInfo
                    {
                        Id = d.Id,
                        PropertyId = d.PropertyId,
                        FileName = d.FileName,
                        ContentType = d.ContentType,
                        SizeBytes = d.SizeBytes,
                        Description = d.Description,
                        UploadedAt = d.UploadedAt,
                        ExtractedCompany = d.ExtractedCompany,
                        ExtractedAmount = d.ExtractedAmount,
                        ExtractedDate = d.ExtractedDate,
                        ExtractedType = d.ExtractedType,
                        AiReadAt = d.AiReadAt,
                        PropertyOperationId = d.PropertyOperationId
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar documentos");
                return new List<PropertyDocumentInfo>();
            }
        }

        public async Task<long> GetTotalSizeAsync(int propertyId)
        {
            try
            {
                return await _context.PropertyDocuments
                    .Where(d => d.PropertyId == propertyId)
                    .SumAsync(d => d.SizeBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al sumar tamaño de documentos");
                return 0;
            }
        }

        public async Task<int> CountByPropertyAsync(int propertyId)
        {
            try
            {
                return await _context.PropertyDocuments.CountAsync(d => d.PropertyId == propertyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al contar documentos");
                return 0;
            }
        }

        public async Task<byte[]?> GetContentAsync(int documentId)
        {
            try
            {
                return await _context.PropertyDocuments
                    .AsNoTracking()
                    .Where(d => d.Id == documentId)
                    .Select(d => d.Content)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al leer contenido del documento");
                return null;
            }
        }

        public async Task<PropertyDocumentResult> UpdateExtractedDataAsync(int documentId, string? company, decimal? amount, DateTime? date, string? type, bool markAiRead)
        {
            var comp = string.IsNullOrWhiteSpace(company) ? null : company.Trim();
            if (comp != null && comp.Length > 200)
            {
                return PropertyDocumentResult.Fail("La empresa no puede superar 200 caracteres.");
            }

            if (amount.HasValue && (amount.Value <= 0 || amount.Value >= 1000000000m))
            {
                return PropertyDocumentResult.Fail("El importe debe ser mayor que 0.");
            }

            var typeValue = string.IsNullOrWhiteSpace(type) ? null : ExpenseTypes.FirstOrDefault(t => t == type);

            try
            {
                var document = await _context.PropertyDocuments
                    .Select(d => new { d.Id })
                    .FirstOrDefaultAsync(d => d.Id == documentId);
                if (document == null)
                {
                    return PropertyDocumentResult.Fail("El documento ya no existe.");
                }

                // Se actualiza sin cargar el contenido del archivo
                var stub = new PropertyDocument { Id = documentId };
                _context.PropertyDocuments.Attach(stub);
                stub.ExtractedCompany = comp;
                stub.ExtractedAmount = amount.HasValue ? Math.Round(amount.Value, 2) : null;
                stub.ExtractedDate = date;
                stub.ExtractedType = typeValue;
                if (markAiRead)
                {
                    stub.AiReadAt = DateTime.Now;
                }

                _context.Entry(stub).Property(d => d.ExtractedCompany).IsModified = true;
                _context.Entry(stub).Property(d => d.ExtractedAmount).IsModified = true;
                _context.Entry(stub).Property(d => d.ExtractedDate).IsModified = true;
                _context.Entry(stub).Property(d => d.ExtractedType).IsModified = true;
                if (markAiRead)
                {
                    _context.Entry(stub).Property(d => d.AiReadAt).IsModified = true;
                }

                await _context.SaveChangesAsync();
                _context.Entry(stub).State = EntityState.Detached;
                return PropertyDocumentResult.Ok(documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar datos del documento");
                _context.ChangeTracker.Clear();
                return PropertyDocumentResult.Fail("No se pudieron guardar los datos del documento.");
            }
        }

        public async Task<PropertyDocumentResult> LinkOperationAsync(int documentId, int operationId)
        {
            try
            {
                var stub = new PropertyDocument { Id = documentId };
                _context.PropertyDocuments.Attach(stub);
                stub.PropertyOperationId = operationId;
                _context.Entry(stub).Property(d => d.PropertyOperationId).IsModified = true;

                await _context.SaveChangesAsync();
                _context.Entry(stub).State = EntityState.Detached;
                return PropertyDocumentResult.Ok(documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enlazar documento con gasto");
                _context.ChangeTracker.Clear();
                return PropertyDocumentResult.Fail("No se pudo enlazar el documento con el gasto.");
            }
        }

        public async Task<PropertyDocumentResult> DeleteAsync(int documentId)
        {
            try
            {
                var stub = new PropertyDocument { Id = documentId };
                _context.PropertyDocuments.Remove(stub);
                await _context.SaveChangesAsync();
                return PropertyDocumentResult.Ok(documentId);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Ya no existía: el resultado es el mismo
                _context.ChangeTracker.Clear();
                return PropertyDocumentResult.Ok(documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar documento");
                _context.ChangeTracker.Clear();
                return PropertyDocumentResult.Fail("No se pudo eliminar el documento.");
            }
        }

        public async Task<PropertyDocumentResult> OpenAsync(int documentId)
        {
            try
            {
                var info = await _context.PropertyDocuments
                    .AsNoTracking()
                    .Where(d => d.Id == documentId)
                    .Select(d => new { d.FileName, d.Content })
                    .FirstOrDefaultAsync();
                if (info == null)
                {
                    return PropertyDocumentResult.Fail("El documento ya no existe.");
                }

                var folder = Path.Combine(FileSystem.CacheDirectory, "property-documents");
                Directory.CreateDirectory(folder);

                var safeName = string.Concat(info.FileName.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
                var path = Path.Combine(folder, $"{Guid.NewGuid():N}_{safeName}");
                await File.WriteAllBytesAsync(path, info.Content);

                await Launcher.Default.OpenAsync(new OpenFileRequest(info.FileName, new ReadOnlyFile(path)));
                return PropertyDocumentResult.Ok(documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al abrir documento");
                return PropertyDocumentResult.Fail("No se pudo abrir el documento. Es posible que no haya ninguna aplicación instalada para este tipo de archivo.");
            }
        }

        /// <summary>Borra las copias temporales de documentos abiertos (se llama al arrancar)</summary>
        public static void CleanTempFiles()
        {
            try
            {
                var folder = Path.Combine(FileSystem.CacheDirectory, "property-documents");
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
            catch (Exception)
            {
                // Si algún archivo sigue abierto, se limpiará en el próximo arranque
            }
        }

        private static string ResolveContentType(string? contentType, string extension)
        {
            if (!string.IsNullOrWhiteSpace(contentType) && contentType != "application/octet-stream")
            {
                return contentType.Length > 100 ? contentType[..100] : contentType;
            }

            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".txt" => "text/plain",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }
    }
}
