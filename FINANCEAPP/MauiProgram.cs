using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FINANCEAPP.Data;
using FINANCEAPP.Services;

using System;
using System.IO;











































































































































namespace FINANCEAPP
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            // Configuración de la ruta de la base de datos SQLite
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "financeapp.db");

            // Registro de Entity Framework Core con SQLite
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Registro de servicios con inyección de dependencias
            builder.Services.AddScoped<IFinancialService, FinancialService>();
            builder.Services.AddScoped<IOperationService, OperationService>();
            builder.Services.AddScoped<ICompanyService, CompanyService>();
            builder.Services.AddScoped<IFundService, FundService>();
            builder.Services.AddScoped<IBankService, BankService>();
            builder.Services.AddScoped<ISafeHavenService, SafeHavenService>();
            builder.Services.AddScoped<IPropertyService, PropertyService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddHttpClient<IStockPriceService, StockPriceService>();
            builder.Services.AddScoped<IPriceHistoryService, PriceHistoryService>();
            builder.Services.AddScoped<ISaleHistoryService, SaleHistoryService>();
            builder.Services.AddScoped<IAppSettingsService, AppSettingsService>();
            builder.Services.AddScoped<ICountryService, CountryService>();
            builder.Services.AddScoped<IMarketCatalogService, MarketCatalogService>();
            builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();
            builder.Services.AddScoped<IPropertyDocumentService, PropertyDocumentService>();
            builder.Services.AddScoped<IClaudeApiKeyStore, ClaudeApiKeyStore>();
            builder.Services.AddScoped<IWealthSnapshotService, WealthSnapshotService>();
            builder.Services.AddScoped<IChartMarkService, ChartMarkService>();
            builder.Services.AddScoped<IDividendService, DividendService>();
            builder.Services.AddScoped<IBenchmarkIndexService, BenchmarkIndexService>();
            builder.Services.AddScoped<ICurrencyHistoryService, CurrencyHistoryService>();
            builder.Services.AddScoped<IPortfolioPeriodService, PortfolioPeriodService>();
            // Capturas diarias de la cartera y JSON para IA local (SPEC-17-03)
            builder.Services.AddScoped<IDailySnapshotService, DailySnapshotService>();
            builder.Services.AddScoped<IAiPortfolioExportService, AiPortfolioExportService>();
            builder.Services.AddHttpClient<IClaudeDocumentReader, ClaudeDocumentReader>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            });

            var app = builder.Build();

            // Inicialización de la base de datos
            // Solo crear si no existe; los datos persisten entre ejecuciones
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Database.EnsureCreated();

                // Migración ligera y segura: añade columnas nuevas si faltan, sin recrear ni borrar datos.
                DatabaseSchemaUpgrader.EnsureCompanyDetailsColumns(context);
                DatabaseSchemaUpgrader.EnsureCompanyDetailLivePriceColumns(context);
                DatabaseSchemaUpgrader.EnsurePriceHistoryTable(context);
                DatabaseSchemaUpgrader.EnsureSalesHistoryTable(context);
                DatabaseSchemaUpgrader.EnsureOperationSaleColumns(context);
                DatabaseSchemaUpgrader.EnsureAppSettingsTable(context);
                DatabaseSchemaUpgrader.EnsureCountriesTable(context);
                DatabaseSchemaUpgrader.EnsureCompanyCountryColumn(context);
                DatabaseSchemaUpgrader.EnsureOperationCurrencyColumn(context);
                DatabaseSchemaUpgrader.EnsureExchangeRatesTable(context);
                DatabaseSchemaUpgrader.EnsureOperationExchangeRateColumns(context);
                DatabaseSchemaUpgrader.EnsureMarketCatalogTable(context);
                DatabaseSchemaUpgrader.EnsurePropertyDocumentsTable(context);
                DatabaseSchemaUpgrader.EnsureWealthSnapshotsTable(context);
                DatabaseSchemaUpgrader.EnsureBankAccountOpeningBalanceColumn(context);
                DatabaseSchemaUpgrader.EnsureBankOperationSaleColumn(context);
                DatabaseSchemaUpgrader.EnsureBankOperationTradeColumn(context);
                DatabaseSchemaUpgrader.EnsureBankOperationTransferColumn(context);
                DatabaseSchemaUpgrader.EnsureChartMarksTable(context);
                DatabaseSchemaUpgrader.EnsureDividendPaymentsTable(context);
                DatabaseSchemaUpgrader.EnsureCompanyNextDividendDateColumn(context);
                DatabaseSchemaUpgrader.EnsureBenchmarkTables(context);
                DatabaseSchemaUpgrader.EnsureCurrencyHistoryTables(context);
                DatabaseSchemaUpgrader.EnsureDailySnapshotTables(context);

                // Corrige inmuebles antiguos guardados con total invertido 0 (no tocaba el patrimonio)
                DatabaseSchemaUpgrader.RepairPropertyTotals(context);

                // Reabre compras que quedaron cerradas por una venta ya deshecha (y recalcula sus empresas).
                // Task.Run evita bloquear el hilo principal esperando a sí mismo.
                var operationService = scope.ServiceProvider.GetRequiredService<FINANCEAPP.Services.IOperationService>();
                Task.Run(() => operationService.RepairOrphanSaleLinksAsync()).GetAwaiter().GetResult();

                // Devuelve a Pendiente las líneas de dividendo cobradas cuya operación ya no existe (SPEC-21-01, RF-4)
                var dividendService = scope.ServiceProvider.GetRequiredService<FINANCEAPP.Services.IDividendService>();
                Task.Run(() => dividendService.RepairOrphanLinksAsync()).GetAwaiter().GetResult();

                // Borra copias temporales de documentos abiertos en sesiones anteriores
                PropertyDocumentService.CleanTempFiles();

                // Capturas diarias (SPEC-17-03): rehace los días pendientes y hoy, y escribe el JSON para la IA.
                // Sin descargas y en memoria; si falla, la app arranca igual y la marca queda para el siguiente intento.
                try
                {
                    var dailySnapshots = scope.ServiceProvider.GetRequiredService<FINANCEAPP.Services.IDailySnapshotService>();
                    Task.Run(() => dailySnapshots.RefreshAsync()).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al calcular las capturas diarias: {ex.Message}");
                }

#if DEBUG
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
                logger.LogInformation($"Base de datos inicializada en: {dbPath}");
#endif
            }

            return app;
        }
    }
}
