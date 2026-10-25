using Microsoft.EntityFrameworkCore;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Implementación de <see cref="IChartMarkService"/> sobre la tabla ChartMarks (SPEC-05-03).
    /// Cada llamada toca una sola tabla y guarda una vez: no abre transacción.
    /// </summary>
    public class ChartMarkService : IChartMarkService
    {
        private const int MaxDescriptionLength = 200;

        private readonly AppDbContext _context;

        public ChartMarkService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ChartMark>> GetForCompanyAsync(int companyId)
        {
            return await _context.ChartMarks
                .AsNoTracking()
                .Where(m => m.CompanyId == companyId || m.CompanyId == null)
                .OrderByDescending(m => m.Date)
                .ThenByDescending(m => m.Id)
                .ToListAsync();
        }

        public async Task<ChartMarkResult> CreateAsync(int companyId, ChartMarkInput input)
        {
            var error = await ValidateAsync(companyId, input);
            if (error != null)
            {
                return ChartMarkResult.Fail(error);
            }

            var now = DateTime.Now;
            var mark = new ChartMark
            {
                CompanyId = input.IsGlobal ? null : companyId,
                Date = input.Date.Date,
                Description = input.Description.Trim(),
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.ChartMarks.Add(mark);
            await _context.SaveChangesAsync();
            return ChartMarkResult.Ok(mark);
        }

        public async Task<ChartMarkResult> UpdateAsync(int markId, int companyId, ChartMarkInput input)
        {
            var mark = await _context.ChartMarks.FindAsync(markId);
            if (mark == null)
            {
                return ChartMarkResult.Fail("La marca ya no existe.");
            }

            var error = await ValidateAsync(companyId, input);
            if (error != null)
            {
                return ChartMarkResult.Fail(error);
            }

            mark.Date = input.Date.Date;
            mark.Description = input.Description.Trim();
            // Una global que pasa a ser de Empresa queda ligada a la empresa del gráfico desde el que se edita
            mark.CompanyId = input.IsGlobal ? null : companyId;
            mark.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return ChartMarkResult.Ok(mark);
        }

        public async Task<ChartMarkResult> SetActiveAsync(int markId, bool isActive)
        {
            var mark = await _context.ChartMarks.FindAsync(markId);
            if (mark == null)
            {
                return ChartMarkResult.Fail("La marca ya no existe.");
            }

            mark.IsActive = isActive;
            mark.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return ChartMarkResult.Ok(mark);
        }

        public async Task<ChartMarkResult> DeleteAsync(int markId)
        {
            var mark = await _context.ChartMarks.FindAsync(markId);
            if (mark == null)
            {
                return ChartMarkResult.Fail("La marca ya no existe.");
            }

            _context.ChartMarks.Remove(mark);
            await _context.SaveChangesAsync();
            return ChartMarkResult.Ok();
        }

        /// <summary>
        /// Reglas de RF-8: descripción obligatoria y de 200 caracteres como máximo, fecha no futura y empresa existente
        /// </summary>
        private async Task<string?> ValidateAsync(int companyId, ChartMarkInput input)
        {
            var description = input.Description?.Trim() ?? string.Empty;
            if (description.Length == 0)
            {
                return "La descripción es obligatoria";
            }

            if (description.Length > MaxDescriptionLength)
            {
                return $"La descripción no puede superar los {MaxDescriptionLength} caracteres";
            }

            if (input.Date == default)
            {
                return "La fecha es obligatoria";
            }

            if (input.Date.Date > DateTime.Today)
            {
                return "La fecha no puede ser futura";
            }

            if (!input.IsGlobal && !await _context.CompanyDetails.AnyAsync(c => c.Id == companyId))
            {
                return "La empresa ya no existe.";
            }

            return null;
        }
    }
}
