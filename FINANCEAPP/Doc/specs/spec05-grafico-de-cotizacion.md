# SPEC-05 · Gráfico de cotización con operaciones superpuestas

| | |
|---|---|
| **Categoría** | 1. Acciones y empresas |
| **Identificador** | SPEC-05 |
| **Documento** | `spec05-grafico-de-cotizacion.md` |
| **Volver al índice** | [README de specs](README.md) |

> Gráfico histórico por empresa en granularidad diaria, mensual o anual.

## 1. Descripción funcional

Abre, desde el listado de empresas, un modal con el gráfico de cotización histórico de la empresa. Superpone los puntos de compra del usuario y la línea del precio actual, y permite cambiar entre vista diaria, mensual y anual.

## 2. Pantallas y rutas

- `/companies` – botón gris «gráfico» en cada fila, que abre el modal `CompanyChartModal`.

## 3. Reglas de negocio

- Granularidades: **Diaria** (por defecto al abrir), **Mensual** y **Anual**; al cambiar de granularidad se vuelve a dibujar.
- El histórico diario OHLC se descarga de Yahoo Finance (`range=5y`) y se **cachea en SQLite** (tabla `PriceHistoryPoints`).
- La caché se considera **fresca durante 12 horas**; pasado ese tiempo se vuelve a consultar. Si la consulta no devuelve datos, se sirve lo que haya en caché.
- Los puntos existentes se actualizan por fecha y los nuevos se insertan (índice único empresa + fecha).
- La agregación **mensual y anual** se calcula en el servidor a partir de los puntos diarios: apertura del primer día, cierre del último, máximo, mínimo y volumen acumulado.
- Se superponen como marcadores solo las operaciones cuyo tipo contiene «Compra», con tooltip de fecha, precio y número de acciones.
- Se dibuja una línea con el precio actual (en vivo si existe, si no el manual).
- Si no hay datos, el modal lo indica en lugar de mostrar un gráfico vacío.

## 4. Datos implicados

- `PriceHistoryPoint`: Id, CompanyId (FK a CompanyDetail), Date, Open, High, Low, Close, Volume, FetchedAt.
- Tabla creada por `DatabaseSchemaUpgrader.EnsurePriceHistoryTable`, con índice único (CompanyId, Date).

## 5. Flujo principal

1. El usuario pulsa el botón gris de una empresa.
2. `CompanyChartModal` pide la serie a `PriceHistoryService` y las compras a `OperationService`.
3. `PriceHistoryService` refresca la caché si es necesario y devuelve los puntos en la granularidad pedida.
4. Los datos se serializan a JSON y se pasan por JS interop a `companyChart.render` (ApexCharts).
5. El usuario cambia a Mensual o Anual y el gráfico se redibuja.

## 6. Componentes técnicos

- `CompanyChartModal.razor`, `wwwroot/js/companyChart.js` (ApexCharts, anotaciones de punto y tooltip).
- `IPriceHistoryService.GetHistoryAsync` / `PriceHistoryService`.
- `IStockPriceService.GetDailyHistoryAsync`, enumeración `ChartGranularity` (Daily, Monthly, Yearly).

## 7. Criterios de aceptación

- [ ] El modal abre con vista diaria y muestra la serie histórica.
- [ ] Cambiar a Mensual y Anual reduce el número de puntos.
- [ ] Las compras aparecen como marcadores con su tooltip.
- [ ] En una segunda apertura dentro de 12 horas no se vuelve a llamar a Yahoo.
- [ ] Sin datos se muestra un mensaje informativo.

## 8. Observaciones y limitaciones detectadas

- ApexCharts se carga desde CDN (`cdn.jsdelivr.net`), por lo que sin Internet el gráfico no se dibuja; conviene empaquetarlo en `wwwroot/lib`.
- El histórico se descarga siempre con rango de 5 años; no se pueden consultar periodos más largos.
- El gráfico dibuja el precio de cierre; los valores OHLC completos se guardan pero no se visualizan como velas.
