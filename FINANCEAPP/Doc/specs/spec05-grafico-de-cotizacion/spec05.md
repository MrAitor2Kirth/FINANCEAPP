# SPEC-05 · Gráfico de cotización con operaciones superpuestas

| | |
|---|---|
| **Categoría** | 1. Acciones y empresas |
| **Identificador** | SPEC-05 |
| **Documento** | `spec05-grafico-de-cotizacion/spec05.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Gráfico histórico por empresa en granularidad diaria, mensual o anual, con precio en vivo y compra o venta desde el propio gráfico (SPEC-05-01).

## 1. Descripción funcional

Abre, desde el listado de empresas, un modal con el gráfico de cotización histórico de la empresa. Muestra un visor del precio en vivo que se refresca solo, permite comprar o vender con un mini-formulario, marca las compras, traspasos, ventas y dividendos del usuario y traza la línea del precio actual. Se puede cambiar entre vista diaria, mensual y anual, y activar capas opcionales: velas japonesas, volumen y precio medio con resultado.

## 2. Pantallas y rutas

- `/companies` – botón gris «Ver gráfico de cotización» en cada fila, que abre el modal `CompanyChartModal`. Al cerrar el modal, la lista se recarga.

## 3. Reglas de negocio

### Histórico

- Granularidades: **Diaria** (por defecto al abrir), **Mensual** y **Anual**; al cambiar de granularidad se vuelve a consultar el histórico y a dibujar.
- El histórico diario OHLC se descarga de Yahoo Finance (`range=5y`) y se **cachea en SQLite** (tabla `PriceHistoryPoints`).
- La caché se considera **fresca durante 12 horas**; pasado ese tiempo se vuelve a consultar. Si la consulta no devuelve datos, se sirve lo que haya en caché y se avisa.
- «Recargar histórico» ignora la caché de 12 horas y vuelve a descargar; si falla, conserva la caché y avisa.
- Los puntos existentes se actualizan por fecha y los nuevos se insertan (índice único empresa + fecha).
- La agregación **mensual y anual** se calcula en el servidor a partir de los puntos diarios: apertura del primer día, cierre del último, máximo, mínimo y volumen acumulado.

### Precio en vivo

- Panel con el último precio de Yahoo (precio único de referencia: Yahoo no da precio de compra y de venta separados), variación frente al cierre anterior, máximo y mínimo del día, hora del precio y estado del mercado.
- Se refresca cada **30 segundos** mientras el modal está abierto y con el botón «Actualizar precio»; nunca hay dos consultas a la vez. Al cerrar el modal se detiene.
- Cada precio obtenido se guarda en la empresa (`LivePrice`, `LivePriceUpdatedAt`). Si falla, se conserva el último precio y su hora y se avisa.
- El símbolo resuelto (p. ej. `ACS.MC`) se reutiliza en los refrescos para no repetir la prueba de sufijos.
- Estado del mercado según la sesión regular de Yahoo (`currentTradingPeriod.regular`): Abierto, Cerrado o Desconocido (si Yahoo no la da).
- La línea «Precio actual» y, en vista diaria, el punto de hoy siguen al precio en vivo (cierre provisional, no se guarda en la caché).

### Comprar y vender desde el gráfico

- «Comprar» y «Vender» abren un mini-formulario: fecha (hoy, editable, no futura), acciones, precio por acción, comisión, tipo de cambio (solo si la empresa no es de la divisa base) e importe calculado con `OperationAmountCalculator` (solo lectura).
- El precio del formulario **sigue al precio en vivo** hasta confirmar; si se escribe a mano deja de seguirlo y aparece «Volver al precio en vivo».
- Bloqueo (lo decide `OperationService.GetQuickTradeBlockReason` y se revalida al confirmar): fecha futura; fecha de hoy con mercado **Cerrado**; Venta sin acciones disponibles. Con fecha anterior a hoy no se exige mercado abierto. Con mercado Desconocido se permite con aviso.
- Compra → `OperationService.AddAsync`; Venta → `OperationService.RegisterSaleAsync` (FIFO e histórico de ventas). Ambas pasan por `RegisterQuickTradeAsync`.
- Acciones disponibles: lotes abiertos de Compra y Traspaso menos Ventas antiguas abiertas (misma cuenta que `RegisterSaleAsync`).

### Marcadores y capas

- Compras y traspasos (tipo literal `"Compra"`/`"Traspaso"`, precio > 0), agrupados por lote: las filas partidas por una venta parcial (`OriginOperationId`) se ven como un solo marcador con las acciones sumadas.
- Ventas en rojo y dividendos en verde (sobre el cierre de ese día), con leyenda.
- Importes en la **divisa de la empresa** en eje, tooltips y etiquetas.
- Capas opcionales, apagadas por defecto y recordadas en `AppSettings` (`CompanyChart.ShowAverage`, `CompanyChart.ShowCandles`, `CompanyChart.ShowVolume`): precio medio y resultado latente (deshabilitada sin acciones), velas japonesas y volumen (segundo gráfico sincronizado en zoom).
- Si no hay datos, el modal distingue: sin histórico para el ticker, sin conexión con Yahoo y librería de gráficos no cargada.

## 4. Datos implicados

- `PriceHistoryPoint`: Id, CompanyId (FK a CompanyDetail), Date, Open, High, Low, Close, Volume, FetchedAt.
- Tabla creada por `DatabaseSchemaUpgrader.EnsurePriceHistoryTable`, con índice único (CompanyId, Date).
- `CompanyDetail.LivePrice` y `LivePriceUpdatedAt` (escritos por el refresco del gráfico).
- `Operations` y `SalesHistory` (operaciones creadas desde el gráfico).
- `AppSettings`: tres claves `CompanyChart.*` con `"true"`/`"false"`. Sin cambios de esquema.

## 5. Flujo principal

1. El usuario pulsa el botón gris de una empresa.
2. `CompanyChartModal` (con el `DbContext` compartido de la app y un semáforo para no solapar consultas) carga la empresa, las preferencias, la divisa base, los marcadores, las acciones disponibles y el histórico.
3. Dibuja con `companyChart.render` y pide la primera cotización (`CompanyService.RefreshLiveQuoteAsync`); arranca el refresco cada 30 s, que llama a `companyChart.updateLive` sin redibujar.
4. El usuario pulsa Comprar o Vender, ajusta el formulario y confirma; `OperationService.RegisterQuickTradeAsync` registra la operación y el gráfico recarga marcadores y posición.
5. Al cerrar, se detiene el refresco, se destruye el gráfico, se espera a que termine la consulta en curso y `/companies` recarga la lista.

## 6. Componentes técnicos

- `CompanyChartModal.razor` (`DbContext` compartido, `SemaphoreSlim` para el acceso a datos, `PeriodicTimer`, `IAsyncDisposable`).
- `wwwroot/js/companyChart.js` (ApexCharts: `render`, `updateLive`, `destroy`; línea o velas, anotaciones, volumen agrupado).
- `IStockPriceService.GetQuoteAsync` / `StockQuote`, `MarketState` (`Services/StockQuote.cs`).
- `ICompanyService.RefreshLiveQuoteAsync`.
- `IOperationService.GetAvailableSharesAsync`, `GetQuickTradeBlockReason`, `RegisterQuickTradeAsync`, `GetChartMarkersAsync` (`Services/QuickTrade.cs`).
- `IAppSettingsService.GetChartPreferencesAsync` / `SaveChartPreferencesAsync` (`ChartPreferences`).
- `IPriceHistoryService.GetHistoryAsync` y `GetHistoryWithStatusAsync` (recarga forzada), enumeración `ChartGranularity`.

## 7. Criterios de aceptación

- [ ] El modal abre con vista diaria y muestra la serie histórica.
- [ ] Cambiar a Mensual y Anual reduce el número de puntos.
- [ ] Compras, traspasos, ventas y dividendos aparecen como marcadores con su tooltip y en la divisa de la empresa.
- [ ] En una segunda apertura dentro de 12 horas no se vuelve a llamar a Yahoo para el histórico; «Recargar histórico» sí lo hace.
- [ ] Sin datos se muestra un mensaje informativo que distingue la causa.
- [ ] El precio en vivo se refresca cada ~30 s y se guarda en la empresa.
- [ ] Una compra desde el gráfico aparece en `/operations` y una venta en `/sales-history`.
- [ ] Con el mercado cerrado y fecha de hoy no se puede confirmar; con fecha anterior sí.
- [ ] Velas, volumen y precio medio se activan y se recuerdan al reiniciar.

Criterios detallados en [SPEC-05-01](spec05-01-chart-live-trading/spec05-01.md).

## 8. Observaciones y limitaciones detectadas

- ApexCharts se carga desde CDN (`cdn.jsdelivr.net`) **sin versión fija**, por lo que sin Internet el gráfico no se dibuja y una versión mayor nueva podría romperlo; conviene empaquetarlo en `wwwroot/lib` y fijar la versión (fuera de alcance en SPEC-05-01).
- El histórico se descarga siempre con rango de 5 años; no se pueden consultar periodos más largos.
- Yahoo da las cotizaciones europeas con unos 15 minutos de retraso; la hora del precio lo deja ver.
- El estado del mercado depende de la sesión que informa Yahoo; en festivos puede no ser exacto.

## 9. Features relacionadas

| Feature | Estado | Documento |
|---|---|---|
| SPEC-05-01 · Precio en vivo y compra/venta desde el gráfico, correcciones y capas opcionales (`chart-live-trading`) | Implementada, pendiente de compilar y probar a mano | [spec05-01.md](spec05-01-chart-live-trading/spec05-01.md) · [plan](spec05-01-chart-live-trading/plan05-01.md) · [tareas](spec05-01-chart-live-trading/task05-01.md) |
