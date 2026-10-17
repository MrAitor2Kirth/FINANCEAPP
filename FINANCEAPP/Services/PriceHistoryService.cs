using Microsoft.EntityFrameworkCore;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Implementación de <see cref="IPriceHistoryService"/> que cachea el histórico diario
    /// en SQLite (tabla PriceHistoryPoints) y solo consulta a Yahoo Finance cuando la caché
    /// está vacía o desactualizada. La agregación mensual/anual se calcula siempre en C#
    /// a partir de los puntos diarios ya cacheados.
    /// </summary>
    public class PriceHistoryService : IPriceHistoryService
    {
        // Si el punto más reciente en caché tiene menos de este tiempo, se considera
        // "fresco" y no se vuelve a consultar Yahoo Finance.
        private static readonly TimeSpan CacheFreshness = TimeSpan.FromHours(12);

        private readonly AppDbContext _context;
        private readonly IStockPriceService _stockPriceService;

        public PriceHistoryService(AppDbContext context, IStockPriceService stockPriceService)
        {
            _context = context;
            _stockPriceService = stockPriceService;
        }

        public async Task<List<PriceHistoryPoint>> GetHistoryAsync(int companyId, string ticker, ChartGranularity granularity)
        {
            var result = await GetHistoryWithStatusAsync(companyId, ticker, granularity, false);
            return result.Points;
        }

        public async Task<PriceHistoryResult> GetHistoryWithStatusAsync(int companyId, string ticker, ChartGranularity granularity, bool forceRefresh)
        {
            var refreshFailed = await RefreshCacheIfNeededAsync(companyId, ticker, forceRefresh);

            var dailyPoints = await _context.PriceHistoryPoints
                .AsNoTracking()
                .Where(p => p.CompanyId == companyId)
                .OrderBy(p => p.Date)
                .ToListAsync();

            var points = granularity switch
            {
                ChartGranularity.Daily => dailyPoints,
                ChartGranularity.Monthly => Aggregate(dailyPoints, p => new DateTime(p.Date.Year, p.Date.Month, 1)),
                ChartGranularity.Yearly => Aggregate(dailyPoints, p => new DateTime(p.Date.Year, 1, 1)),
                _ => dailyPoints,
            };

            return new PriceHistoryResult { Points = points, RefreshFailed = refreshFailed };
        }

        /// <summary>
        /// Refresca la caché si está desactualizada o si se fuerza. Devuelve true si se intentó descargar
        /// y Yahoo no devolvió datos.
        /// </summary>
        private async Task<bool> RefreshCacheIfNeededAsync(int companyId, string ticker, bool forceRefresh)
        {
            var latestFetch = await _context.PriceHistoryPoints
                .AsNoTracking()
                .Where(p => p.CompanyId == companyId)
                .OrderByDescending(p => p.FetchedAt)
                .Select(p => (DateTime?)p.FetchedAt)
                .FirstOrDefaultAsync();

            var isFresh = latestFetch.HasValue && DateTime.Now - latestFetch.Value < CacheFreshness;
            if (isFresh && !forceRefresh)
            {
                return false;
            }

            var bars = await _stockPriceService.GetDailyHistoryAsync(ticker, "5y");
            if (bars.Count == 0)
            {
                // No se pudo obtener nada nuevo; se sigue sirviendo lo que hubiera en caché.
                return true;
            }

            var existingByDate = await _context.PriceHistoryPoints
                .Where(p => p.CompanyId == companyId)
                .ToDictionaryAsync(p => p.Date.Date);

            var now = DateTime.Now;

            foreach (var bar in bars)
            {
                if (existingByDate.TryGetValue(bar.Date.Date, out var existing))
                {
                    existing.Open = bar.Open;
                    existing.High = bar.High;
                    existing.Low = bar.Low;
                    existing.Close = bar.Close;
                    existing.Volume = bar.Volume;
                    existing.FetchedAt = now;
                }
                else
                {
                    _context.PriceHistoryPoints.Add(new PriceHistoryPoint
                    {
                        CompanyId = companyId,
                        Date = bar.Date.Date,
                        Open = bar.Open,
                        High = bar.High,
                        Low = bar.Low,
                        Close = bar.Close,
                        Volume = bar.Volume,
                        FetchedAt = now,
                    });
                }
            }

            await _context.SaveChangesAsync();
            return false;
        }

        private static List<PriceHistoryPoint> Aggregate(List<PriceHistoryPoint> dailyPoints, Func<PriceHistoryPoint, DateTime> bucketSelector)
        {
            return dailyPoints
                .GroupBy(bucketSelector)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var ordered = g.OrderBy(p => p.Date).ToList();
                    return new PriceHistoryPoint
                    {
                        CompanyId = ordered[0].CompanyId,
                        Date = g.Key,
                        Open = ordered.First().Open,
                        Close = ordered.Last().Close,
                        High = ordered.Max(p => p.High),
                        Low = ordered.Min(p => p.Low),
                        Volume = ordered.Sum(p => p.Volume),
                        FetchedAt = ordered.Max(p => p.FetchedAt),
                    };
                })
                .ToList();
        }
    }
}
