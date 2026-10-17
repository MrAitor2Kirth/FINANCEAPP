# SPEC-04 · Precio en vivo desde Yahoo Finance

| | |
|---|---|
| **Categoría** | 1. Acciones y empresas |
| **Identificador** | SPEC-04 |
| **Documento** | `spec04-precio-en-vivo/spec04.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Consulta de la cotización actual de cada empresa por su ticker.

## 1. Descripción funcional

Obtiene el último precio de cada empresa desde Yahoo Finance y lo guarda en la propia empresa, de modo que la cartera se valore con el precio de hoy. Incluye una resolución automática de tickers europeos mediante sufijos de bolsa.

## 2. Pantallas y rutas

- `/companies` – botón «Actualizar Precios en Vivo» y columna Precio en Vivo con la hora relativa de la última actualización.
- `/operations` – columna Precio en Vivo y refresco individual al elegir empresa en una operación.
- Gráfico de cotización (`CompanyChartModal`, SPEC-05-01) – visor en vivo que se refresca cada 30 s con el mismo endpoint y guarda el precio en la empresa.

## 3. Reglas de negocio

- La actualización masiva es **manual**: solo se ejecuta cuando el usuario pulsa el botón; no hay refresco automático al abrir la pantalla.
- Se consulta `https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?interval=1d&range=1d` y se lee `regularMarketPrice`.
- Todas las peticiones llevan cabecera `User-Agent` (Yahoo rechaza las que no la tienen) y tienen un tiempo máximo de **10 segundos**.
- Si el ticker «plano» (sin punto) no devuelve precio, se prueban en orden los sufijos de bolsa: `.MC`, `.PA`, `.MI`, `.DE`, `.AS`, `.BR`, `.L`, `.LS`, `.SW`.
- Si hay precio: se guardan `LivePrice` y `LivePriceUpdatedAt`. Si falla: se informa «No se pudo obtener el precio en vivo» (`LivePriceError`, no se guarda en base de datos) y se conserva el último precio válido.
- Solo se actualizan las empresas con ticker informado; los tickers repetidos se consultan una única vez.
- El botón se deshabilita mientras la actualización está en curso.
- El valor de cartera usa el precio en vivo y, si no existe, el precio actual manual.
- Desde el gráfico de cotización (SPEC-05-01) se usa `StockPriceService.GetQuoteAsync`, que lee además cierre anterior, máximo y mínimo del día, hora del precio y sesión regular, y `CompanyService.RefreshLiveQuoteAsync`, que solo guarda precio y hora si hay precio.

## 4. Datos implicados

- `CompanyDetail.LivePrice`, `LivePriceUpdatedAt`, `LivePriceError` (no persistido).
- Esquema ampliado por `DatabaseSchemaUpgrader.EnsureCompanyDetailLivePriceColumns`.

## 5. Flujo principal

1. El usuario pulsa «Actualizar Precios en Vivo».
2. `CompanyService.RefreshAllLivePricesAsync` reúne los tickers distintos y llama a `StockPriceService.GetLatestPricesAsync`.
3. Por cada ticker se consulta Yahoo, con la prueba de sufijos si hace falta.
4. Se guardan los precios y se recarga el listado con la hora de actualización.

## 6. Componentes técnicos

- `IStockPriceService.GetLatestPriceAsync`, `GetLatestPricesAsync` / `StockPriceService`.
- `ICompanyService.RefreshLivePriceAsync`, `RefreshAllLivePricesAsync`, `RefreshLiveQuoteAsync` (SPEC-05-01).
- `IStockPriceService.GetQuoteAsync` y `StockQuote` (SPEC-05-01).
- `HttpClient` tipado registrado con `AddHttpClient`.

## 7. Criterios de aceptación

- [ ] Un ticker válido muestra su precio y la hora de actualización.
- [ ] Un ticker europeo sin sufijo (por ejemplo ACS) se resuelve con el sufijo adecuado.
- [ ] Si Yahoo no responde, la app no se bloquea y conserva el precio anterior.
- [ ] Pulsar el botón no permite lanzar dos actualizaciones a la vez.

## 8. Observaciones y limitaciones detectadas

- Los tickers se consultan **de forma secuencial**, por lo que con muchas empresas la actualización es lenta.
- Depende de un endpoint no oficial de Yahoo Finance que puede cambiar o limitar peticiones.
- `RefreshLivePriceAsync` pone `LivePriceUpdatedAt` a la hora actual también cuando falla, así que el listado puede mostrar una hora reciente sin precio nuevo. `RefreshLiveQuoteAsync` (gráfico) no lo hace; corregir el primero queda pendiente.
- Requiere conexión a Internet; sin ella se mantiene el último precio guardado.
