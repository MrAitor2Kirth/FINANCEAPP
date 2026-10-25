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
        /// Recalcula el saldo de una cuenta: saldo inicial más el efecto de sus operaciones.
        /// No abre transacción propia; se llama dentro de las transacciones de alta, edición y baja.
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
        /// Crea una nueva operación bancaria y actualiza el saldo de la cuenta.
        /// El importe se indica en positivo y el servicio le da el signo según el tipo.
        /// Lanza InvalidOperationException con un mensaje en español si no es válida.
        /// </summary>
        Task<BankOperation> CreateOperationAsync(BankOperation operation);

        /// <summary>
        /// Actualiza tipo, importe, fecha y descripción de una operación (la cuenta no cambia)
        /// y recalcula el saldo. Mismas reglas de signo y validación que al crearla.
        /// </summary>
        Task<BankOperation> UpdateOperationAsync(BankOperation operation);

        /// <summary>
        /// Motivo por el que un movimiento no se edita desde /banco (ingreso de una venta, o compra o dividendo cuya
        /// operación sigue existiendo), o null si se puede editar. Es la misma regla que aplica UpdateOperationAsync.
        /// </summary>
        Task<string?> GetEditLockMessageAsync(BankOperation operation);

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

        #region Ingresos de ventas de acciones (SPEC-02-04)
        /// <summary>
        /// Cuenta bancaria más antigua (menor fecha de creación y, a igualdad, menor Id); null si no hay cuentas
        /// </summary>
        Task<BankAccount?> GetDefaultBankAccountAsync();

        /// <summary>
        /// Cuenta en la que se ingresa una venta: la indicada o, si viene vacía, la más antigua.
        /// Lanza InvalidOperationException si no hay cuentas o la indicada ya no existe.
        /// </summary>
        Task<BankAccount> ResolveSaleAccountAsync(string? accountName);

        /// <summary>
        /// Crea el Ingreso de una venta enlazado a su registro del histórico y recalcula el saldo.
        /// No abre transacción: debe llamarse dentro de una transacción abierta.
        /// </summary>
        Task<BankOperation> AddSaleDepositAsync(string accountName, decimal amount, DateTime date, string description, int saleHistoryId);

        /// <summary>
        /// Cambia importe y descripción del ingreso de una venta (si existe) y recalcula el saldo.
        /// No abre transacción: debe llamarse dentro de una transacción abierta.
        /// </summary>
        Task UpdateSaleDepositAsync(int saleHistoryId, decimal amount, string description);

        /// <summary>
        /// Elimina los ingresos de una venta y recalcula el saldo de sus cuentas.
        /// No abre transacción: debe llamarse dentro de una transacción abierta.
        /// </summary>
        Task RemoveSaleDepositsAsync(int saleHistoryId);

        /// <summary>
        /// Cuenta en la que está ingresada cada venta indicada (Id del histórico → nombre de la cuenta); las no ingresadas no aparecen
        /// </summary>
        Task<Dictionary<int, string>> GetSaleDepositsAsync(IEnumerable<int> saleHistoryIds);
        #endregion

        #region Traspasos entre cuentas (SPEC-10-02)
        /// <summary>
        /// Traspasa un importe entre dos cuentas: crea, en una transacción, un Traspaso Salida en la cuenta de origen
        /// y un Traspaso Entrada en la de destino, enlazados por TransferId, y recalcula los dos saldos.
        /// Descripción vacía = «Traspaso a …» / «Traspaso desde …». Lanza InvalidOperationException con un mensaje
        /// en español si no es válido.
        /// </summary>
        Task<(BankOperation Outgoing, BankOperation Incoming)> CreateTransferAsync(string fromAccount, string toAccount, decimal amount, DateTime date, string? description);

        /// <summary>
        /// Cuenta de la otra pata de cada traspaso indicado (TransferId → nombre de la cuenta contraria);
        /// los traspasos sin pareja no aparecen
        /// </summary>
        Task<Dictionary<int, string>> GetTransferCounterpartsAsync(IEnumerable<int> transferIds, string accountName);
        #endregion

        #region Movimientos de compras y dividendos (SPEC-02-06)
        /// <summary>
        /// Cuenta en la que se registra una compra o dividendo: null si se eligió «No registrar en banco» o si
        /// no hay cuentas y no se indicó ninguna; la más antigua si viene vacía. Lanza InvalidOperationException
        /// si la cuenta indicada ya no existe.
        /// </summary>
        Task<BankAccount?> ResolveTradeAccountAsync(string? accountName);

        /// <summary>
        /// Crea el movimiento (Gasto o Ingreso) de una compra o dividendo enlazado a su operación y recalcula el saldo.
        /// No abre transacción: debe llamarse dentro de una transacción abierta.
        /// </summary>
        Task<BankOperation> AddTradeMovementAsync(string accountName, string type, decimal amount, DateTime date, string description, int operationKey);

        /// <summary>
        /// Cambia importe, fecha y descripción del movimiento de una operación (si existe) y recalcula el saldo.
        /// No abre transacción: debe llamarse dentro de una transacción abierta.
        /// </summary>
        Task UpdateTradeMovementAsync(int operationKey, decimal amount, DateTime date, string description);

        /// <summary>
        /// Pasa el movimiento de una operación a otra cuenta (SPEC-02-07) y recalcula las dos cuentas.
        /// No abre transacción: debe llamarse dentro de una transacción abierta.
        /// </summary>
        Task MoveTradeMovementAsync(int operationKey, string newAccountName);

        /// <summary>
        /// Elimina los movimientos de las operaciones indicadas y recalcula el saldo de sus cuentas.
        /// No abre transacción: debe llamarse dentro de una transacción abierta.
        /// </summary>
        Task RemoveTradeMovementsAsync(IEnumerable<int> operationKeys);

        /// <summary>
        /// Cuenta en la que está registrado el movimiento de cada clave de operación indicada; las que no tienen movimiento no aparecen
        /// </summary>
        Task<Dictionary<int, string>> GetTradeMovementAccountsAsync(IEnumerable<int> operationKeys);

        /// <summary>
        /// Número de movimientos enlazados a las claves de operación indicadas
        /// </summary>
        Task<int> CountTradeMovementsAsync(IEnumerable<int> operationKeys);
        #endregion
    }
}
