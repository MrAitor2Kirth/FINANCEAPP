# SPEC-19 · Persistencia, evolución del esquema y copias de seguridad

| | |
|---|---|
| **Categoría** | 7. Plataforma y datos |
| **Identificador** | SPEC-19 |
| **Documento** | `spec19-persistencia-esquema-y-copias-de-seguridad.md` |
| **Volver al índice** | [README de specs](README.md) |

> Base de datos SQLite local, migración ligera sin pérdida de datos y backups.

## 1. Descripción funcional

Garantiza que los datos del usuario se guarden en un fichero SQLite local, que la estructura de la base de datos evolucione al actualizar la app sin borrar información y que existan procedimientos de copia y reinicio.

## 2. Pantallas y rutas

- Transversal: se ejecuta al arrancar la aplicación (`MauiProgram`).
- Utilidades externas: `RecreateDatabase.ps1`, carpeta `Backups/`, proyecto `TempDbViewer`.

## 3. Reglas de negocio

- La base de datos `financeapp.db` se crea en el directorio de datos de la aplicación (`FileSystem.AppDataDirectory`) solo si no existe; **los datos persisten entre ejecuciones**.
- Al arrancar se ejecuta `EnsureCreated()` y a continuación las migraciones ligeras de `DatabaseSchemaUpgrader`, que añaden columnas o tablas si faltan: `EnsureCompanyDetailsColumns`, `EnsureCompanyDetailLivePriceColumns` y `EnsurePriceHistoryTable` (con su índice único empresa + fecha). Nunca borran datos.
- En una base de datos nueva se cargan **datos de ejemplo** (empresas MSFT, AAPL, KO, JNJ, PG y operaciones de muestra).
- **Copias de seguridad**: se copia el fichero `financeapp.db` de la ubicación de datos de la app (no el `bin/`) a `Backups/` con nombre `BACKUPddmmaaaa.db`; como esa carpeta está en `.gitignore`, se añaden a git con `git add -f`.
- **Reinicio**: `RecreateDatabase.ps1` elimina la base de datos para que se regenere vacía (**borra todos los datos**).
- En modo DEBUG se registra la ruta de la base de datos en el log.

## 4. Datos implicados

- Todas las tablas del `AppDbContext`: FinancialRecords, Operations, CompanyDetails, Funds, FundOperations, FundValueHistories, BankAccounts, BankOperations, SafeHavenAssets, SafeHavenOperations, Properties, PropertyOperations y PriceHistoryPoints.

## 5. Flujo principal

1. La app arranca y registra el `DbContext` con SQLite.
2. Se crea la base si no existe y se aplican las migraciones ligeras.
3. Los servicios acceden a los datos mediante inyección de dependencias.
4. El usuario realiza copias manuales a `Backups/` cuando lo necesita.

## 6. Componentes técnicos

- `MauiProgram.cs`, `Data/AppDbContext.cs`, `Data/DatabaseSchemaUpgrader.cs`.
- `Microsoft.EntityFrameworkCore.Sqlite`, `SQLitePCLRaw.lib.e_sqlite3` (por plataforma).
- Scripts `RecreateDatabase.ps1` y utilidad `TempDbViewer`.

## 7. Criterios de aceptación

- [ ] Al reiniciar la app los datos siguen ahí.
- [ ] Una columna nueva se añade sin perder datos.
- [ ] Un backup restaurado devuelve la app al estado de esa copia.
- [ ] `RecreateDatabase.ps1` deja la app con base de datos nueva.

## 8. Observaciones y limitaciones detectadas

- No se usan migraciones de Entity Framework, por lo que los cambios de esquema deben añadirse a mano en `DatabaseSchemaUpgrader`.
- No hay función de **exportar/importar** datos dentro de la app: es necesario para llevar los datos a un móvil.
- La ruta de la base de datos y los backups descritos son específicos de Windows sin empaquetar.
- Los datos de ejemplo se cargan al crear la base de datos, por lo que una base nueva no empieza vacía.
