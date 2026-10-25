using Microsoft.EntityFrameworkCore;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Implementación de <see cref="IBenchmarkIndexService"/> sobre las tablas BenchmarkIndices y BenchmarkPricePoints
    /// (SPEC-05-04). Sin claves foráneas: la baja borra el histórico desde aquí.
    /// </summary>
    public class BenchmarkIndexService : IBenchmarkIndexService
    {
        private const int MaxNameLength = 100;
        private const int MaxSymbolLength = 20;

        // Índices del catálogo de mercado (SPEC-01-04) que se ofrecen para dar de alta con un clic (RF-1)
        private static readonly (string Name, string Symbol)[] SuggestedIndices =
        {
            ("IBEX 35", "^IBEX"),
            ("EURO STOXX 50", "^STOXX50E"),
            ("DAX 40", "^GDAXI"),
            ("CAC 40", "^FCHI"),
            ("S&P 500", "^GSPC"),
            ("Nasdaq 100", "^NDX"),
            ("Dow Jones", "^DJI"),
        };

        private readonly AppDbContext _context;
        private readonly IStockPriceService _stockPriceService;

        public BenchmarkIndexService(AppDbContext context, IStockPriceService stockPriceService)
        {
            _context = context;
            _stockPriceService = stockPriceService;
        }

        public async Task<List<BenchmarkIndexSummary>> GetAllAsync()
        {
            var indices = await _context.BenchmarkIndices
                .AsNoTracking()
                .OrderBy(i => i.Name)
                .ToListAsync();

            var summaries = new List<BenchmarkIndexSummary>();
            foreach (var index in indices)
            {
                var last = await _context.BenchmarkPricePoints
                    .AsNoTracking()
                    .Where(p => p.BenchmarkIndexId == index.Id)
                    .OrderByDescending(p => p.Date)
                    .Select(p => new { p.Date, p.Close })
                    .FirstOrDefaultAsync();

                summaries.Add(new BenchmarkIndexSummary
                {
                    Id = index.Id,
                    Name = index.Name,
                    Symbol = index.Symbol,
                    LastClose = last?.Close,
                    LastDate = last?.Date
                });
            }

            return summaries;
        }

        public async Task<BenchmarkIndexResult> CreateAsync(BenchmarkIndexInput input)
        {
            var error = await ValidateAsync(null, input);
            if (error != null)
            {
                return BenchmarkIndexResult.Fail(error);
            }

            var now = DateTime.Now;
            var index = new BenchmarkIndex
            {
                Name = input.Name.Trim(),
                Symbol = NormalizeSymbol(input.Symbol),
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.BenchmarkIndices.Add(index);
            await _context.SaveChangesAsync();
            return BenchmarkIndexResult.Ok();
        }

        public async Task<BenchmarkIndexResult> UpdateAsync(int id, BenchmarkIndexInput input)
        {
            var index = await _context.BenchmarkIndices.FindAsync(id);
            if (index == null)
            {
                return BenchmarkIndexResult.Fail("El índice ya no existe");
            }

            var error = await ValidateAsync(id, input);
            if (error != null)
            {
                return BenchmarkIndexResult.Fail(error);
            }

            var symbol = NormalizeSymbol(input.Symbol);
            var symbolChanged = !string.Equals(index.Symbol, symbol, StringComparison.Ordinal);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Otro símbolo es otra serie: el histórico guardado ya no le corresponde
                if (symbolChanged)
                {
                    await RemoveHistoryAsync(id);
                }

                index.Name = input.Name.Trim();
                index.Symbol = symbol;
                index.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return BenchmarkIndexResult.Ok();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<BenchmarkIndexResult> DeleteAsync(int id)
        {
            var index = await _context.BenchmarkIndices.FindAsync(id);
            if (index == null)
            {
                return BenchmarkIndexResult.Fail("El índice ya no existe");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await RemoveHistoryAsync(id);
                _context.BenchmarkIndices.Remove(index);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return BenchmarkIndexResult.Ok();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<BenchmarkSymbolCheck> CheckSymbolAsync(string symbol)
        {
            var normalized = NormalizeSymbol(symbol);
            if (normalized.Length == 0)
            {
                return new BenchmarkSymbolCheck { Message = "Escribe el símbolo de Yahoo" };
            }

            var quote = await _stockPriceService.GetQuoteAsync(normalized);
            if (quote == null)
            {
                return new BenchmarkSymbolCheck { Message = "Yahoo no reconoce este símbolo" };
            }

            return new BenchmarkSymbolCheck
            {
                Found = true,
                Price = quote.Price,
                Currency = quote.Currency,
                Message = $"Último valor: {quote.Price:N2}{(string.IsNullOrEmpty(quote.Currency) ? string.Empty : " " + quote.Currency)}"
            };
        }

        public async Task<List<BenchmarkIndexInput>> GetSuggestionsAsync()
        {
            var existing = await _context.BenchmarkIndices
                .AsNoTracking()
                .Select(i => i.Symbol)
                .ToListAsync();

            return SuggestedIndices
                .Where(s => !existing.Contains(s.Symbol))
                .Select(s => new BenchmarkIndexInput { Name = s.Name, Symbol = s.Symbol })
                .ToList();
        }

        /// <summary>
        /// Borra los puntos del índice y su marca de histórico completo (sin guardar; dentro de la transacción del llamante)
        /// </summary>
        private async Task RemoveHistoryAsync(int id)
        {
            var points = await _context.BenchmarkPricePoints
                .Where(p => p.BenchmarkIndexId == id)
                .ToListAsync();
            _context.BenchmarkPricePoints.RemoveRange(points);

            var complete = await _context.AppSettings.FindAsync(PriceHistoryService.GetBenchmarkCompleteKey(id));
            if (complete != null)
            {
                _context.AppSettings.Remove(complete);
            }
        }

        private async Task<string?> ValidateAsync(int? id, BenchmarkIndexInput input)
        {
            var name = input.Name?.Trim() ?? string.Empty;
            var symbol = NormalizeSymbol(input.Symbol);

            if (name.Length == 0)
            {
                return "El nombre es obligatorio";
            }

            if (name.Length > MaxNameLength)
            {
                return $"El nombre no puede superar los {MaxNameLength} caracteres";
            }

            if (symbol.Length == 0)
            {
                return "El símbolo de Yahoo es obligatorio";
            }

            if (symbol.Length > MaxSymbolLength)
            {
                return $"El símbolo no puede superar los {MaxSymbolLength} caracteres";
            }

            var duplicated = await _context.BenchmarkIndices
                .AsNoTracking()
                .AnyAsync(i => i.Symbol == symbol && (id == null || i.Id != id));
            if (duplicated)
            {
                return "Ya hay un índice con este símbolo";
            }

            return null;
        }

        private static string NormalizeSymbol(string? symbol) => (symbol ?? string.Empty).Trim().ToUpperInvariant();
    }
}
