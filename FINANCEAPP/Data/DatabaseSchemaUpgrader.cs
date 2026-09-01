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
