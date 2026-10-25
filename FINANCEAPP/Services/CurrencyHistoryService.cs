using Microsoft.EntityFrameworkCore;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Implementación de <see cref="ICurrencyHistoryService"/> (SPEC-01-09). El par es una serie más de
    /// <see cref="IPriceHistoryService"/> (<see cref="ChartSeriesKind.Currency"/>): bloques, caché de 12 horas y relleno
    /// hacia atrás son los mismos que los de las empresas y los índices.
    /// </summary>
    public class CurrencyHistoryService : ICurrencyHistoryService
    {
        private readonly AppDbContext _context;
        private readonly IPriceHistoryService _priceHistoryService;

        public CurrencyHistoryService(AppDbContext context, IPriceHistoryService priceHistoryService)
        {
            _context = context;
            _priceHistoryService = priceHistoryService;
        }

        public async Task<Dictionary<DateTime, decimal>> GetRatesAtAsync(string currencyCode, string baseCurrencyCode, IEnumerable<DateTime> dates, CancellationToken cancellationToken = default)
        {
            var currency = CurrencyFormatter.Normalize(currencyCode);
            var baseCurrency = CurrencyFormatter.Normalize(baseCurrencyCode);
            var days = dates.Select(d => d.Date).Distinct().ToList();
            var rates = new Dictionary<DateTime, decimal>();
            if (days.Count == 0)
            {
                return rates;
            }

            if (currency == baseCurrency)
            {
                return days.ToDictionary(d => d, _ => 1m);
            }

            var pair = await GetOrCreatePairAsync(currency, baseCurrency);
            var series = ChartSeriesRef.ForCurrency(pair.Id, pair.Symbol, $"{currency}/{baseCurrency}");

            // Una sola cobertura hasta la fecha más antigua; después, una consulta por fecha
            await _priceHistoryService.EnsureDailyCoverageAsync(series, days.Min(), cancellationToken);
            foreach (var day in days)
            {
                var close = await _priceHistoryService.GetDailyCloseAtAsync(series, day);
                if (close is > 0)
                {
                    rates[day] = close.Value;
                }
            }

            return rates;
        }

        /// <summary>Par de divisas guardado o uno nuevo con su símbolo de Yahoo</summary>
        private async Task<CurrencyPair> GetOrCreatePairAsync(string currency, string baseCurrency)
        {
            var pair = await _context.CurrencyPairs
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.CurrencyCode == currency && p.BaseCurrencyCode == baseCurrency);
            if (pair != null)
            {
                return pair;
            }

            pair = new CurrencyPair
            {
                CurrencyCode = currency,
                BaseCurrencyCode = baseCurrency,
                Symbol = $"{currency}{baseCurrency}=X",
                CreatedAt = DateTime.Now
            };
            _context.CurrencyPairs.Add(pair);
            await _context.SaveChangesAsync();
            _context.Entry(pair).State = EntityState.Detached;
            return pair;
        }
    }
}
