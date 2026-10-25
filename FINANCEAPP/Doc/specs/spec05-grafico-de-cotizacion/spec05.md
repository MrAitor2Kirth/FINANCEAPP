# SPEC-05 · Gráfico de cotización con operaciones superpuestas

| | |
|---|---|
| **Categoría** | 1. Acciones y empresas |
| **Identificador** | SPEC-05 |
| **Documento** | `spec05-grafico-de-cotizacion/spec05.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Gráfico histórico por empresa en cinco temporalidades (Horas, Diario, Semanal, Mensual y Anual) con todo el histórico disponible (SPEC-05-02), precio en vivo y compra o venta desde el propio gráfico (SPEC-05-01), volumen en dinero y marcas temporales propias (SPEC-05-03) y comparación en base 100 con otra empresa o un índice, con rentabilidades y correlación del tramo visible (SPEC-05-04).

## 1. Descripción funcional

Abre, desde el listado de empresas, un modal con el gráfico de cotización histórico de la empresa. Muestra un visor del precio en vivo que se refresca solo, permite comprar o vender con un mini-formulario, marca las compras, traspasos, ventas y dividendos del usuario y traza la línea del precio actual. Se puede cambiar entre Horas, Diario, Semanal, Mensual y Anual; cada una se abre con su tramo de tiempo (cuanto más fina, menos tiempo) y el zoom permite alejar hasta todo el histórico, que llega hasta la primera cotización de la empresa. El eje es por sesiones: no hay huecos de fines de semana, festivos ni noches. Capas opcionales: velas japonesas, volumen en dinero, precio medio con resultado y marcas temporales del usuario (de la empresa o globales). Con «Comparar» se superpone otra empresa o un índice en base 100.

## 2. Pantallas y rutas

- `/companies` – botón gris «Ver gráfico de cotización» en cada fila, que abre el modal `CompanyChartModal`. Al cerrar el modal, la lista se recarga.
- `/indices` (`BenchmarkIndicesList.razor`, SPEC-05-04) – índices para comparar: listado con el último cierre guardado, alta, edición, baja, «Comprobar» y sugerencias; se llega con el botón «Índices» de `/companies`.

## 3. Reglas de negocio

### Histórico

- Temporalidades (SPEC-05-02), de más fina a más gruesa: **Horas**, **Diario** (por defecto al abrir), **Semanal**, **Mensual** y **Anual**. Cambiar de temporalidad no pierde las capas activas y, salvo Horas, lee de la caché sin llamar a Yahoo.
- El histórico diario OHLC se descarga de Yahoo Finance por tramos (`interval=1d` con `period1`/`period2`) y se **cachea en SQLite** (tabla `PriceHistoryPoints`):
  - Sin caché: primero los **últimos 2 años**, para dibujar enseguida.
  - Después, en **segundo plano**, bloques de **10 años** hacia atrás desde el punto más antiguo guardado, hasta la primera cotización que da Yahoo (`firstTradeDate`) o, si no la da, hasta el primer bloque vacío (nunca antes de 1970). Cada bloque se guarda al llegar y el gráfico lo añade **sin mover la vista**, con el aviso «Descargando histórico anterior… (desde AAAA)». Al terminar, la clave `PriceHistory.Complete.{CompanyId}` de `AppSettings` marca la empresa como completa.
  - Cerrar el modal cancela el segundo plano; lo guardado se queda y la siguiente apertura continúa desde el punto más antiguo, aunque la caché sea reciente. Si un bloque falla se avisa y se reintenta en la siguiente apertura.
  - La descarga HTTP va fuera del semáforo del modal y el guardado dentro, con el `DbContext` compartido; mientras tanto se puede operar con normalidad.
- La caché se considera **fresca durante 12 horas** según el punto más reciente; pasado ese tiempo solo se piden los días nuevos (desde 7 días antes del último punto). Si la consulta no devuelve datos, se sirve lo que haya en caché y se avisa.
- «Recargar histórico» ignora la caché de 12 horas, vuelve a descargar los últimos 2 años y reanuda el segundo plano si falta histórico; en Horas, además, vacía y vuelve a descargar las barras de una hora. Si falla, conserva la caché y avisa.
- Los puntos existentes se actualizan por fecha y los nuevos se insertan (índice único empresa + fecha); tras guardar se sueltan del `DbContext` para no acumularlos.
- La agregación **semanal (lunes a domingo, fechada el lunes), mensual y anual** se calcula en el servidor a partir de los puntos diarios: apertura del primer día, cierre del último, máximo, mínimo y volumen acumulado.
- **Horas**: barras de una hora (`interval=1h`) en hora local de Windows. Se descarga primero el último mes y el resto, en bloques de 6 meses, hasta el límite de Yahoo (unos 730 días). **No se guardan en la BD**: viven en memoria 15 minutos por empresa. Su descarga se cancela al salir de Horas o cerrar. Mensajes distintos para «Yahoo no ofrece barras horarias para este ticker» y sin conexión.
- **Tramo inicial** de cada temporalidad (lo calcula `PriceHistoryService.GetInitialView`): Horas 1 mes, Diario 1 año, Semanal 5 años, Mensual 20 años, Anual todo; termina en el último punto. El «reset» de la barra de ApexCharts enseña todo lo cargado y el botón «Vista inicial» vuelve al tramo.
- Tooltips con fecha según la temporalidad: Horas `dd MMM aaaa HH:mm`, Diario `dd MMM aaaa`, Semanal «Semana del…», Mensual `MMM aaaa`, Anual `aaaa`.
- **Eje por sesiones** (SPEC-05-03): cada periodo ocupa un hueco del eje según su orden, no según su fecha; no hay huecos de fines de semana, festivos ni noches, y las etiquetas muestran la fecha del periodo. Lo traduce `companyChart.js`; el modal y los servicios siguen trabajando con fechas.
- Activar o quitar una capa, operar desde el gráfico o filtrar la leyenda **conserva el tramo visible** (`companyChart.getView` + `keepView`).

### Volumen

- Barras en **dinero aproximado** en la divisa de la empresa: acciones × cierre (`PriceHistoryPoint.TradedValue`, calculado al leer y no guardado). En Semanal, Mensual y Anual es la suma día a día de acciones × cierre. El tooltip muestra «Negociado: ≈ importe» y las acciones; el eje, el importe compacto.
- Una barra por periodo, todas de la misma anchura; **verde** si el periodo cerró igual o por encima de su apertura y **roja** si cerró por debajo. Periodo con volumen 0: sin barra.

### Precio en vivo

- Panel con el último precio de Yahoo (precio único de referencia: Yahoo no da precio de compra y de venta separados), variación frente al cierre anterior, máximo y mínimo del día, hora del precio y estado del mercado.
- Se refresca cada **30 segundos** mientras el modal está abierto y con el botón «Actualizar precio»; nunca hay dos consultas a la vez. Al cerrar el modal se detiene.
- Cada precio obtenido se guarda en la empresa (`LivePrice`, `LivePriceUpdatedAt`). Si falla, se conserva el último precio y su hora y se avisa.
- El símbolo resuelto (p. ej. `ACS.MC`) se reutiliza en los refrescos para no repetir la prueba de sufijos.
- Estado del mercado según la sesión regular de Yahoo (`currentTradingPeriod.regular`): Abierto, Cerrado o Desconocido (si Yahoo no la da).
- La línea «Precio actual» y, en vista diaria, el punto de hoy siguen al precio en vivo (cierre provisional, no se guarda en la caché). En las demás temporalidades solo se mueve la línea. El refresco no cambia el tramo que se está mirando.

### Comprar y vender desde el gráfico

- «Comprar» y «Vender» abren un mini-formulario: fecha (hoy, editable, no futura), acciones, precio por acción, comisión, tipo de cambio (solo si la empresa no es de la divisa base) e importe calculado con `OperationAmountCalculator` (solo lectura).
- El precio del formulario **sigue al precio en vivo** hasta confirmar; si se escribe a mano deja de seguirlo y aparece «Volver al precio en vivo».
- Bloqueo (lo decide `OperationService.GetQuickTradeBlockReason` y se revalida al confirmar): fecha futura; fecha de hoy con mercado **Cerrado**; Venta sin acciones disponibles. Con fecha anterior a hoy no se exige mercado abierto. Con mercado Desconocido se permite con aviso.
- Compra → `OperationService.AddAsync`; Venta → `OperationService.RegisterSaleAsync` (FIFO e histórico de ventas). Ambas pasan por `RegisterQuickTradeAsync`.
- Acciones disponibles: lotes abiertos de Compra y Traspaso menos Ventas antiguas abiertas (misma cuenta que `RegisterSaleAsync`).

### Marcadores y capas

- Compras y traspasos (tipo literal `"Compra"`/`"Traspaso"`, precio > 0), agrupados por lote: las filas partidas por una venta parcial (`OriginOperationId`) se ven como un solo marcador con las acciones sumadas.
- Cada marcador se coloca en su periodo (`AlignMarkers`): en Horas, la primera barra de una hora de su día (sin barras ese día, no se dibuja); en Semanal, Mensual y Anual, el inicio del periodo. El tooltip muestra la fecha real de la operación.
- Colores (SPEC-05-03): compra **verde**, venta **roja** (cuadrado), dividendo **azul** (relleno, sobre el cierre de ese día), traspaso **violeta**; compras, ventas y traspasos con relleno blanco. Línea «Precio actual» en **gris** y «Precio medio» en **magenta** discontinua.
- **Leyenda que filtra**: pulsar Compra, Venta, Dividendo o Traspaso oculta o muestra ese tipo sin mover la vista; no se guarda y se vacía al abrir el modal.
- Importes en la **divisa de la empresa** en eje, tooltips y etiquetas.
- Capas opcionales recordadas en `AppSettings` (`CompanyChart.ShowAverage`, `CompanyChart.ShowCandles`, `CompanyChart.ShowVolume`, apagadas por defecto, y `CompanyChart.ShowMarks`, encendida por defecto): precio medio y resultado latente (deshabilitada sin acciones), velas japonesas, volumen (segundo gráfico sincronizado en zoom) y marcas.

### Marcas temporales (SPEC-05-03)

- «Añadir marca» abre un mini-formulario: fecha (obligatoria, no futura), descripción (obligatoria, máximo 200 caracteres) y tipo **Empresa** (solo en el gráfico de esta empresa) o **Global** (en el de todas). Valida `ChartMarkService` con mensajes en español.
- Cada marca es un **punto marrón** sobre el cierre de su periodo (relleno si es de Empresa, hueco si es Global, colocado con `AlignMarks` como los marcadores). Al pasar el ratón o tocarla aparece una **línea vertical marrón discontinua**.
- **Tooltip del día**: al pasar el ratón por un periodo, su tooltip de cotización (velas o línea) añade debajo las operaciones y marcas dibujadas en él con su detalle (tipo, fecha real, acciones, precio, importe o descripción escapada) y un icono del color y forma de su punto.
- El interruptor «Marcas» oculta todas sin mover la vista; la leyenda no las filtra.
- Panel desplegable «Marcas (N)» con las marcas de la empresa y las globales (de la más reciente a la más antigua): interruptor **Activa** (una inactiva no se dibuja), **Editar** (fecha, descripción y tipo; una Global pasada a Empresa queda ligada a la empresa del gráfico) y **Borrar** con confirmación («Se borrará en todas las empresas» si es Global).
- Una marca anterior al histórico cargado no se dibuja (sí sale en el panel) hasta que llega ese histórico.
- La línea vertical de marcas y pagos de dividendo la gobierna el tooltip del día (ApexCharts no avisa al pasar por un punto anotado) y se quita al salir del gráfico.

### Pagos de dividendo, tramo futuro y navegador (SPEC-21-01)

- Cada línea del calendario de dividendos de la empresa es un **rombo azul** (hueco Pendiente, relleno Cobrado) en su fecha de pago; el tooltip del día añade «Pago de dividendo» con fechas, estado e importe, y aparece una línea vertical azul. Se oculta con el interruptor «Marcas»; en Horas solo las pasadas.
- El eje se prolonga con **sesiones futuras vacías** hasta lo más lejano de hoy + 1 año o el pago pendiente más lejano (`PriceHistoryService.BuildFutureSessions`; Diario: lunes a viernes). Los pagos futuros van a la altura del último precio; una serie auxiliar «Referencia», oculta por CSS, sostiene la escala cuando solo se ve el futuro. La vista inicial acaba en el último precio y el precio en vivo rellena la sesión de hoy si ya existía como futura.
- **Minigráfico navegador** bajo el gráfico y el volumen: todo el histórico y el tramo futuro sombreado; su ventana («brush» de ApexCharts) mueve el gráfico y el volumen, y sigue al zoom, al desplazamiento, a «Vista inicial» y a los bloques antiguos.
### Comparación con otra empresa o un índice (SPEC-05-04)

- **Índices**: se dan de alta en `/indices` (nombre ≤ 100 y símbolo de Yahoo ≤ 20, único y en mayúsculas; cualquier símbolo: `^IBEX`, `GC=F`, `EURUSD=X`…). «Comprobar» consulta el último valor sin probar sufijos de bolsa (los símbolos con `^` o `=` no los prueban nunca). Sugerencias: IBEX 35, EURO STOXX 50, DAX 40, CAC 40, S&P 500, Nasdaq 100 y Dow Jones. Borrar un índice (o cambiar su símbolo) borra su histórico guardado.
- **Histórico**: el de los índices se guarda en `BenchmarkPricePoints` con las mismas reglas que el de las empresas (2 años primero, bloques de 10 años hasta la primera cotización, 12 horas de caché, Horas en memoria). `PriceHistoryService` trabaja con una serie (`ChartSeriesRef`: empresa o índice) y los métodos de empresa delegan en los de serie.
- **«Comparar»** abre un selector con las otras empresas con ticker y los índices; «Quitar comparación» vuelve al gráfico normal en el mismo tramo. No se recuerda entre aperturas. Una empresa o índice sin caché se descarga al elegirlo (aviso de carga) y, si su histórico no está completo, se completa en segundo plano **después** del de la empresa abierta.
- **Base 100**: las dos series son líneas que valen 100 en el primer periodo visible con precio en las dos; al hacer zoom, desplazarse o mover el navegador, el gráfico avisa al modal del tramo (250 ms de espera, solo si cambió) y `ChartComparison` recalcula la base, las rentabilidades y la correlación; el JS solo redibuja (`setComparison`) sin mover la vista.
- Mientras se compara se ocultan volumen, velas, «Precio actual», «Precio medio», operaciones y pagos de dividendo (sus interruptores se desactivan); las **marcas** se dibujan sobre las dos líneas. El navegador sigue con la empresa abierta y el tramo futuro va sin valores. El precio en vivo no mueve la base 100.
- **Alineación**: en cada periodo de la empresa, el último cierre de la serie comparada con fecha (u hora, en Horas) menor o igual; nada antes de su primer dato. Misma agregación en Semanal, Mensual y Anual; en Horas, las barras de la comparada viven en memoria y se amplían junto con las de la empresa.
- **Panel bajo el gráfico**: nombre y rentabilidad del tramo de cada serie (último ÷ primero − 1), correlación de Pearson de las rentabilidades de cada periodo (pares completos, 2 decimales) con su palabra: fuerte ≥ 0,7, moderada 0,4–0,7, débil 0,2–0,4, sin relación −0,2–0,2, inversa < −0,2; con menos de 10 periodos, «Pocos datos para calcular la correlación».

- Si no hay datos, el modal distingue: sin histórico para el ticker, sin conexión con Yahoo y librería de gráficos no cargada.
- **Cuenta bancaria al vender** (SPEC-02-04): el formulario «Vender» incluye «Cuenta bancaria» (vacía = la más antigua); sin cuentas, la venta queda bloqueada con aviso.
- **Cuenta bancaria al comprar** (SPEC-02-06): el formulario «Comprar» incluye también «Cuenta bancaria» (vacía = la más antigua) con la opción «No registrar en banco»; la compra crea un Gasto en esa cuenta. Sin cuentas, el campo no aparece y la compra se guarda sin movimiento.

## 4. Datos implicados

- `PriceHistoryPoint`: Id, CompanyId (FK a CompanyDetail), Date, Open, High, Low, Close, Volume, FetchedAt.
- Tabla creada por `DatabaseSchemaUpgrader.EnsurePriceHistoryTable`, con índice único (CompanyId, Date).
- `CompanyDetail.LivePrice` y `LivePriceUpdatedAt` (escritos por el refresco del gráfico).
- `Operations` y `SalesHistory` (operaciones creadas desde el gráfico).
- `AppSettings`: claves `CompanyChart.*` con `"true"`/`"false"` y una clave `PriceHistory.Complete.{CompanyId}` = `"true"` por empresa con el histórico diario completo (se borra al eliminar la empresa). Sin cambios de esquema.
- Barras de una hora: solo en memoria de `PriceHistoryService` (15 minutos), nunca en la BD.
- `ChartMarks` (SPEC-05-03, `DatabaseSchemaUpgrader.EnsureChartMarksTable`): Id, CompanyId (nulo = Global; sin clave foránea), Date, Description, IsActive, CreatedAt, UpdatedAt; índice (CompanyId, Date). Al eliminar una empresa, `CompanyService` borra sus marcas de Empresa en la misma transacción; las globales se conservan.
- `AppSettings`: clave `CompanyChart.ShowMarks` (sin fila = activada).
- `BenchmarkIndices` (SPEC-05-04, `DatabaseSchemaUpgrader.EnsureBenchmarkTables`): Id, Name, Symbol, CreatedAt, UpdatedAt; índice único `Symbol`.
- `BenchmarkPricePoints` (SPEC-05-04): Id, BenchmarkIndexId (sin clave foránea), Date, Open, High, Low, Close (`decimal(18,4)`), Volume, FetchedAt; índice único (BenchmarkIndexId, Date). `AppSettings`: clave `BenchmarkHistory.Complete.{BenchmarkIndexId}` = `"true"` con el histórico completo; `BenchmarkIndexService` borra puntos y clave con el índice.

## 5. Flujo principal

1. El usuario pulsa el botón gris de una empresa.
2. `CompanyChartModal` (con el `DbContext` compartido de la app y un semáforo para no solapar consultas) carga la empresa, las preferencias, la divisa base, los marcadores, las marcas, las acciones disponibles y el histórico.
3. Dibuja con `companyChart.render` en el tramo inicial de Diario y pide la primera cotización (`CompanyService.RefreshLiveQuoteAsync`); arranca el refresco cada 30 s, que llama a `companyChart.updateLive` sin redibujar.
4. Si el histórico no está completo, arranca el segundo plano: descarga bloques de 10 años (`FetchOlderDailyBlockAsync`), los guarda bajo el semáforo (`SaveDailyBlockAsync`) y los añade con `companyChart.updateData`.
5. El usuario pulsa Comprar o Vender, ajusta el formulario y confirma; `OperationService.RegisterQuickTradeAsync` registra la operación y el gráfico recarga marcadores y posición sin mover la vista.
   Al añadir, editar, activar o borrar una marca, `ChartMarkService` la guarda bajo el semáforo y el gráfico la muestra con `companyChart.updateData`.
6. Al cerrar, se detienen el refresco y las descargas en segundo plano, se destruye el gráfico, se espera a que termine la consulta en curso y `/companies` recarga la lista.

## 6. Componentes técnicos

- `CompanyChartModal.razor` (`DbContext` compartido, `SemaphoreSlim` para el acceso a datos, `PeriodicTimer`, `IAsyncDisposable`).
- `wwwroot/js/companyChart.js` (ApexCharts: `render` (con `data.comparison` y la referencia del modal para el modo comparación), `getView`, `updateData`, `setComparison`, `setHiddenKinds`, `resetView`, `updateLive`, `destroy`; eje por sesiones, línea o velas, anotaciones, marcas con línea vertical al pasar el ratón, volumen en dinero coloreado y agrupado, tramo inicial o conservado con `zoomX` y actualizaciones encadenadas para no perder el zoom).
- `IDividendService.GetChartMarksAsync` y `PriceHistoryService.BuildFutureSessions` / `AlignDividendMarks` (SPEC-21-01).
- Comparación (SPEC-05-04): `IBenchmarkIndexService` / `BenchmarkIndexService` (`GetAllAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `CheckSymbolAsync`, `GetSuggestionsAsync`), `Models/BenchmarkIndex.cs` y `BenchmarkPricePoint.cs`; `ChartComparison` (`Align`, `Compute`, `GetCorrelationLabel`; `ComparisonResult`); métodos por serie de `IPriceHistoryService` (`GetSeriesHistoryWithStatusAsync`, `GetCachedSeriesHistoryAsync`, `GetSeriesBackfillStateAsync`, `SaveSeriesDailyBlockAsync`, `GetSeriesHourlyAsync`, `FetchOlderSeriesHourlyBlockAsync`, `GetCachedSeriesHourly`) con `ChartSeriesRef` / `ChartSeriesKind`; `[JSInvokable] OnComparisonRangeChanged` en el modal.
- `IChartMarkService` / `ChartMarkService` (`GetForCompanyAsync`, `CreateAsync`, `UpdateAsync`, `SetActiveAsync`, `DeleteAsync`; `ChartMarkInput`, `ChartMarkResult`, `ChartMarkPoint`) y `Models/ChartMark.cs`.
- `IStockPriceService.GetQuoteAsync` / `StockQuote`, `MarketState` (`Services/StockQuote.cs`) y `GetHistoryRangeAsync` / `StockHistoryChunk` (tramos con `period1`/`period2`).
- `ICompanyService.RefreshLiveQuoteAsync`.
- `IOperationService.GetAvailableSharesAsync`, `GetQuickTradeBlockReason`, `RegisterQuickTradeAsync`, `GetChartMarkersAsync` (`Services/QuickTrade.cs`).
- `IAppSettingsService.GetChartPreferencesAsync` / `SaveChartPreferencesAsync` (`ChartPreferences`).
- `IPriceHistoryService`: `GetHistoryWithStatusAsync` (bloque reciente, actualización incremental y recarga forzada), `GetCachedHistoryAsync`, `GetBackfillStateAsync` / `FetchOlderDailyBlockAsync` / `SaveDailyBlockAsync` (segundo plano), `GetHourlyAsync` / `FetchOlderHourlyBlockAsync` / `GetCachedHourly` / `ClearHourly` (Horas en memoria) y `AlignMarkers` / `AlignMarks` (colocación por temporalidad); resultados `PriceHistoryResult`, `HistoryBackfillState` y `HistoryBlockResult`; enumeración `ChartGranularity` (`Daily`, `Monthly`, `Yearly`, `Hourly`, `Weekly`).

## 7. Criterios de aceptación

- [ ] El modal abre con vista diaria en el último año y muestra la serie histórica.
- [ ] Horas, Diario, Semanal, Mensual y Anual abren con 1 mes, 1 año, 5 años, 20 años y todo; el zoom permite alejar y «Vista inicial» vuelve al tramo.
- [ ] El histórico diario llega hasta la primera cotización de la empresa, por bloques en segundo plano y sin mover la vista.
- [ ] Cambiar a Semanal, Mensual y Anual reduce el número de puntos.
- [ ] Compras, traspasos, ventas y dividendos aparecen como marcadores con su tooltip y en la divisa de la empresa.
- [ ] En una segunda apertura dentro de 12 horas no se vuelve a llamar a Yahoo para el histórico; «Recargar histórico» sí lo hace.
- [ ] Sin datos se muestra un mensaje informativo que distingue la causa.
- [ ] El precio en vivo se refresca cada ~30 s y se guarda en la empresa.
- [ ] Una compra desde el gráfico aparece en `/operations` y una venta en `/sales-history`.
- [ ] Con el mercado cerrado y fecha de hoy no se puede confirmar; con fecha anterior sí.
- [ ] Velas, volumen, precio medio y marcas se activan, se recuerdan al reiniciar y no cambian el tramo visible.
- [ ] El volumen se ve en dinero, una barra por periodo, verde o roja, sin huecos de fines de semana.
- [ ] Compras verdes, ventas rojas, dividendos azules, traspasos violeta; la leyenda los oculta y muestra.
- [ ] Una marca de Empresa se ve solo en su empresa y una Global en todas; se activan, editan y borran desde el panel.
- [ ] «Comparar» superpone otra empresa o un índice en base 100 desde el borde izquierdo; al desplazar el tramo se rebasa y cambian rentabilidades y correlación; «Quitar comparación» vuelve todo en el mismo tramo.

Criterios detallados en [SPEC-05-01](spec05-01-chart-live-trading/spec05-01.md), [SPEC-05-02](spec05-02-chart-timeframes/spec05-02.md), [SPEC-05-03](spec05-03-chart-volume-and-marks/spec05-03.md) y [SPEC-05-04](spec05-04-chart-comparison/spec05-04.md).

## 8. Observaciones y limitaciones detectadas

- ApexCharts se carga desde CDN (`cdn.jsdelivr.net`) **sin versión fija**, por lo que sin Internet el gráfico no se dibuja y una versión mayor nueva podría romperlo; conviene empaquetarlo en `wwwroot/lib` y fijar la versión (fuera de alcance en SPEC-05-01).
- El histórico anterior a 1970 no se descarga (Yahoo no admite `period1` negativo); las barras de una hora llegan como máximo a unos 730 días atrás.
- `period1`/`period2`, `firstTradeDate` y el límite de Horas dependen del endpoint no oficial de Yahoo; si cambian, se ajustan las constantes de `PriceHistoryService`.
- Yahoo da las cotizaciones europeas con unos 15 minutos de retraso; la hora del precio lo deja ver.
- El volumen en dinero es aproximado (acciones × cierre del periodo); Yahoo da a veces volumen 0 en barras de una hora de valores europeos.
- El estado del mercado depende de la sesión que informa Yahoo; en festivos puede no ser exacto.
- La correlación es de rentabilidades de periodo en el tramo visible: con pocos periodos o tramos muy cortos es poco fiable. La base 100 no convierte divisas (no hace falta: compara variaciones).

## 9. Features relacionadas

| Feature | Estado | Documento |
|---|---|---|
| SPEC-05-01 · Precio en vivo y compra/venta desde el gráfico, correcciones y capas opcionales (`chart-live-trading`) | Implementada, pendiente de compilar y probar a mano | [spec05-01.md](spec05-01-chart-live-trading/spec05-01.md) · [plan](spec05-01-chart-live-trading/plan05-01.md) · [tareas](spec05-01-chart-live-trading/task05-01.md) |
| SPEC-05-02 · Histórico completo y temporalidades del gráfico: Horas, Diario, Semanal, Mensual y Anual (`chart-timeframes`) | Implementada y compilada, pendiente de probar a mano | [spec05-02.md](spec05-02-chart-timeframes/spec05-02.md) · [plan](spec05-02-chart-timeframes/plan05-02.md) · [tareas](spec05-02-chart-timeframes/task05-02.md) |
| SPEC-05-03 · Volumen en dinero, colores de los marcadores y marcas temporales (`chart-volume-and-marks`) | Implementada y compilada, pendiente de probar a mano | [spec05-03.md](spec05-03-chart-volume-and-marks/spec05-03.md) · [plan](spec05-03-chart-volume-and-marks/plan05-03.md) · [tareas](spec05-03-chart-volume-and-marks/task05-03.md) |
| SPEC-05-04 · Comparación de dos gráficos (empresas e índices) y correlación (`chart-comparison`) | Implementada y compilada, pendiente de probar a mano | [spec05-04.md](spec05-04-chart-comparison/spec05-04.md) · [plan](spec05-04-chart-comparison/plan05-04.md) · [tareas](spec05-04-chart-comparison/task05-04.md) |
| SPEC-21-01 · Calendario de dividendos: marca «Pago de dividendo» con fecha futura, tramo futuro del eje y minigráfico navegador (`dividend-calendar`, spec principal SPEC-21) | Implementada y compilada, pendiente de probar a mano | [spec21-01.md](../spec21-dividend-calendar/spec21-01-dividend-calendar/spec21-01.md) · [plan](../spec21-dividend-calendar/spec21-01-dividend-calendar/plan21-01.md) · [tareas](../spec21-dividend-calendar/spec21-01-dividend-calendar/task21-01.md) |
| SPEC-02-08 · Número de acciones con decimales (`fractional-shares`, spec principal SPEC-02): acciones con 2 decimales en el mini-formulario de compra y venta, en las acciones disponibles y en los tooltips | Implementada y compilada, pendiente de probar a mano | [spec02-08.md](../spec02-operaciones-de-acciones/spec02-08-fractional-shares/spec02-08.md) |
