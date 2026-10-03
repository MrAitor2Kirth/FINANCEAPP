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
│   ├── wwwroot/js/companyChart.js       interop JS del gráfico de cotización
│   └── doc/                   documentación: README.md + specs/spec01…spec20
├── Backups/                   copias de la BD (BACKUPddmmaaaa.db), ignorada en .gitignore
├── TempDbViewer/              utilidad auxiliar para inspeccionar la BD
├── RecreateDatabase.ps1       BORRA la BD para regenerarla
└── *.md (raíz y FINANCEAPP/)  notas históricas de desarrollo, NO son referencia vigente
```

La referencia vigente es `FINANCEAPP/doc/README.md` y las specs de `FINANCEAPP/doc/specs/`. Los `.md` históricos (`DISEÑO_ARQUITECTONICO_V2.md`, `RESUMEN_*.md`, etc.) pueden estar desactualizados.

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
- **Patrón de capas**: página `.razor` → `I*Service` → `AppDbContext`. Los servicios se registran `Scoped` en `MauiProgram.cs`. Toda lógica de negocio va en el servicio, no en la página.
- **Patrón de UX por categoría**: listado de la entidad principal → formulario de alta/edición → operaciones desplegables bajo cada fila.
- **Importes**: `decimal` con `[Column(TypeName = "decimal(18,2)")]` y formato `ToString("C")`.
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
- **Acciones**: solo cuentan las operaciones **abiertas** (sin `CloseDate`) para inversión y número de acciones; los dividendos cuentan siempre. Una Venta resta su importe completo (no usa coste medio ni FIFO). El valor de cartera usa `LivePrice ?? CurrentPrice`.
- **Fondos**: el valor se actualiza **solo** con el historial de valores (`UpdateFundValueWithHistoryAsync`). No reintroducir operaciones de tipo Plusvalía; los métodos de la antigua opción A siguen en el servicio sin uso.
- **Dashboard**: `SafeHavenInitialTotal` = total invertido y `SafeHavenTotal` = valor actual (mismo criterio que acciones y fondos). Los inmuebles entran por `CurrentValue ?? TotalInvested` y solo los no vendidos. El rendimiento de acciones usa `TotalInvestedValue`.
- **Eliminar una empresa es en cascada** (operaciones + histórico de cotización) y definitivo. Eliminar fondo, cuenta, activo refugio o inmueble también borra sus operaciones.
- **SQLite no admite transacciones anidadas**: los métodos de recálculo se llaman dentro de transacciones ya abiertas y no deben abrir otra.
- **Esquema de BD**: `EnsureCreated()` solo crea la BD si no existe; **no aplica cambios a una BD existente**. Cualquier columna o tabla nueva debe añadirse en `DatabaseSchemaUpgrader` (con `IF NOT EXISTS` / comprobación `PRAGMA table_info`) y llamarse desde `MauiProgram`. Cambiar solo el modelo rompe las BD ya creadas.
- **Datos semilla**: `HasData` carga empresas de ejemplo (MSFT, AAPL, KO, JNJ, PG) y operaciones solo al crear una BD nueva.
- **Yahoo Finance**: exige cabecera `User-Agent`; timeout de 10 s; los tickers europeos sin sufijo se resuelven probando `.MC`, `.PA`, `.MI`, `.DE`, `.AS`, `.BR`, `.L`, `.LS`, `.SW`. La actualización masiva es secuencial y manual. La caché de histórico dura 12 h.
- **CDN**: ApexCharts e iconos se cargan por red; sin conexión, el gráfico y los iconos no aparecen.
- **Ruta de la BD** (`FileSystem.AppDataDirectory/financeapp.db`): en Windows sin empaquetar es `C:\Users\Mr X\AppData\Local\User Name\com.companyname.financeapp\Data\financeapp.db`. No usar la copia del `bin/`.
- **Datos reales**: la BD del usuario contiene sus finanzas reales. Cualquier prueba debe hacerse sobre una copia.
- `FinancialRecord` / `IFinancialService` son **legado** de la v1; no ampliar.

## Forma de trabajar

- **Planifica antes de tocar código** cuando el cambio afecte a más de un servicio, al modelo de datos, al esquema de BD o a cálculos de dinero. Resume el plan en pocas líneas y espera conformidad si hay alternativas razonables.
- **Cambios pequeños y enfocados**: una funcionalidad o corrección por cambio; no mezcles refactors con arreglos.
- **Lee antes de editar**: abre el servicio, la página y la spec relacionada (`FINANCEAPP/doc/specs/`) antes de modificar.
- **Actualiza la documentación** cuando cambies un comportamiento: la spec afectada (reglas de negocio, criterios de aceptación, observaciones) y, si cambia la estructura, `FINANCEAPP/doc/README.md`.
- **Al terminar**, explica en español y de forma breve: qué cambiaste y por qué, qué ficheros tocaste, qué no pudiste verificar y qué debe probar el usuario.
- Si no puedes compilar (entorno sin SDK/MAUI), dilo claramente: no afirmes que el cambio funciona.

## Memoria

- Al empezar, lee `MEMORY.md` para conocer el estado del proyecto y las decisiones tomadas.
- Al terminar una tarea, actualízalo: estado actual, decisiones importantes (con su porqué) y errores a evitar.
- Mantenlo breve (máximo ~50 líneas): resume o elimina lo que ya no aporte.
- Si algo se convierte en una regla permanente, propón moverlo a `AGENTS.md` en lugar de dejarlo en la memoria.
- No guardes nunca datos sensibles (claves, tokens, datos personales).

## Límites

- ✅ **Siempre**:
  - Mantener el patrón página → servicio → contexto y los mensajes en español.
  - Recalcular totales tras modificar operaciones.
  - Respetar CRLF/BOM y el estilo del fichero que editas.
  - Usar transacciones en operaciones que tocan varias tablas.
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
  - Hacer commit o push sin que el usuario lo pida.
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
4. **Contrasta con la spec** correspondiente en `FINANCEAPP/doc/specs/`: cada una tiene criterios de aceptación en casillas marcables; recórrelos.
5. **Revisa el diff** (`git diff`): solo los ficheros previstos, sin cambios de fines de línea masivos.
6. Resume al usuario lo verificado y lo pendiente de probar en su máquina.
