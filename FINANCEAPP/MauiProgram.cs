using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using FINANCEAPP.Data;
using FINANCEAPP.Services;

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
            // La base de datos se almacenará en la carpeta AppData de la aplicación
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

            var app = builder.Build();

            // Inicialización de la base de datos
            // Crea la base de datos y aplica las migraciones si no existe
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

#if DEBUG
                // En modo DEBUG, eliminar y recrear la base de datos para asegurar el esquema actualizado
                // ADVERTENCIA: Esto eliminará todos los datos existentes
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();

                var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
                logger.LogInformation($"Base de datos recreada en: {dbPath}");
#else
                // En modo RELEASE, solo crear si no existe
                context.Database.EnsureCreated();
#endif
            }

            return app;
        }
    }
}
