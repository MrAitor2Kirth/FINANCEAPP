using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Interfaz del servicio financiero
    /// Define los contratos para operaciones CRUD con registros financieros
    /// </summary>
    public interface IFinancialService
    {
        /// <summary>
        /// Obtiene todos los registros financieros de forma asíncrona
        /// </summary>
        /// <returns>Lista de todos los registros financieros ordenados por fecha descendente</returns>
        Task<List<FinancialRecord>> GetAllRecordsAsync();

        /// <summary>
        /// Obtiene un registro financiero específico por su ID
        /// </summary>
        /// <param name="id">ID del registro a buscar</param>
        /// <returns>El registro encontrado o null si no existe</returns>
        Task<FinancialRecord?> GetRecordByIdAsync(int id);

        /// <summary>
        /// Agrega un nuevo registro financiero a la base de datos
        /// </summary>
        /// <param name="record">Registro financiero a agregar</param>
        /// <returns>El registro agregado con su ID generado</returns>
        Task<FinancialRecord> AddRecordAsync(FinancialRecord record);

        /// <summary>
        /// Actualiza un registro financiero existente
        /// </summary>
        /// <param name="record">Registro con los datos actualizados</param>
        /// <returns>El registro actualizado</returns>
        Task<FinancialRecord> UpdateRecordAsync(FinancialRecord record);

        /// <summary>
        /// Elimina un registro financiero por su ID
        /// </summary>
        /// <param name="id">ID del registro a eliminar</param>
        /// <returns>True si se eliminó correctamente, False si no se encontró</returns>
        Task<bool> DeleteRecordAsync(int id);

        /// <summary>
        /// Obtiene el total acumulado de todos los registros
        /// </summary>
        /// <returns>Suma total de todos los activos</returns>
        Task<decimal> GetTotalBalanceAsync();

        /// <summary>
        /// Obtiene los registros dentro de un rango de fechas
        /// </summary>
        /// <param name="startDate">Fecha inicial</param>
        /// <param name="endDate">Fecha final</param>
        /// <returns>Lista de registros dentro del rango especificado</returns>
        Task<List<FinancialRecord>> GetRecordsByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}
