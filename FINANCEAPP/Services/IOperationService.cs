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
        /// <returns>La operación añadida con su Id asignado</returns>
        Task<Operation> AddAsync(Operation operation);

        /// <summary>
        /// Actualiza una operación existente
        /// </summary>
        /// <param name="operation">Operación con los datos actualizados</param>
        /// <returns>True si se actualizó correctamente</returns>
        Task<bool> UpdateAsync(Operation operation);

        /// <summary>
        /// Elimina una operación del sistema
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
