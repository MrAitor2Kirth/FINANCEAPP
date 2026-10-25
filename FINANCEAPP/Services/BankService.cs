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

                // Sin movimientos aún, el saldo escrito es el saldo inicial
                account.OpeningBalance = account.CurrentBalance;

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

                // Los movimientos siguen ligados al nombre anterior: se calcula antes de renombrar
                var accountOperations = await GetOperationsByAccountNameAsync(existingAccount.Name);

                existingAccount.Name = account.Name;
                existingAccount.BankName = account.BankName;
                existingAccount.IBAN = account.IBAN;
                existingAccount.AccountType = account.AccountType;
                existingAccount.CurrentBalance = account.CurrentBalance;

                // El saldo escrito es el que se ve: el saldo inicial absorbe la diferencia con los movimientos
                existingAccount.OpeningBalance = account.CurrentBalance - accountOperations.Sum(GetEffect);
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
                // Sin transacción propia: SQLite no admite anidadas y este método se llama
                // desde las transacciones de alta, edición y baja de movimientos.
                var account = await GetBankAccountByNameAsync(accountName);
                if (account == null)
                {
                    throw new InvalidOperationException($"No existe cuenta llamada '{accountName}'");
                }

                var operations = await GetOperationsByAccountNameAsync(accountName);

                // Saldo = saldo inicial + efecto de cada movimiento según su tipo
                decimal newBalance = account.OpeningBalance + operations.Sum(GetEffect);

                account.CurrentBalance = newBalance;
                account.UpdatedAt = DateTime.Now;
                _context.BankAccounts.Update(account);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Saldo de cuenta '{accountName}' recalculado: {newBalance}");
                return newBalance;
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
                ValidateOperation(operation);

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

                        // 2. Crear la operación con el signo que corresponde a su tipo
                        operation.Amount = GetSignedAmount(operation.Type, operation.Amount);
                        operation.Description = string.IsNullOrWhiteSpace(operation.Description) ? null : operation.Description.Trim();
                        _context.BankOperations.Add(operation);
                        await _context.SaveChangesAsync();

                        // 3. Recalcular el saldo de la cuenta
                        await RecalculateAccountBalanceAsync(operation.AccountName);

                        await transaction.CommitAsync();

                        _logger.LogInformation($"Operación bancaria en '{operation.AccountName}' ({operation.Type}) creada exitosamente");
                        return operation;
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        // El contexto es de toda la app: no debe quedar la operación pendiente de insertar
                        _context.Entry(operation).State = EntityState.Detached;
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
                ValidateOperation(operation);

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    BankOperation? existingOperation = null;
                    BankOperation? partner = null;
                    try
                    {
                        existingOperation = await GetOperationByIdAsync(operation.Id);
                        if (existingOperation == null)
                        {
                            throw new InvalidOperationException($"No existe operación bancaria con ID {operation.Id}");
                        }

                        await ThrowIfLinkedMovementAsync(existingOperation);

                        // Pata de un traspaso (SPEC-10-02): el tipo no cambia; importe y fecha van a las dos patas
                        partner = await GetTransferPartnerAsync(existingOperation);
                        if (partner != null && !string.Equals(operation.Type, existingOperation.Type, StringComparison.Ordinal))
                        {
                            throw new InvalidOperationException("Este movimiento es un traspaso: solo puedes cambiar importe, fecha y descripción.");
                        }

                        // La cuenta de un movimiento no se cambia
                        existingOperation.Type = operation.Type;
                        existingOperation.Amount = GetSignedAmount(operation.Type, operation.Amount);
                        existingOperation.Date = operation.Date;
                        existingOperation.Description = string.IsNullOrWhiteSpace(operation.Description) ? null : operation.Description.Trim();

                        if (partner != null)
                        {
                            partner.Amount = GetSignedAmount(partner.Type, operation.Amount);
                            partner.Date = operation.Date;
                        }

                        _context.BankOperations.Update(existingOperation);
                        await _context.SaveChangesAsync();

                        if (partner != null)
                        {
                            await RecalculateAccountsAsync(new[] { existingOperation.AccountName, partner.AccountName });
                        }
                        else
                        {
                            await RecalculateAccountBalanceAsync(existingOperation.AccountName);
                        }

                        await transaction.CommitAsync();

                        _logger.LogInformation($"Operación bancaria con ID {operation.Id} actualizada exitosamente");
                        return existingOperation;
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        if (existingOperation != null)
                        {
                            // Descarta los cambios en memoria del contexto compartido
                            await _context.Entry(existingOperation).ReloadAsync();
                        }

                        if (partner != null)
                        {
                            await _context.Entry(partner).ReloadAsync();
                        }
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

                        await ThrowIfLinkedMovementAsync(operation);

                        string accountName = operation.AccountName;
                        _context.BankOperations.Remove(operation);

                        // Una pata de traspaso se borra junto a su pareja (SPEC-10-02)
                        var partner = await GetTransferPartnerAsync(operation);
                        if (partner != null)
                        {
                            _context.BankOperations.Remove(partner);
                        }

                        await _context.SaveChangesAsync();

                        // Recalcular el saldo de la cuenta (y el de la cuenta de la otra pata)
                        if (partner != null)
                        {
                            await RecalculateAccountsAsync(new[] { accountName, partner.AccountName });
                        }
                        else
                        {
                            await RecalculateAccountBalanceAsync(accountName);
                        }

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

        #region Ingresos de ventas de acciones (SPEC-02-04)
        /// <summary>
        /// Mensaje cuando se intenta vender sin ninguna cuenta bancaria
        /// </summary>
        public const string NoBankAccountsMessage = "Crea una cuenta bancaria en Banco antes de vender: el importe de la venta se ingresa en ella.";

        public async Task<BankAccount?> GetDefaultBankAccountAsync()
        {
            return await _context.BankAccounts
                .OrderBy(ba => ba.CreatedAt)
                .ThenBy(ba => ba.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<BankAccount> ResolveSaleAccountAsync(string? accountName)
        {
            var defaultAccount = await GetDefaultBankAccountAsync();
            if (defaultAccount == null)
            {
                throw new InvalidOperationException(NoBankAccountsMessage);
            }

            if (string.IsNullOrWhiteSpace(accountName))
            {
                return defaultAccount;
            }

            // Nombre exacto: algunas cuentas guardan espacios al final
            var account = await GetBankAccountByNameAsync(accountName);
            return account ?? throw new InvalidOperationException($"La cuenta bancaria «{accountName.Trim()}» ya no existe.");
        }

        public async Task<BankOperation> AddSaleDepositAsync(string accountName, decimal amount, DateTime date, string description, int saleHistoryId)
        {
            var operation = new BankOperation
            {
                AccountName = accountName,
                Type = "Ingreso",
                Amount = amount,
                Date = date,
                Description = description,
                SaleHistoryId = saleHistoryId
            };

            ValidateOperation(operation);
            operation.Amount = GetSignedAmount(operation.Type, operation.Amount);

            _context.BankOperations.Add(operation);
            await _context.SaveChangesAsync();
            await RecalculateAccountBalanceAsync(accountName);

            _logger.LogInformation($"Ingreso de la venta nº {saleHistoryId} en '{accountName}' creado");
            return operation;
        }

        public async Task UpdateSaleDepositAsync(int saleHistoryId, decimal amount, string description)
        {
            var deposits = await _context.BankOperations
                .Where(bo => bo.SaleHistoryId == saleHistoryId)
                .ToListAsync();
            if (deposits.Count == 0)
            {
                return;
            }

            foreach (var deposit in deposits)
            {
                deposit.Amount = amount;
                deposit.Description = description;
                ValidateOperation(deposit);
                deposit.Amount = GetSignedAmount(deposit.Type, deposit.Amount);
            }

            await _context.SaveChangesAsync();
            await RecalculateAccountsAsync(deposits.Select(d => d.AccountName));
        }

        public async Task RemoveSaleDepositsAsync(int saleHistoryId)
        {
            var deposits = await _context.BankOperations
                .Where(bo => bo.SaleHistoryId == saleHistoryId)
                .ToListAsync();
            if (deposits.Count == 0)
            {
                return;
            }

            _context.BankOperations.RemoveRange(deposits);
            await _context.SaveChangesAsync();
            await RecalculateAccountsAsync(deposits.Select(d => d.AccountName));
        }

        public async Task<Dictionary<int, string>> GetSaleDepositsAsync(IEnumerable<int> saleHistoryIds)
        {
            var ids = saleHistoryIds.ToList();
            var deposits = await _context.BankOperations
                .AsNoTracking()
                .Where(bo => bo.SaleHistoryId != null && ids.Contains(bo.SaleHistoryId.Value))
                .Select(bo => new { Id = bo.SaleHistoryId!.Value, bo.AccountName })
                .ToListAsync();

            return deposits
                .GroupBy(d => d.Id)
                .ToDictionary(g => g.Key, g => g.First().AccountName);
        }

        /// <summary>
        /// Recalcula una vez cada cuenta que todavía existe (una cuenta renombrada ya no tiene ese nombre)
        /// </summary>
        private async Task RecalculateAccountsAsync(IEnumerable<string> accountNames)
        {
            foreach (var name in accountNames.Distinct(StringComparer.Ordinal))
            {
                if (await GetBankAccountByNameAsync(name) != null)
                {
                    await RecalculateAccountBalanceAsync(name);
                }
            }
        }

        /// <summary>
        /// Los ingresos generados por una venta solo se cambian desde el histórico de ventas (SPEC-02-04, RF-9) y
        /// los movimientos de una compra o dividendo, desde Operaciones (SPEC-02-06, RF-9). Si la operación
        /// de un movimiento ya no existe, se trata como un movimiento normal.
        /// </summary>
        public async Task<string?> GetEditLockMessageAsync(BankOperation operation)
        {
            try
            {
                await ThrowIfLinkedMovementAsync(operation);
                return null;
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }
        }

        private async Task ThrowIfLinkedMovementAsync(BankOperation operation)
        {
            if (operation.SaleHistoryId.HasValue)
            {
                throw new InvalidOperationException(
                    $"Este ingreso es de la venta nº {operation.SaleHistoryId.Value}: para cambiarlo, deshaz la venta en «Histórico de ventas» o edita allí su precio.");
            }

            if (operation.OperationId.HasValue)
            {
                var key = operation.OperationId.Value;
                var exists = await _context.Operations.AnyAsync(o => o.Id == key || o.OriginOperationId == key);
                if (exists)
                {
                    throw new InvalidOperationException(
                        $"Este movimiento es de la operación nº {key}: para cambiarlo, edítala o bórrala en «Operaciones».");
                }
            }
        }
        #endregion

        #region Traspasos entre cuentas (SPEC-10-02)
        /// <summary>
        /// Otra pata del traspaso al que pertenece el movimiento; null si no es de un traspaso o la pareja ya no existe
        /// </summary>
        private async Task<BankOperation?> GetTransferPartnerAsync(BankOperation operation)
        {
            if (!operation.TransferId.HasValue)
            {
                return null;
            }

            var transferId = operation.TransferId.Value;
            return await _context.BankOperations
                .FirstOrDefaultAsync(bo => bo.TransferId == transferId && bo.Id != operation.Id);
        }

        public async Task<(BankOperation Outgoing, BankOperation Incoming)> CreateTransferAsync(
            string fromAccount, string toAccount, decimal amount, DateTime date, string? description)
        {
            if (string.IsNullOrWhiteSpace(fromAccount))
            {
                throw new InvalidOperationException("Selecciona la cuenta de origen.");
            }

            if (string.IsNullOrWhiteSpace(toAccount))
            {
                throw new InvalidOperationException("Selecciona la cuenta de destino.");
            }

            if (string.Equals(fromAccount, toAccount, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("La cuenta de origen y la de destino deben ser distintas.");
            }

            if (await GetBankAccountByNameAsync(fromAccount) == null)
            {
                throw new InvalidOperationException($"La cuenta bancaria «{fromAccount.Trim()}» ya no existe.");
            }

            if (await GetBankAccountByNameAsync(toAccount) == null)
            {
                throw new InvalidOperationException($"La cuenta bancaria «{toAccount.Trim()}» ya no existe.");
            }

            var text = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            var outgoing = new BankOperation
            {
                AccountName = fromAccount,
                Type = "Traspaso Salida",
                Amount = amount,
                Date = date,
                Description = text ?? $"Traspaso a {toAccount.Trim()}"
            };
            var incoming = new BankOperation
            {
                AccountName = toAccount,
                Type = "Traspaso Entrada",
                Amount = amount,
                Date = date,
                Description = text ?? $"Traspaso desde {fromAccount.Trim()}"
            };

            // Reglas de importe, fecha y descripción (mismas que cualquier movimiento)
            ValidateOperation(outgoing);
            ValidateOperation(incoming);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var transferId = (await _context.BankOperations.MaxAsync(bo => (int?)bo.TransferId) ?? 0) + 1;
                outgoing.TransferId = transferId;
                incoming.TransferId = transferId;
                outgoing.Amount = GetSignedAmount(outgoing.Type, outgoing.Amount);
                incoming.Amount = GetSignedAmount(incoming.Type, incoming.Amount);

                _context.BankOperations.AddRange(outgoing, incoming);
                await _context.SaveChangesAsync();

                await RecalculateAccountBalanceAsync(fromAccount);
                await RecalculateAccountBalanceAsync(toAccount);

                await transaction.CommitAsync();

                _logger.LogInformation($"Traspaso nº {transferId} de '{fromAccount}' a '{toAccount}' creado");
                return (outgoing, incoming);
            }
            catch
            {
                await transaction.RollbackAsync();
                // El contexto es de toda la app: no deben quedar las patas pendientes de insertar
                _context.Entry(outgoing).State = EntityState.Detached;
                _context.Entry(incoming).State = EntityState.Detached;
                throw;
            }
        }

        public async Task<Dictionary<int, string>> GetTransferCounterpartsAsync(IEnumerable<int> transferIds, string accountName)
        {
            var ids = transferIds.Distinct().ToList();
            var legs = await _context.BankOperations
                .AsNoTracking()
                .Where(bo => bo.TransferId != null && ids.Contains(bo.TransferId.Value))
                .Select(bo => new { Id = bo.TransferId!.Value, bo.AccountName })
                .ToListAsync();

            // La cuenta de la otra pata; si la pareja ya no existe, el movimiento no aparece (se trata como normal)
            return legs
                .GroupBy(l => l.Id)
                .Where(g => g.Count() > 1)
                .ToDictionary(
                    g => g.Key,
                    g => g.FirstOrDefault(l => l.AccountName != accountName)?.AccountName ?? g.First().AccountName);
        }
        #endregion

        #region Movimientos de compras y dividendos (SPEC-02-06)
        /// <summary>
        /// Valor de la opción «No registrar en banco» del desplegable de cuenta
        /// </summary>
        public const string NoBankMovementValue = "__none__";

        public async Task<BankAccount?> ResolveTradeAccountAsync(string? accountName)
        {
            if (accountName == NoBankMovementValue)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(accountName))
            {
                return await GetDefaultBankAccountAsync();
            }

            var account = await GetBankAccountByNameAsync(accountName);
            return account ?? throw new InvalidOperationException($"La cuenta bancaria «{accountName.Trim()}» ya no existe.");
        }

        public async Task<BankOperation> AddTradeMovementAsync(string accountName, string type, decimal amount, DateTime date, string description, int operationKey)
        {
            var operation = new BankOperation
            {
                AccountName = accountName,
                Type = type,
                Amount = amount,
                Date = date,
                Description = description,
                OperationId = operationKey
            };

            ValidateOperation(operation);
            operation.Amount = GetSignedAmount(operation.Type, operation.Amount);

            _context.BankOperations.Add(operation);
            await _context.SaveChangesAsync();
            await RecalculateAccountBalanceAsync(accountName);

            _logger.LogInformation($"Movimiento de la operación nº {operationKey} en '{accountName}' creado");
            return operation;
        }

        public async Task UpdateTradeMovementAsync(int operationKey, decimal amount, DateTime date, string description)
        {
            var movements = await _context.BankOperations
                .Where(bo => bo.OperationId == operationKey)
                .ToListAsync();
            if (movements.Count == 0)
            {
                return;
            }

            foreach (var movement in movements)
            {
                movement.Amount = amount;
                movement.Date = date;
                movement.Description = description;
                ValidateOperation(movement);
                movement.Amount = GetSignedAmount(movement.Type, movement.Amount);
            }

            await _context.SaveChangesAsync();
            await RecalculateAccountsAsync(movements.Select(m => m.AccountName));
        }

        public async Task MoveTradeMovementAsync(int operationKey, string newAccountName)
        {
            var movements = await _context.BankOperations
                .Where(bo => bo.OperationId == operationKey)
                .ToListAsync();
            if (movements.Count == 0)
            {
                return;
            }

            var accountNames = movements.Select(m => m.AccountName).ToList();
            foreach (var movement in movements)
            {
                movement.AccountName = newAccountName;
            }

            await _context.SaveChangesAsync();
            accountNames.Add(newAccountName);
            await RecalculateAccountsAsync(accountNames);

            _logger.LogInformation($"Movimiento de la operación nº {operationKey} pasado a '{newAccountName}'");
        }

        public async Task RemoveTradeMovementsAsync(IEnumerable<int> operationKeys)
        {
            var keys = operationKeys.Distinct().ToList();
            var movements = await _context.BankOperations
                .Where(bo => bo.OperationId != null && keys.Contains(bo.OperationId.Value))
                .ToListAsync();
            if (movements.Count == 0)
            {
                return;
            }

            _context.BankOperations.RemoveRange(movements);
            await _context.SaveChangesAsync();
            await RecalculateAccountsAsync(movements.Select(m => m.AccountName));
        }

        public async Task<Dictionary<int, string>> GetTradeMovementAccountsAsync(IEnumerable<int> operationKeys)
        {
            var keys = operationKeys.Distinct().ToList();
            var movements = await _context.BankOperations
                .AsNoTracking()
                .Where(bo => bo.OperationId != null && keys.Contains(bo.OperationId.Value))
                .Select(bo => new { Key = bo.OperationId!.Value, bo.AccountName })
                .ToListAsync();

            return movements
                .GroupBy(m => m.Key)
                .ToDictionary(g => g.Key, g => g.First().AccountName);
        }

        public async Task<int> CountTradeMovementsAsync(IEnumerable<int> operationKeys)
        {
            var keys = operationKeys.Distinct().ToList();
            return await _context.BankOperations
                .CountAsync(bo => bo.OperationId != null && keys.Contains(bo.OperationId.Value));
        }
        #endregion

        #region Reglas de signo y validación
        private static readonly string[] IncomeTypes = { "Ingreso", "Intereses", "Traspaso Entrada" };
        private static readonly string[] ExpenseTypes = { "Gasto", "Comisiones", "Traspaso Salida" };

        /// <summary>
        /// Efecto del movimiento en el saldo: suma en ingresos, resta en gastos y 0 en tipos desconocidos.
        /// Usa el valor absoluto, así que también vale para filas antiguas con el signo cambiado.
        /// </summary>
        private static decimal GetEffect(BankOperation op)
        {
            if (IncomeTypes.Contains(op.Type)) return Math.Abs(op.Amount);
            if (ExpenseTypes.Contains(op.Type)) return -Math.Abs(op.Amount);
            return 0;
        }

        /// <summary>
        /// Importe con dos decimales y el signo que corresponde al tipo (negativo en gastos).
        /// </summary>
        private static decimal GetSignedAmount(string type, decimal amount)
        {
            var rounded = Math.Round(Math.Abs(amount), 2, MidpointRounding.AwayFromZero);
            return ExpenseTypes.Contains(type) ? -rounded : rounded;
        }

        private static void ValidateOperation(BankOperation operation)
        {
            if (!IncomeTypes.Contains(operation.Type) && !ExpenseTypes.Contains(operation.Type))
            {
                throw new InvalidOperationException("Selecciona un tipo de operación válido.");
            }

            if (operation.Amount <= 0)
            {
                throw new InvalidOperationException("El importe debe ser mayor que 0.");
            }

            if (operation.Amount > 999999999.99m)
            {
                throw new InvalidOperationException("El importe no puede superar 999.999.999,99.");
            }

            if (operation.Date == default)
            {
                throw new InvalidOperationException("La fecha es obligatoria.");
            }

            if (operation.Description != null && operation.Description.Length > 500)
            {
                throw new InvalidOperationException("La descripción no puede exceder 500 caracteres.");
            }
        }
        #endregion
    }
}
