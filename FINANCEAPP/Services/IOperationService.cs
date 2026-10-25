using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Interfaz que define las operaciones disponibles para gestionar operaciones financieras
    /// Proporciona métodos CRUD y consultas específicas
    /// </summary>
    public interface IOperationService
    {
        /// <summary>
        /// Obtiene todas las operaciones registradas en el sistema
        /// </summary>
        /// <returns>Lista de operaciones ordenadas por fecha descendente</returns>
        Task<List<Operation>> GetAllAsync();

        /// <summary>
        /// Obtiene una operación específica por su identificador
        /// </summary>
        /// <param name="id">Identificador de la operación</param>
        /// <returns>La operación si existe, null en caso contrario</returns>
        Task<Operation?> GetByIdAsync(int id);

        /// <summary>
        /// Obtiene todas las operaciones de una empresa específica
        /// </summary>
        /// <param name="companyName">Nombre de la empresa</param>
        /// <returns>Lista de operaciones de la empresa</returns>
        Task<List<Operation>> GetByCompanyAsync(string companyName);

        /// <summary>
        /// Obtiene las operaciones filtradas por tipo
        /// </summary>
        /// <param name="type">Tipo de operación (ej: "Compra", "Venta")</param>
        /// <returns>Lista de operaciones del tipo especificado</returns>
        Task<List<Operation>> GetByTypeAsync(string type);

        /// <summary>
        /// Obtiene las operaciones abiertas (sin fecha de cierre)
        /// </summary>
        /// <returns>Lista de operaciones abiertas</returns>
        Task<List<Operation>> GetOpenOperationsAsync();

        /// <summary>
        /// Obtiene las operaciones cerradas
        /// </summary>
        /// <returns>Lista de operaciones cerradas</returns>
        Task<List<Operation>> GetClosedOperationsAsync();

        /// <summary>
        /// Añade una nueva operación al sistema
        /// </summary>
        /// <param name="operation">Operación a añadir</param>
        /// <param name="bankAccountName">Cuenta del movimiento bancario de una Compra o Dividendo (SPEC-02-06): vacía = la más antigua; <c>OperationAmountCalculator.NoBankMovementValue</c> = sin movimiento</param>
        /// <param name="dividendPaymentId">Línea del calendario de dividendos que se cobra con esta operación (SPEC-21-01): se marca Cobrada en la misma transacción</param>
        /// <returns>La operación añadida con su Id asignado</returns>
        Task<Operation> AddAsync(Operation operation, string? bankAccountName = null, int? dividendPaymentId = null);

        /// <summary>
        /// Registra una Venta de acciones: cierra por FIFO las compras abiertas de la empresa
        /// (dividiendo la última si se cierra solo en parte), cierra la propia Venta y guarda
        /// el registro en el histórico de ventas. Todo en una transacción.
        /// </summary>
        /// <param name="sale">Operación de tipo Venta (precio por acción y acciones mayores que 0)</param>
        /// <param name="sellPrice">Precio de venta por acción para el histórico</param>
        /// <param name="bankAccountName">Cuenta bancaria donde se ingresa la liquidez (SPEC-02-04); vacía = la más antigua</param>
        /// <returns>Resultado con éxito, mensaje de error e Id del registro de histórico</returns>
        Task<SaleRegistrationResult> RegisterSaleAsync(Operation sale, decimal sellPrice, string? bankAccountName = null);

        /// <summary>
        /// Ingresa en una cuenta bancaria una venta ya registrada que todavía no tiene ingreso (SPEC-02-04, RF-10).
        /// Si la empresa no es de la divisa base y la Venta no tiene tipo de cambio, usa el indicado y lo guarda en la Venta.
        /// </summary>
        Task<SaleRegistrationResult> DepositExistingSaleAsync(int saleHistoryId, string? bankAccountName, decimal? exchangeRate);

        /// <summary>
        /// Datos que necesita la pantalla para ingresar una venta ya registrada: divisa y si hay que pedir el tipo de cambio
        /// </summary>
        Task<SaleDepositInfo?> GetSaleDepositInfoAsync(int saleHistoryId);

        /// <summary>
        /// Deshace una venta: reabre las compras que cerró, vuelve a unir los lotes partidos,
        /// elimina la operación Venta y el registro del histórico y recalcula la empresa (todo en una transacción)
        /// </summary>
        /// <param name="saleHistoryId">Id del registro del histórico de ventas</param>
        /// <returns>Resultado con éxito, mensaje de error y número de compras reabiertas y filas unidas</returns>
        Task<SaleUndoResult> UndoSaleAsync(int saleHistoryId);

        /// <summary>
        /// Aviso en español al editar una operación ligada a una venta del histórico (compra o traspaso que cerró,
        /// o la propia Venta); null si no está ligada. Editar está permitido.
        /// </summary>
        /// <param name="operationId">Identificador de la operación</param>
        Task<string?> GetSaleEditWarningAsync(int operationId);

        /// <summary>
        /// Reabre las compras y traspasos que siguen cerrados por una venta que ya no existe en el histórico
        /// y recalcula sus empresas. Se ejecuta al arrancar; es idempotente.
        /// </summary>
        /// <returns>Número de operaciones reabiertas</returns>
        Task<int> RepairOrphanSaleLinksAsync();

        /// <summary>
        /// Indica si una operación está cerrada por una venta del histórico y por tanto no se puede eliminar
        /// (editar sí se puede, con el aviso de <see cref="GetSaleEditWarningAsync"/>)
        /// </summary>
        /// <param name="operationId">Identificador de la operación</param>
        /// <returns>Mensaje en español si está bloqueada; null si se puede editar y eliminar</returns>
        Task<string?> GetSaleLockMessageAsync(int operationId);

        /// <summary>
        /// Acciones disponibles para vender de una empresa (lotes abiertos de Compra y Traspaso menos
        /// Ventas antiguas abiertas), mínimo 0. Misma cuenta que <see cref="RegisterSaleAsync"/>.
        /// </summary>
        /// <param name="companyName">Nombre de la empresa</param>
        Task<decimal> GetAvailableSharesAsync(string companyName);

        /// <summary>
        /// Motivo en español por el que no se puede operar desde el gráfico, o null si se puede (SPEC-05-01, RF-6)
        /// </summary>
        /// <param name="type">"Compra" o "Venta"</param>
        /// <param name="date">Fecha de la operación</param>
        /// <param name="marketState">Estado del mercado</param>
        /// <param name="availableShares">Acciones disponibles para vender</param>
        /// <param name="hasBankAccounts">Si existe alguna cuenta bancaria (una Venta la necesita, SPEC-02-04)</param>
        string? GetQuickTradeBlockReason(string type, DateTime date, MarketState marketState, decimal availableShares, bool hasBankAccounts);

        /// <summary>
        /// Registra una Compra (con <see cref="AddAsync"/>) o una Venta (con <see cref="RegisterSaleAsync"/>)
        /// desde el gráfico de cotización, revalidando el bloqueo por mercado y el importe (SPEC-05-01)
        /// </summary>
        /// <param name="request">Datos del mini-formulario del gráfico</param>
        Task<QuickTradeResult> RegisterQuickTradeAsync(QuickTradeRequest request);

        /// <summary>
        /// Marcadores de compras, traspasos, ventas y dividendos de una empresa para el gráfico de cotización
        /// </summary>
        /// <param name="companyName">Nombre de la empresa</param>
        Task<List<ChartMarker>> GetChartMarkersAsync(string companyName);

        /// <summary>
        /// Actualiza una operación existente
        /// </summary>
        /// <param name="operation">Operación con los datos actualizados</param>
        /// <param name="bankAccountName">Cuenta del movimiento de una Compra o Dividendo (SPEC-02-07): vacía = no cambia; un nombre = pasa el movimiento a esa cuenta o lo crea si la operación abierta no lo tenía; <c>OperationAmountCalculator.NoBankMovementValue</c> se rechaza si ya tiene movimiento</param>
        /// <returns>True si se actualizó correctamente</returns>
        Task<bool> UpdateAsync(Operation operation, string? bankAccountName = null);

        /// <summary>
        /// Elimina una operación del sistema. Si generó una línea de dividendo, la línea vuelve a Pendiente (SPEC-21-01, RF-7)
        /// </summary>
        /// <param name="id">Identificador de la operación a eliminar</param>
        /// <returns>True si se eliminó correctamente</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Obtiene el total invertido en operaciones abiertas
        /// </summary>
        /// <returns>Suma total de importes de operaciones abiertas</returns>
        Task<decimal> GetTotalOpenAmountAsync();

        /// <summary>
        /// Obtiene el conteo de operaciones por tipo
        /// </summary>
        /// <returns>Diccionario con el tipo y cantidad de operaciones</returns>
        Task<Dictionary<string, int>> GetOperationCountByTypeAsync();
    }
}

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Resultado de registrar una Venta con <see cref="IOperationService.RegisterSaleAsync"/>
    /// </summary>
    public class SaleRegistrationResult
    {
        /// <summary>
        /// Indica si la venta se registró correctamente
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Mensaje de error en español cuando Success es false
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Id del registro creado en el histórico de ventas (0 si hubo error)
        /// </summary>
        public int SaleHistoryId { get; set; }

        public static SaleRegistrationResult Fail(string message) =>
            new() { Success = false, ErrorMessage = message };
    }

    /// <summary>
    /// Resultado de deshacer una venta con <see cref="IOperationService.UndoSaleAsync"/>
    /// </summary>
    public class SaleUndoResult
    {
        /// <summary>
        /// Indica si la venta se deshizo correctamente
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Mensaje de error en español cuando Success es false
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Número de compras o traspasos que se han reabierto (0 si el registro no tenía compras vinculadas)
        /// </summary>
        public int ReopenedCount { get; set; }

        /// <summary>
        /// Número de filas de lotes partidos que se han vuelto a unir
        /// </summary>
        public int MergedCount { get; set; }

        public static SaleUndoResult Fail(string message) =>
            new() { Success = false, ErrorMessage = message };
    }

    /// <summary>
    /// Datos para ingresar en el banco una venta ya registrada (SPEC-02-04, RF-10)
    /// </summary>
    public class SaleDepositInfo
    {
        /// <summary>Divisa de la venta</summary>
        public string CurrencyCode { get; set; } = CurrencyFormatter.DefaultCurrency;

        /// <summary>Divisa base de la app (la de las cuentas bancarias)</summary>
        public string BaseCurrencyCode { get; set; } = CurrencyFormatter.DefaultCurrency;

        /// <summary>Indica si hay que pedir el tipo de cambio (otra divisa y la Venta no lo tiene)</summary>
        public bool RequiresExchangeRate { get; set; }
    }
}
