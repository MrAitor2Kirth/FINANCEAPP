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

        public SaleHistoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SaleHistory>> GetAllAsync()
        {
            return await _context.SalesHistory
                .AsNoTracking()
                .OrderByDescending(s => s.SaleDate)
                .ThenByDescending(s => s.Id)
                .ToListAsync();
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

            // Mismo cálculo que al registrar la venta:
            // beneficio = importe cobrado - coste medio de compra - comisiones
            record.SellPrice = Math.Round(sellPrice, 2);
            record.Amount = Math.Round(record.SellPrice * record.NumberOfShares, 2);
            record.Profit = Math.Round(
                record.Amount - (record.AverageBuyPrice * record.NumberOfShares) - record.Commission, 2);
            record.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
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
