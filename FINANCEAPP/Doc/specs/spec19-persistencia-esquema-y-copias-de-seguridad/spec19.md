# SPEC-19 · Persistencia, evolución del esquema y copias de seguridad

| | |
|---|---|
| **Categoría** | 7. Plataforma y datos |
| **Identificador** | SPEC-19 |
| **Documento** | `spec19-persistencia-esquema-y-copias-de-seguridad/spec19.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Base de datos SQLite local, migración ligera sin pérdida de datos y backups.

## 1. Descripción funcional

Garantiza que los datos del usuario se guarden en un fichero SQLite local, que la estructura de la base de datos evolucione al actualizar la app sin borrar información y que existan procedimientos de copia y reinicio.

## 2. Pantallas y rutas

- Transversal: se ejecuta al arrancar la aplicación (`MauiProgram`).
- Utilidades externas: `RecreateDatabase.ps1`, carpeta `Backups/`, proyecto `TempDbViewer`.

## 3. Reglas de negocio

- La base de datos `financeapp.db` se crea en el directorio de datos de la aplicación (`FileSystem.AppDataDirectory`) solo si no existe; **los datos persisten entre ejecuciones**.
- Al arrancar se ejecuta `EnsureCreated()` y a continuación las migraciones ligeras de `DatabaseSchemaUpgrader`, que añaden columnas o tablas si faltan: `EnsureCompanyDetailsColumns`, `EnsureCompanyDetailLivePriceColumns`, `EnsurePriceHistoryTable` (con su índice único empresa + fecha), `EnsureSalesHistoryTable` `EnsureOperationSaleColumns` (columnas nulas `SaleHistoryId` y `OriginOperationId` de `Operations` e índice) `EnsureAppSettingsTable` (tabla `AppSettings` con el logo y el país base), `EnsureCountriesTable` (tabla `Countries` con una precarga única de 14 países marcada con `CountriesSeeded`), `EnsureCompanyCountryColumn` (columna nula `CountryCode` de `CompanyDetails`) `EnsureOperationCurrencyColumn` (columna nula `CurrencyCode` de `Operations`) `EnsureBankOperationSaleColumn` (columna nula `SaleHistoryId` de `BankOperations` e índice, SPEC-02-04), `EnsureBankAccountOpeningBalanceColumn` (columna `OpeningBalance` de `BankAccounts`: al crearla se calcula como saldo actual menos el efecto de los movimientos, una sola vez) y `EnsurePropertyDocumentsTable` (tabla `PropertyDocuments` con los archivos adjuntos de inmuebles, con índice por `PropertyId`; la clave `AiModel` de `AppSettings` guarda el modelo de IA). Nunca borran datos.
- En una base de datos nueva se cargan **datos de ejemplo** (empresas MSFT, AAPL, KO, JNJ, PG y operaciones de muestra).
- **Copias de seguridad**: se copia el fichero `financeapp.db` de la ubicación de datos de la app (no el `bin/`) a `Backups/` con nombre `BACKUPddmmaaaa.db`; como esa carpeta está en `.gitignore`, se añaden a git con `git add -f`.
- **Reinicio**: `RecreateDatabase.ps1` elimina la base de datos para que se regenere vacía (**borra todos los datos**).
- En modo DEBUG se registra la ruta de la base de datos en el log.

## 4. Datos implicados

- Todas las tablas del `AppDbContext`: FinancialRecords, Operations, CompanyDetails, Funds, FundOperations, FundValueHistories, BankAccounts, BankOperations, SafeHavenAssets, SafeHavenOperations, Properties, PropertyOperations, PriceHistoryPoints, SalesHistory, AppSettings, Countries, ExchangeRates, MarketCompanies y PropertyDocuments.

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

## 9. Features relacionadas

- [SPEC-10-02 · Traspasos entre cuentas bancarias](../spec10-movimientos-bancarios-y-saldo/spec10-02-account-transfers/spec10-02.md): columna nueva `BankOperations.TransferId` (`INTEGER NULL`) con índice `IX_BankOperations_TransferId`, creada por `DatabaseSchemaUpgrader.EnsureBankOperationTransferColumn` (estado: implementada y compilada, pendiente de probar a mano).
- [SPEC-02-06 · Movimientos bancarios al comprar acciones y cobrar dividendos](../spec02-operaciones-de-acciones/spec02-06-trade-bank-movements/spec02-06.md): columna nueva `BankOperations.OperationId` (`INTEGER NULL`) con índice `IX_BankOperations_OperationId`, creada por `DatabaseSchemaUpgrader.EnsureBankOperationTradeColumn` (estado: implementada y compilada, pendiente de probar a mano).
- [SPEC-01-02 · Países y divisa](../spec01-gestion-de-empresas/spec01-02-countries-and-currency/spec01-02.md): nueva tabla `Countries` con precarga única, columnas nulas `CompanyDetails.CountryCode` y `Operations.CurrencyCode`, y las claves `BaseCountryCode` y `CountriesSeeded` en `AppSettings`, todo desde `DatabaseSchemaUpgrader` (estado: plan aprobado, pendiente de implementar).
- [SPEC-01-03 · Tipo de cambio y totales convertidos](../spec01-gestion-de-empresas/spec01-03-exchange-rate/spec01-03.md): nueva tabla `ExchangeRates` (divisa, divisa base, tipo, origen y fecha) y columnas nulas `Operations.ExchangeRate` y `Operations.ExchangeRateTo`, desde `DatabaseSchemaUpgrader` (estado: implementada, pendiente de compilar y probar a mano).
- [SPEC-01-04 · Catálogo de empresas de mercado](../spec01-gestion-de-empresas/spec01-04-market-catalog/spec01-04.md): nueva tabla `MarketCompanies` (ticker, nombre, país del mercado e índices) cargada y actualizada al arrancar por `EnsureMarketCatalogTable` según la clave `MarketCatalogVersion` de `AppSettings`, sin tocar los datos del usuario (estado: implementada, pendiente de compilar y probar a mano).
- [SPEC-14-01 · Documentos adjuntos de inmuebles y lectura con IA](../spec14-gestion-de-inmuebles/spec14-01-property-documents/spec14-01.md): nueva tabla `PropertyDocuments` (archivos adjuntos de inmuebles como binario, con sus datos leídos) y la clave `AiModel` en `AppSettings`, desde `DatabaseSchemaUpgrader`; la clave de API de Claude no va en la base de datos (estado: implementada, pendiente de compilar y probar a mano).
- [SPEC-17-01 · Módulo Histórico y capturas mensuales del patrimonio](../spec17-patrimonio-global-y-distribucion/spec17-01-wealth-snapshots/spec17-01.md): nueva tabla `WealthSnapshots` (una fila por año y mes, índice único) creada por `EnsureWealthSnapshotsTable` desde `DatabaseSchemaUpgrader`, sin tocar los datos existentes (estado: implementada, pendiente de compilar y probar a mano).
- [SPEC-17-02 · Informe fiscal anual para la Renta](../spec17-patrimonio-global-y-distribucion/spec17-02-tax-report/spec17-02.md): columnas nulas `Operations.WithholdingOrigin`, `Operations.WithholdingSpain`, `BankOperations.WithholdingSpain`, `FundOperations.TaxableGain`, `FundOperations.WithholdingSpain` y `SafeHavenOperations.TaxableGain`, añadidas por `EnsureTaxColumns` (estado: plan aprobado, pendiente de implementar después de SPEC-17-01).
- [SPEC-05-02 · Histórico completo y temporalidades del gráfico](../spec05-grafico-de-cotizacion/spec05-02-chart-timeframes/spec05-02.md): sin cambios de esquema; la caché `PriceHistoryPoints` pasa de 5 años al máximo disponible y un indicador por empresa en `AppSettings` marca las cachés ya completadas.
- [SPEC-05-03 · Volumen, colores y marcas del gráfico](../spec05-grafico-de-cotizacion/spec05-03-chart-volume-and-marks/spec05-03.md): nueva tabla `ChartMarks` (empresa o global, fecha, descripción, activa y fechas de creación y modificación) creada con `CREATE TABLE IF NOT EXISTS`; nueva clave `CompanyChart.ShowMarks` en `AppSettings`.
- [SPEC-21-01 · Calendario de dividendos](../spec21-dividend-calendar/spec21-01-dividend-calendar/spec21-01.md): tabla nueva `DividendPayments` y columna `CompanyDetails.NextDividendDate`, creadas con `DatabaseSchemaUpgrader` (estado: implementada y compilada, pendiente de probar a mano).
- [SPEC-05-04 · Comparación de dos gráficos (empresas e índices)](../spec05-grafico-de-cotizacion/spec05-04-chart-comparison/spec05-04.md): tablas nuevas `BenchmarkIndices` (nombre y símbolo de Yahoo, índice único `Symbol`) y `BenchmarkPricePoints` (histórico diario de cada índice, índice único (BenchmarkIndexId, Date), sin clave foránea), creadas con `DatabaseSchemaUpgrader.EnsureBenchmarkTables` y `CREATE TABLE IF NOT EXISTS`; clave nueva `BenchmarkHistory.Complete.{id}` en `AppSettings`. Probado sobre una copia: las demás tablas no cambian (estado: implementada y compilada, pendiente de probar a mano).
- [SPEC-02-08 · Número de acciones con decimales](../spec02-operaciones-de-acciones/spec02-08-fractional-shares/spec02-08.md): `NumberOfShares` pasa a `decimal(18,2)` en el modelo. Las columnas existentes (`INTEGER` en `Operations`, `CompanyDetails`, `SalesHistory` y `DividendPayments`) **no se reconstruyen**: SQLite tiene tipado dinámico y guarda 3,45 como real y los enteros como estaban (comprobado sobre una copia). Las tablas nuevas se crean con `decimal(18,2)`.
