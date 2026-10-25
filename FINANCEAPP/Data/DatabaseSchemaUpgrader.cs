using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FINANCEAPP.Data
{
    /// <summary>
    /// Utilidad para aplicar cambios de esquema ligeros (nuevas columnas) sin recrear
    /// ni borrar la base de datos existente. EnsureCreated() no actualiza el esquema
    /// de una base de datos ya existente, por lo que este componente cubre ese hueco
    /// para evitar pérdida de datos al añadir nuevas propiedades a las entidades.
    /// </summary>
    public static class DatabaseSchemaUpgrader
    {
        /// <summary>
        /// Verifica que la tabla CompanyDetails tenga las columnas TotalDividends,
        /// PlusvaliaReturnPercent y DividendPlusvaliaReturnPercent. Si faltan, las añade
        /// con valor por defecto 0, sin afectar al resto de los datos existentes.
        /// </summary>
        public static void EnsureCompanyDetailsColumns(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                var existingColumns = GetExistingColumns(connection, "CompanyDetails");

                AddColumnIfMissing(connection, existingColumns, "CompanyDetails", "TotalDividends", "decimal(18,2) NOT NULL DEFAULT 0");
                AddColumnIfMissing(connection, existingColumns, "CompanyDetails", "PlusvaliaReturnPercent", "decimal(18,2) NOT NULL DEFAULT 0");
                AddColumnIfMissing(connection, existingColumns, "CompanyDetails", "DividendPlusvaliaReturnPercent", "decimal(18,2) NOT NULL DEFAULT 0");
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Verifica que la tabla CompanyDetails tenga las columnas LivePrice y LivePriceUpdatedAt.
        /// </summary>
        public static void EnsureCompanyDetailLivePriceColumns(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                var existingColumns = GetExistingColumns(connection, "CompanyDetails");

                AddColumnIfMissing(connection, existingColumns, "CompanyDetails", "LivePrice", "decimal(18,2) NULL");
                AddColumnIfMissing(connection, existingColumns, "CompanyDetails", "LivePriceUpdatedAt", "TEXT NULL");
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Crea la tabla PriceHistoryPoints si no existe todavía (para bases de datos
        /// creadas antes de introducir la caché de histórico de cotización).
        /// </summary>
        public static void EnsurePriceHistoryTable(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ""PriceHistoryPoints"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_PriceHistoryPoints"" PRIMARY KEY AUTOINCREMENT,
                        ""CompanyId"" INTEGER NOT NULL,
                        ""Date"" TEXT NOT NULL,
                        ""Open"" decimal(18,4) NOT NULL,
                        ""High"" decimal(18,4) NOT NULL,
                        ""Low"" decimal(18,4) NOT NULL,
                        ""Close"" decimal(18,4) NOT NULL,
                        ""Volume"" INTEGER NOT NULL,
                        ""FetchedAt"" TEXT NOT NULL,
                        CONSTRAINT ""FK_PriceHistoryPoints_CompanyDetails_CompanyId"" FOREIGN KEY (""CompanyId"") REFERENCES ""CompanyDetails"" (""Id"") ON DELETE CASCADE
                    );

                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_PriceHistoryPoints_CompanyId_Date"" ON ""PriceHistoryPoints"" (""CompanyId"", ""Date"");
                ";
                command.ExecuteNonQuery();
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Crea la tabla SalesHistory si no existe todavía (histórico de ventas de acciones).
        /// No modifica ni borra datos existentes.
        /// </summary>
        public static void EnsureSalesHistoryTable(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ""SalesHistory"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_SalesHistory"" PRIMARY KEY AUTOINCREMENT,
                        ""OperationId"" INTEGER NOT NULL,
                        ""CompanyName"" TEXT NOT NULL,
                        ""Ticker"" TEXT NOT NULL,
                        ""SaleDate"" TEXT NOT NULL,
                        ""NumberOfShares"" decimal(18,2) NOT NULL,
                        ""AverageBuyPrice"" decimal(18,4) NOT NULL,
                        ""SellPrice"" decimal(18,2) NOT NULL,
                        ""Amount"" decimal(18,2) NOT NULL,
                        ""Commission"" decimal(18,2) NOT NULL,
                        ""Profit"" decimal(18,2) NOT NULL,
                        ""CreatedAt"" TEXT NOT NULL,
                        ""UpdatedAt"" TEXT NOT NULL
                    );

                    CREATE INDEX IF NOT EXISTS ""IX_SalesHistory_CompanyName"" ON ""SalesHistory"" (""CompanyName"");
                    CREATE INDEX IF NOT EXISTS ""IX_SalesHistory_SaleDate"" ON ""SalesHistory"" (""SaleDate"");
                ";
                command.ExecuteNonQuery();
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Añade a la tabla Operations las columnas SaleHistoryId y OriginOperationId (nulas) y el índice
        /// de SaleHistoryId si faltan. No modifica ni borra datos existentes.
        /// </summary>
        public static void EnsureOperationSaleColumns(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                var existingColumns = GetExistingColumns(connection, "Operations");

                AddColumnIfMissing(connection, existingColumns, "Operations", "SaleHistoryId", "INTEGER NULL");
                AddColumnIfMissing(connection, existingColumns, "Operations", "OriginOperationId", "INTEGER NULL");

                using var command = connection.CreateCommand();
                command.CommandText = @"CREATE INDEX IF NOT EXISTS ""IX_Operations_SaleHistoryId"" ON ""Operations"" (""SaleHistoryId"");";
                command.ExecuteNonQuery();
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Crea la tabla AppSettings si no existe todavía (ajustes de la aplicación, por ahora el logo).
        /// No modifica ni borra datos existentes.
        /// </summary>
        public static void EnsureAppSettingsTable(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ""AppSettings"" (
                        ""Key"" TEXT NOT NULL CONSTRAINT ""PK_AppSettings"" PRIMARY KEY,
                        ""TextValue"" TEXT NULL,
                        ""BinaryValue"" BLOB NULL,
                        ""UpdatedAt"" TEXT NOT NULL
                    );
                ";
                command.ExecuteNonQuery();
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Crea la tabla Countries si no existe y, una sola vez (marca CountriesSeeded en AppSettings),
        /// la rellena con 14 países de partida y fija ES como país base. Debe ejecutarse después de
        /// EnsureAppSettingsTable. Si el usuario borra después un país sembrado, no se vuelve a crear.
        /// </summary>
        public static void EnsureCountriesTable(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                using (var create = connection.CreateCommand())
                {
                    create.CommandText = @"
                        CREATE TABLE IF NOT EXISTS ""Countries"" (
                            ""Code"" TEXT NOT NULL CONSTRAINT ""PK_Countries"" PRIMARY KEY,
                            ""Name"" TEXT NOT NULL,
                            ""Region"" TEXT NOT NULL,
                            ""CurrencyCode"" TEXT NOT NULL,
                            ""CreatedAt"" TEXT NOT NULL,
                            ""UpdatedAt"" TEXT NOT NULL
                        );

                        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Countries_Name"" ON ""Countries"" (""Name"" COLLATE NOCASE);
                    ";
                    create.ExecuteNonQuery();
                }

                bool seeded;
                using (var check = connection.CreateCommand())
                {
                    check.CommandText = @"SELECT COUNT(1) FROM ""AppSettings"" WHERE ""Key"" = 'CountriesSeeded';";
                    seeded = Convert.ToInt32(check.ExecuteScalar()) > 0;
                }

                if (seeded)
                {
                    return;
                }

                var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff");
                var rows = new (string Code, string Name, string Region, string Currency)[]
                {
                    ("ES", "España", "EU", "EUR"),
                    ("DE", "Alemania", "EU", "EUR"),
                    ("FR", "Francia", "EU", "EUR"),
                    ("IT", "Italia", "EU", "EUR"),
                    ("NL", "Países Bajos", "EU", "EUR"),
                    ("PT", "Portugal", "EU", "EUR"),
                    ("IE", "Irlanda", "EU", "EUR"),
                    ("BE", "Bélgica", "EU", "EUR"),
                    ("GB", "Reino Unido", "EU", "GBP"),
                    ("CH", "Suiza", "EU", "CHF"),
                    ("US", "Estados Unidos", "AMERICA", "USD"),
                    ("CA", "Canadá", "AMERICA", "CAD"),
                    ("JP", "Japón", "ASIA", "JPY"),
                    ("BR", "Brasil", "SUDAMERICA", "BRL")
                };

                using var transaction = connection.BeginTransaction();

                foreach (var row in rows)
                {
                    using var insert = connection.CreateCommand();
                    insert.Transaction = transaction;
                    insert.CommandText = @"INSERT OR IGNORE INTO ""Countries"" (""Code"", ""Name"", ""Region"", ""CurrencyCode"", ""CreatedAt"", ""UpdatedAt"")
                                           VALUES ($code, $name, $region, $currency, $now, $now);";
                    AddParameter(insert, "$code", row.Code);
                    AddParameter(insert, "$name", row.Name);
                    AddParameter(insert, "$region", row.Region);
                    AddParameter(insert, "$currency", row.Currency);
                    AddParameter(insert, "$now", now);
                    insert.ExecuteNonQuery();
                }

                using (var mark = connection.CreateCommand())
                {
                    mark.Transaction = transaction;
                    mark.CommandText = @"INSERT OR IGNORE INTO ""AppSettings"" (""Key"", ""TextValue"", ""UpdatedAt"") VALUES ('CountriesSeeded', '1', $now),
                                                                                                                         ('BaseCountryCode', 'ES', $now);";
                    AddParameter(mark, "$now", now);
                    mark.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Añade a CompanyDetails la columna CountryCode (nula) si falta. No modifica datos existentes.
        /// </summary>
        public static void EnsureCompanyCountryColumn(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                var existingColumns = GetExistingColumns(connection, "CompanyDetails");
                AddColumnIfMissing(connection, existingColumns, "CompanyDetails", "CountryCode", "TEXT NULL");
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Añade a Operations la columna CurrencyCode (nula = EUR) si falta. No modifica datos existentes.
        /// </summary>
        public static void EnsureOperationCurrencyColumn(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                var existingColumns = GetExistingColumns(connection, "Operations");
                AddColumnIfMissing(connection, existingColumns, "Operations", "CurrencyCode", "TEXT NULL");
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Crea la tabla ExchangeRates si no existe (SPEC-01-03). No toca ningún dato existente.
        /// </summary>
        public static void EnsureExchangeRatesTable(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                using var create = connection.CreateCommand();
                create.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ""ExchangeRates"" (
                        ""CurrencyCode"" TEXT NOT NULL,
                        ""BaseCurrencyCode"" TEXT NOT NULL,
                        ""Rate"" decimal(18,6) NOT NULL,
                        ""Source"" TEXT NOT NULL,
                        ""UpdatedAt"" TEXT NOT NULL,
                        CONSTRAINT ""PK_ExchangeRates"" PRIMARY KEY (""CurrencyCode"", ""BaseCurrencyCode"")
                    );
                ";
                create.ExecuteNonQuery();
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Crea la tabla PropertyDocuments (documentos adjuntos a inmuebles) si no existe (SPEC-14-01).
        /// No toca ninguna otra tabla ni dato.
        /// </summary>
        public static void EnsurePropertyDocumentsTable(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                using var create = connection.CreateCommand();
                create.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ""PropertyDocuments"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_PropertyDocuments"" PRIMARY KEY AUTOINCREMENT,
                        ""PropertyId"" INTEGER NOT NULL,
                        ""FileName"" TEXT NOT NULL,
                        ""ContentType"" TEXT NOT NULL,
                        ""SizeBytes"" INTEGER NOT NULL,
                        ""Content"" BLOB NOT NULL,
                        ""Description"" TEXT NULL,
                        ""UploadedAt"" TEXT NOT NULL,
                        ""ExtractedCompany"" TEXT NULL,
                        ""ExtractedAmount"" decimal(18,2) NULL,
                        ""ExtractedDate"" TEXT NULL,
                        ""ExtractedType"" TEXT NULL,
                        ""AiReadAt"" TEXT NULL,
                        ""PropertyOperationId"" INTEGER NULL
                    );
                ";
                create.ExecuteNonQuery();

                using var index = connection.CreateCommand();
                index.CommandText = @"CREATE INDEX IF NOT EXISTS ""IX_PropertyDocuments_PropertyId"" ON ""PropertyDocuments"" (""PropertyId"");";
                index.ExecuteNonQuery();
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Crea la tabla DividendPayments (calendario de dividendos, SPEC-21-01) y sus índices si no existen.
        /// No toca ninguna otra tabla ni dato.
        /// </summary>
        public static void EnsureDividendPaymentsTable(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                using var create = connection.CreateCommand();
                create.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ""DividendPayments"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_DividendPayments"" PRIMARY KEY AUTOINCREMENT,
                        ""CompanyId"" INTEGER NOT NULL,
                        ""PaymentDate"" TEXT NOT NULL,
                        ""ExDividendDate"" TEXT NULL,
                        ""AmountPerShare"" decimal(18,4) NULL,
                        ""NumberOfShares"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""DividendPercent"" decimal(18,2) NULL,
                        ""Status"" TEXT NOT NULL DEFAULT 'Pendiente',
                        ""OperationId"" INTEGER NULL,
                        ""Source"" TEXT NOT NULL DEFAULT 'Manual',
                        ""Notes"" TEXT NULL,
                        ""CreatedAt"" TEXT NOT NULL,
                        ""UpdatedAt"" TEXT NOT NULL
                    );
                ";
                create.ExecuteNonQuery();

                using var indexes = connection.CreateCommand();
                indexes.CommandText = @"
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_DividendPayments_CompanyId_PaymentDate"" ON ""DividendPayments"" (""CompanyId"", ""PaymentDate"");
                    CREATE INDEX IF NOT EXISTS ""IX_DividendPayments_OperationId"" ON ""DividendPayments"" (""OperationId"");
                ";
                indexes.ExecuteNonQuery();
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Añade la columna nula NextDividendDate a CompanyDetails si falta (SPEC-21-01). Las empresas existentes se quedan sin fecha.
        /// </summary>
        public static void EnsureCompanyNextDividendDateColumn(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                var existingColumns = GetExistingColumns(connection, "CompanyDetails");
                AddColumnIfMissing(connection, existingColumns, "CompanyDetails", "NextDividendDate", "TEXT NULL");
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Crea la tabla ChartMarks (marcas del gráfico de cotización, SPEC-05-03) y su índice si no existen.
        /// CompanyId nulo = marca global. No toca ninguna otra tabla ni dato.
        /// </summary>
        public static void EnsureChartMarksTable(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                using var create = connection.CreateCommand();
                create.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ""ChartMarks"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_ChartMarks"" PRIMARY KEY AUTOINCREMENT,
                        ""CompanyId"" INTEGER NULL,
                        ""Date"" TEXT NOT NULL,
                        ""Description"" TEXT NOT NULL,
                        ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                        ""CreatedAt"" TEXT NOT NULL,
                        ""UpdatedAt"" TEXT NOT NULL
                    );
                ";
                create.ExecuteNonQuery();

                using var index = connection.CreateCommand();
                index.CommandText = @"CREATE INDEX IF NOT EXISTS ""IX_ChartMarks_CompanyId_Date"" ON ""ChartMarks"" (""CompanyId"", ""Date"");";
                index.ExecuteNonQuery();
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Crea las tablas BenchmarkIndices y BenchmarkPricePoints (índices para comparar en el gráfico, SPEC-05-04)
        /// y sus índices únicos si no existen. Sin claves foráneas. No toca ninguna otra tabla ni dato.
        /// </summary>
        public static void EnsureBenchmarkTables(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ""BenchmarkIndices"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_BenchmarkIndices"" PRIMARY KEY AUTOINCREMENT,
                        ""Name"" TEXT NOT NULL,
                        ""Symbol"" TEXT NOT NULL,
                        ""CreatedAt"" TEXT NOT NULL,
                        ""UpdatedAt"" TEXT NOT NULL
                    );

                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_BenchmarkIndices_Symbol"" ON ""BenchmarkIndices"" (""Symbol"");

                    CREATE TABLE IF NOT EXISTS ""BenchmarkPricePoints"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_BenchmarkPricePoints"" PRIMARY KEY AUTOINCREMENT,
                        ""BenchmarkIndexId"" INTEGER NOT NULL,
                        ""Date"" TEXT NOT NULL,
                        ""Open"" decimal(18,4) NOT NULL,
                        ""High"" decimal(18,4) NOT NULL,
                        ""Low"" decimal(18,4) NOT NULL,
                        ""Close"" decimal(18,4) NOT NULL,
                        ""Volume"" INTEGER NOT NULL,
                        ""FetchedAt"" TEXT NOT NULL
                    );

                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_BenchmarkPricePoints_BenchmarkIndexId_Date"" ON ""BenchmarkPricePoints"" (""BenchmarkIndexId"", ""Date"");
                ";
                command.ExecuteNonQuery();
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Crea las tablas de las capturas diarias de la cartera (SPEC-17-03): DailyPortfolioSnapshots (una fila por día)
        /// y DailyCompanySnapshots (una fila por empresa y día), con sus índices únicos si no existen. Son datos derivados
        /// que se pueden regenerar; sin claves foráneas. No toca ninguna otra tabla ni dato.
        /// </summary>
        public static void EnsureDailySnapshotTables(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                using var create = connection.CreateCommand();
                create.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ""DailyPortfolioSnapshots"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_DailyPortfolioSnapshots"" PRIMARY KEY AUTOINCREMENT,
                        ""Date"" TEXT NOT NULL,
                        ""CurrencyCode"" TEXT NOT NULL DEFAULT 'EUR',
                        ""CalculatedAt"" TEXT NOT NULL,
                        ""IsReconstructed"" INTEGER NOT NULL DEFAULT 0,
                        ""IsApproximate"" INTEGER NOT NULL DEFAULT 0,
                        ""ApproximateNote"" TEXT NULL,
                        ""TotalValue"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""TotalInvested"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""StocksValue"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""StocksInvested"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""FundsValue"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""FundsInvested"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""SafeHavenValue"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""SafeHavenInvested"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""RealEstateValue"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""RealEstateInvested"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""BankBalance"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""LiquidityPct"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""OpenInvested"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""OpenValue"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""OpenDividends"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""OpenReturnPct"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""OpenReturnWithDivPct"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""DayBought"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""DayBuyCount"" INTEGER NOT NULL DEFAULT 0,
                        ""AllInvested"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""AllValue"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""AllDividends"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""AllReturnPct"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""AllReturnWithDivPct"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""SoldCost"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""SoldProceeds"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""SoldResult"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""SoldReturnPct"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""DaySoldNet"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""DayNetContribution"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""DayDividends"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""DayCommissions"" decimal(18,2) NOT NULL DEFAULT 0
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_DailyPortfolioSnapshots_Date"" ON ""DailyPortfolioSnapshots"" (""Date"");
                    CREATE TABLE IF NOT EXISTS ""DailyCompanySnapshots"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_DailyCompanySnapshots"" PRIMARY KEY AUTOINCREMENT,
                        ""Date"" TEXT NOT NULL,
                        ""CompanyId"" INTEGER NOT NULL,
                        ""CompanyName"" TEXT NOT NULL DEFAULT '',
                        ""Ticker"" TEXT NOT NULL DEFAULT '',
                        ""Sector"" TEXT NOT NULL DEFAULT '',
                        ""CurrencyCode"" TEXT NOT NULL DEFAULT 'EUR',
                        ""Shares"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""Price"" decimal(18,4) NOT NULL DEFAULT 0,
                        ""CostBase"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""ValueBase"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""DividendsBase"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""ReturnPct"" decimal(18,2) NOT NULL DEFAULT 0,
                        ""WeightPct"" decimal(18,2) NOT NULL DEFAULT 0
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_DailyCompanySnapshots_Date_CompanyId"" ON ""DailyCompanySnapshots"" (""Date"", ""CompanyId"");
                ";
                create.ExecuteNonQuery();
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Crea las tablas CurrencyPairs y CurrencyRatePoints (histórico diario de tipos de cambio, SPEC-01-09) y sus
        /// índices únicos si no existen. Sin claves foráneas. No toca ninguna otra tabla ni dato.
        /// </summary>
        public static void EnsureCurrencyHistoryTables(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ""CurrencyPairs"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_CurrencyPairs"" PRIMARY KEY AUTOINCREMENT,
                        ""CurrencyCode"" TEXT NOT NULL,
                        ""BaseCurrencyCode"" TEXT NOT NULL,
                        ""Symbol"" TEXT NOT NULL,
                        ""CreatedAt"" TEXT NOT NULL
                    );

                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_CurrencyPairs_CurrencyCode_BaseCurrencyCode"" ON ""CurrencyPairs"" (""CurrencyCode"", ""BaseCurrencyCode"");

                    CREATE TABLE IF NOT EXISTS ""CurrencyRatePoints"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_CurrencyRatePoints"" PRIMARY KEY AUTOINCREMENT,
                        ""CurrencyPairId"" INTEGER NOT NULL,
                        ""Date"" TEXT NOT NULL,
                        ""Open"" decimal(18,6) NOT NULL,
                        ""High"" decimal(18,6) NOT NULL,
                        ""Low"" decimal(18,6) NOT NULL,
                        ""Close"" decimal(18,6) NOT NULL,
                        ""Volume"" INTEGER NOT NULL,
                        ""FetchedAt"" TEXT NOT NULL
                    );

                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_CurrencyRatePoints_CurrencyPairId_Date"" ON ""CurrencyRatePoints"" (""CurrencyPairId"", ""Date"");
                ";
                command.ExecuteNonQuery();
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Crea la tabla WealthSnapshots (capturas mensuales del patrimonio, SPEC-17-01) y su índice
        /// único por año y mes si no existen. No toca ninguna otra tabla ni dato.
        /// </summary>
        public static void EnsureWealthSnapshotsTable(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                using var create = connection.CreateCommand();
                create.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ""WealthSnapshots"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_WealthSnapshots"" PRIMARY KEY AUTOINCREMENT,
                        ""Year"" INTEGER NOT NULL,
                        ""Month"" INTEGER NOT NULL,
                        ""CapturedAt"" TEXT NOT NULL,
                        ""CurrencyCode"" TEXT NOT NULL,
                        ""TotalPatrimony"" decimal(18,2) NOT NULL,
                        ""TotalInvested"" decimal(18,2) NOT NULL,
                        ""StocksValue"" decimal(18,2) NOT NULL,
                        ""StocksInvested"" decimal(18,2) NOT NULL,
                        ""FundsValue"" decimal(18,2) NOT NULL,
                        ""FundsInvested"" decimal(18,2) NOT NULL,
                        ""BankBalance"" decimal(18,2) NOT NULL,
                        ""SafeHavenValue"" decimal(18,2) NOT NULL,
                        ""SafeHavenInvested"" decimal(18,2) NOT NULL,
                        ""RealEstateValue"" decimal(18,2) NOT NULL,
                        ""RealEstateInvested"" decimal(18,2) NOT NULL,
                        ""ExcludedCompanies"" INTEGER NOT NULL DEFAULT 0,
                        ""Note"" TEXT NULL,
                        ""CreatedAt"" TEXT NOT NULL,
                        ""UpdatedAt"" TEXT NOT NULL
                    );
                ";
                create.ExecuteNonQuery();

                using var index = connection.CreateCommand();
                index.CommandText = @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_WealthSnapshots_Year_Month"" ON ""WealthSnapshots"" (""Year"", ""Month"");";
                index.ExecuteNonQuery();
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Añade las columnas nulas ExchangeRate y ExchangeRateTo a Operations si faltan (SPEC-01-03).
        /// Las operaciones existentes se quedan sin tipo de cambio.
        /// </summary>
        public static void EnsureOperationExchangeRateColumns(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                var existingColumns = GetExistingColumns(connection, "Operations");
                AddColumnIfMissing(connection, existingColumns, "Operations", "ExchangeRate", "decimal(18,6) NULL");
                AddColumnIfMissing(connection, existingColumns, "Operations", "ExchangeRateTo", "TEXT NULL");
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Crea la tabla MarketCompanies si no existe y la carga desde MarketCatalogData cuando la versión
        /// guardada en AppSettings (clave MarketCatalogVersion) es distinta de la de la aplicación.
        /// El catálogo es de solo lectura, así que se rehace entero en una transacción; no toca
        /// CompanyDetails ni Operations. Debe ejecutarse después de EnsureAppSettingsTable.
        /// </summary>
        public static void EnsureMarketCatalogTable(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                using (var create = connection.CreateCommand())
                {
                    create.CommandText = @"
                        CREATE TABLE IF NOT EXISTS ""MarketCompanies"" (
                            ""Ticker"" TEXT NOT NULL COLLATE NOCASE CONSTRAINT ""PK_MarketCompanies"" PRIMARY KEY,
                            ""Name"" TEXT NOT NULL,
                            ""CountryCode"" TEXT NOT NULL,
                            ""Indices"" TEXT NOT NULL
                        );
                    ";
                    create.ExecuteNonQuery();
                }

                string? savedVersion;
                using (var check = connection.CreateCommand())
                {
                    check.CommandText = @"SELECT ""TextValue"" FROM ""AppSettings"" WHERE ""Key"" = 'MarketCatalogVersion';";
                    savedVersion = check.ExecuteScalar() as string;
                }

                if (savedVersion == MarketCatalogData.Version)
                {
                    return;
                }

                using var transaction = connection.BeginTransaction();

                using (var clear = connection.CreateCommand())
                {
                    clear.Transaction = transaction;
                    clear.CommandText = @"DELETE FROM ""MarketCompanies"";";
                    clear.ExecuteNonQuery();
                }

                using (var insert = connection.CreateCommand())
                {
                    insert.Transaction = transaction;
                    insert.CommandText = @"INSERT INTO ""MarketCompanies"" (""Ticker"", ""Name"", ""CountryCode"", ""Indices"")
                                           VALUES ($ticker, $name, $country, $indices)
                                           ON CONFLICT(""Ticker"") DO UPDATE SET ""Name"" = excluded.""Name"",
                                               ""CountryCode"" = excluded.""CountryCode"", ""Indices"" = excluded.""Indices"";";
                    var ticker = AddParameter(insert, "$ticker", string.Empty);
                    var name = AddParameter(insert, "$name", string.Empty);
                    var country = AddParameter(insert, "$country", string.Empty);
                    var indices = AddParameter(insert, "$indices", string.Empty);

                    foreach (var entry in MarketCatalogData.Entries)
                    {
                        ticker.Value = entry.Ticker;
                        name.Value = entry.Name;
                        country.Value = entry.CountryCode;
                        indices.Value = entry.Indices;
                        insert.ExecuteNonQuery();
                    }
                }

                using (var mark = connection.CreateCommand())
                {
                    mark.Transaction = transaction;
                    mark.CommandText = @"INSERT INTO ""AppSettings"" (""Key"", ""TextValue"", ""UpdatedAt"") VALUES ('MarketCatalogVersion', $version, $now)
                                         ON CONFLICT(""Key"") DO UPDATE SET ""TextValue"" = excluded.""TextValue"", ""UpdatedAt"" = excluded.""UpdatedAt"";";
                    AddParameter(mark, "$version", MarketCatalogData.Version);
                    AddParameter(mark, "$now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
                    mark.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Repara el total invertido de los inmuebles creados antes de la corrección de alta: quedaban con
        /// TotalInvested = 0 y el patrimonio no los sumaba. Solo toca los que tienen un total imposible
        /// (menor que el precio de compra) y lo deja en precio de compra + gastos. Es idempotente.
        /// </summary>
        public static void RepairPropertyTotals(AppDbContext context)
        {
            var broken = context.Properties
                .AsEnumerable()
                .Where(p => p.TotalInvested < p.PurchasePrice)
                .ToList();

            if (broken.Count == 0)
            {
                return;
            }

            var expensesByProperty = context.PropertyOperations
                .Where(o => o.Category == "Gasto")
                .AsEnumerable()
                .GroupBy(o => o.PropertyName)
                .ToDictionary(g => g.Key, g => g.Sum(o => Math.Abs(o.Amount)));

            foreach (var property in broken)
            {
                property.TotalInvested = property.PurchasePrice +
                    (expensesByProperty.TryGetValue(property.Name, out var expenses) ? expenses : 0m);
                property.UpdatedAt = DateTime.Now;
            }

            context.SaveChanges();
            context.ChangeTracker.Clear();
        }

        private static System.Data.Common.DbParameter AddParameter(System.Data.Common.DbCommand command, string name, string value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
            return parameter;
        }

        /// <summary>
        /// Verifica que la tabla BankAccounts tenga la columna OpeningBalance (saldo inicial).
        /// Solo al crearla calcula su valor como saldo actual menos el efecto de los movimientos
        /// de cada cuenta, de modo que el saldo mostrado no cambia. Si ya existe, no hace nada.
        /// </summary>
        public static void EnsureBankAccountOpeningBalanceColumn(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                var existingColumns = GetExistingColumns(connection, "BankAccounts");
                if (existingColumns.Contains("OpeningBalance"))
                {
                    return;
                }

                AddColumnIfMissing(connection, existingColumns, "BankAccounts", "OpeningBalance", "decimal(18,2) NOT NULL DEFAULT 0");

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE ""BankAccounts"" SET ""OpeningBalance"" = ""CurrentBalance"" - COALESCE((
                        SELECT SUM(CASE
                            WHEN ""Type"" IN ('Ingreso', 'Intereses', 'Traspaso Entrada') THEN ABS(""Amount"")
                            WHEN ""Type"" IN ('Gasto', 'Comisiones', 'Traspaso Salida') THEN -ABS(""Amount"")
                            ELSE 0 END)
                        FROM ""BankOperations""
                        WHERE ""BankOperations"".""AccountName"" = ""BankAccounts"".""Name""), 0);";
                command.ExecuteNonQuery();
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Verifica que la tabla BankOperations tenga la columna SaleHistoryId (ingreso generado por una venta,
        /// SPEC-02-04) y su índice. Columna nula: no toca los datos existentes.
        /// </summary>
        public static void EnsureBankOperationSaleColumn(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                var existingColumns = GetExistingColumns(connection, "BankOperations");

                AddColumnIfMissing(connection, existingColumns, "BankOperations", "SaleHistoryId", "INTEGER NULL");

                using var command = connection.CreateCommand();
                command.CommandText = @"CREATE INDEX IF NOT EXISTS ""IX_BankOperations_SaleHistoryId"" ON ""BankOperations"" (""SaleHistoryId"");";
                command.ExecuteNonQuery();
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Verifica que la tabla BankOperations tenga la columna OperationId (movimiento generado por una compra
        /// o un dividendo, SPEC-02-06) y su índice. Columna nula: no toca los datos existentes.
        /// </summary>
        public static void EnsureBankOperationTradeColumn(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                var existingColumns = GetExistingColumns(connection, "BankOperations");

                AddColumnIfMissing(connection, existingColumns, "BankOperations", "OperationId", "INTEGER NULL");

                using var command = connection.CreateCommand();
                command.CommandText = @"CREATE INDEX IF NOT EXISTS ""IX_BankOperations_OperationId"" ON ""BankOperations"" (""OperationId"");";
                command.ExecuteNonQuery();
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Verifica que la tabla BankOperations tenga la columna TransferId (patas de un traspaso entre cuentas,
        /// SPEC-10-02) y su índice. Columna nula: no toca los datos existentes.
        /// </summary>
        public static void EnsureBankOperationTransferColumn(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                var existingColumns = GetExistingColumns(connection, "BankOperations");

                AddColumnIfMissing(connection, existingColumns, "BankOperations", "TransferId", "INTEGER NULL");

                using var command = connection.CreateCommand();
                command.CommandText = @"CREATE INDEX IF NOT EXISTS ""IX_BankOperations_TransferId"" ON ""BankOperations"" (""TransferId"");";
                command.ExecuteNonQuery();
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        private static HashSet<string> GetExistingColumns(System.Data.Common.DbConnection connection, string tableName)
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                // La columna "name" está en el índice 1 del resultado de PRAGMA table_info
                columns.Add(reader.GetString(1));
            }

            return columns;
        }

        private static void AddColumnIfMissing(
            System.Data.Common.DbConnection connection,
            HashSet<string> existingColumns,
            string tableName,
            string columnName,
            string columnDefinition)
        {
            if (existingColumns.Contains(columnName))
            {
                return;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {columnDefinition};";
            command.ExecuteNonQuery();

            existingColumns.Add(columnName);
        }
    }
}
