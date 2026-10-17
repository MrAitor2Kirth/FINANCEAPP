using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Implementación de <see cref="IMarketCatalogService"/> sobre la tabla MarketCompanies.
    /// El catálogo no cambia mientras la app está abierta, así que se lee una vez y se guarda en memoria.
    /// </summary>
    public class MarketCatalogService : IMarketCatalogService
    {
        private readonly AppDbContext _context;
        private readonly IStockPriceService _stockPriceService;
        private List<MarketCompany>? _all;

        public MarketCatalogService(AppDbContext context, IStockPriceService stockPriceService)
        {
            _context = context;
            _stockPriceService = stockPriceService;
        }

        public async Task<List<MarketCompany>> GetAllAsync()
        {
            if (_all == null)
            {
                var rows = await _context.MarketCompanies.AsNoTracking().ToListAsync();
                _all = rows.OrderBy(c => Normalize(c.Name), StringComparer.Ordinal).ToList();
            }

            return _all;
        }

        public async Task<List<MarketCompany>> SearchAsync(string text, int take = 10)
        {
            var term = Normalize(text);
            if (term.Length == 0)
            {
                return new List<MarketCompany>();
            }

            var all = await GetAllAsync();
            return all
                .Select(c => new { Company = c, Name = Normalize(c.Name), Ticker = Normalize(c.Ticker) })
                .Where(x => x.Name.Contains(term, StringComparison.Ordinal) || x.Ticker.Contains(term, StringComparison.Ordinal))
                .OrderBy(x => x.Name.StartsWith(term, StringComparison.Ordinal) || x.Ticker.StartsWith(term, StringComparison.Ordinal) ? 0 : 1)
                .ThenBy(x => x.Name, StringComparer.Ordinal)
                .Take(take)
                .Select(x => x.Company)
                .ToList();
        }

        public IReadOnlyDictionary<string, string> GetIndexLabels() => MarketCatalogData.IndexLabels;

        public string GetCatalogVersion() => MarketCatalogData.Version;

        public async Task<HashSet<string>> GetOwnedTickersAsync()
        {
            var tickers = await _context.CompanyDetails
                .AsNoTracking()
                .Select(c => c.Ticker)
                .ToListAsync();
            return new HashSet<string>(tickers.Select(t => t.Trim().ToUpperInvariant()));
        }

        public async Task<MarketCompanyPrepareResult> PrepareCompanyAsync(string ticker)
        {
            var normalized = (ticker ?? string.Empty).Trim().ToUpperInvariant();
            var all = await GetAllAsync();
            var entry = all.FirstOrDefault(c => string.Equals(c.Ticker, normalized, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
            {
                return new MarketCompanyPrepareResult();
            }

            var companies = await _context.CompanyDetails
                .AsNoTracking()
                .Select(c => new { c.Name, c.Ticker })
                .ToListAsync();

            if (companies.Any(c => string.Equals(c.Ticker.Trim(), entry.Ticker, StringComparison.OrdinalIgnoreCase)))
            {
                return new MarketCompanyPrepareResult
                {
                    Error = $"Ya tienes {entry.Name} ({entry.Ticker}) en tu lista de empresas."
                };
            }

            if (companies.Any(c => string.Equals(c.Name.Trim(), entry.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return new MarketCompanyPrepareResult
                {
                    Error = $"Ya tienes una empresa llamada «{entry.Name}» con otro ticker. Escribe un nombre distinto a mano para no mezclar sus operaciones."
                };
            }

            var countryExists = await _context.Countries.AnyAsync(c => c.Code == entry.CountryCode);

            return new MarketCompanyPrepareResult
            {
                Found = true,
                Name = entry.Name,
                Ticker = entry.Ticker,
                CountryCode = countryExists ? entry.CountryCode : null,
                Warning = countryExists
                    ? null
                    : $"El país del mercado ({entry.CountryCode}) no está en tu catálogo de países: elige uno a mano o añádelo en Configuración."
            };
        }

        public async Task<decimal?> CheckTickerAsync(string ticker)
        {
            try
            {
                return await _stockPriceService.GetLatestPriceAsync(ticker);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al comprobar el ticker {ticker}: {ex.Message}");
                return null;
            }
        }

        public async Task<int> CheckManyAsync(IEnumerable<string> tickers, Func<string, decimal?, Task> onResult)
        {
            var batch = tickers.Take(IMarketCatalogService.MaxCheckBatch).ToList();
            foreach (var ticker in batch)
            {
                var price = await CheckTickerAsync(ticker);
                await onResult(ticker, price);
            }

            return batch.Count;
        }

        /// <summary>Minúsculas y sin tildes, para buscar sin distinguir mayúsculas ni acentos</summary>
        private static string Normalize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var decomposed = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (var c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
