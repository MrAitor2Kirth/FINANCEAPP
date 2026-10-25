using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace FINANCEAPP.Services
{
    public class StockPriceService : IStockPriceService
    {
        /// <summary>
        /// true si se prueban los sufijos de bolsa: el ticker no lleva sufijo y no es un índice (^IBEX), un futuro
        /// o una divisa (GC=F, EURUSD=X), que se escriben siempre completos (SPEC-05-04)
        /// </summary>
        internal static bool CanTrySuffixes(string ticker) => !ticker.Contains('.') && !ticker.StartsWith('^') && !ticker.Contains('=');

        // Sufijos de bolsas no estadounidenses habitualmente usados por Yahoo Finance.
        // Se prueban cuando el ticker "plano" (sin sufijo) no devuelve resultado, ya que
        // muchas empresas europeas (p.ej. ACS -> ACS.MC, Airbus -> AIR.PA) requieren el sufijo
        // de su bolsa de cotización para poder identificarse en Yahoo Finance.
        internal static readonly string[] ExchangeSuffixFallbacks =
        {
            ".MC", // Bolsa de Madrid
            ".PA", // Euronext Paris
            ".MI", // Borsa Italiana (Milán)
            ".DE", // XETRA (Alemania)
            ".AS", // Euronext Ámsterdam
            ".BR", // Euronext Bruselas
            ".L",  // London Stock Exchange
            ".LS", // Euronext Lisboa
            ".SW", // SIX Swiss Exchange
        };

        private readonly HttpClient _httpClient;
        private readonly ILogger<StockPriceService> _logger;

        public StockPriceService(HttpClient httpClient, ILogger<StockPriceService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromSeconds(10);

            // Yahoo Finance rechaza (401/429) las peticiones que no incluyen un User-Agent
            // de navegador válido. Sin esta cabecera, todas las consultas fallan silenciosamente
            // y el precio en vivo queda como null (se muestra "N/D" en la UI).
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            }
        }

        public async Task<decimal?> GetLatestPriceAsync(string ticker)
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                return null;
            }

            var normalizedTicker = ticker.Trim();
            var prices = await GetLatestPricesAsync(new[] { normalizedTicker });
            return prices.TryGetValue(normalizedTicker, out var value) ? value : null;
        }

        public async Task<Dictionary<string, decimal?>> GetLatestPricesAsync(IEnumerable<string> tickers)
        {
            var normalizedTickers = tickers
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var result = normalizedTickers.ToDictionary(t => t, _ => (decimal?)null, StringComparer.OrdinalIgnoreCase);
            if (normalizedTickers.Count == 0)
            {
                return result;
            }

            // El endpoint v6/finance/quote de Yahoo Finance dejó de funcionar de forma anónima
            // (requiere "crumb" + cookies de sesión) y devolvía siempre error, dejando el
            // precio en vivo sin recuperar. Se usa en su lugar el endpoint público de gráficos
            // (v8/finance/chart), que sí admite consultas anónimas por ticker.
            foreach (var ticker in normalizedTickers)
            {
                var price = await GetPriceFromChartEndpointAsync(ticker);

                // Si el ticker "plano" no incluye ya un sufijo de bolsa (contiene un punto,
                // p.ej. "AIR.PA") y no se obtuvo precio, se prueban los sufijos de bolsas
                // no estadounidenses más habituales antes de darlo por fallido.
                if (!price.HasValue && !ticker.Contains('.'))
                {
                    foreach (var suffix in ExchangeSuffixFallbacks)
                    {
                        var candidate = ticker + suffix;
                        price = await GetPriceFromChartEndpointAsync(candidate);
                        if (price.HasValue)
                        {
                            _logger.LogInformation(
                                "El ticker {Ticker} no devolvió precio directamente; se encontró usando el sufijo de bolsa {Candidate}.",
                                ticker, candidate);
                            break;
                        }
                    }
                }

                result[ticker] = price;
            }

            return result;
        }

        private async Task<decimal?> GetPriceFromChartEndpointAsync(string ticker)
        {
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(ticker)}?interval=1d&range=1d";

            try
            {
                using var response = await _httpClient.GetAsync(url);
                var payload = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Yahoo Finance respondió con estado {StatusCode} para el ticker {Ticker}. Cuerpo: {Body}",
                        response.StatusCode, ticker, Truncate(payload));
                    return null;
                }

                using var doc = JsonDocument.Parse(payload);

                if (!doc.RootElement.TryGetProperty("chart", out var chart))
                {
                    _logger.LogWarning("Respuesta de Yahoo Finance sin nodo 'chart' para el ticker {Ticker}. Cuerpo: {Body}", ticker, Truncate(payload));
                    return null;
                }

                if (chart.TryGetProperty("error", out var errorNode) && errorNode.ValueKind != JsonValueKind.Null)
                {
                    _logger.LogWarning("Yahoo Finance devolvió error para el ticker {Ticker}: {Error}", ticker, errorNode.GetRawText());
                    return null;
                }

                if (!chart.TryGetProperty("result", out var resultArray) || resultArray.ValueKind != JsonValueKind.Array || resultArray.GetArrayLength() == 0)
                {
                    _logger.LogWarning("Yahoo Finance no devolvió resultados para el ticker {Ticker}. Cuerpo: {Body}", ticker, Truncate(payload));
                    return null;
                }

                var firstResult = resultArray[0];
                if (!firstResult.TryGetProperty("meta", out var meta))
                {
                    _logger.LogWarning("Respuesta de Yahoo Finance sin nodo 'meta' para el ticker {Ticker}.", ticker);
                    return null;
                }

                if (!meta.TryGetProperty("regularMarketPrice", out var priceProperty) ||
                    priceProperty.ValueKind == JsonValueKind.Null || priceProperty.ValueKind == JsonValueKind.Undefined)
                {
                    _logger.LogWarning("Yahoo Finance no incluyó 'regularMarketPrice' para el ticker {Ticker}.", ticker);
                    return null;
                }

                var priceText = priceProperty.GetRawText();
                if (decimal.TryParse(priceText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var price))
                {
                    _logger.LogInformation("Precio en vivo recuperado para {Ticker}: {Price}", ticker, price);
                    return price;
                }

                _logger.LogWarning("No se pudo interpretar el precio '{PriceText}' para el ticker {Ticker}.", priceText, ticker);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar Yahoo Finance para el ticker {Ticker}", ticker);
                return null;
            }
        }

        public async Task<StockQuote?> GetQuoteAsync(string ticker)
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                return null;
            }

            var normalizedTicker = ticker.Trim();
            var quote = await GetQuoteFromChartEndpointAsync(normalizedTicker);

            // Igual que el precio en vivo: si el ticker no lleva sufijo de bolsa, se prueban los habituales
            if (quote == null && CanTrySuffixes(normalizedTicker))
            {
                foreach (var suffix in ExchangeSuffixFallbacks)
                {
                    quote = await GetQuoteFromChartEndpointAsync(normalizedTicker + suffix);
                    if (quote != null)
                    {
                        break;
                    }
                }
            }

            return quote;
        }

        private async Task<StockQuote?> GetQuoteFromChartEndpointAsync(string ticker)
        {
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(ticker)}?interval=1d&range=1d";

            try
            {
                using var response = await _httpClient.GetAsync(url);
                var payload = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Yahoo Finance respondió con estado {StatusCode} a la cotización de {Ticker}. Cuerpo: {Body}",
                        response.StatusCode, ticker, Truncate(payload));
                    return null;
                }

                using var doc = JsonDocument.Parse(payload);

                if (!doc.RootElement.TryGetProperty("chart", out var chart) ||
                    (chart.TryGetProperty("error", out var errorNode) && errorNode.ValueKind != JsonValueKind.Null) ||
                    !chart.TryGetProperty("result", out var resultArray) ||
                    resultArray.ValueKind != JsonValueKind.Array || resultArray.GetArrayLength() == 0 ||
                    !resultArray[0].TryGetProperty("meta", out var meta))
                {
                    return null;
                }

                var price = ReadDecimal(meta, "regularMarketPrice");
                if (!price.HasValue)
                {
                    return null;
                }

                var quote = new StockQuote
                {
                    Symbol = ReadString(meta, "symbol") ?? ticker,
                    Price = price.Value,
                    PreviousClose = ReadDecimal(meta, "chartPreviousClose") ?? ReadDecimal(meta, "previousClose"),
                    DayHigh = ReadDecimal(meta, "regularMarketDayHigh"),
                    DayLow = ReadDecimal(meta, "regularMarketDayLow"),
                    PriceTime = ReadUnixTime(meta, "regularMarketTime")?.ToLocalTime(),
                    Currency = ReadString(meta, "currency")
                };

                if (meta.TryGetProperty("currentTradingPeriod", out var period) &&
                    period.ValueKind == JsonValueKind.Object &&
                    period.TryGetProperty("regular", out var regular) &&
                    regular.ValueKind == JsonValueKind.Object)
                {
                    quote.RegularSessionStartUtc = ReadUnixTime(regular, "start");
                    quote.RegularSessionEndUtc = ReadUnixTime(regular, "end");
                }

                return quote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar la cotización de Yahoo Finance para el ticker {Ticker}", ticker);
                return null;
            }
        }

        private static decimal? ReadDecimal(JsonElement element, string property)
        {
            if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number &&
                decimal.TryParse(value.GetRawText(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            return null;
        }

        private static string? ReadString(JsonElement element, string property)
        {
            return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        /// <summary>
        /// Lee segundos unix y los devuelve como fecha UTC
        /// </summary>
        private static DateTime? ReadUnixTime(JsonElement element, string property)
        {
            if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt64(out var seconds) && seconds > 0)
            {
                return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
            }

            return null;
        }

        public async Task<List<StockHistoryBar>> GetDailyHistoryAsync(string ticker, string range = "5y")
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                return new List<StockHistoryBar>();
            }

            var normalizedTicker = ticker.Trim();
            var bars = await GetHistoryFromChartEndpointAsync(normalizedTicker, range);

            if (bars.Count == 0 && !normalizedTicker.Contains('.'))
            {
                foreach (var suffix in ExchangeSuffixFallbacks)
                {
                    var candidate = normalizedTicker + suffix;
                    bars = await GetHistoryFromChartEndpointAsync(candidate, range);
                    if (bars.Count > 0)
                    {
                        _logger.LogInformation(
                            "El ticker {Ticker} no devolvió histórico directamente; se encontró usando el sufijo de bolsa {Candidate}.",
                            normalizedTicker, candidate);
                        break;
                    }
                }
            }

            return bars;
        }

        private async Task<List<StockHistoryBar>> GetHistoryFromChartEndpointAsync(string ticker, string range)
        {
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(ticker)}?interval=1d&range={Uri.EscapeDataString(range)}";
            var bars = new List<StockHistoryBar>();

            try
            {
                using var response = await _httpClient.GetAsync(url);
                var payload = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Yahoo Finance respondió con estado {StatusCode} para el histórico del ticker {Ticker}. Cuerpo: {Body}",
                        response.StatusCode, ticker, Truncate(payload));
                    return bars;
                }

                using var doc = JsonDocument.Parse(payload);

                if (!doc.RootElement.TryGetProperty("chart", out var chart))
                {
                    return bars;
                }

                if (chart.TryGetProperty("error", out var errorNode) && errorNode.ValueKind != JsonValueKind.Null)
                {
                    _logger.LogWarning("Yahoo Finance devolvió error en histórico para el ticker {Ticker}: {Error}", ticker, errorNode.GetRawText());
                    return bars;
                }

                if (!chart.TryGetProperty("result", out var resultArray) || resultArray.ValueKind != JsonValueKind.Array || resultArray.GetArrayLength() == 0)
                {
                    return bars;
                }

                return ParseBars(resultArray[0], intraday: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar histórico de Yahoo Finance para el ticker {Ticker}", ticker);
                return bars;
            }
        }

        public async Task<StockHistoryChunk> GetHistoryRangeAsync(string ticker, string interval, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                return new StockHistoryChunk();
            }

            var normalizedTicker = ticker.Trim();
            var chunk = await GetRangeFromChartEndpointAsync(normalizedTicker, interval, fromUtc, toUtc, cancellationToken);

            // Solo se prueban los sufijos si Yahoo no reconoce el símbolo; un fallo de red no se repite 9 veces
            if (!chunk.Failed && !chunk.SymbolFound && CanTrySuffixes(normalizedTicker))
            {
                foreach (var suffix in ExchangeSuffixFallbacks)
                {
                    var candidate = await GetRangeFromChartEndpointAsync(normalizedTicker + suffix, interval, fromUtc, toUtc, cancellationToken);
                    if (candidate.Failed || candidate.SymbolFound)
                    {
                        return candidate;
                    }
                }
            }

            return chunk;
        }

        private async Task<StockHistoryChunk> GetRangeFromChartEndpointAsync(string ticker, string interval, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
        {
            var period1 = ToUnixSeconds(fromUtc);
            var period2 = ToUnixSeconds(toUtc);
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(ticker)}?interval={Uri.EscapeDataString(interval)}&period1={period1}&period2={period2}";
            var chunk = new StockHistoryChunk();

            try
            {
                using var response = await _httpClient.GetAsync(url, cancellationToken);
                var payload = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    // 404 = símbolo desconocido (no es un fallo de red); el resto se trata como fallo
                    chunk.Failed = response.StatusCode != System.Net.HttpStatusCode.NotFound;
                    _logger.LogWarning(
                        "Yahoo Finance respondió con estado {StatusCode} para el tramo {Interval} de {Ticker}. Cuerpo: {Body}",
                        response.StatusCode, interval, ticker, Truncate(payload));
                    return chunk;
                }

                using var doc = JsonDocument.Parse(payload);

                if (!doc.RootElement.TryGetProperty("chart", out var chart) ||
                    (chart.TryGetProperty("error", out var errorNode) && errorNode.ValueKind != JsonValueKind.Null) ||
                    !chart.TryGetProperty("result", out var resultArray) ||
                    resultArray.ValueKind != JsonValueKind.Array || resultArray.GetArrayLength() == 0)
                {
                    return chunk;
                }

                var firstResult = resultArray[0];
                if (firstResult.TryGetProperty("meta", out var meta))
                {
                    chunk.SymbolFound = true;
                    chunk.Symbol = ReadString(meta, "symbol") ?? ticker;

                    // firstTradeDate puede ser negativo (empresas que cotizan desde antes de 1970)
                    if (meta.TryGetProperty("firstTradeDate", out var firstTrade) &&
                        firstTrade.ValueKind == JsonValueKind.Number && firstTrade.TryGetInt64(out var firstTradeSeconds))
                    {
                        chunk.FirstTradeDateUtc = DateTimeOffset.FromUnixTimeSeconds(firstTradeSeconds).UtcDateTime;
                    }
                }

                chunk.Bars = ParseBars(firstResult, intraday: interval != "1d");
                return chunk;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cancelación pedida (se cerró el modal): no es un error
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar el tramo {Interval} de Yahoo Finance para el ticker {Ticker}", interval, ticker);
                chunk.Failed = true;
                return chunk;
            }
        }

        public async Task<StockDividendLookup> GetLastDividendAsync(string ticker)
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                return new StockDividendLookup();
            }

            var normalizedTicker = ticker.Trim();
            var (lookup, found) = await GetLastDividendFromChartEndpointAsync(normalizedTicker);

            // Igual que el histórico: los sufijos solo se prueban si Yahoo no reconoce el símbolo
            if (!lookup.Failed && !found && !normalizedTicker.Contains('.'))
            {
                foreach (var suffix in ExchangeSuffixFallbacks)
                {
                    var (candidate, candidateFound) = await GetLastDividendFromChartEndpointAsync(normalizedTicker + suffix);
                    if (candidate.Failed || candidateFound)
                    {
                        return candidate;
                    }
                }
            }

            return lookup;
        }

        private async Task<(StockDividendLookup Lookup, bool SymbolFound)> GetLastDividendFromChartEndpointAsync(string ticker)
        {
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(ticker)}?interval=1d&range=5y&events=div";
            var lookup = new StockDividendLookup();

            try
            {
                using var response = await _httpClient.GetAsync(url);
                var payload = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    lookup.Failed = response.StatusCode != System.Net.HttpStatusCode.NotFound;
                    _logger.LogWarning("Yahoo Finance respondió con estado {StatusCode} a los dividendos de {Ticker}. Cuerpo: {Body}",
                        response.StatusCode, ticker, Truncate(payload));
                    return (lookup, false);
                }

                using var doc = JsonDocument.Parse(payload);
                if (!doc.RootElement.TryGetProperty("chart", out var chart) ||
                    (chart.TryGetProperty("error", out var errorNode) && errorNode.ValueKind != JsonValueKind.Null) ||
                    !chart.TryGetProperty("result", out var resultArray) ||
                    resultArray.ValueKind != JsonValueKind.Array || resultArray.GetArrayLength() == 0)
                {
                    return (lookup, false);
                }

                var firstResult = resultArray[0];
                var found = firstResult.TryGetProperty("meta", out _);

                // events.dividends es un objeto { "<unix>": { "amount": 0.25, "date": <unix> } }
                if (firstResult.TryGetProperty("events", out var events) &&
                    events.TryGetProperty("dividends", out var dividends) &&
                    dividends.ValueKind == JsonValueKind.Object)
                {
                    foreach (var entry in dividends.EnumerateObject())
                    {
                        var dividend = entry.Value;
                        var amount = ReadDecimal(dividend, "amount");
                        if (!amount.HasValue ||
                            !dividend.TryGetProperty("date", out var dateNode) ||
                            dateNode.ValueKind != JsonValueKind.Number ||
                            !dateNode.TryGetInt64(out var seconds))
                        {
                            continue;
                        }

                        var date = DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime.Date;
                        if (lookup.Last == null || date > lookup.Last.Date)
                        {
                            lookup.Last = new StockDividend(date, amount.Value);
                        }
                    }
                }

                return (lookup, found);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar los dividendos de Yahoo Finance para el ticker {Ticker}", ticker);
                lookup.Failed = true;
                return (lookup, false);
            }
        }

        /// <summary>
        /// Lee las barras OHLC de un resultado de v8/finance/chart. En diario la fecha va sin hora (como hasta ahora);
        /// en barras de una hora se usa la hora local de Windows (SPEC-05-02, D5).
        /// </summary>
        private static List<StockHistoryBar> ParseBars(JsonElement firstResult, bool intraday)
        {
            var bars = new List<StockHistoryBar>();

            if (!firstResult.TryGetProperty("timestamp", out var timestamps) || timestamps.ValueKind != JsonValueKind.Array)
            {
                return bars;
            }

            if (!firstResult.TryGetProperty("indicators", out var indicators) ||
                !indicators.TryGetProperty("quote", out var quoteArray) || quoteArray.ValueKind != JsonValueKind.Array || quoteArray.GetArrayLength() == 0)
            {
                return bars;
            }

            var quote = quoteArray[0];
            quote.TryGetProperty("open", out var opens);
            quote.TryGetProperty("high", out var highs);
            quote.TryGetProperty("low", out var lows);
            quote.TryGetProperty("close", out var closes);
            quote.TryGetProperty("volume", out var volumes);

            var count = timestamps.GetArrayLength();
            for (var i = 0; i < count; i++)
            {
                if (!TryGetDecimal(opens, i, out var open) ||
                    !TryGetDecimal(highs, i, out var high) ||
                    !TryGetDecimal(lows, i, out var low) ||
                    !TryGetDecimal(closes, i, out var close))
                {
                    // Sesión sin datos (día festivo dentro del rango); se omite.
                    continue;
                }

                var moment = DateTimeOffset.FromUnixTimeSeconds(timestamps[i].GetInt64());
                var date = intraday ? moment.LocalDateTime : moment.UtcDateTime.Date;
                var volume = TryGetInt64(volumes, i, out var vol) ? vol : 0L;

                bars.Add(new StockHistoryBar(date, open, high, low, close, volume));
            }

            return bars;
        }

        private static long ToUnixSeconds(DateTime utc)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeSeconds();
        }

        private static bool TryGetDecimal(JsonElement array, int index, out decimal value)
        {
            value = 0m;
            if (array.ValueKind != JsonValueKind.Array || index >= array.GetArrayLength())
            {
                return false;
            }

            var element = array[index];
            if (element.ValueKind != JsonValueKind.Number)
            {
                return false;
            }

            value = element.GetDecimal();
            return true;
        }

        private static bool TryGetInt64(JsonElement array, int index, out long value)
        {
            value = 0L;
            if (array.ValueKind != JsonValueKind.Array || index >= array.GetArrayLength())
            {
                return false;
            }

            var element = array[index];
            if (element.ValueKind != JsonValueKind.Number)
            {
                return false;
            }

            value = element.GetInt64();
            return true;
        }

        private static string Truncate(string value, int maxLength = 300)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.Length <= maxLength ? value : value[..maxLength] + "...";
        }
    }
}
