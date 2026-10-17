using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Interfaz para el Dashboard agregador de todas las categorías
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Obtiene un resumen consolidado del patrimonio
        /// </summary>
        Task<DashboardSummary> GetDashboardSummaryAsync();

        /// <summary>
        /// Obtiene el patrimonio total del usuario
        /// </summary>
        Task<decimal> GetTotalPatrimonyAsync();

        /// <summary>
        /// Obtiene el desglose del patrimonio por categoría
        /// </summary>
        Task<Dictionary<string, decimal>> GetPatrimonyByAssetClassAsync();

        /// <summary>
        /// Obtiene la rentabilidad total agregada
        /// </summary>
        Task<PortfolioPerformance> GetPortfolioPerformanceAsync();

        /// <summary>
        /// Obtiene estadísticas de ingresos y gastos
        /// </summary>
        Task<IncomeExpenseStats> GetIncomeExpenseStatsAsync();

        /// <summary>
        /// Obtiene operaciones recientes de todas las categorías
        /// </summary>
        Task<List<RecentOperation>> GetRecentOperationsAsync(int limit = 10);

        /// <summary>
        /// Obtiene alertas o informaciones importantes
        /// </summary>
        Task<List<DashboardAlert>> GetDashboardAlertsAsync();

        /// <summary>
        /// Divisa base de los totales y empresas excluidas por tener otra divisa (SPEC-01-02)
        /// </summary>
        Task<DashboardCurrencyInfo> GetCurrencyInfoAsync();
    }

    /// <summary>
    /// Divisa en la que se muestran los totales del Dashboard
    /// </summary>
    public class DashboardCurrencyInfo
    {
        /// <summary>Divisa base (la del país base de Configuración)</summary>
        public string CurrencyCode { get; set; } = CurrencyFormatter.DefaultCurrency;

        /// <summary>Empresas en otra divisa que no se suman porque no tienen tipo de cambio actual (SPEC-01-03)</summary>
        public int ExcludedCompanies { get; set; }

        /// <summary>True si el país base guardado no existe en el catálogo y se usa EUR</summary>
        public bool BaseCountryMissing { get; set; }
    }

    /// <summary>
    /// Resumen del dashboard con todos los totales agregados
    /// </summary>
    public class DashboardSummary
    {
        public decimal TotalPatrimony { get; set; }
        public decimal AccionesTotal { get; set; }
        public decimal AccionesInitialTotal { get; set; }
        public decimal FundsTotal { get; set; }
        public decimal FundsInitialTotal { get; set; }
        public decimal BankTotal { get; set; }
        public decimal SafeHavenTotal { get; set; }
        public decimal SafeHavenInitialTotal { get; set; }
        public decimal RealEstateTotal { get; set; }

        /// <summary>Total invertido de los inmuebles no vendidos (SPEC-17-01, para las capturas del patrimonio)</summary>
        public decimal RealEstateInvestedTotal { get; set; }

        public int CompaniesCount { get; set; }
        public int FundsCount { get; set; }
        public int BankAccountsCount { get; set; }
        public int SafeHavenAssetsCount { get; set; }
        public int PropertiesCount { get; set; }

        public DateTime LastUpdate { get; set; }
    }

    /// <summary>
    /// Información sobre el rendimiento del portafolio
    /// </summary>
    public class PortfolioPerformance
    {
        public decimal TotalInvested { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal TotalReturn { get; set; }
        public decimal ReturnPercentage { get; set; }
        public List<AssetClassPerformance> ByAssetClass { get; set; } = new();
    }

    /// <summary>
    /// Rendimiento por clase de activo
    /// </summary>
    public class AssetClassPerformance
    {
        public string AssetClass { get; set; } = string.Empty;
        public decimal Invested { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal Return { get; set; }
        public decimal ReturnPercentage { get; set; }
    }

    /// <summary>
    /// Estadísticas de ingresos y gastos
    /// </summary>
    public class IncomeExpenseStats
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetCashFlow { get; set; }
        public Dictionary<string, decimal> IncomeBySource { get; set; } = new();
        public Dictionary<string, decimal> ExpensesByCategory { get; set; } = new();
    }

    /// <summary>
    /// Operación reciente de cualquier categoría
    /// </summary>
    public class RecentOperation
    {
        public string Category { get; set; } = string.Empty; // "Acciones", "Fondos", "Banco", "SafeHaven", "RealEstate"
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty; // "Compra", "Venta", "Ingreso", "Gasto", etc.
    }

    /// <summary>
    /// Alertas del dashboard
    /// </summary>
    public class DashboardAlert
    {
        public string Level { get; set; } = "info"; // "info", "warning", "danger"
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
