using Microsoft.Extensions.Logging;
using FINANCEAPP.Models;

namespace FINANCEAPP.Services
{
    /// <summary>
    /// Servicio agregador de datos para el Dashboard
    /// Consolida información de todas las categorías de activos
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly IFundService _fundService;
        private readonly IBankService _bankService;
        private readonly ISafeHavenService _safeHavenService;
        private readonly IPropertyService _propertyService;
        private readonly ICompanyService _companyService;
        private readonly ICountryService _countryService;
        private readonly IExchangeRateService _exchangeRateService;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            IFundService fundService,
            IBankService bankService,
            ISafeHavenService safeHavenService,
            IPropertyService propertyService,
            ICompanyService companyService,
            ICountryService countryService,
            IExchangeRateService exchangeRateService,
            ILogger<DashboardService> logger)
        {
            _fundService = fundService;
            _bankService = bankService;
            _safeHavenService = safeHavenService;
            _propertyService = propertyService;
            _companyService = companyService;
            _countryService = countryService;
            _exchangeRateService = exchangeRateService;
            _logger = logger;
        }

        /// <summary>
        /// Empresas cuya divisa es la divisa base (SPEC-01-02), junto con cuántas quedan fuera
        /// y la divisa base. Las partes de acciones del Dashboard solo suman estas empresas.
        /// </summary>
        private async Task<ConvertedCompanyTotals> GetCompanyTotalsAsync()
        {
            var all = await _companyService.GetAllAsync();
            return await _exchangeRateService.GetConvertedTotalsAsync(all);
        }

        public async Task<DashboardCurrencyInfo> GetCurrencyInfoAsync()
        {
            var totals = await GetCompanyTotalsAsync();
            var baseInfo = await _countryService.GetBaseCurrencyAsync();
            return new DashboardCurrencyInfo
            {
                CurrencyCode = baseInfo.CurrencyCode,
                ExcludedCompanies = totals.MissingRateCompanies,
                BaseCountryMissing = baseInfo.IsFallback
            };
        }

        public async Task<DashboardSummary> GetDashboardSummaryAsync()
        {
            try
            {
                // Obtener totales de cada categoría
                var companyTotals = await GetCompanyTotalsAsync();
                var companiesTotal = companyTotals.CurrentValue;
                var companiesInitialTotal = companyTotals.InvestedInitial;
                var fundsTotal = await _fundService.GetTotalCurrentValueAsync();
                var fundsInitialTotal = await _fundService.GetTotalInvestedAsync();
                var bankTotal = await _bankService.GetTotalBalanceAsync();
                // Total invertido (inicial) y valor actual, con el mismo criterio que acciones y fondos
                var safeHavenInitialTotal = await _safeHavenService.GetTotalInvestedAsync();
                var safeHavenTotal = await _safeHavenService.GetTotalCurrentValueAsync();
                // Inmuebles no vendidos: valor actual si está informado; si no, total invertido
                var activeProperties = await _propertyService.GetActivePropertiesAsync();
                var realEstateTotal = activeProperties.Sum(p => p.CurrentValue ?? p.TotalInvested);
                var realEstateInvestedTotal = activeProperties.Sum(p => p.TotalInvested);

                var summary = new DashboardSummary
                {
                    AccionesTotal = companiesTotal,
                    AccionesInitialTotal = companiesInitialTotal,
                    FundsTotal = fundsTotal,
                    FundsInitialTotal = fundsInitialTotal,
                    BankTotal = bankTotal,
                    SafeHavenTotal = safeHavenTotal,
                    SafeHavenInitialTotal = safeHavenInitialTotal,
                    RealEstateTotal = realEstateTotal,
                    RealEstateInvestedTotal = realEstateInvestedTotal,
                    TotalPatrimony = companiesTotal + fundsTotal + bankTotal + safeHavenTotal + realEstateTotal,

                    CompaniesCount = (await _companyService.GetAllAsync()).Count,
                    FundsCount = (await _fundService.GetAllFundsAsync()).Count,
                    BankAccountsCount = (await _bankService.GetAllBankAccountsAsync()).Count,
                    SafeHavenAssetsCount = (await _safeHavenService.GetAllAssetsAsync()).Count,
                    PropertiesCount = activeProperties.Count,

                    LastUpdate = DateTime.Now
                };

                _logger.LogInformation("Dashboard summary obtenido exitosamente");
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resumen del dashboard");
                throw;
            }
        }

        public async Task<decimal> GetTotalPatrimonyAsync()
        {
            try
            {
                var summary = await GetDashboardSummaryAsync();
                return summary.TotalPatrimony;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener patrimonio total");
                throw;
            }
        }

        public async Task<Dictionary<string, decimal>> GetPatrimonyByAssetClassAsync()
        {
            try
            {
                var result = new Dictionary<string, decimal>();

                // Acciones
                result["Acciones"] = (await GetCompanyTotalsAsync()).CurrentValue;

                // Fondos
                var funds = await _fundService.GetAllFundsAsync();
                result["Fondos"] = funds.Sum(f => f.CurrentValue);

                // Banco
                result["Banco"] = await _bankService.GetTotalBalanceAsync();

                // Activos Refugio
                result["Activos Refugio"] = await _safeHavenService.GetTotalCurrentValueAsync();

                // Inmobiliario
                var activeProperties = await _propertyService.GetActivePropertiesAsync();
                result["Inmobiliario"] = activeProperties.Sum(p => p.CurrentValue ?? p.TotalInvested);

                _logger.LogInformation("Patrimonio por clase de activo obtenido");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener patrimonio por clase de activo");
                throw;
            }
        }

        public async Task<PortfolioPerformance> GetPortfolioPerformanceAsync()
        {
            try
            {
                var performance = new PortfolioPerformance();

                // Fondos - tienen rentabilidad calculada
                var funds = await _fundService.GetAllFundsAsync();
                var fundsTotalInvested = funds.Sum(f => f.TotalInvested);
                var fundsTotalValue = funds.Sum(f => f.CurrentValue);

                // Acciones
                var companyTotals = await GetCompanyTotalsAsync();
                var companiesTotalInvested = companyTotals.InvestedInitial;
                // Valor actual de las acciones: precio en vivo (o precio actual) x acciones en cartera, convertido a la divisa base
                var companiesTotalValue = companyTotals.CurrentValue;

                // Agregados
                performance.TotalInvested = fundsTotalInvested + companiesTotalInvested;
                performance.CurrentValue = fundsTotalValue + companiesTotalValue;
                performance.TotalReturn = performance.CurrentValue - performance.TotalInvested;
                performance.ReturnPercentage = performance.TotalInvested > 0
                    ? (performance.TotalReturn / performance.TotalInvested) * 100
                    : 0;

                // Detalle por clase de activo
                if (fundsTotalInvested > 0)
                {
                    performance.ByAssetClass.Add(new AssetClassPerformance
                    {
                        AssetClass = "Fondos",
                        Invested = fundsTotalInvested,
                        CurrentValue = fundsTotalValue,
                        Return = fundsTotalValue - fundsTotalInvested,
                        ReturnPercentage = ((fundsTotalValue - fundsTotalInvested) / fundsTotalInvested) * 100
                    });
                }

                if (companiesTotalInvested > 0)
                {
                    performance.ByAssetClass.Add(new AssetClassPerformance
                    {
                        AssetClass = "Acciones",
                        Invested = companiesTotalInvested,
                        CurrentValue = companiesTotalValue,
                        Return = companiesTotalValue - companiesTotalInvested,
                        ReturnPercentage = ((companiesTotalValue - companiesTotalInvested) / companiesTotalInvested) * 100
                    });
                }

                _logger.LogInformation("Rendimiento del portafolio calculado");
                return performance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular rendimiento del portafolio");
                throw;
            }
        }

        public async Task<IncomeExpenseStats> GetIncomeExpenseStatsAsync()
        {
            try
            {
                var stats = new IncomeExpenseStats();

                // Ingresos por alquiler desde inmobiliario
                var rentalIncome = await _propertyService.GetTotalRentalIncomeAsync();
                stats.TotalIncome = rentalIncome;
                stats.IncomeBySource["Alquiler"] = rentalIncome;

                // Gastos desde inmobiliario (aproximado desde propiedades)
                var allProperties = await _propertyService.GetAllPropertiesAsync();
                var totalExpenses = 0m;
                foreach (var property in allProperties.Where(p => p.Status != "Vendida"))
                {
                    var propertyExpenses = await _propertyService.GetTotalExpensesAsync(property.Name);
                    totalExpenses += propertyExpenses;
                }

                stats.TotalExpenses = totalExpenses;
                stats.ExpensesByCategory["Inmobiliario"] = totalExpenses;
                stats.NetCashFlow = stats.TotalIncome - stats.TotalExpenses;

                _logger.LogInformation("Estadísticas de ingresos/gastos obtenidas");
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de ingresos/gastos");
                throw;
            }
        }

        public async Task<List<RecentOperation>> GetRecentOperationsAsync(int limit = 10)
        {
            try
            {
                var operations = new List<RecentOperation>();

                // Operaciones de fondos
                var allFunds = await _fundService.GetAllFundsAsync();
                foreach (var fund in allFunds)
                {
                    var fundOps = await _fundService.GetOperationsByFundNameAsync(fund.Name);
                    foreach (var op in fundOps.OrderByDescending(o => o.Date).Take(3))
                    {
                        operations.Add(new RecentOperation
                        {
                            Category = "Fondos",
                            Description = $"{fund.Name} - {op.Type}",
                            Amount = op.Amount,
                            Date = op.Date,
                            Type = op.Type
                        });
                    }
                }

                // Operaciones de banco
                var allAccounts = await _bankService.GetAllBankAccountsAsync();
                foreach (var account in allAccounts)
                {
                    var bankOps = await _bankService.GetOperationsByAccountNameAsync(account.Name);
                    foreach (var op in bankOps.OrderByDescending(o => o.Date).Take(3))
                    {
                        operations.Add(new RecentOperation
                        {
                            Category = "Banco",
                            Description = $"{account.Name} - {op.Type}",
                            Amount = op.Amount,
                            Date = op.Date,
                            Type = op.Type
                        });
                    }
                }

                // Operaciones de propiedades
                var allProperties = await _propertyService.GetAllPropertiesAsync();
                foreach (var property in allProperties)
                {
                    var propOps = await _propertyService.GetOperationsByPropertyNameAsync(property.Name);
                    foreach (var op in propOps.OrderByDescending(o => o.Date).Take(3))
                    {
                        operations.Add(new RecentOperation
                        {
                            Category = "Inmobiliario",
                            Description = $"{property.Name} - {op.Type}",
                            Amount = op.Amount,
                            Date = op.Date,
                            Type = op.Type
                        });
                    }
                }

                // Retornar las más recientes limitadas
                var result = operations
                    .OrderByDescending(o => o.Date)
                    .Take(limit)
                    .ToList();

                _logger.LogInformation($"Operaciones recientes obtenidas ({result.Count} elementos)");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener operaciones recientes");
                throw;
            }
        }

        public async Task<List<DashboardAlert>> GetDashboardAlertsAsync()
        {
            try
            {
                var alerts = new List<DashboardAlert>();

                // Alerta: Propiedades sin alquiler
                var unrentedProperties = await _propertyService.GetPropertiesByStatusAsync("Vacío");
                if (unrentedProperties.Count > 0)
                {
                    alerts.Add(new DashboardAlert
                    {
                        Level = "warning",
                        Title = "Propiedades sin alquiler",
                        Message = $"Tienes {unrentedProperties.Count} propiedad(es) vacía(s) que no generan ingresos",
                        CreatedAt = DateTime.Now
                    });
                }

                // Alerta: Cuentas bancarias con saldo bajo
                var accounts = await _bankService.GetAllBankAccountsAsync();
                var lowBalanceAccounts = accounts.Where(a => a.CurrentBalance < 1000).ToList();
                if (lowBalanceAccounts.Count > 0)
                {
                    alerts.Add(new DashboardAlert
                    {
                        Level = "warning",
                        Title = "Saldo bajo en cuentas bancarias",
                        Message = $"{lowBalanceAccounts.Count} cuenta(s) con saldo inferior a 1.000€",
                        CreatedAt = DateTime.Now
                    });
                }

                // Alerta: Fondos con bajo rendimiento
                var funds = await _fundService.GetAllFundsAsync();
                var lowPerformanceFunds = funds.Where(f => f.CurrentReturn < -5).ToList();
                if (lowPerformanceFunds.Count > 0)
                {
                    alerts.Add(new DashboardAlert
                    {
                        Level = "warning",
                        Title = "Fondos con bajo rendimiento",
                        Message = $"{lowPerformanceFunds.Count} fondo(s) con rentabilidad inferior a -5%",
                        CreatedAt = DateTime.Now
                    });
                }

                // Alerta informativa de que todo está actualizado
                alerts.Add(new DashboardAlert
                {
                    Level = "info",
                    Title = "Dashboard actualizado",
                    Message = "Tu información patrimonial está al día",
                    CreatedAt = DateTime.Now
                });

                _logger.LogInformation($"Alertas del dashboard generadas ({alerts.Count} alertas)");
                return alerts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener alertas del dashboard");
                throw;
            }
        }
    }
}
