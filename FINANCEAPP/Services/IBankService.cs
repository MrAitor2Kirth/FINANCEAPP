using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Interfaz para la gestión de cuentas bancarias y operaciones
    /// </summary>
    public interface IBankService
    {
        #region Cuentas Bancarias
        /// <summary>
        /// Obtiene todas las cuentas bancarias
        /// </summary>
        Task<List<BankAccount>> GetAllBankAccountsAsync();

        /// <summary>
        /// Obtiene una cuenta bancaria por ID
        /// </summary>
        Task<BankAccount?> GetBankAccountByIdAsync(int id);

        /// <summary>
        /// Obtiene una cuenta bancaria por nombre
        /// </summary>
        Task<BankAccount?> GetBankAccountByNameAsync(string name);

        /// <summary>
        /// Crea una nueva cuenta bancaria
        /// </summary>
        Task<BankAccount> CreateBankAccountAsync(BankAccount account);

        /// <summary>
        /// Actualiza una cuenta bancaria existente
        /// </summary>
        Task<BankAccount> UpdateBankAccountAsync(BankAccount account);

        /// <summary>
        /// Elimina una cuenta bancaria y todas sus operaciones
        /// </summary>
        Task<bool> DeleteBankAccountAsync(int id);

        /// <summary>
        /// Obtiene el saldo total de todas las cuentas
        /// </summary>
        Task<decimal> GetTotalBalanceAsync();

        /// <summary>
        /// Recalcula el saldo de una cuenta desde sus operaciones
        /// </summary>
        Task<decimal> RecalculateAccountBalanceAsync(string accountName);
        #endregion

        #region Operaciones Bancarias
        /// <summary>
        /// Obtiene todas las operaciones de una cuenta
        /// </summary>
        Task<List<BankOperation>> GetOperationsByAccountNameAsync(string accountName);

        /// <summary>
        /// Obtiene una operación por ID
        /// </summary>
        Task<BankOperation?> GetOperationByIdAsync(int id);

        /// <summary>
        /// Crea una nueva operación bancaria y actualiza el saldo de la cuenta
        /// </summary>
        Task<BankOperation> CreateOperationAsync(BankOperation operation);

        /// <summary>
        /// Actualiza una operación existente y recalcula el saldo
        /// </summary>
        Task<BankOperation> UpdateOperationAsync(BankOperation operation);

        /// <summary>
        /// Elimina una operación y recalcula el saldo
        /// </summary>
        Task<bool> DeleteOperationAsync(int id);

        /// <summary>
        /// Obtiene el total de ingresos de una cuenta
        /// </summary>
        Task<decimal> GetTotalIncomesAsync(string accountName);

        /// <summary>
        /// Obtiene el total de gastos de una cuenta
        /// </summary>
        Task<decimal> GetTotalExpensesAsync(string accountName);

        /// <summary>
        /// Obtiene las operaciones de una cuenta filtradas por rango de fechas
        /// </summary>
        Task<List<BankOperation>> GetOperationsByDateRangeAsync(string accountName, DateTime startDate, DateTime endDate);
        #endregion
    }
}
