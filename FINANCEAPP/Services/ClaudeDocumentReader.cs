using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Implementación con la API de mensajes de Anthropic. La clave nunca se escribe en registros ni en mensajes.
    /// </summary>
    public class ClaudeDocumentReader : IClaudeDocumentReader
    {
        private const string ApiUrl = "https://api.anthropic.com/v1/messages";
        private const int MaxImageBytes = 7 * 1024 * 1024;
        private const int MaxPdfBytes = 20 * 1024 * 1024;
        private static readonly string[] ExpenseTypes = { "Comunidad", "IBI", "Hipoteca", "Reforma", "Seguro", "Servicios" };
        private static readonly string[] ImageTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };

        private const string Prompt =
            "Eres un asistente que lee facturas y recibos. Analiza el documento y responde SOLO con un JSON, sin texto adicional, " +
            "con esta forma exacta: {\"isInvoice\": true|false, \"company\": \"empresa emisora\"|null, \"amount\": importe total a pagar como número con punto decimal|null, " +
            "\"date\": \"yyyy-MM-dd\" fecha de la factura|null, \"type\": uno de \"Comunidad\", \"IBI\", \"Hipoteca\", \"Reforma\", \"Seguro\", \"Servicios\" o null}. " +
            "Usa \"Servicios\" para luz, agua, gas, internet o teléfono. Si el documento no es una factura o recibo, pon isInvoice a false y el resto a null.";

        private readonly HttpClient _httpClient;
        private readonly IClaudeApiKeyStore _keyStore;
        private readonly IAppSettingsService _settings;

        public ClaudeDocumentReader(HttpClient httpClient, IClaudeApiKeyStore keyStore, IAppSettingsService settings)
        {
            _httpClient = httpClient;
            _keyStore = keyStore;
            _settings = settings;
        }

        public bool CanRead(string contentType)
        {
            var ct = (contentType ?? string.Empty).ToLowerInvariant();
            return ct == "application/pdf" || ImageTypes.Contains(ct);
        }

        public async Task<DocumentReadResult> ReadAsync(byte[] content, string contentType, CancellationToken ct = default)
        {
            var type = (contentType ?? string.Empty).ToLowerInvariant();
            if (!CanRead(type))
            {
                return Fail("Este tipo de archivo no se puede leer con IA. Solo imágenes (JPG, PNG, GIF, WebP) y PDF.");
            }

            var isPdf = type == "application/pdf";
            if (!isPdf && content.Length > MaxImageBytes)
            {
                return Fail("La imagen supera los 7 MB y no se puede leer con IA.");
            }

            if (isPdf && content.Length > MaxPdfBytes)
            {
                return Fail("El PDF es demasiado grande para leerlo con IA.");
            }

            var key = await _keyStore.GetKeyAsync();
            if (string.IsNullOrEmpty(key))
            {
                return Fail("No hay clave de API guardada. Añádela en Configuración.");
            }

            var model = await _settings.GetAiModelAsync();
            var data = Convert.ToBase64String(content);

            object block = isPdf
                ? new { type = "document", source = new { type = "base64", media_type = "application/pdf", data } }
                : new { type = "image", source = new { type = "base64", media_type = type, data } };

            var body = new
            {
                model,
                max_tokens = 512,
                messages = new[]
                {
                    new { role = "user", content = new object[] { block, new { type = "text", text = Prompt } } }
                }
            };

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
                {
                    Content = JsonContent.Create(body)
                };
                request.Headers.Add("x-api-key", key);
                request.Headers.Add("anthropic-version", "2023-06-01");

                using var response = await _httpClient.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    return Fail(MapError(response.StatusCode));
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                return Parse(json);
            }
            catch (TaskCanceledException)
            {
                return Fail("La IA ha tardado demasiado en responder. Inténtalo de nuevo.");
            }
            catch (HttpRequestException)
            {
                return Fail("Sin conexión con el servicio de IA. Revisa tu conexión a internet.");
            }
            catch (Exception)
            {
                return Fail("No se pudo leer el documento con IA.");
            }
        }

        private static string MapError(HttpStatusCode status) => (int)status switch
        {
            401 or 403 => "La clave de API no es válida. Revísala en Configuración.",
            404 or 400 => "La IA rechazó la petición. Revisa el nombre del modelo en Configuración.",
            429 => "Se ha superado el límite de uso de la API. Espera un momento y vuelve a intentarlo.",
            >= 500 => "El servicio de IA no está disponible ahora. Inténtalo más tarde.",
            _ => "No se pudo leer el documento con IA."
        };

        private static DocumentReadResult Parse(string responseJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;

                var match = Regex.Match(text, "\\{.*\\}", RegexOptions.Singleline);
                if (!match.Success)
                {
                    return Fail("No se entendió la respuesta de la IA. Puedes rellenar los datos a mano.");
                }

                using var parsed = JsonDocument.Parse(match.Value);
                var root = parsed.RootElement;

                var isInvoice = root.TryGetProperty("isInvoice", out var inv) && inv.ValueKind == JsonValueKind.True;
                if (!isInvoice)
                {
                    return new DocumentReadResult
                    {
                        Success = true,
                        Warning = "El documento no parece una factura o recibo; no se ha rellenado ningún dato."
                    };
                }

                var result = new DocumentReadResult { Success = true };

                if (root.TryGetProperty("company", out var c) && c.ValueKind == JsonValueKind.String)
                {
                    var company = c.GetString()?.Trim();
                    if (!string.IsNullOrEmpty(company))
                    {
                        result.Company = company.Length > 200 ? company[..200] : company;
                    }
                }

                if (root.TryGetProperty("amount", out var a))
                {
                    decimal amount = 0;
                    var ok = a.ValueKind == JsonValueKind.Number
                        ? a.TryGetDecimal(out amount)
                        : a.ValueKind == JsonValueKind.String
                          && decimal.TryParse(a.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
                    if (ok && amount > 0 && amount < 1000000000m)
                    {
                        result.Amount = Math.Round(amount, 2);
                    }
                }

                if (root.TryGetProperty("date", out var d) && d.ValueKind == JsonValueKind.String
                    && DateTime.TryParseExact(d.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    result.Date = date;
                }

                if (root.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String)
                {
                    var typeName = ExpenseTypes.FirstOrDefault(x => string.Equals(x, t.GetString()?.Trim(), StringComparison.OrdinalIgnoreCase));
                    result.Type = typeName;
                }

                return result;
            }
            catch (Exception)
            {
                return Fail("No se entendió la respuesta de la IA. Puedes rellenar los datos a mano.");
            }
        }

        private static DocumentReadResult Fail(string message) =>
            new() { Success = false, ErrorMessage = message };
    }
}
