using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Interfaz que define las operaciones disponibles para gestionar detalles de empresas
    /// Proporciona métodos CRUD y consultas específicas
    /// </summary>
    public interface ICompanyService
    {
        /// <summary>
        /// Obtiene todas las empresas registradas en el sistema
        /// </summary>
        /// <returns>Lista de empresas ordenadas por nombre</returns>
        Task<List<CompanyDetail>> GetAllAsync();

        /// <summary>
        /// Obtiene una empresa específica por su identificador
        /// </summary>
        /// <param name="id">Identificador de la empresa</param>
        /// <returns>La empresa si existe, null en caso contrario</returns>
        Task<CompanyDetail?> GetByIdAsync(int id);

        /// <summary>
        /// Obtiene una empresa por su ticker de cotización
        /// </summary>
        /// <param name="ticker">Símbolo ticker de la empresa</param>
        /// <returns>La empresa si existe, null en caso contrario</returns>
        Task<CompanyDetail?> GetByTickerAsync(string ticker);

        /// <summary>
        /// Obtiene todas las empresas de un sector específico
        /// </summary>
        /// <param name="sector">Nombre del sector económico</param>
        /// <returns>Lista de empresas del sector</returns>
        Task<List<CompanyDetail>> GetBySectorAsync(string sector);

        /// <summary>
        /// Busca empresas por nombre (búsqueda parcial)
        /// </summary>
        /// <param name="searchTerm">Término de búsqueda</param>
        /// <returns>Lista de empresas que coinciden con el término</returns>
        Task<List<CompanyDetail>> SearchByNameAsync(string searchTerm);

        /// <summary>
        /// Obtiene las empresas con dividend yield superior a un valor mínimo
        /// </summary>
        /// <param name="minYield">Valor mínimo de dividend yield</param>
        /// <returns>Lista de empresas ordenadas por dividend yield descendente</returns>
        Task<List<CompanyDetail>> GetByMinDividendYieldAsync(decimal minYield);

        /// <summary>
        /// Obtiene las empresas ordenadas por dividend yield descendente
        /// </summary>
        /// <param name="topCount">Número de empresas a devolver (por defecto 10)</param>
        /// <returns>Lista de empresas con mayor dividend yield</returns>
        Task<List<CompanyDetail>> GetTopByDividendYieldAsync(int topCount = 10);

        /// <summary>
        /// Añade una nueva empresa al sistema
        /// </summary>
        /// <param name="company">Empresa a añadir</param>
        /// <returns>La empresa añadida con su Id asignado</returns>
        Task<CompanyDetail> AddAsync(CompanyDetail company);

        /// <summary>
        /// Actualiza una empresa existente
        /// </summary>
        /// <param name="company">Empresa con los datos actualizados</param>
        /// <returns>True si se actualizó correctamente</returns>
        Task<bool> UpdateAsync(CompanyDetail company);

        /// <summary>
        /// Actualiza únicamente el precio actual de una empresa
        /// </summary>
        /// <param name="companyName">Nombre de la empresa</param>
        /// <param name="price">Nuevo precio actual</param>
        Task<bool> UpdateCurrentPriceAsync(string companyName, decimal price);

        /// <summary>
        /// Elimina una empresa del sistema
        /// </summary>
        /// <param name="id">Identificador de la empresa a eliminar</param>
        /// <returns>True si se eliminó correctamente</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Obtiene todos los sectores únicos registrados
        /// </summary>
        /// <returns>Lista de nombres de sectores</returns>
        Task<List<string>> GetAllSectorsAsync();

        /// <summary>
        /// Verifica si existe una empresa con el ticker especificado
        /// </summary>
        /// <param name="ticker">Ticker a verificar</param>
        /// <param name="excludeId">ID a excluir de la verificación (útil en actualizaciones)</param>
        /// <returns>True si el ticker ya existe</returns>
        Task<bool> TickerExistsAsync(string ticker, int? excludeId = null);

        /// <summary>
        /// Recalcula el importe total invertido de una empresa a partir de sus operaciones abiertas
        /// </summary>
        /// <param name="companyName">Nombre de la empresa</param>
        /// <returns>Importe total sumado</returns>
        Task<decimal> RecalculateTotalInvestedAsync(string companyName);
    }
}
