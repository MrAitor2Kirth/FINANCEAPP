# FINANCEAPP

Aplicación personal de seguimiento de patrimonio e inversiones. Centraliza en un único lugar las acciones (con precio en vivo y gráfico de cotización), los fondos de inversión, las cuentas bancarias, los activos refugio y los inmuebles, y ofrece un dashboard con el patrimonio global.

> Esta documentación vive en la carpeta `FINANCEAPP/doc/` del proyecto, al mismo nivel que `Components/`. Las especificaciones detalladas de las 20 funcionalidades principales están en [`doc/specs/`](specs/README.md).

---

## 1. Resumen general

| Aspecto | Detalle |
|---|---|
| Tipo de aplicación | .NET MAUI Blazor Hybrid (UI en Razor/Blazor dentro de un `BlazorWebView` nativo) |
| Plataformas destino | Android, iOS, Mac Catalyst y Windows (`net10.0-*`) |
| Persistencia | SQLite local mediante Entity Framework Core 10 |
| Fuente de precios | Yahoo Finance (endpoint público `v8/finance/chart`) |
| Gráficos | ApexCharts.js (cargado por CDN) |
| UI | Bootstrap + Bootstrap Icons + Open Iconic |
| Solución | `FINANCEAPP.slnx` |

La aplicación es de uso individual y funciona sin servidor: toda la información se guarda en un fichero SQLite en el dispositivo. Solo necesita Internet para consultar cotizaciones y cargar ApexCharts/iconos desde CDN.

---

## 2. Estructura funcional

### 2.1 Categorías de inversión

La app está organizada en cinco categorías de activos, cada una con el mismo patrón de uso: un **listado de la entidad principal**, un **formulario de alta/edición** y un **listado de operaciones** asociadas.

| Categoría | Entidad principal | Operaciones asociadas | Ruta |
|---|---|---|---|
| Acciones / Empresas | `CompanyDetail` | `Operation` | `/companies`, `/operations` |
| Fondos de inversión | `Fund` | `FundOperation`, `FundValueHistory` | `/fondos` |
| Banco | `BankAccount` | `BankOperation` | `/banco` |
| Activos refugio | `SafeHavenAsset` | `SafeHavenOperation` | `/activos-refugio` |
| Inmobiliario | `Property` | `PropertyOperation` | `/inmuebles` |

Además existe el **Dashboard** (`/dashboard`) y la página de inicio (`/`).

### 2.2 Acciones (módulo más completo)

- **Listado de empresas** (`/companies`): nombre, ticker, sector, precio en vivo, dividendos, rentabilidad por plusvalía y por dividendos, número de acciones e inversión total. Cada fila tiene botones de **editar** (naranja), **eliminar** (rojo) y **ver gráfico** (gris).
- **Alta y edición de empresa** (`/companies/new`, `/companies/edit/{id}`).
- **Listado de operaciones** (`/operations`): compras/ventas por empresa con fecha, fecha de cierre, precio por acción, número de acciones, comisión e importe.
- **Precio en vivo**: se consulta a Yahoo Finance por ticker; el último valor y su fecha de actualización se guardan en `CompanyDetail` (`LivePrice`, `LivePriceUpdatedAt`). Si falla la consulta se informa el error en pantalla sin romper el listado.
- **Gráfico de cotización** (`CompanyChartModal`): modal con la serie de precios de la empresa en granularidad **diaria, mensual o anual**, con las operaciones de compra superpuestas como marcadores (con tooltip) y la línea del precio actual.

### 2.3 Fondos, banco, activos refugio e inmuebles

- **Fondos**: tipo, gestora, TER, ISIN, rentabilidad histórica, total invertido y valor actual; registro de aportaciones/reembolsos y de la evolución del valor en el tiempo.
- **Banco**: cuentas (banco, IBAN, tipo, saldo actual) y movimientos.
- **Activos refugio**: activos (p. ej. oro u otros), total invertido y valor actual, con sus operaciones.
- **Inmuebles**: tipo, dirección, precio y fecha de compra, valor actual, estado, alquiler mensual, fecha de venta, total invertido e ingresos acumulados; operaciones categorizadas (gastos, ingresos, etc.).

### 2.4 Dashboard

`DashboardService` agrega los datos de todas las categorías y expone: resumen global, patrimonio total, patrimonio por clase de activo, rendimiento de cartera, estadísticas de ingresos/gastos, operaciones recientes y alertas.

---

## 3. Estructura técnica

### 3.1 Arquitectura en capas

```
┌─────────────────────────────────────────────┐
│  UI (Blazor)        Components/             │  Páginas .razor, layout, componentes
├─────────────────────────────────────────────┤
│  Servicios          Services/               │  Lógica de negocio (interfaz + implementación)
├─────────────────────────────────────────────┤
│  Datos              Data/ + Models/         │  EF Core, DbContext, entidades
├─────────────────────────────────────────────┤
│  SQLite  (financeapp.db)   |   Yahoo Finance│
└─────────────────────────────────────────────┘
```

El arranque y el registro de dependencias están en `MauiProgram.cs`:

- `AddMauiBlazorWebView()` habilita el `BlazorWebView`.
- `AddDbContext<AppDbContext>` con SQLite; el fichero se crea en `FileSystem.AppDataDirectory` con el nombre `financeapp.db`.
- Los servicios de dominio se registran como `Scoped` mediante su interfaz.
- `StockPriceService` se registra con `AddHttpClient` (timeout de 10 s y cabecera `User-Agent`, necesaria para que Yahoo Finance no rechace las peticiones).
- Al iniciar: `EnsureCreated()` y las migraciones ligeras de `DatabaseSchemaUpgrader`.

### 3.2 Estructura de carpetas

```
FINANCEAPP/                          ← raíz del repositorio
├── FINANCEAPP.slnx                  Solución
├── FINANCEAPP/                      Proyecto MAUI Blazor Hybrid
│   ├── MauiProgram.cs               Configuración, DI e inicialización de BD
│   ├── App.xaml / MainPage.xaml     Shell MAUI que aloja el BlazorWebView
│   ├── Components/
│   │   ├── Layout/                  MainLayout y NavMenu
│   │   ├── Pages/                   Páginas Blazor (Acciones, Dashboard…)
│   │   │   ├── Bank/                Cuentas y movimientos bancarios
│   │   │   ├── Funds/               Fondos, operaciones e histórico de valor
│   │   │   ├── Properties/          Inmuebles y sus operaciones
│   │   │   └── SafeHaven/           Activos refugio y sus operaciones
│   │   ├── DataTableBase.razor      Componente base reutilizable de tablas
│   │   └── Routes.razor             Enrutado
│   ├── doc/                         Documentación del proyecto (este directorio)
│   │   ├── README.md
│   │   └── specs/                   Especificaciones detalladas (specs/specNN-<nombre>/specNN.md) y features
│   ├── Data/
│   │   ├── AppDbContext.cs          DbContext de EF Core
│   │   └── DatabaseSchemaUpgrader.cs  Migraciones ligeras sin pérdida de datos
│   ├── Models/                      Entidades (ver 3.3)
│   ├── Services/                    Interfaces `I*Service` + implementaciones
│   ├── Platforms/                   Código específico (Android, iOS, MacCatalyst, Windows)
│   ├── Resources/                   Iconos, fuentes, imágenes, splash
│   └── wwwroot/
│       ├── index.html               Host web (carga Bootstrap, ApexCharts y scripts)
│       ├── app.css
│       └── js/companyChart.js       Interop JS del gráfico de cotización
├── Backups/                         Copias de la BD (BACKUPddmmaaaa.db)
├── TempDbViewer/                    Utilidad auxiliar para inspeccionar la BD
├── RecreateDatabase.ps1             Elimina la BD para que se recree desde cero
├── FixRazorErrors.ps1               Script auxiliar de corrección de ficheros Razor
└── *.md (raíz y FINANCEAPP/)        Notas históricas de diseño e implementación
```

### 3.3 Modelo de datos

| Entidad | Campos principales |
|---|---|
| `CompanyDetail` | Name, Ticker, Sector, CurrentPrice, LivePrice, LivePriceUpdatedAt, DividendYield, TotalInvestedInitial, TotalDividends, PlusvaliaReturnPercent, DividendPlusvaliaReturnPercent, NumberOfShares |
| `Operation` | CompanyName, Date, CloseDate, PricePerShare, NumberOfShares, Commission, Amount, Type |
| `PriceHistoryPoint` | CompanyId (FK a `CompanyDetail`), Date, Open, High, Low, Close, Volume, FetchedAt |
| `Fund` | Name, FundType, Manager, TER, ISIN, HistoricalReturn, TotalInvested, CurrentValue |
| `FundOperation` | FundName, Type, Amount, Date |
| `FundValueHistory` | FundName, Value, Date, ReturnAtDate, Notes |
| `BankAccount` | Name, BankName, IBAN, AccountType, CurrentBalance |
| `BankOperation` | AccountName, Type, Amount, Date, Description |
| `SafeHavenAsset` | Name, AssetType, TotalInvested, CurrentValue |
| `SafeHavenOperation` | AssetName, Type, Amount, Date |
| `Property` | Name, PropertyType, Address, PurchasePrice, PurchaseDate, CurrentValue, Status, MonthlyRent, SaleDate, TotalInvested, TotalIncome |
| `PropertyOperation` | PropertyName, Category, Type, Amount, Date, Description |
| `FinancialRecord` | Entidad **heredada (legacy)** de la v1; el rediseño v2 la da por obsoleta |

Todas incluyen `Id`, `CreatedAt` y, en las entidades principales, `UpdatedAt`. Varias propiedades calculadas están marcadas `[NotMapped]` y no se persisten.

Nota de diseño: las operaciones se enlazan con su entidad principal **por nombre** (`CompanyName`, `FundName`, `AccountName`…), no por clave foránea. La única relación con FK real es `PriceHistoryPoint` → `CompanyDetail`.

### 3.4 Servicios

| Servicio | Responsabilidad |
|---|---|
| `ICompanyService` | CRUD de empresas y refresco de precios en vivo |
| `IOperationService` | CRUD de operaciones de acciones |
| `IFundService` | Fondos, operaciones e histórico de valor |
| `IBankService` | Cuentas y movimientos bancarios |
| `ISafeHavenService` | Activos refugio y operaciones |
| `IPropertyService` | Inmuebles y operaciones |
| `IDashboardService` | Agregados para el dashboard |
| `IFinancialService` | Registros financieros heredados (`FinancialRecord`) |
| `IStockPriceService` | Precio actual y OHLC diario desde Yahoo Finance |
| `IPriceHistoryService` | Histórico con caché en SQLite y agregación diaria/mensual/anual |

### 3.5 Cotizaciones y gráfico

1. `StockPriceService` consulta `https://query1.finance.yahoo.com/v8/finance/chart/{ticker}` (precio actual con `range=1d`; histórico con `range=5y` por defecto). Registra en log cualquier respuesta no válida.
2. `PriceHistoryService` guarda las barras diarias en la tabla `PriceHistoryPoints`. La caché se considera **fresca durante 12 horas**; pasado ese tiempo se vuelve a consultar Yahoo Finance.
3. La agregación **mensual y anual** se calcula en el servidor a partir de los puntos diarios cacheados (`ChartGranularity`: `Daily`, `Monthly`, `Yearly`).
4. `CompanyChartModal.razor` pide la serie y la entrega a `wwwroot/js/companyChart.js` por JS interop, que dibuja el gráfico con ApexCharts, superpone las operaciones de compra y la línea del precio actual.

### 3.6 Base de datos y migraciones

- **Creación**: `Database.EnsureCreated()` crea el esquema solo si el fichero no existe; los datos persisten entre ejecuciones.
- **Evolución del esquema**: no se usan migraciones de EF. `DatabaseSchemaUpgrader` añade columnas o tablas nuevas si faltan (`EnsureCompanyDetailsColumns`, `EnsureCompanyDetailLivePriceColumns`, `EnsurePriceHistoryTable`) sin borrar datos.
- **Ubicación en Windows (sin empaquetar)**: `C:\Users\<usuario>\AppData\Local\User Name\com.companyname.financeapp\Data\financeapp.db`.
- **Backups**: se guardan en `Backups/` con nombre `BACKUPddmmaaaa.db` copiando la BD anterior (no el `bin/`). Como `Backups/` está en `.gitignore`, se añaden con `git add -f`.
- **Reinicio completo**: `RecreateDatabase.ps1` elimina el fichero de BD (¡borra todos los datos!) para que se regenere al arrancar.

### 3.7 Dependencias principales

- `Microsoft.Maui.Controls`, `Microsoft.AspNetCore.Components.WebView.Maui`
- `Microsoft.EntityFrameworkCore.Sqlite` y `.Design` 10.0.0
- `SQLitePCLRaw.lib.e_sqlite3` (variante por plataforma)
- `Microsoft.Extensions.Http`, `Microsoft.Extensions.Logging.Debug`
- ApexCharts.js, Bootstrap Icons (CDN)

---

## 4. Puesta en marcha

Requisitos: Visual Studio con la carga de trabajo **.NET MAUI** y el SDK de **.NET 10**.

1. Abrir `FINANCEAPP.slnx` en Visual Studio.
2. Seleccionar el destino (por ejemplo *Windows Machine*) y ejecutar el proyecto `FINANCEAPP`.
3. En el primer arranque se crea la base de datos automáticamente.

---

## 5. Convenciones y notas

- Interfaz y comentarios del código en español.
- Patrón UX unificado en todas las categorías: listado → formulario → operaciones.
- Los ficheros `.md` de la raíz y de `FINANCEAPP/` (`DISEÑO_ARQUITECTONICO_V2.md`, `RESUMEN_IMPLEMENTACION*.md`, `MIGRACION_BASE_DATOS.md`, etc.) son notas históricas del proceso de desarrollo; este README es la referencia vigente.
- El rediseño v2 se describe en `DISEÑO_ARQUITECTONICO_V2.md` (visión multi-activo, entidades y fases de implementación).

## 6. Índice de documentación

| Documento | Contenido |
|---|---|
| `FINANCEAPP/doc/README.md` | Este documento: visión general, estructura funcional y técnica |
| `FINANCEAPP/doc/constitution.md` | Los seis principios innegociables del proyecto |
| `FINANCEAPP/doc/specs/README.md` | Índice de las 20 especificaciones funcionales, agrupadas en 7 categorías |
| `FINANCEAPP/doc/specs/specNN-<nombre>/specNN.md` | Una especificación detallada por funcionalidad, cada una en su carpeta |
| `FINANCEAPP/doc/specs/specNN-<nombre>/specNN-MM-<name>/` | Features de esa spec: `specNN-MM.md`, `planNN-MM.md` y `taskNN-MM.md` |
