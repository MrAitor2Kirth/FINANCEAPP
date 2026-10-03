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

#if DEBUG
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
                logger.LogInformation($"Base de datos inicializada en: {dbPath}");
#endif
            }

            return app;
        }
    }
}
