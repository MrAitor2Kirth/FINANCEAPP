using Microsoft.EntityFrameworkCore;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Implementación de <see cref="ISaleHistoryService"/> sobre la tabla SalesHistory.
    /// </summary>
    public class SaleHistoryService : ISaleHistoryService
    {
        private readonly AppDbContext _context;
        private readonly IBankService _bankService;

        public SaleHistoryService(AppDbContext context, IBankService bankService)
        {
            _context = context;
            _bankService = bankService;
        }

        public Task<List<SaleHistory>> GetAllAsync() => GetByPeriodAsync(SalesPeriodFilter.All);

        public async Task<List<SaleHistory>> GetByPeriodAsync(SalesPeriodFilter filter)
        {
            var query = _context.SalesHistory.AsNoTracking();

            // Rango semiabierto: una venta con hora entra en su día natural
            var (from, toExclusive) = filter.GetRange();
            if (from.HasValue)
            {
                query = query.Where(s => s.SaleDate >= from.Value);
            }
            if (toExclusive.HasValue)
            {
                query = query.Where(s => s.SaleDate < toExclusive.Value);
            }

            return await query
                .OrderByDescending(s => s.SaleDate)
                .ThenByDescending(s => s.Id)
                .ToListAsync();
        }

        public async Task<bool> HasAnyAsync()
        {
            return await _context.SalesHistory.AnyAsync();
        }

        public async Task<SaleHistory?> GetByIdAsync(int id)
        {
            return await _context.SalesHistory
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<bool> UpdateSellPriceAsync(int id, decimal sellPrice)
        {
            if (sellPrice <= 0)
            {
                return false;
            }

            var record = await _context.SalesHistory.FirstOrDefaultAsync(s => s.Id == id);
            if (record == null)
            {
                return false;
            }

            // El ingreso bancario de la venta sigue al precio (SPEC-02-04): mismo neto que al vender
            var sale = await _context.Operations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == record.OperationId && o.Type == "Venta");
            var commission = sale?.Commission ?? 0m;
            if (OperationAmountCalculator.CalculateSaleNet(sellPrice, record.NumberOfShares, commission, null) <= 0)
            {
                throw new InvalidOperationException(OperationAmountCalculator.SaleNetNotPositiveMessage);
            }

            // SQLite no admite transacciones anidadas: los métodos del banco se llaman dentro de esta
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Mismo cálculo que al registrar la venta:
                // beneficio = importe cobrado - coste medio de compra - comisiones
                record.SellPrice = Math.Round(sellPrice, 2);
                record.Amount = Math.Round(record.SellPrice * record.NumberOfShares, 2);
                record.Profit = Math.Round(
                    record.Amount - (record.AverageBuyPrice * record.NumberOfShares) - record.Commission, 2);
                record.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                await _bankService.UpdateSaleDepositAsync(
                    record.Id,
                    OperationAmountCalculator.CalculateSaleNet(record.SellPrice, record.NumberOfShares, commission, sale?.ExchangeRate),
                    OperationAmountCalculator.BuildSaleDepositDescription(record, sale?.CurrencyCode));

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var record = await _context.SalesHistory.FirstOrDefaultAsync(s => s.Id == id);
            if (record == null)
            {
                return false;
            }

            _context.SalesHistory.Remove(record);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
