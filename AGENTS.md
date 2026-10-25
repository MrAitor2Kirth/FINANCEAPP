# AGENTS.md — FINANCEAPP

Aplicación personal de seguimiento de patrimonio e inversiones (acciones con precio en vivo y gráfico de cotización, fondos, banco, activos refugio e inmuebles) con un dashboard de patrimonio global. Es de uso individual, sin servidor: todo se guarda en un SQLite local. Su objetivo es que una sola persona vea y gestione todo su patrimonio desde un único sitio, en escritorio y, a futuro, en móvil.

## Stack y estructura

- **.NET 10 MAUI Blazor Hybrid** (UI en Razor/Blazor dentro de un `BlazorWebView`). Destinos: `net10.0-windows10.0.19041.0`, `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst` (los de Apple solo compilan en macOS).
- **EF Core 10 + SQLite** (`Microsoft.EntityFrameworkCore.Sqlite`, `SQLitePCLRaw.lib.e_sqlite3`). **Sin migraciones de EF.**
- **Yahoo Finance** (endpoint no oficial `query1.finance.yahoo.com/v8/finance/chart`) para precios e histórico.
- **ApexCharts.js** y Bootstrap Icons, cargados por CDN desde `wwwroot/index.html`. Bootstrap local en `wwwroot/lib`.
- Solución: `FINANCEAPP.slnx` (formato nuevo de Visual Studio).

Estructura (solo lo que no es obvio):

```
FINANCEAPP/                    raíz del repositorio
├── FINANCEAPP.slnx
├── AGENTS.md
├── MEMORY.md                  memoria del proyecto entre sesiones (estado, decisiones, aprendizajes)
├── FINANCEAPP/                proyecto MAUI
│   ├── MauiProgram.cs         DI, ruta de la BD, EnsureCreated y migraciones ligeras
│   ├── Components/Pages/      páginas por categoría: raíz (acciones, dashboard), Bank/, Funds/, Properties/, SafeHaven/
│   ├── Components/DataTableBase.razor   tabla genérica definida pero poco usada
│   ├── Data/AppDbContext.cs   modelo EF, índices y datos semilla (HasData)
│   ├── Data/DatabaseSchemaUpgrader.cs   ÚNICA vía para evolucionar el esquema
│   ├── Models/                entidades; calculados marcados [NotMapped]
│   ├── Services/              I*Service + implementación (lógica de negocio)
│   ├── wwwroot/js/companyChart.js       interop JS del gráfico de cotización (render, getView, updateData, setComparison, setHiddenKinds, resetView, updateLive, destroy)
│   ├── wwwroot/js/logoTools.js          interop JS que recorta y reduce el logo al subirlo
│   ├── wwwroot/js/wealthChart.js        interop JS del gráfico de evolución del patrimonio (SPEC-17-01)
│   ├── Components/Pages/History/        módulo Histórico: hub, capturas del patrimonio y modal de captura
│   ├── Components/Layout/AppLogo.razor  pinta el logo de la app (placa clara) o el contenido NoLogo
│   └── doc/                   documentación: README.md + specs/specNN-<nombre>/specNN.md (+ features)
├── Backups/                   copias de la BD (BACKUPddmmaaaa.db), ignorada en .gitignore
├── TempDbViewer/              utilidad auxiliar para inspeccionar la BD
├── RecreateDatabase.ps1       BORRA la BD para regenerarla
└── *.md (raíz y FINANCEAPP/)  notas históricas de desarrollo, NO son referencia vigente
```

Los seis principios innegociables están en `FINANCEAPP/doc/constitution.md`: léelos antes de empezar. La referencia vigente es `FINANCEAPP/doc/README.md` y las specs de `FINANCEAPP/doc/specs/` (una carpeta por spec: `specNN-<nombre>/specNN.md`). Los `.md` históricos (`DISEÑO_ARQUITECTONICO_V2.md`, `RESUMEN_*.md`, etc.) pueden estar desactualizados.

## Comandos

Requisitos: SDK de .NET 10 y la carga de trabajo MAUI (`dotnet workload install maui`). Ejecutar desde la raíz del repositorio. Pensado para Windows; en Linux solo compila Android.

```bash
# Compilar (Windows)
dotnet build FINANCEAPP/FINANCEAPP.csproj -f net10.0-windows10.0.19041.0

# Ejecutar (Windows)
dotnet run --project FINANCEAPP/FINANCEAPP.csproj -f net10.0-windows10.0.19041.0

# Compilar para Android
dotnet build FINANCEAPP/FINANCEAPP.csproj -f net10.0-android

# Comprobar formato (no hay linter configurado)
dotnet format FINANCEAPP/FINANCEAPP.csproj --verify-no-changes

# Restaurar paquetes
dotnet restore FINANCEAPP.slnx
```

- **No hay proyecto de pruebas automáticas** en el repositorio.
- Alternativa habitual: abrir `FINANCEAPP.slnx` en Visual Studio, elegir *Windows Machine* y ejecutar.
- Copia de seguridad de la BD (PowerShell): copiar `C:\Users\Mr X\AppData\Local\User Name\com.companyname.financeapp\Data\financeapp.db` a `Backups\BACKUPddmmaaaa.db` y añadirla con `git add -f` (la carpeta está en `.gitignore`).
- Reiniciar la BD desde cero (**borra todos los datos**): `./RecreateDatabase.ps1`.

## Convenciones

- **Idioma**: interfaz, textos, mensajes de error, comentarios XML y documentación en **español**. Identificadores de código en inglés, salvo las rutas ya existentes en español (`/fondos`, `/banco`, `/inmuebles`, `/activos-refugio`) o inglés (`/companies`, `/operations`): no renombrarlas.
- **Nombres en inglés**: las carpetas, ficheros, specs, features, entidades, servicios, páginas, tablas y rutas **nuevas** se nombran en inglés (p. ej. `spec02-01-sales-history`, `/sales-history`, `SalesHistoryList.razor`). Los textos visibles y la documentación siguen en español. Lo ya existente en español no se renombra.
- **Specs y features**: cada spec vive en `FINANCEAPP/doc/specs/specNN-<nombre>/specNN.md`. Una funcionalidad nueva o un cambio importante se documenta como feature en `FINANCEAPP/doc/specs/specNN-<nombre>/specNN-MM-<english-name>/` con tres ficheros: `specNN-MM.md` (requisitos y decisiones), `planNN-MM.md` (plan aprobado) y `taskNN-MM.md` (tareas con casillas). Al implementarla se vuelca el resultado en la spec base.
- **Patrón de capas**: página `.razor` → `I*Service` → `AppDbContext`. Los servicios se registran `Scoped` en `MauiProgram.cs`. Toda lógica de negocio va en el servicio, no en la página.
- **Patrón de UX por categoría**: listado de la entidad principal → formulario de alta/edición → operaciones desplegables bajo cada fila.
- **Importes**: `decimal` con `[Column(TypeName = "decimal(18,2)")]`. Se muestran con `CurrencyFormatter.Format(importe, divisa)` (símbolo de la divisa de la empresa u operación), no con `ToString("C")`, que depende de la cultura de Windows.
- **Validación**: anotaciones de datos en el modelo (`[Required]`, `[Range]`, `[StringLength]`) con mensajes en español.
- **Fines de línea y codificación**: los ficheros del proyecto usan **CRLF**; algunos llevan BOM. Respétalo al editar y no reformatees ficheros enteros.
- **Archivos de referencia**:
  - Servicio con recálculo tras cada operación: `Services/CompanyService.cs` + `OperationService.cs`.
  - Servicio con transacciones: `Services/FundService.cs` y `PropertyService.cs`.
  - Formulario: `Components/Pages/Bank/BankAccountForm.razor` o `Properties/PropertySell.razor`.
  - Listado con detalle desplegable: `Components/Pages/Funds/FundsList.razor`.
  - Interop JS con gráfico: `Components/Pages/CompanyChartModal.razor` + `wwwroot/js/companyChart.js`.

## Reglas de dominio / trampas conocidas

- **Las operaciones se enlazan por nombre, no por clave foránea** (`CompanyName`, `FundName`, `AccountName`, `AssetName`, `PropertyName`). Cambiar el nombre de una entidad desvincula sus operaciones. La única FK real es `PriceHistoryPoint.CompanyId`.
- **Tipos de operación, estados y categorías son cadenas literales** (p. ej. `"Compra"`, `"Aportación"`, `"Traspaso Entrada"`, `"Vendida"`, `"Alquilado"`). Se comparan con `==`: respeta tildes y mayúsculas exactas.
- **Los totales se guardan y se recalculan, no se calculan al leer**. Tras crear, editar o borrar una operación hay que llamar al recálculo del servicio correspondiente (empresa: `RecalculateTotalInvestedAsync`; banco: `RecalculateAccountBalanceAsync`; inmuebles: `RecalculatePropertyTotalsAsync`; refugio: `RecalculateAssetTotalAsync`). Los fondos no recalculan `TotalInvested` a partir de aportaciones y reembolsos: se introduce a mano.
- **Número de acciones con decimales** (SPEC-02-08): `NumberOfShares` es `decimal` con hasta **2 decimales** en `Operation`, `CompanyDetail`, `SaleHistory` y `DividendPayment` (y en `QuickTradeRequest`, `ChartMarker`, dividendos). Redondear y validar con `ShareQuantity` (`Round`, `HasValidPrecision`) y mostrar con `ShareQuantity.Format` (12 · 12,5 · 0,25), nunca con `ToString("N0")`. Las columnas existentes son `INTEGER` en SQLite y **no se reconstruyen**: el tipado dinámico guarda los decimales como real. Sumar acciones en memoria, no en SQL.
- **Acciones**: solo cuentan las operaciones **abiertas** (sin `CloseDate`) para inversión y número de acciones; los dividendos cuentan siempre. Las Ventas antiguas abiertas restan su importe completo. El valor de cartera usa `LivePrice ?? CurrentPrice`.
- **Ventas de acciones nuevas** (`OperationService.RegisterSaleAsync`, SPEC-02-01): cierran por FIFO las compras y traspasos abiertos con la fecha de la venta, se cierran ellas mismas y guardan un registro en `SalesHistory`. Un lote parcial se divide en dos filas (cerrada y abierta duplicada) sin perder céntimos. El beneficio del histórico se guarda y solo se recalcula al editar el precio de venta. Cada compra cerrada guarda `SaleHistoryId` y las filas de un lote partido comparten `OriginOperationId`. **Deshacer venta** (`OperationService.UndoSaleAsync`, desde `/sales-history`) reabre las compras, une las filas partidas, borra la Venta y el registro y recalcula; las operaciones cerradas por una venta no se editan ni borran desde `/operations` (`GetSaleLockMessageAsync`). Las ventas anteriores no se tocan. Las Ventas deben crearse siempre con `RegisterSaleAsync`, no con `AddAsync`.
- **Importe de Compra y Venta** (SPEC-02-02): lo calcula `OperationAmountCalculator` (precio base × acciones + comisión; manda el precio por acción y, si no hay, el precio en vivo), es editable pero se recalcula al cambiar precio, acciones o comisión. Dividendo y Traspaso llevan el importe a mano. La fórmula no va en las páginas `.razor`. El precio de venta del histórico sigue la misma prioridad.
- **Fondos**: el valor se actualiza **solo** con el historial de valores (`UpdateFundValueWithHistoryAsync`). No reintroducir operaciones de tipo Plusvalía; los métodos de la antigua opción A siguen en el servicio sin uso.
- **Dashboard**: `SafeHavenInitialTotal` = total invertido y `SafeHavenTotal` = valor actual (mismo criterio que acciones y fondos). Los inmuebles entran por `CurrentValue ?? TotalInvested` y solo los no vendidos. El rendimiento de acciones usa `TotalInvestedValue`.
- **Eliminar una empresa es en cascada** (operaciones + histórico de cotización) y definitivo. Eliminar fondo, cuenta, activo refugio o inmueble también borra sus operaciones.
- **SQLite no admite transacciones anidadas**: los métodos de recálculo se llaman dentro de transacciones ya abiertas y no deben abrir otra.
- **Esquema de BD**: `EnsureCreated()` solo crea la BD si no existe; **no aplica cambios a una BD existente**. Cualquier columna o tabla nueva (p. ej. `SalesHistory`) debe añadirse en `DatabaseSchemaUpgrader` (con `IF NOT EXISTS` / comprobación `PRAGMA table_info`) y llamarse desde `MauiProgram`. Cambiar solo el modelo rompe las BD ya creadas.
- **Logo de la app** (SPEC-20-01): se sube en `/settings` (`Settings.razor`), `logoTools.js` lo recorta y lo reduce a PNG de 640 px y `AppSettingsService` lo guarda en `AppSettings` (clave `AppLogo`) con caché en memoria y evento `LogoChanged`. Se pinta solo con el componente `AppLogo` (variantes `sidebar`, `hero`, `compact`) en el menú, Inicio y Dashboard; sin logo se ve la marca de siempre. Los estilos `.app-logo*` están en `app.css`. **Icono de la app** (25/10/2026): `Resources/AppIcon/appiconfg.png` (solo el símbolo del logo, sin texto, 1024 px, fondo transparente) sobre `appicon.svg` transparente, con un `MauiIcon` por plataforma en el `.csproj` (Windows sin fondo; Android e iOS/Mac con fondo blanco y el símbolo en la zona segura). La pantalla de carga (Android) usa el mismo PNG en `Resources/Splash/splash.png` sobre `#F5F2ED` (`--paper`). El acceso directo del escritorio usa `Resources/AppIcon/financeapp.ico` (16–256 px; el `.ico` que genera MAUI solo trae 64 px), que copia la skill `publish-app`. Si cambia el logo, regenerar los dos PNG y el `.ico`; no salen de `AppSettings`.
- **Datos semilla**: `HasData` carga empresas de ejemplo (MSFT, AAPL, KO, JNJ, PG) y operaciones solo al crear una BD nueva.
- **Yahoo Finance**: exige cabecera `User-Agent`; timeout de 10 s; los tickers europeos sin sufijo se resuelven probando `.MC`, `.PA`, `.MI`, `.DE`, `.AS`, `.BR`, `.L`, `.LS`, `.SW`. La actualización masiva es secuencial y manual. La caché de histórico dura 12 h y se actualiza solo con los días nuevos; el histórico se pide por tramos con `GetHistoryRangeAsync` (`period1`/`period2`), no con `range`.
- **CDN**: ApexCharts e iconos se cargan por red; sin conexión, el gráfico y los iconos no aparecen.
- **Gráfico de cotización** (SPEC-05-01): `CompanyChartModal` usa el **mismo `DbContext`** que el resto de la app y serializa sus accesos con un `SemaphoreSlim`; al cerrarse espera a que termine la consulta en curso. **No crear un `DbContext` aparte en un componente**: el contexto principal se queda con copias antiguas de las entidades y luego EF no guarda cambios sobre ellas (pasó con una venta desde el gráfico que, al deshacerla, no reabrió las compras). Las Compras y Ventas desde el gráfico pasan por `OperationService.RegisterQuickTradeAsync` (no por `AddAsync` directo): revalida el bloqueo por mercado (solo con fecha de hoy y mercado Cerrado) y llama a `AddAsync` o `RegisterSaleAsync`. Las capas opcionales se guardan en `AppSettings` (`CompanyChart.*`). **Histórico y temporalidades** (SPEC-05-02): el diario se completa en segundo plano por bloques de 10 años hasta la primera cotización; la descarga va fuera del semáforo y **el guardado siempre dentro** (`SaveDailyBlockAsync`). La clave `PriceHistory.Complete.{CompanyId}` de `AppSettings` marca el histórico completo y se borra al eliminar la empresa. Las barras de Horas viven **solo en memoria** (15 min) y no se guardan en la BD. Los tramos iniciales de cada temporalidad los decide `PriceHistoryService.GetInitialView`, no el JS. **Eje por sesiones y marcas** (SPEC-05-03): el JS dibuja con X = índice del periodo (sin huecos de fines de semana ni noches) y traduce fechas ⇄ índices; el modal y los servicios siguen en fechas. Operaciones y marcas se colocan en su periodo con `AlignMarkers`/`AlignMarks`. El volumen es dinero aproximado (`PriceHistoryPoint.TradedValue`, `[NotMapped]`, acciones × cierre). Las marcas viven en `ChartMarks` (`CompanyId` nulo = Global, sin FK; `IChartMarkService`, sin fecha futura); `CompanyService.DeleteAsync` borra las de la empresa y conserva las globales. Cambiar capas u operar conserva el tramo visible (`getView` + `keepView`). En ApexCharts, `updateOptions` con el 4.º parámetro a `false` para no copiar datos al volumen.
- **Ruta de la BD** (`FileSystem.AppDataDirectory/financeapp.db`): en Windows sin empaquetar es `C:\Users\Mr X\AppData\Local\User Name\com.companyname.financeapp\Data\financeapp.db`. No usar la copia del `bin/`.
- **Datos reales**: la BD del usuario contiene sus finanzas reales. Cualquier prueba debe hacerse sobre una copia.
- **Tipos de cambio** (`ExchangeRates`): la clave incluye la divisa base, así que cambiar el país base deja las empresas sin tipo hasta actualizar. Los totales de Empresas y Dashboard usan siempre el tipo actual. **Excepción (SPEC-02-05):** el histórico de ventas (`/sales-history`) convierte con el tipo guardado en la Venta y en cada compra cerrada, y solo usa el actual si falta alguno (la venta se avisa como aproximada). «Detalles» no cambia. Los pares de Yahoo son `XXXYYY=X` y pueden fallar: siempre hay que poder escribirlo a mano. **Histórico de tipos** (SPEC-01-09): `CurrencyPairs` + `CurrencyRatePoints` (par `{DIVISA}{BASE}=X`, serie `ChartSeriesKind.Currency` de `PriceHistoryService`; `ICurrencyHistoryService.GetRatesAtAsync`); solo lo usan «Cartera al cierre» y «Proponer tipos» de `/settings`, que rellena `Operation.ExchangeRate` vacíos sin tocar importes ni movimientos.
- **Catálogo de mercado** (`MarketCompanies`): solo lectura para el usuario; se carga desde `Data/MarketCatalogData.cs` en `EnsureMarketCatalogTable` al cambiar `Version`. Para cambiarlo, edita ese fichero y sube la versión. Yahoo no es accesible desde la nube: los tickers se comprueban en la app.
- **Saldo bancario** (SPEC-10-01): `CurrentBalance` = `OpeningBalance` (saldo inicial de la cuenta) + efecto de los movimientos por tipo, con valor absoluto. El usuario escribe los importes en positivo y `BankService` fija el signo (negativo en Gasto, Comisiones y Traspaso Salida) y valida con mensajes en español. `RecalculateAccountBalanceAsync` **no abre transacción**: se llama dentro de la del alta, edición o baja. Al editar el saldo de una cuenta, `OpeningBalance` se reajusta.
- **Ingreso de ventas en el banco** (SPEC-02-04): `RegisterSaleAsync` crea en su transacción un «Ingreso» con el neto (`OperationAmountCalculator.CalculateSaleNet`: precio × acciones − comisión de la Venta, × tipo de cambio) en la cuenta elegida o la más antigua (`BankService.ResolveSaleAccountAsync`), enlazado por `BankOperations.SaleHistoryId`. Los métodos de banco para ventas (`AddSaleDepositAsync`, `UpdateSaleDepositAsync`, `RemoveSaleDepositsAsync`) **no abren transacción**. `UndoSaleAsync` borra el ingreso y `UpdateSellPriceAsync` lo actualiza; `/banco` no lo deja editar ni borrar. Sin cuentas, la venta se bloquea.
- **Movimientos bancarios de compras y dividendos** (SPEC-02-06): `OperationService.AddAsync(operation, bankAccountName)` crea en su transacción un «Gasto» (Compra) o un «Ingreso» (Dividendo) en la cuenta elegida o la más antigua (`BankService.ResolveTradeAccountAsync`); `OperationAmountCalculator.NoBankMovementValue` («No registrar en banco») o la ausencia de cuentas lo evitan. Se enlaza por `BankOperations.OperationId` = clave del lote (`OriginOperationId ?? Id`) y su importe es la suma convertida de las filas del lote (no cambia al partir o unir con ventas). `UpdateAsync` y `DeleteAsync` lo mantienen (no se puede cambiar el tipo de una operación con movimiento), borrar una empresa borra sus movimientos y `/banco` no los edita mientras la operación exista. Los métodos `*TradeMovement*` de `BankService` **no abren transacción**. Las operaciones anteriores no generan movimiento.
- **Cuenta editable de compras y dividendos** (SPEC-02-07): `OperationService.UpdateAsync(operation, bankAccountName)` pasa el movimiento a otra cuenta (`BankService.MoveTradeMovementAsync`, mismo registro y recálculo de las dos cuentas) o lo crea en una operación **abierta** que no lo tenía (suma del lote, tipo de cambio obligatorio en otra divisa). Vacío = no cambia; `NoBankMovementValue` se rechaza si ya hay movimiento (para quitarlo, borrar la operación). En `/operations` la columna «Cuenta bancaria» solo se ve con una fila en alta o edición (`showBankColumn`).
- **Traspasos entre cuentas** (SPEC-10-02): `BankService.CreateTransferAsync` (abre su transacción; se llama desde la página) crea un «Traspaso Salida» y un «Traspaso Entrada» con el mismo `BankOperations.TransferId` (máximo + 1) y recalcula las dos cuentas; sin conversión de divisa ni comprobación de saldo. Las dos patas van juntas: borrar una borra la otra, importe y fecha se editan en las dos (la descripción es de cada pata) y el tipo no cambia. Una pata sin pareja o un traspaso manual anterior se trata como un movimiento normal. Los totales de ingresos y gastos (SPEC-11) siguen contando los traspasos.
- **Resumen de empresas con inversión** (SPEC-01-05): en `/companies` una empresa tiene inversión si `TotalInvestedInitial` > 0. Las tarjetas «Total empresas», «Sectores únicos» y «Dividend yield promedio», el listado corto y la nueva «Rentabilidad media» (por precio, ponderada por lo invertido, con una segunda llamada a `GetConvertedTotalsAsync` solo con esas empresas) usan solo esas empresas; la lista extendida muestra todo el catálogo. Los cálculos están en `InvestedCompaniesSummary`; «Total invertido» y el Dashboard no cambian. **Rentabilidad media con dividendos** (SPEC-01-07): segunda tarjeta con (valor + dividendos − invertido) ÷ invertido de las mismas empresas y totales (`WeightedReturnWithDividendsPercent`); la de precio no cambia. **Peso por empresa** (SPEC-01-06): la lista extendida muestra «Peso» = inversión convertida ÷ suma de la inversión de las empresas con inversión (`InvestedCompaniesSummary.GetWeights`, a partir de `ConvertedCompanyTotals.InitialByCompanyId`). **Tarjetas** (SPEC-01-08): bloques «Cartera» (Invertido (coste), Valor actual, Resultado = valor − invertido con los dividendos aparte) y «Rentabilidad y diversificación»; todas con `investedTotals` (el antiguo «Total invertido» de todas las empresas ya no existe: daba lo mismo porque las empresas sin inversión suman 0). **Listado por periodo** (SPEC-01-10): con un periodo, el listado corto usa `PeriodSummary.Companies` (copias solo de lectura con cifras del periodo; `Convert` calcula en ellas las rentabilidades); los botones de fila usan `RealCompany` (la empresa del contexto). **Filtro por periodo** (SPEC-01-09): las tarjetas usan `CardTotals`/`CardCompanies` (el periodo de `IPortfolioPeriodService` o, en Total histórico, las de hoy). «Compras del periodo» = Compras y Traspasos abiertos con fecha en el periodo; «Cartera al cierre» = operaciones con fecha ≤ corte y `CloseDate` nulo o posterior, precio de cierre (`EnsureDailyCoverageAsync` + `GetDailyCloseAtAsync`) y tipo de esa fecha. **Incluir vendidas** (SPEC-01-11): con el selector (clave `Companies.IncludeSold`), `IPortfolioPeriodService.GetSoldAsync` reúne lo vendido de la vista (`SalesHistory` + compras cerradas por `SaleHistoryId`; coste = importe de las compras con comisión, cobrado neto = importe − comisión de la Venta, convertidos con los tipos guardados por `ExchangeRateService.GetConvertedSalesAsync`, que también usa `GetConvertedSaleTotalsAsync`) y `SoldPositionsSummary` lo suma a los totales (`Combine`) y a las filas (`BuildRows`, copias «Parcial»/«Vendida»). Ventas de empresas borradas fuera con aviso. El peso sigue siendo el de la cartera abierta. **Desglose por empresa** (SPEC-01-12): `IPortfolioPeriodService.GetBreakdownAsync` selecciona las operaciones con las mismas reglas que la fila de cada vista y suma sin redondear; si cambias cómo se calcula una fila (periodo, vendidas), cambia también el desglose o dejarán de coincidir. La lista extendible usa siempre las cifras de hoy (también lo vendido). `/operations?company=` filtra por empresa.
- **Capturas del patrimonio** (`WealthSnapshots`, SPEC-17-01): una por año y mes (índice único); solo se puede capturar el mes en curso o el anterior. Los importes salen de `IDashboardService.GetDashboardSummaryAsync` y son una **foto**: no se recalculan nunca (sustituir o borrar son las únicas formas de cambiarlos). El banco no tiene invertido y no entra en `TotalInvested`. Las variaciones se calculan al leer y no comparan capturas de distinta divisa base. Si cambia la fórmula del patrimonio del Dashboard, las capturas antiguas no cambian. **Capturas diarias** (SPEC-17-03): `DailyPortfolioSnapshots` (una fila por día con los cinco registros y los flujos) y `DailyCompanySnapshots` son **datos derivados**: `DailySnapshotService.RefreshAsync` (al arrancar, tras «Actualizar precios en vivo» y con «Exportar para IA») rehace en memoria los días desde la clave `DailySnapshots.DirtyFrom` y el de hoy (con los servicios del Dashboard y de `/companies`), y `AiPortfolioExportService` escribe `cartera-ia.json` (carpeta en `/settings`, por defecto `Documentos\FINANCEAPP\IA`). La marca la pone `AppDbContext.SaveChanges` (`MarkDailySnapshotsDirty`): si añades una entidad que mueva el patrimonio, inclúyela ahí. Los días pasados no descargan nada; «Completar históricos» sí.
- **Calendario de dividendos** (SPEC-21-01, `/dividends`): tabla `DividendPayments` enlazada a la empresa por `CompanyId` y a la operación por `OperationId` (sin FK; índice único empresa + fecha de pago). Estados `"Pendiente"`/`"Cobrado"` y orígenes `"Manual"`/`"Ficha"`/`"Búsqueda"` son literales (`DividendPaymentRules`). `AmountPerShare` es `decimal(18,4)` (excepción documentada); el importe total se calcula al leer. La operación Dividendo de una línea se genera **solo** con `DividendService.GenerateOperationAsync`, que llama a `OperationService.AddAsync(..., dividendPaymentId)` para marcar la línea Cobrada en la misma transacción; `OperationService.DeleteAsync` la reabre. `CompanyService` y `OperationService` tocan `DividendPayments` con el contexto (sin inyectar `IDividendService`, para evitar dependencias circulares). La «Próxima fecha de dividendo» de la ficha (`CompanyDetails.NextDividendDate`) crea una línea si es nueva y nunca toca las anteriores. Las propuestas están en `Data/DividendProposalsData.cs`: solo dividendos **anunciados**, nunca estimaciones; la app nunca genera operaciones sola. En el gráfico, los pagos son rombos azules, el eje se prolonga con sesiones futuras vacías (serie auxiliar «Referencia» oculta por CSS para la escala) y hay un navegador («brush»).
- **Comparación en el gráfico** (SPEC-05-04): índices en `BenchmarkIndices` (símbolo único en mayúsculas, `/indices`) con su histórico en `BenchmarkPricePoints` (sin FK; `BenchmarkIndexService.DeleteAsync` borra puntos y la clave `BenchmarkHistory.Complete.{id}`; cambiar el símbolo también borra el histórico). `PriceHistoryService` trabaja por serie (`ChartSeriesRef`: empresa o índice); los métodos de empresa delegan en los `*Series*`, así que un cambio en las reglas del histórico vale para los dos. Los cálculos (alineación con el último cierre ≤ fecha, base 100 en el primer periodo visible, rentabilidades y Pearson de las rentabilidades de periodo, mínimo 10) están en `ChartComparison`, no en el JS: el gráfico solo avisa del tramo (`OnComparisonRangeChanged`, 250 ms, solo si cambió) y redibuja con `setComparison` **conservando `minX`/`maxX` exactos** (rehacerlo desde fechas añade margen y crea un bucle). Los símbolos con `^` o `=` no prueban sufijos de bolsa (`StockPriceService.CanTrySuffixes`). La comparación no se guarda.
- `FinancialRecord` / `IFinancialService` son **legado** de la v1; no ampliar.

## Forma de trabajar

- **Planifica antes de tocar código** cuando el cambio afecte a más de un servicio, al modelo de datos, al esquema de BD o a cálculos de dinero. Resume el plan en pocas líneas y espera conformidad si hay alternativas razonables.
- **Funcionalidad nueva**: antes de tocar código, crea la carpeta de la feature con su spec, plan y tareas, y espera la aprobación del usuario. Marca las casillas de `taskNN-MM.md` según avances.
- **Cambios pequeños y enfocados**: una funcionalidad o corrección por cambio; no mezcles refactors con arreglos.
- **Lee antes de editar**: abre el servicio, la página y la spec relacionada (`FINANCEAPP/doc/specs/specNN-<nombre>/specNN.md`) antes de modificar.
- **Actualiza la documentación** cuando cambies un comportamiento: la spec afectada (reglas de negocio, criterios de aceptación, observaciones) y, si cambia la estructura, `FINANCEAPP/doc/README.md`.
- **Mapa de ayuda** (idea ID-12 de `FINANCEAPP/Doc/IDEAS.md`, aún sin implementar): diseña cada pantalla nueva de modo que se pueda describir como una tarea del usuario («registrar una venta», «ver el informe fiscal»), con su ruta. Cada `taskNN-MM.md` nueva lleva un paso «Mapa de ayuda»: si el mapa ya existe, añadir o corregir el nodo; si no, anotar la tarea y la ruta en la idea ID-12.
- **Al terminar**, explica en español y de forma breve: qué cambiaste y por qué, qué ficheros tocaste, qué no pudiste verificar y qué debe probar el usuario.
- **Texto de commit propuesto**: al final de cada tarea que cambie ficheros, y también tras `/publish-app` o cuando `git status` muestre cambios hechos por otra sesión o por el usuario que tu último mensaje no cubría, propón en un bloque de código un texto de commit que cubra **todos** los cambios sin commitear (no solo los de la tarea), y rehazlo a medida que se acumulan cambios. Formato: un título corto en español y, debajo, una viñeta por área con la spec entre paréntesis (p. ej. `(SPEC-05-02)`). Las copias de `Backups/` van siempre en un commit aparte. **Los commits los hace siempre el usuario**: tú solo redactas el mensaje y nunca ejecutas `git commit`, `git push` ni `git add`/`git restore --staged` para prepararlo, aunque te diga «sí» al texto propuesto.
- Si no puedes compilar (entorno sin SDK/MAUI), dilo claramente: no afirmes que el cambio funciona.

## Memoria

- Al empezar, lee `MEMORY.md` para conocer el estado del proyecto y las decisiones tomadas.
- Al terminar una tarea, actualízalo: estado actual, decisiones importantes (con su porqué) y errores a evitar.
- Mantenlo breve (máximo ~100 líneas): resume o elimina lo que ya no aporte.
- Si algo se convierte en una regla permanente, propón moverlo a `AGENTS.md` en lugar de dejarlo en la memoria.
- No guardes nunca datos sensibles (claves, tokens, datos personales).

## Límites

- ✅ **Siempre**:
  - Mantener el patrón página → servicio → contexto y los mensajes en español.
  - Recalcular totales tras modificar operaciones.
  - Respetar CRLF/BOM y el estilo del fichero que editas.
  - Usar transacciones en operaciones que tocan varias tablas.
  - Enviar un documento a la IA solo tras la confirmación del usuario en esa acción, y guardar la clave de API únicamente en `SecureStorage`.
  - Indicar qué se ha verificado y qué no.
  - Actualizar `MEMORY.md` al terminar cada tarea.
- ⚠️ **Pregunta antes de**:
  - Añadir paquetes NuGet o librerías JS nuevas.
  - Crear archivos o carpetas nuevos fuera de `doc/`, `Components/Pages/` y `Services/`.
  - Cambiar el esquema de BD, el formato de los datos o la ruta del fichero de BD.
  - Cambiar fórmulas de cálculo (rentabilidad, patrimonio, saldos).
  - Renombrar rutas, entidades o tipos de operación (afecta a los datos guardados).
  - Eliminar código de servicios que parezca sin uso.
  - Pasar de vinculación por nombre a claves foráneas.
- 🚫 **Nunca**:
  - Borrar, sobrescribir o ejecutar scripts destructivos sobre `financeapp.db` ni sobre `Backups/` (incluido `RecreateDatabase.ps1`).
  - Subir a git la BD real, secretos o datos personales; guardar claves, tokens o datos personales en `MEMORY.md`; ni editar `bin/`, `obj/`, `.vs/` o `.git/`.
  - Usar `EnsureDeleted()` o migraciones destructivas.
  - Guardar la clave de API de Claude en la base de datos, en copias de seguridad o en registros, ni mostrarla completa en pantalla.
  - Hacer commit o push, ni preparar el área de staging: los commits los hace el usuario; tú solo propones el mensaje.
  - Tocar `Platforms/` o la configuración de firma/publicación sin indicación expresa.
  - Presentar como probado un cambio que no se ha compilado ni ejecutado.

## Verificación

No hay pruebas automáticas, así que la verificación es compilar y comprobar a mano:

1. **Compila** sin errores ni advertencias nuevas: `dotnet build FINANCEAPP/FINANCEAPP.csproj -f net10.0-windows10.0.19041.0`.
2. **Ejecuta la app sobre una copia de la BD** (haz antes un backup en `Backups/`) y recorre el flujo afectado:
   - **Acciones**: alta de empresa y de operaciones (Compra, Venta, Dividendo, Traspaso); comprobar inversión, nº de acciones, dividendos y rentabilidades; «Actualizar Precios en Vivo»; abrir el gráfico en diario, mensual y anual; borrar una empresa y ver el aviso de operaciones.
   - **Fondos**: registrar y eliminar un valor del historial; aportación y reembolso.
   - **Banco**: ingreso, gasto y traspaso; el saldo debe cambiar y volver al borrar el movimiento.
   - **Refugio**: compra y venta; total invertido.
   - **Inmuebles**: ingreso y gasto; venta desde `/inmuebles/vender/{id}` (el inmueble pasa a Vendida y sale del patrimonio).
   - **Dashboard**: el patrimonio total coincide con la suma de las tarjetas; alertas, rendimiento, flujo de caja y operaciones recientes se muestran.
3. **Si tocaste el esquema**, arranca con una BD ya existente (no una nueva) y comprueba que no se pierden datos y que la columna o tabla nueva aparece.
4. **Contrasta con la spec** correspondiente en `FINANCEAPP/doc/specs/specNN-<nombre>/`: cada una tiene criterios de aceptación en casillas marcables; recórrelos.
5. **Revisa el diff** (`git diff`): solo los ficheros previstos, sin cambios de fines de línea masivos.
6. Resume al usuario lo verificado y lo pendiente de probar en su máquina.
