using Microsoft.EntityFrameworkCore;
using FINANCEAPP.Models;

namespace FINANCEAPP.Data
{
    /// <summary>
    /// Contexto de base de datos para la aplicación financiera
    /// Gestiona la conexión con SQLite y las operaciones con la base de datos
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Constructor que recibe las opciones de configuración
        /// </summary>
        /// <param name="options">Opciones de configuración del contexto</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// DbSet de registros financieros
        /// Representa la tabla FinancialRecords en la base de datos
        /// </summary>
        public DbSet<FinancialRecord> FinancialRecords { get; set; }

        /// <summary>
        /// DbSet de operaciones financieras (compra/venta de acciones)
        /// Representa la tabla Operations en la base de datos
        /// </summary>
        public DbSet<Operation> Operations { get; set; }

        /// <summary>
        /// DbSet de detalles de empresas
        /// Representa la tabla CompanyDetails en la base de datos
        /// </summary>
        public DbSet<CompanyDetail> CompanyDetails { get; set; }

        /// <summary>
        /// DbSet de fondos de inversión
        /// Representa la tabla Funds en la base de datos
        /// </summary>
        public DbSet<Fund> Funds { get; set; }

        /// <summary>
        /// DbSet de operaciones de fondos
        /// Representa la tabla FundOperations en la base de datos
        /// </summary>
        public DbSet<FundOperation> FundOperations { get; set; }

        /// <summary>
        /// DbSet de historial de valores de fondos (para Opción B de gestión de plusvalías)
        /// Representa la tabla FundValueHistories en la base de datos
        /// </summary>
        public DbSet<FundValueHistory> FundValueHistories { get; set; }

        /// <summary>
        /// DbSet de cuentas bancarias
        /// Representa la tabla BankAccounts en la base de datos
        /// </summary>
        public DbSet<BankAccount> BankAccounts { get; set; }

        /// <summary>
        /// DbSet de operaciones bancarias
        /// Representa la tabla BankOperations en la base de datos
        /// </summary>
        public DbSet<BankOperation> BankOperations { get; set; }

        /// <summary>
        /// DbSet de activos refugio (oro, plata, crypto)
        /// Representa la tabla SafeHavenAssets en la base de datos
        /// </summary>
        public DbSet<SafeHavenAsset> SafeHavenAssets { get; set; }

        /// <summary>
        /// DbSet de operaciones de activos refugio
        /// Representa la tabla SafeHavenOperations en la base de datos
        /// </summary>
        public DbSet<SafeHavenOperation> SafeHavenOperations { get; set; }

        /// <summary>
        /// DbSet de propiedades inmobiliarias
        /// Representa la tabla Properties en la base de datos
        /// </summary>
        public DbSet<Property> Properties { get; set; }

        /// <summary>
        /// DbSet de operaciones inmobiliarias
        /// Representa la tabla PropertyOperations en la base de datos
        /// </summary>
        public DbSet<PropertyOperation> PropertyOperations { get; set; }

        /// <summary>
        /// DbSet de puntos históricos de cotización (caché OHLC diario por empresa)
        /// Representa la tabla PriceHistoryPoints en la base de datos
        /// </summary>
        public DbSet<PriceHistoryPoint> PriceHistoryPoints { get; set; }

        /// <summary>
        /// DbSet del histórico de ventas de acciones (beneficio generado por cada venta)
        /// Representa la tabla SalesHistory en la base de datos
        /// </summary>
        public DbSet<SaleHistory> SalesHistory { get; set; }

        /// <summary>
        /// DbSet de los ajustes de la aplicación (clave/valor; por ahora el logo)
        /// Representa la tabla AppSettings en la base de datos
        /// </summary>
        public DbSet<AppSetting> AppSettings { get; set; }

        /// <summary>
        /// DbSet del catálogo de países (SPEC-01-02). Tabla Countries
        /// </summary>
        public DbSet<Country> Countries { get; set; }

        /// <summary>
        /// DbSet del catálogo de empresas de mercado (SPEC-01-04, solo lectura). Tabla MarketCompanies
        /// </summary>
        public DbSet<MarketCompany> MarketCompanies { get; set; }

        /// <summary>
        /// DbSet de los tipos de cambio actuales (SPEC-01-03). Tabla ExchangeRates
        /// </summary>
        public DbSet<ExchangeRate> ExchangeRates { get; set; }

        /// <summary>
        /// DbSet de documentos adjuntos a inmuebles (SPEC-14-01). Tabla PropertyDocuments
        /// </summary>
        public DbSet<PropertyDocument> PropertyDocuments { get; set; }

        /// <summary>
        /// DbSet de las capturas mensuales del patrimonio (SPEC-17-01). Tabla WealthSnapshots
        /// </summary>
        public DbSet<WealthSnapshot> WealthSnapshots { get; set; }

        /// <summary>
        /// DbSet de las marcas temporales del gráfico de cotización (SPEC-05-03). Tabla ChartMarks
        /// </summary>
        public DbSet<ChartMark> ChartMarks { get; set; }

        /// <summary>
        /// DbSet de los índices para comparar en el gráfico (SPEC-05-04). Tabla BenchmarkIndices
        /// </summary>
        public DbSet<BenchmarkIndex> BenchmarkIndices { get; set; }

        /// <summary>
        /// DbSet del histórico diario de los índices (SPEC-05-04). Tabla BenchmarkPricePoints
        /// </summary>
        public DbSet<BenchmarkPricePoint> BenchmarkPricePoints { get; set; }

        /// <summary>
        /// DbSet de los pares de divisas con histórico de tipos (SPEC-01-09). Tabla CurrencyPairs
        /// </summary>
        public DbSet<CurrencyPair> CurrencyPairs { get; set; }

        /// <summary>
        /// DbSet del histórico diario de tipos de cambio (SPEC-01-09). Tabla CurrencyRatePoints
        /// </summary>
        public DbSet<CurrencyRatePoint> CurrencyRatePoints { get; set; }

        /// <summary>
        /// DbSet del calendario de dividendos (SPEC-21-01). Tabla DividendPayments
        /// </summary>
        public DbSet<DividendPayment> DividendPayments { get; set; }

        /// <summary>
        /// Configuración del modelo de datos
        /// Establece índices, relaciones y constraints adicionales
        /// </summary>
        /// <param name="modelBuilder">Constructor del modelo</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de la entidad FinancialRecord
            modelBuilder.Entity<FinancialRecord>(entity =>
            {
                // Índice en la fecha para búsquedas más rápidas
                entity.HasIndex(e => e.Date)
                    .HasDatabaseName("IX_FinancialRecords_Date");

                // Ordenamiento por defecto por fecha descendente
                entity.Property(e => e.Date)
                    .IsRequired();

                // Configurar propiedad Total como calculada (no se almacena en DB)
                entity.Ignore(e => e.Total);
            });

            // Configuración de la entidad Operation
            modelBuilder.Entity<Operation>(entity =>
            {
                // Índice en CompanyName para búsquedas rápidas
                entity.HasIndex(e => e.CompanyName)
                    .HasDatabaseName("IX_Operations_CompanyName");

                // Índice en Date para ordenamientos y filtros
                entity.HasIndex(e => e.Date)
                    .HasDatabaseName("IX_Operations_Date");

                // Índice en Type para filtrado por tipo de operación
                entity.HasIndex(e => e.Type)
                    .HasDatabaseName("IX_Operations_Type");

                // Índice en SaleHistoryId para localizar las compras cerradas por una venta
                entity.HasIndex(e => e.SaleHistoryId)
                    .HasDatabaseName("IX_Operations_SaleHistoryId");

                // Configurar propiedades requeridas
                entity.Property(e => e.CompanyName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Date)
                    .IsRequired();

                // Ignorar propiedades calculadas
                entity.Ignore(e => e.IsClosed);
                entity.Ignore(e => e.Status);
            });

            // Configuración de la entidad CompanyDetail
            modelBuilder.Entity<CompanyDetail>(entity =>
            {
                // Índice único en Ticker (no puede haber dos empresas con el mismo ticker)
                entity.HasIndex(e => e.Ticker)
                    .IsUnique()
                    .HasDatabaseName("IX_CompanyDetails_Ticker_Unique");

                // Índice en Name para búsquedas
                entity.HasIndex(e => e.Name)
                    .HasDatabaseName("IX_CompanyDetails_Name");

                // Índice en Sector para agrupaciones
                entity.HasIndex(e => e.Sector)
                    .HasDatabaseName("IX_CompanyDetails_Sector");

                // Configurar propiedades requeridas
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Ticker)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Sector)
                    .IsRequired()
                    .HasMaxLength(100);

                // Ignorar propiedades calculadas
                entity.Ignore(e => e.InvestingUrl);
                entity.Ignore(e => e.FormattedDividendYield);
            });

            // Configuración de la entidad Fund
            modelBuilder.Entity<Fund>(entity =>
            {
                entity.HasIndex(e => e.Name)
                    .HasDatabaseName("IX_Funds_Name");

                entity.HasIndex(e => e.FundType)
                    .HasDatabaseName("IX_Funds_FundType");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.FundType)
                    .IsRequired()
                    .HasMaxLength(50);

                // Ignorar propiedades calculadas
                entity.Ignore(e => e.CurrentReturn);
                entity.Ignore(e => e.FormattedCurrentReturn);
                entity.Ignore(e => e.FormattedTER);
                entity.Ignore(e => e.FormattedHistoricalReturn);
            });

            // Configuración de la entidad FundOperation
            modelBuilder.Entity<FundOperation>(entity =>
            {
                entity.HasIndex(e => e.FundName)
                    .HasDatabaseName("IX_FundOperations_FundName");

                entity.HasIndex(e => e.Date)
                    .HasDatabaseName("IX_FundOperations_Date");

                entity.HasIndex(e => e.Type)
                    .HasDatabaseName("IX_FundOperations_Type");

                entity.Property(e => e.FundName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(50);

                // Ignorar propiedades calculadas
                entity.Ignore(e => e.IsPlusvaliaOperation);
                entity.Ignore(e => e.IsIncome);
                entity.Ignore(e => e.IsExpense);
            });

            // Configuración de la entidad FundValueHistory
            modelBuilder.Entity<FundValueHistory>(entity =>
            {
                entity.HasIndex(e => e.FundName)
                    .HasDatabaseName("IX_FundValueHistories_FundName");

                entity.HasIndex(e => e.Date)
                    .HasDatabaseName("IX_FundValueHistories_Date");

                entity.Property(e => e.FundName)
                    .IsRequired()
                    .HasMaxLength(200);
            });

            // Configuración de la entidad BankAccount
            modelBuilder.Entity<BankAccount>(entity =>
            {
                entity.HasIndex(e => e.Name)
                    .HasDatabaseName("IX_BankAccounts_Name");

                entity.HasIndex(e => e.BankName)
                    .HasDatabaseName("IX_BankAccounts_BankName");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.BankName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.AccountType)
                    .IsRequired()
                    .HasMaxLength(50);

                // Ignorar propiedades calculadas
                entity.Ignore(e => e.FormattedBalance);
            });

            // Configuración de la entidad BankOperation
            modelBuilder.Entity<BankOperation>(entity =>
            {
                entity.HasIndex(e => e.AccountName)
                    .HasDatabaseName("IX_BankOperations_AccountName");

                entity.HasIndex(e => e.Date)
                    .HasDatabaseName("IX_BankOperations_Date");

                entity.HasIndex(e => e.Type)
                    .HasDatabaseName("IX_BankOperations_Type");

                entity.HasIndex(e => e.SaleHistoryId)
                    .HasDatabaseName("IX_BankOperations_SaleHistoryId");

                entity.HasIndex(e => e.OperationId)
                    .HasDatabaseName("IX_BankOperations_OperationId");

                entity.HasIndex(e => e.TransferId)
                    .HasDatabaseName("IX_BankOperations_TransferId");

                entity.Property(e => e.AccountName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(50);

                // Ignorar propiedades calculadas
                entity.Ignore(e => e.IsIncome);
                entity.Ignore(e => e.IsExpense);
            });

            // Configuración de la entidad SafeHavenAsset
            modelBuilder.Entity<SafeHavenAsset>(entity =>
            {
                entity.HasIndex(e => e.Name)
                    .HasDatabaseName("IX_SafeHavenAssets_Name");

                entity.HasIndex(e => e.AssetType)
                    .HasDatabaseName("IX_SafeHavenAssets_AssetType");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.AssetType)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            // Configuración de la entidad SafeHavenOperation
            modelBuilder.Entity<SafeHavenOperation>(entity =>
            {
                entity.HasIndex(e => e.AssetName)
                    .HasDatabaseName("IX_SafeHavenOperations_AssetName");

                entity.HasIndex(e => e.Date)
                    .HasDatabaseName("IX_SafeHavenOperations_Date");

                entity.HasIndex(e => e.Type)
                    .HasDatabaseName("IX_SafeHavenOperations_Type");

                entity.Property(e => e.AssetName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(50);

                // Ignorar propiedades calculadas
                entity.Ignore(e => e.IsPurchase);
                entity.Ignore(e => e.IsSale);
            });

            // Configuración de la entidad Property
            modelBuilder.Entity<Property>(entity =>
            {
                entity.HasIndex(e => e.Name)
                    .HasDatabaseName("IX_Properties_Name");

                entity.HasIndex(e => e.PropertyType)
                    .HasDatabaseName("IX_Properties_PropertyType");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_Properties_Status");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.PropertyType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(50);

                // Ignorar propiedades calculadas
                entity.Ignore(e => e.NetResult);
                entity.Ignore(e => e.FormattedNetResult);
                entity.Ignore(e => e.IsSold);
                entity.Ignore(e => e.IsRented);
            });

            // Configuración de la entidad PropertyOperation
            modelBuilder.Entity<PropertyOperation>(entity =>
            {
                entity.HasIndex(e => e.PropertyName)
                    .HasDatabaseName("IX_PropertyOperations_PropertyName");

                entity.HasIndex(e => e.Date)
                    .HasDatabaseName("IX_PropertyOperations_Date");

                entity.HasIndex(e => e.Category)
                    .HasDatabaseName("IX_PropertyOperations_Category");

                entity.HasIndex(e => e.Type)
                    .HasDatabaseName("IX_PropertyOperations_Type");

                entity.Property(e => e.PropertyName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(50);

                // Ignorar propiedades calculadas
                entity.Ignore(e => e.IsIncome);
                entity.Ignore(e => e.IsExpense);
            });

            // Configuración de la entidad PropertyDocument (SPEC-14-01)
            modelBuilder.Entity<PropertyDocument>(entity =>
            {
                entity.HasIndex(e => e.PropertyId)
                    .HasDatabaseName("IX_PropertyDocuments_PropertyId");

                entity.Property(e => e.FileName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            });

            // Datos de ejemplo (seed data) para pruebas
            modelBuilder.Entity<FinancialRecord>().HasData(
                new FinancialRecord
                {
                    Id = 1,
                    Date = DateTime.Now.AddMonths(-2),
                    Stocks = 5000.00m,
                    Dividends = 150.00m,
                    CapitalGains = 200.00m,
                    Profits = 350.00m,
                    Funds = 3000.00m,
                    SafeHaven = 1000.00m,
                    InitialCapital = 10000.00m,
                    BankAccount = 2500.00m,
                    CreatedAt = DateTime.Now.AddMonths(-2),
                    Notes = "Registro inicial de ejemplo"
                },
                new FinancialRecord
                {
                    Id = 2,
                    Date = DateTime.Now.AddMonths(-1),
                    Stocks = 5500.00m,
                    Dividends = 175.00m,
                    CapitalGains = 300.00m,
                    Profits = 475.00m,
                    Funds = 3200.00m,
                    SafeHaven = 1100.00m,
                    InitialCapital = 10000.00m,
                    BankAccount = 2800.00m,
                    CreatedAt = DateTime.Now.AddMonths(-1),
                    Notes = "Segundo registro de ejemplo"
                },
                new FinancialRecord
                {
                    Id = 3,
                    Date = DateTime.Now,
                    Stocks = 6000.00m,
                    Dividends = 200.00m,
                    CapitalGains = 400.00m,
                    Profits = 600.00m,
                    Funds = 3500.00m,
                    SafeHaven = 1200.00m,
                    InitialCapital = 10000.00m,
                    BankAccount = 3000.00m,
                    CreatedAt = DateTime.Now,
                    Notes = "Registro actual de ejemplo"
                }
            );

            // Datos de ejemplo para CompanyDetails
            modelBuilder.Entity<CompanyDetail>().HasData(
                new CompanyDetail
                {
                    Id = 1,
                    Name = "Microsoft Corporation",
                    Ticker = "MSFT",
                    Sector = "Tecnología",
                    CurrentPrice = 415.50m,
                    DividendYield = 0.75m,
                    TotalInvestedInitial = 5000.00m,
                    NumberOfShares = 12,
                    CreatedAt = DateTime.Now.AddMonths(-3),
                    UpdatedAt = DateTime.Now.AddMonths(-3)
                },
                new CompanyDetail
                {
                    Id = 2,
                    Name = "Apple Inc.",
                    Ticker = "AAPL",
                    Sector = "Tecnología",
                    CurrentPrice = 225.30m,
                    DividendYield = 0.45m,
                    TotalInvestedInitial = 0.00m,
                    NumberOfShares = 0,
                    CreatedAt = DateTime.Now.AddMonths(-3),
                    UpdatedAt = DateTime.Now.AddMonths(-3)
                },
                new CompanyDetail
                {
                    Id = 3,
                    Name = "Coca-Cola Company",
                    Ticker = "KO",
                    Sector = "Consumo",
                    CurrentPrice = 63.20m,
                    DividendYield = 3.10m,
                    TotalInvestedInitial = 2500.00m,
                    NumberOfShares = 40,
                    CreatedAt = DateTime.Now.AddMonths(-2),
                    UpdatedAt = DateTime.Now.AddMonths(-2)
                },
                new CompanyDetail
                {
                    Id = 4,
                    Name = "Johnson & Johnson",
                    Ticker = "JNJ",
                    Sector = "Salud",
                    CurrentPrice = 158.75m,
                    DividendYield = 3.05m,
                    TotalInvestedInitial = 4000.00m,
                    NumberOfShares = 25,
                    CreatedAt = DateTime.Now.AddMonths(-2),
                    UpdatedAt = DateTime.Now.AddMonths(-2)
                },
                new CompanyDetail
                {
                    Id = 5,
                    Name = "Procter & Gamble Co.",
                    Ticker = "PG",
                    Sector = "Consumo",
                    CurrentPrice = 168.90m,
                    DividendYield = 2.35m,
                    TotalInvestedInitial = 0.00m,
                    NumberOfShares = 0,
                    CreatedAt = DateTime.Now.AddMonths(-1),
                    UpdatedAt = DateTime.Now.AddMonths(-1)
                }
            );

            // Datos de ejemplo para Operations
            modelBuilder.Entity<Operation>().HasData(
                new Operation
                {
                    Id = 1,
                    CompanyName = "Microsoft Corporation",
                    Type = "Compra",
                    Date = DateTime.Now.AddMonths(-3),
                    PricePerShare = 416.67m,
                    NumberOfShares = 12,
                    Amount = 5000.00m,
                    CreatedAt = DateTime.Now.AddMonths(-3)
                },
                new Operation
                {
                    Id = 2,
                    CompanyName = "Apple Inc.",
                    Type = "Compra",
                    Date = DateTime.Now.AddMonths(-2),
                    PricePerShare = 214.29m,
                    NumberOfShares = 14,
                    Amount = 3000.00m,
                    CreatedAt = DateTime.Now.AddMonths(-2)
                },
                new Operation
                {
                    Id = 3,
                    CompanyName = "Coca-Cola Company",
                    Type = "Compra",
                    Date = DateTime.Now.AddMonths(-2),
                    PricePerShare = 62.50m,
                    NumberOfShares = 40,
                    Amount = 2500.00m,
                    CreatedAt = DateTime.Now.AddMonths(-2)
                },
                new Operation
                {
                    Id = 4,
                    CompanyName = "Microsoft Corporation",
                    Type = "Dividendo",
                    Date = DateTime.Now.AddMonths(-1),
                    Amount = 45.50m,
                    CreatedAt = DateTime.Now.AddMonths(-1)
                },
                new Operation
                {
                    Id = 5,
                    CompanyName = "Apple Inc.",
                    Type = "Venta",
                    Date = DateTime.Now.AddDays(-15),
                    CloseDate = DateTime.Now.AddDays(-15),
                    PricePerShare = 214.29m,
                    NumberOfShares = 14,
                    Amount = 1500.00m,
                    CreatedAt = DateTime.Now.AddDays(-15)
                },
                new Operation
                {
                    Id = 6,
                    CompanyName = "Johnson & Johnson",
                    Type = "Compra",
                    Date = DateTime.Now.AddDays(-10),
                    PricePerShare = 160.00m,
                    NumberOfShares = 25,
                    Amount = 4000.00m,
                    CreatedAt = DateTime.Now.AddDays(-10)
                },
                new Operation
                {
                    Id = 7,
                    CompanyName = "Coca-Cola Company",
                    Type = "Dividendo",
                    Date = DateTime.Now.AddDays(-5),
                    Amount = 78.25m,
                    CreatedAt = DateTime.Now.AddDays(-5)
                }
            );

            // Datos de ejemplo para Funds
            modelBuilder.Entity<Fund>().HasData(
                new Fund
                {
                    Id = 1,
                    Name = "Vanguard S&P 500",
                    FundType = "Indexado",
                    Manager = "Vanguard",
                    TER = 0.15m,
                    ISIN = "IE00B4L5Y983",
                    HistoricalReturn = 8.5m,
                    TotalInvested = 10000.00m,
                    CurrentValue = 10850.00m,
                    CreatedAt = DateTime.Now.AddMonths(-6),
                    UpdatedAt = DateTime.Now.AddMonths(-1)
                },
                new Fund
                {
                    Id = 2,
                    Name = "MyInvestor Cartera Permanente",
                    FundType = "Mixto",
                    Manager = "MyInvestor",
                    TER = 0.60m,
                    ISIN = "ES0184947020",
                    HistoricalReturn = 5.2m,
                    TotalInvested = 5000.00m,
                    CurrentValue = 5260.00m,
                    CreatedAt = DateTime.Now.AddMonths(-4),
                    UpdatedAt = DateTime.Now.AddMonths(-1)
                }
            );

            // Datos de ejemplo para FundOperations
            modelBuilder.Entity<FundOperation>().HasData(
                new FundOperation
                {
                    Id = 1,
                    FundName = "Vanguard S&P 500",
                    Type = "Aportación",
                    Amount = 5000.00m,
                    Date = DateTime.Now.AddMonths(-6),
                    CreatedAt = DateTime.Now.AddMonths(-6)
                },
                new FundOperation
                {
                    Id = 2,
                    FundName = "Vanguard S&P 500",
                    Type = "Aportación",
                    Amount = 5000.00m,
                    Date = DateTime.Now.AddMonths(-3),
                    CreatedAt = DateTime.Now.AddMonths(-3)
                },
                new FundOperation
                {
                    Id = 3,
                    FundName = "MyInvestor Cartera Permanente",
                    Type = "Aportación",
                    Amount = 5000.00m,
                    Date = DateTime.Now.AddMonths(-4),
                    CreatedAt = DateTime.Now.AddMonths(-4)
                },
                new FundOperation
                {
                    Id = 4,
                    FundName = "Vanguard S&P 500",
                    Type = "Plusvalía",
                    Amount = 850.00m,
                    Date = DateTime.Now.AddMonths(-1),
                    CreatedAt = DateTime.Now.AddMonths(-1)
                },
                new FundOperation
                {
                    Id = 5,
                    FundName = "MyInvestor Cartera Permanente",
                    Type = "Plusvalía",
                    Amount = 260.00m,
                    Date = DateTime.Now.AddMonths(-1),
                    CreatedAt = DateTime.Now.AddMonths(-1)
                }
            );

            // Datos de ejemplo para BankAccounts
            modelBuilder.Entity<BankAccount>().HasData(
                new BankAccount
                {
                    Id = 1,
                    Name = "Cuenta Corriente Principal",
                    BankName = "Santander",
                    IBAN = "ES1234567890123456789012",
                    AccountType = "Corriente",
                    CurrentBalance = 5500.00m,
                    OpeningBalance = 3850.00m,
                    CreatedAt = DateTime.Now.AddYears(-2),
                    UpdatedAt = DateTime.Now
                },
                new BankAccount
                {
                    Id = 2,
                    Name = "Cuenta Ahorro",
                    BankName = "ING",
                    IBAN = "ES9876543210987654321098",
                    AccountType = "Ahorro",
                    CurrentBalance = 12000.00m,
                    OpeningBalance = 10000.00m,
                    CreatedAt = DateTime.Now.AddYears(-1),
                    UpdatedAt = DateTime.Now
                }
            );

            // Datos de ejemplo para BankOperations
            modelBuilder.Entity<BankOperation>().HasData(
                new BankOperation
                {
                    Id = 1,
                    AccountName = "Cuenta Corriente Principal",
                    Type = "Ingreso",
                    Amount = 2500.00m,
                    Date = DateTime.Now.AddMonths(-1),
                    Description = "Nómina Enero",
                    CreatedAt = DateTime.Now.AddMonths(-1)
                },
                new BankOperation
                {
                    Id = 2,
                    AccountName = "Cuenta Corriente Principal",
                    Type = "Gasto",
                    Amount = -850.00m,
                    Date = DateTime.Now.AddDays(-20),
                    Description = "Alquiler",
                    CreatedAt = DateTime.Now.AddDays(-20)
                },
                new BankOperation
                {
                    Id = 3,
                    AccountName = "Cuenta Ahorro",
                    Type = "Traspaso Entrada",
                    Amount = 2000.00m,
                    Date = DateTime.Now.AddDays(-15),
                    Description = "Ahorro mensual",
                    CreatedAt = DateTime.Now.AddDays(-15)
                }
            );

            // Datos de ejemplo para SafeHavenAssets
            modelBuilder.Entity<SafeHavenAsset>().HasData(
                new SafeHavenAsset
                {
                    Id = 1,
                    Name = "Oro Físico",
                    AssetType = "Oro",
                    TotalInvested = 3000.00m,
                    CurrentValue = 3200.00m,
                    CreatedAt = DateTime.Now.AddYears(-1),
                    UpdatedAt = DateTime.Now
                },
                new SafeHavenAsset
                {
                    Id = 2,
                    Name = "Bitcoin Core",
                    AssetType = "Bitcoin",
                    TotalInvested = 2000.00m,
                    CurrentValue = 2100.00m,
                    CreatedAt = DateTime.Now.AddMonths(-8),
                    UpdatedAt = DateTime.Now
                }
            );

            // Datos de ejemplo para SafeHavenOperations
            modelBuilder.Entity<SafeHavenOperation>().HasData(
                new SafeHavenOperation
                {
                    Id = 1,
                    AssetName = "Oro Físico",
                    Type = "Compra",
                    Amount = 3000.00m,
                    Date = DateTime.Now.AddYears(-1),
                    CreatedAt = DateTime.Now.AddYears(-1)
                },
                new SafeHavenOperation
                {
                    Id = 2,
                    AssetName = "Bitcoin Core",
                    Type = "Compra",
                    Amount = 2000.00m,
                    Date = DateTime.Now.AddMonths(-8),
                    CreatedAt = DateTime.Now.AddMonths(-8)
                }
            );

            // Datos de ejemplo para Properties
            modelBuilder.Entity<Property>().HasData(
                new Property
                {
                    Id = 1,
                    Name = "Piso Madrid Centro",
                    PropertyType = "Vivienda",
                    Address = "Calle Gran Vía 28, Madrid",
                    PurchasePrice = 250000.00m,
                    PurchaseDate = DateTime.Now.AddYears(-3),
                    CurrentValue = 280000.00m,
                    Status = "Alquilado",
                    MonthlyRent = 1200.00m,
                    TotalInvested = 265000.00m,
                    TotalIncome = 43200.00m,
                    CreatedAt = DateTime.Now.AddYears(-3),
                    UpdatedAt = DateTime.Now
                }
            );

            // Datos de ejemplo para PropertyOperations
            modelBuilder.Entity<PropertyOperation>().HasData(
                new PropertyOperation
                {
                    Id = 1,
                    PropertyName = "Piso Madrid Centro",
                    Category = "Gasto",
                    Type = "Compra Inicial",
                    Amount = -250000.00m,
                    Date = DateTime.Now.AddYears(-3),
                    Description = "Compra de propiedad",
                    CreatedAt = DateTime.Now.AddYears(-3)
                },
                new PropertyOperation
                {
                    Id = 2,
                    PropertyName = "Piso Madrid Centro",
                    Category = "Gasto",
                    Type = "Reforma Integral",
                    Amount = -15000.00m,
                    Date = DateTime.Now.AddYears(-3).AddMonths(1),
                    Description = "Renovación completa antes de alquilar",
                    CreatedAt = DateTime.Now.AddYears(-3).AddMonths(1)
                },
                new PropertyOperation
                {
                    Id = 3,
                    PropertyName = "Piso Madrid Centro",
                    Category = "Ingreso",
                    Type = "Alquiler",
                    Amount = 1200.00m,
                    Date = DateTime.Now.AddMonths(-1),
                    Description = "Alquiler Enero 2025",
                    CreatedAt = DateTime.Now.AddMonths(-1)
                }
            );

            // Configuración de la entidad PriceHistoryPoint
            modelBuilder.Entity<PriceHistoryPoint>(entity =>
            {
                // Índice único por empresa + fecha, para evitar duplicados y agilizar el upsert
                entity.HasIndex(e => new { e.CompanyId, e.Date })
                    .IsUnique()
                    .HasDatabaseName("IX_PriceHistoryPoints_CompanyId_Date");

                entity.Property(e => e.Date)
                    .IsRequired();

                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de la entidad SaleHistory (histórico de ventas de acciones)
            modelBuilder.Entity<SaleHistory>(entity =>
            {
                entity.ToTable("SalesHistory");

                // Índices por empresa y por fecha de venta
                entity.HasIndex(e => e.CompanyName)
                    .HasDatabaseName("IX_SalesHistory_CompanyName");

                entity.HasIndex(e => e.SaleDate)
                    .HasDatabaseName("IX_SalesHistory_SaleDate");

                entity.Property(e => e.CompanyName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Ticker)
                    .HasMaxLength(20);
            });

            // Configuración de la entidad AppSetting (ajustes clave/valor)
            modelBuilder.Entity<AppSetting>(entity =>
            {
                entity.ToTable("AppSettings");
                entity.HasKey(e => e.Key);

                entity.Property(e => e.Key)
                    .HasMaxLength(100);

                entity.Property(e => e.TextValue)
                    .HasMaxLength(200);
            });

            // Configuración de la entidad Country (catálogo de países y divisas)
            modelBuilder.Entity<Country>(entity =>
            {
                entity.ToTable("Countries");
                entity.HasKey(e => e.Code);

                entity.Property(e => e.Code).HasMaxLength(3);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Region).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(3);

                entity.HasIndex(e => e.Name)
                    .IsUnique()
                    .HasDatabaseName("IX_Countries_Name");
            });

            // Configuración de la entidad MarketCompany (catálogo de mercado, solo lectura)
            modelBuilder.Entity<MarketCompany>(entity =>
            {
                entity.ToTable("MarketCompanies");
                entity.HasKey(e => e.Ticker);

                entity.Property(e => e.Ticker).HasMaxLength(20);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CountryCode).IsRequired().HasMaxLength(3);
                entity.Property(e => e.Indices).IsRequired().HasMaxLength(100);
            });

            // Configuración de la entidad ExchangeRate (tipos de cambio actuales)
            modelBuilder.Entity<ExchangeRate>(entity =>
            {
                entity.ToTable("ExchangeRates");
                entity.HasKey(e => new { e.CurrencyCode, e.BaseCurrencyCode });

                entity.Property(e => e.CurrencyCode).HasMaxLength(3);
                entity.Property(e => e.BaseCurrencyCode).HasMaxLength(3);
                entity.Property(e => e.Rate).HasColumnType("decimal(18,6)");
                entity.Property(e => e.Source).IsRequired().HasMaxLength(10);
            });

            // Configuración de la entidad WealthSnapshot (capturas mensuales del patrimonio, SPEC-17-01)
            modelBuilder.Entity<WealthSnapshot>(entity =>
            {
                entity.ToTable("WealthSnapshots");

                // Una sola captura por año y mes
                entity.HasIndex(e => new { e.Year, e.Month })
                    .IsUnique()
                    .HasDatabaseName("IX_WealthSnapshots_Year_Month");

                entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(3);
                entity.Property(e => e.Note).HasMaxLength(200);
            });

            // Configuración de la entidad ChartMark (marcas del gráfico de cotización, SPEC-05-03).
            // Sin clave foránea: la baja de una empresa borra sus marcas desde CompanyService
            modelBuilder.Entity<ChartMark>(entity =>
            {
                entity.ToTable("ChartMarks");

                entity.HasIndex(e => new { e.CompanyId, e.Date })
                    .HasDatabaseName("IX_ChartMarks_CompanyId_Date");

                entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
            });

            // Configuración de la entidad DividendPayment (calendario de dividendos, SPEC-21-01).
            // Sin claves foráneas: la baja de una empresa borra sus líneas desde CompanyService
            modelBuilder.Entity<DividendPayment>(entity =>
            {
                entity.ToTable("DividendPayments");

                // Una sola línea por empresa y fecha de pago
                entity.HasIndex(e => new { e.CompanyId, e.PaymentDate })
                    .IsUnique()
                    .HasDatabaseName("IX_DividendPayments_CompanyId_PaymentDate");

                entity.HasIndex(e => e.OperationId)
                    .HasDatabaseName("IX_DividendPayments_OperationId");

                entity.Property(e => e.AmountPerShare).HasColumnType("decimal(18,4)");
                entity.Property(e => e.DividendPercent).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Source).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Notes).HasMaxLength(200);
            });

            // Configuración de los índices para comparar (SPEC-05-04). Sin claves foráneas: la baja de un índice
            // borra sus puntos desde BenchmarkIndexService
            modelBuilder.Entity<BenchmarkIndex>(entity =>
            {
                entity.ToTable("BenchmarkIndices");

                entity.HasIndex(e => e.Symbol)
                    .IsUnique()
                    .HasDatabaseName("IX_BenchmarkIndices_Symbol");

                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            });

            modelBuilder.Entity<BenchmarkPricePoint>(entity =>
            {
                entity.ToTable("BenchmarkPricePoints");

                // Un punto por índice y fecha, para evitar duplicados y agilizar el upsert
                entity.HasIndex(e => new { e.BenchmarkIndexId, e.Date })
                    .IsUnique()
                    .HasDatabaseName("IX_BenchmarkPricePoints_BenchmarkIndexId_Date");
            });

            // Histórico de tipos de cambio (SPEC-01-09). Sin claves foráneas
            modelBuilder.Entity<CurrencyPair>(entity =>
            {
                entity.ToTable("CurrencyPairs");

                entity.HasIndex(e => new { e.CurrencyCode, e.BaseCurrencyCode })
                    .IsUnique()
                    .HasDatabaseName("IX_CurrencyPairs_CurrencyCode_BaseCurrencyCode");

                entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(3);
                entity.Property(e => e.BaseCurrencyCode).IsRequired().HasMaxLength(3);
                entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            });

            modelBuilder.Entity<CurrencyRatePoint>(entity =>
            {
                entity.ToTable("CurrencyRatePoints");

                entity.HasIndex(e => new { e.CurrencyPairId, e.Date })
                    .IsUnique()
                    .HasDatabaseName("IX_CurrencyRatePoints_CurrencyPairId_Date");
            });
        }
    }
}
