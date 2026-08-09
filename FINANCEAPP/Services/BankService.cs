using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FINANCEAPP.Data;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Servicio para la gestión de cuentas bancarias y operaciones
    /// </summary>
    public class BankService : IBankService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BankService> _logger;

        public BankService(AppDbContext context, ILogger<BankService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Cuentas Bancarias
        public async Task<List<BankAccount>> GetAllBankAccountsAsync()
        {
            try
            {
                return await _context.BankAccounts
                    .OrderBy(ba => ba.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todas las cuentas bancarias");
                throw;
            }
        }

        public async Task<BankAccount?> GetBankAccountByIdAsync(int id)
        {
            try
            {
                return await _context.BankAccounts
                    .FirstOrDefaultAsync(ba => ba.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener cuenta bancaria con ID {id}");
                throw;
            }
        }

        public async Task<BankAccount?> GetBankAccountByNameAsync(string name)
        {
            try
            {
                return await _context.BankAccounts
                    .FirstOrDefaultAsync(ba => ba.Name == name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener cuenta bancaria '{name}'");
                throw;
            }
        }

        public async Task<BankAccount> CreateBankAccountAsync(BankAccount account)
        {
            try
            {
                // Validar que no exista una cuenta con el mismo nombre
                var existingAccount = await GetBankAccountByNameAsync(account.Name);
                if (existingAccount != null)
                {
                    throw new InvalidOperationException($"Ya existe una cuenta llamada '{account.Name}'");
                }

                _context.BankAccounts.Add(account);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Cuenta bancaria '{account.Name}' creada exitosamente");
                return account;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear cuenta bancaria '{account.Name}'");
                throw;
            }
        }

        public async Task<BankAccount> UpdateBankAccountAsync(BankAccount account)
        {
            try
            {
                var existingAccount = await GetBankAccountByIdAsync(account.Id);
                if (existingAccount == null)
                {
                    throw new InvalidOperationException($"No existe cuenta bancaria con ID {account.Id}");
                }

                // Si cambió el nombre, validar que no exista otra cuenta con ese nombre
                if (existingAccount.Name != account.Name)
                {
                    var duplicateAccount = await GetBankAccountByNameAsync(account.Name);
                    if (duplicateAccount != null)
                    {
                        throw new InvalidOperationException($"Ya existe una cuenta llamada '{account.Name}'");
                    }
                }

                existingAccount.Name = account.Name;
                existingAccount.BankName = account.BankName;
                existingAccount.IBAN = account.IBAN;
                existingAccount.AccountType = account.AccountType;
                existingAccount.CurrentBalance = account.CurrentBalance;
                existingAccount.UpdatedAt = DateTime.Now;

                _context.BankAccounts.Update(existingAccount);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Cuenta bancaria '{existingAccount.Name}' actualizada exitosamente");
                return existingAccount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar cuenta bancaria con ID {account.Id}");
                throw;
            }
        }

        public async Task<bool> DeleteBankAccountAsync(int id)
        {
            try
            {
                var account = await GetBankAccountByIdAsync(id);
                if (account == null)
                {
                    return false;
                }

                // Eliminar todas las operaciones de la cuenta
                var operations = await GetOperationsByAccountNameAsync(account.Name);
                _context.BankOperations.RemoveRange(operations);

                // Eliminar la cuenta
                _context.BankAccounts.Remove(account);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Cuenta bancaria con ID {id} eliminada exitosamente");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar cuenta bancaria con ID {id}");
                throw;
            }
        }

        public async Task<decimal> GetTotalBalanceAsync()
        {
            try
            {
                return await _context.BankAccounts
                    .SumAsync(ba => ba.CurrentBalance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular saldo total");
                throw;
            }
        }

        public async Task<decimal> RecalculateAccountBalanceAsync(string accountName)
        {
            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. Obtener la cuenta
                        var account = await GetBankAccountByNameAsync(accountName);
                        if (account == null)
                        {
                            throw new InvalidOperationException($"No existe cuenta llamada '{accountName}'");
                        }

                        // 2. Obtener todas las operaciones
                        var operations = await GetOperationsByAccountNameAsync(accountName);

                        // 3. Calcular el nuevo saldo
                        decimal newBalance = 0;
                        foreach (var op in operations)
                        {
                            // Ingresos positivos
                            if (op.Type == "Ingreso" || op.Type == "Intereses" || op.Type == "Traspaso Entrada")
                            {
                                newBalance += op.Amount;
                            }
                            // Gastos negativos
                            else if (op.Type == "Gasto" || op.Type == "Comisiones" || op.Type == "Traspaso Salida")
                            {
                                newBalance += op.Amount; // Ya vienen negativos
                            }
                        }

                        // 4. Actualizar el saldo
                        account.CurrentBalance = newBalance;
                        account.UpdatedAt = DateTime.Now;
                        _context.BankAccounts.Update(account);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        _logger.LogInformation($"Saldo de cuenta '{accountName}' recalculado: {newBalance}");
                        return newBalance;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al recalcular saldo de cuenta '{accountName}'");
                throw;
            }
        }
        #endregion

        #region Operaciones Bancarias
        public async Task<List<BankOperation>> GetOperationsByAccountNameAsync(string accountName)
        {
            try
            {
                return await _context.BankOperations
                    .Where(bo => bo.AccountName == accountName)
                    .OrderByDescending(bo => bo.Date)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener operaciones de cuenta '{accountName}'");
                throw;
            }
        }

        public async Task<BankOperation?> GetOperationByIdAsync(int id)
        {
            try
            {
                return await _context.BankOperations
                    .FirstOrDefaultAsync(bo => bo.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener operación bancaria con ID {id}");
                throw;
            }
        }

        public async Task<BankOperation> CreateOperationAsync(BankOperation operation)
        {
            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. Validar que la cuenta exista
                        var account = await GetBankAccountByNameAsync(operation.AccountName);
                        if (account == null)
                        {
                            throw new InvalidOperationException($"No existe cuenta llamada '{operation.AccountName}'");
                        }

                        // 2. Crear la operación
                        _context.BankOperations.Add(operation);
                        await _context.SaveChangesAsync();

                        // 3. Recalcular el saldo de la cuenta
                        await RecalculateAccountBalanceAsync(operation.AccountName);

                        await transaction.CommitAsync();

                        _logger.LogInformation($"Operación bancaria en '{operation.AccountName}' ({operation.Type}) creada exitosamente");
                        return operation;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear operación bancaria");
                throw;
            }
        }

        public async Task<BankOperation> UpdateOperationAsync(BankOperation operation)
        {
            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var existingOperation = await GetOperationByIdAsync(operation.Id);
                        if (existingOperation == null)
                        {
                            throw new InvalidOperationException($"No existe operación bancaria con ID {operation.Id}");
                        }

                        string oldAccountName = existingOperation.AccountName;

                        existingOperation.AccountName = operation.AccountName;
                        existingOperation.Type = operation.Type;
                        existingOperation.Amount = operation.Amount;
                        existingOperation.Date = operation.Date;
                        existingOperation.Description = operation.Description;

                        _context.BankOperations.Update(existingOperation);
                        await _context.SaveChangesAsync();

                        // Recalcular saldo de la cuenta anterior
                        await RecalculateAccountBalanceAsync(oldAccountName);

                        // Recalcular saldo de la cuenta nueva (si cambió)
                        if (oldAccountName != operation.AccountName)
                        {
                            await RecalculateAccountBalanceAsync(operation.AccountName);
                        }

                        await transaction.CommitAsync();

                        _logger.LogInformation($"Operación bancaria con ID {operation.Id} actualizada exitosamente");
                        return existingOperation;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar operación bancaria con ID {operation.Id}");
                throw;
            }
        }

        public async Task<bool> DeleteOperationAsync(int id)
        {
            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var operation = await GetOperationByIdAsync(id);
                        if (operation == null)
                        {
                            return false;
                        }

                        string accountName = operation.AccountName;
                        _context.BankOperations.Remove(operation);
                        await _context.SaveChangesAsync();

                        // Recalcular el saldo de la cuenta
                        await RecalculateAccountBalanceAsync(accountName);

                        await transaction.CommitAsync();

                        _logger.LogInformation($"Operación bancaria con ID {id} eliminada exitosamente");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar operación bancaria con ID {id}");
                throw;
            }
        }

        public async Task<decimal> GetTotalIncomesAsync(string accountName)
        {
            try
            {
                return await _context.BankOperations
                    .Where(bo => bo.AccountName == accountName && 
                           (bo.Type == "Ingreso" || bo.Type == "Intereses" || bo.Type == "Traspaso Entrada"))
                    .SumAsync(bo => bo.Amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al calcular ingresos de cuenta '{accountName}'");
                throw;
            }
        }

        public async Task<decimal> GetTotalExpensesAsync(string accountName)
        {
            try
            {
                var expenses = await _context.BankOperations
                    .Where(bo => bo.AccountName == accountName && 
                           (bo.Type == "Gasto" || bo.Type == "Comisiones" || bo.Type == "Traspaso Salida"))
                    .SumAsync(bo => Math.Abs(bo.Amount));

                return expenses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al calcular gastos de cuenta '{accountName}'");
                throw;
            }
        }

        public async Task<List<BankOperation>> GetOperationsByDateRangeAsync(string accountName, DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.BankOperations
                    .Where(bo => bo.AccountName == accountName && 
                           bo.Date >= startDate && 
                           bo.Date <= endDate)
                    .OrderByDescending(bo => bo.Date)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener operaciones de cuenta '{accountName}' en rango de fechas");
                throw;
            }
        }
        #endregion
    }
}
