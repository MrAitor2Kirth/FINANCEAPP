using Microsoft.EntityFrameworkCore;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Implementación del servicio financiero
    /// Maneja toda la lógica de negocio y acceso a datos para registros financieros
    /// </summary>
    public class FinancialService : IFinancialService
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Constructor con inyección de dependencias del contexto de base de datos
        /// </summary>
        /// <param name="context">Contexto de Entity Framework</param>
        public FinancialService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todos los registros financieros ordenados por fecha descendente
        /// </summary>
        public async Task<List<FinancialRecord>> GetAllRecordsAsync()
        {
            try
            {
                return await _context.FinancialRecords
                    .OrderByDescending(r => r.Date)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Log del error (en producción usar ILogger)
                Console.WriteLine($"Error al obtener registros: {ex.Message}");
                return new List<FinancialRecord>();
            }
        }

        /// <summary>
        /// Obtiene un registro específico por ID
        /// </summary>
        public async Task<FinancialRecord?> GetRecordByIdAsync(int id)
        {
            try
            {
                return await _context.FinancialRecords
                    .FirstOrDefaultAsync(r => r.Id == id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener registro {id}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Agrega un nuevo registro financiero
        /// Valida que la fecha no sea futura y establece la fecha de creación
        /// </summary>
        public async Task<FinancialRecord> AddRecordAsync(FinancialRecord record)
        {
            try
            {
                // Validación: la fecha no puede ser futura
                if (record.Date > DateTime.Now)
                {
                    throw new ArgumentException("La fecha del registro no puede ser futura");
                }

                // Establecer fecha de creación
                record.CreatedAt = DateTime.Now;

                // Agregar a la base de datos
                _context.FinancialRecords.Add(record);
                await _context.SaveChangesAsync();

                return record;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al agregar registro: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Actualiza un registro existente
        /// </summary>
        public async Task<FinancialRecord> UpdateRecordAsync(FinancialRecord record)
        {
            try
            {
                // Verificar que el registro existe
                var existingRecord = await _context.FinancialRecords
                    .FirstOrDefaultAsync(r => r.Id == record.Id);

                if (existingRecord == null)
                {
                    throw new ArgumentException($"No se encontró el registro con ID {record.Id}");
                }

                // Actualizar propiedades
                existingRecord.Date = record.Date;
                existingRecord.Stocks = record.Stocks;
                existingRecord.Dividends = record.Dividends;
                existingRecord.CapitalGains = record.CapitalGains;
                existingRecord.Profits = record.Profits;
                existingRecord.Funds = record.Funds;
                existingRecord.SafeHaven = record.SafeHaven;
                existingRecord.InitialCapital = record.InitialCapital;
                existingRecord.BankAccount = record.BankAccount;
                existingRecord.Notes = record.Notes;

                // Guardar cambios
                await _context.SaveChangesAsync();

                return existingRecord;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar registro: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Elimina un registro por su ID
        /// </summary>
        public async Task<bool> DeleteRecordAsync(int id)
        {
            try
            {
                var record = await _context.FinancialRecords
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (record == null)
                {
                    return false;
                }

                _context.FinancialRecords.Remove(record);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar registro {id}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Calcula el balance total sumando el Total de todos los registros
        /// Útil para mostrar el patrimonio total
        /// </summary>
        public async Task<decimal> GetTotalBalanceAsync()
        {
            try
            {
                var records = await _context.FinancialRecords.ToListAsync();

                // Sumar el Total calculado de cada registro
                return records.Sum(r => r.Total);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al calcular balance total: {ex.Message}");
                return 0m;
            }
        }

        /// <summary>
        /// Obtiene registros dentro de un rango de fechas específico
        /// Útil para reportes y análisis de períodos
        /// </summary>
        public async Task<List<FinancialRecord>> GetRecordsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.FinancialRecords
                    .Where(r => r.Date >= startDate && r.Date <= endDate)
                    .OrderByDescending(r => r.Date)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener registros por rango de fechas: {ex.Message}");
                return new List<FinancialRecord>();
            }
        }
    }
}
