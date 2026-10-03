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
                        ""NumberOfShares"" INTEGER NOT NULL,
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
