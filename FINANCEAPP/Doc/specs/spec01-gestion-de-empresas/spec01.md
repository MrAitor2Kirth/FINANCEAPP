# SPEC-01 · Gestión de empresas (alta, edición, baja y consulta)

| | |
|---|---|
| **Categoría** | 1. Acciones y empresas |
| **Identificador** | SPEC-01 |
| **Documento** | `spec01-gestion-de-empresas/spec01.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Catálogo de empresas cotizadas que el usuario sigue y en las que invierte.

## 1. Descripción funcional

Permite mantener el catálogo de empresas: darlas de alta con su ticker, sector y precio, editarlas, eliminarlas y consultarlas en un listado con indicadores resumen. Es la entidad principal sobre la que cuelgan las operaciones de acciones, el precio en vivo y el gráfico de cotización.

## 2. Pantallas y rutas

- `/companies` – listado de empresas con tarjetas resumen (total empresas, sectores únicos, dividend yield medio y total invertido) y tabla, con un botón «Lista extendible» / «Lista extendida» que alterna entre una tabla corta de una línea por empresa y el detalle completo.
- `/companies/new` – formulario de alta.
- `/companies/edit/{Id}` – formulario de edición.
- Modal de confirmación al eliminar una empresa.
- `/settings/countries` – catálogo de países (SPEC-01-02), desde Configuración.
- Tarjeta «Tipos de cambio» en `/settings` (SPEC-01-03): tipo actual de cada divisa en uso, «Actualizar» desde Yahoo, escritura a mano y «Actualizar todos».

## 3. Reglas de negocio

- **Nombre** obligatorio, máximo 200 caracteres.
- **Ticker** obligatorio, máximo 20 caracteres y **único** en toda la base de datos (índice único `IX_CompanyDetails_Ticker_Unique`); el servicio ofrece `TickerExistsAsync` para validarlo, excluyendo la propia empresa al editar.
- **Sector** obligatorio, máximo 100; se elige de una lista: Tecnología, Salud, Financiero, Consumo, Energía, Industrial, Telecomunicaciones, Utilidades, Materiales, Inmobiliario y Seguros.
- **Precio actual** obligatorio, mayor que 0.
- **Precio en vivo** opcional; **Dividend yield** entre 0 y 100.
- Los campos Total invertido inicial, Número de acciones, Precio medio de compra, Total valor invertido, Total dividendos y las dos rentabilidades se muestran en el formulario de edición como **solo lectura**: se calculan a partir de las operaciones (ver SPEC-03).
- **Precio medio de compra** = total invertido inicial ÷ número de acciones; no se guarda y es «–» si no hay acciones o el invertido es ≤ 0. El listado no muestra el precio actual guardado; en modo extendido muestra «Medio compra» y «En vivo».
- **Modo del listado**: empieza en «Lista extendible» (corto, una línea por empresa: nombre con ticker y sector, precio en vivo, valor actual, nº acciones —la 4ª cabecera pasa a «Nº acciones»— y rentabilidad por precio, sin etiquetas); al pulsar pasa a «Lista extendida» (precio medio de compra, en vivo, inicial, valor, nº acciones, dividendos y las tres rentabilidades). Afecta a todas las filas a la vez y solo dura mientras la pantalla está abierta.
- **País y divisa** (SPEC-01-02): el país es obligatorio al guardar (alta y edición; las empresas antiguas lo piden la primera vez que se editan) y se elige del catálogo, mostrado como «Nombre (código)», con la divisa resultante debajo. La divisa de la empresa es la de su país (EUR si no tiene). Los importes de la ficha y del listado llevan el símbolo de esa divisa (€, $, £, ¥ o el código) a través de `CurrencyFormatter`, y ya no hay prefijos «$» fijos. Al asignar país a una empresa sin país, sus operaciones sin divisa reciben la del país; cambiar a un país con otra divisa se rechaza si la empresa tiene operaciones.
- **Tipo de cambio** (SPEC-01-03): tabla `ExchangeRates` con el tipo actual de cada divisa respecto a la base (origen Yahoo o manual y fecha). El «Total invertido» de `/companies` y las partes de acciones del Dashboard suman todas las empresas, convertidas con ese tipo; las empresas en otra divisa sin tipo quedan fuera y una nota las cuenta con un enlace a Configuración. En `/companies` todas las empresas tienen un botón «Detalles» (invertido, valor actual y dividendos; en otra divisa, originales y convertidos con el tipo actual; en la divisa base, una sola columna). Las listas siguen mostrando cada empresa en su divisa.
- La tarjeta «Total invertido» suma solo las empresas cuya divisa es la divisa base (la del país base de Configuración) y muestra una nota con las empresas excluidas.
- **Empresas con inversión** (SPEC-01-05): una empresa tiene inversión si `TotalInvestedInitial` > 0. Las tarjetas «Total empresas» (con la nota «de M en el catálogo»), «Sectores únicos» y «Dividend yield promedio» (media simple, «—» sin empresas) solo cuentan esas empresas, y la nueva «Rentabilidad media» es la rentabilidad por precio ponderada por lo invertido, en divisa base (`InvestedCompaniesSummary`). El listado corto muestra solo las empresas con inversión; la lista extendida las muestra todas. «Total invertido» y el Dashboard no cambian.
- **Rentabilidad media con dividendos** (SPEC-01-07): tarjeta junto a «Rentabilidad media» con (valor actual + dividendos − invertido) ÷ invertido de las empresas con inversión, convertidos a la divisa base con el tipo actual (`InvestedCompaniesSummary.WeightedReturnWithDividendsPercent`). Es la media de la rentabilidad «Con div.» ponderada por lo invertido; los dividendos de empresas ya vendidas por completo no entran. La «Rentabilidad media» (por precio) no cambia.
- **Tarjetas de resumen** (SPEC-01-08): dos bloques. «Cartera»: Invertido (coste), Valor actual y Resultado (valor − invertido, `InvestedCompaniesSummary.UnrealizedResult`, con «+ X € en dividendos» debajo, sin sumarlos). «Rentabilidad y diversificación»: «Rentabilidad media» con la de precio en grande y la de dividendos debajo, «Empresas · sectores» y «Yield medio». Todo con los totales convertidos de las empresas con inversión; el aviso de empresas sin tipo de cambio sale una sola vez.
- **Filtro por periodo** (SPEC-01-09): Día · Mes · Año · Total histórico (como `/sales-history`) sobre las tarjetas; con un periodo, el listado corto muestra solo las empresas del periodo y sus cifras (SPEC-01-10). «Compras del periodo»: compras y traspasos abiertos con fecha en el periodo, valorados hoy con el tipo actual (las ya vendidas, en una nota; dividendos cobrados en el periodo aparte). «Cartera al cierre»: posición al último día del periodo (o hoy), precio de cierre de ese día y tipo de cambio de esa fecha del histórico de tipos; yield actual. Lógica en `IPortfolioPeriodService`. **Incluir vendidas** (SPEC-01-11): un selector «Cartera actual / Incluir vendidas» (se recuerda) suma lo vendido dentro de la vista: coste = importe de las compras cerradas por cada venta, cobrado neto = importe − comisión de la Venta, con los tipos guardados; en las tarjetas invertido + coste, valor + cobrado, dividendos de las vendidas incluidos; en la lista, chips «Parcial» / «Vendida» y las vendidas del todo al final y en otro color. Lógica en `IPortfolioPeriodService.GetSoldAsync` y `SoldPositionsSummary`.
- **Peso por empresa** (SPEC-01-06): en la lista extendida, la celda «Inversión» muestra «Peso» = inversión de la empresa (convertida a divisa base) ÷ suma de la inversión de las empresas con inversión × 100 (`InvestedCompaniesSummary.GetWeights`); «—» sin inversión o sin tipo de cambio. Es el peso que usa la rentabilidad media.
- Al guardar una empresa se recalculan sus rentabilidades.
- Cada empresa ofrece un enlace de información externa a Investing.com construido con su ticker.
- **Eliminación en cascada**: el modal de confirmación avisa del número de operaciones asociadas; al confirmar se eliminan, en una única transacción, la empresa, todas sus operaciones (coincidencia exacta de nombre) y su histórico de cotización guardado.

## 4. Datos implicados

- `CompanyDetail`: Id, Name, Ticker, Sector, CurrentPrice, LivePrice, LivePriceUpdatedAt, DividendYield, TotalInvestedInitial, TotalDividends, PlusvaliaReturnPercent, DividendPlusvaliaReturnPercent, NumberOfShares, CountryCode (texto, nulo en las antiguas), CreatedAt, UpdatedAt; calculadas no persistidas: TotalInvestedValue, AveragePurchasePrice y CurrencyCode (la rellena `CompanyService`).
- `Country` (tabla `Countries`): Code (clave), Name (único), Region, CurrencyCode, CreatedAt, UpdatedAt.
- Índices: Ticker (único) y Name.

## 5. Flujo principal

1. El usuario abre `/companies` y ve las empresas y las tarjetas resumen (sectores únicos, dividend yield medio, precio medio…).
2. Pulsa «Nueva empresa», completa el formulario y guarda; la app valida los campos y vuelve al listado.
3. Desde cada fila puede editar (botón naranja), eliminar (rojo, con confirmación) o abrir el gráfico (gris, ver SPEC-05).
4. Al confirmar la eliminación se borran la empresa, sus operaciones y su histórico de cotización, y se refresca el listado.

## 6. Componentes técnicos

- Páginas `CompaniesList.razor`, `CompanyForm.razor`.
- Servicio `ICompanyService` / `CompanyService`: `GetAllAsync`, `GetByIdAsync`, `GetByTickerAsync`, `GetBySectorAsync`, `SearchByNameAsync`, `GetByMinDividendYieldAsync`, `GetTopByDividendYieldAsync`, `GetAllSectorsAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync` (en cascada), `CountOperationsAsync`, `TickerExistsAsync`, `GetCountryChangeErrorAsync`.
- Países: `ICountryService` / `CountryService`, `CurrencyFormatter` y `CountriesList.razor`.
- Modelo `CompanyDetail` con anotaciones de validación.

## 7. Criterios de aceptación

- [ ] Se puede dar de alta una empresa con datos válidos y aparece en el listado.
- [ ] No se puede guardar sin nombre, ticker, sector o con precio ≤ 0.
- [ ] No se admiten dos empresas con el mismo ticker.
- [ ] Editar una empresa conserva sus operaciones asociadas.
- [ ] Eliminar pide confirmación, avisa de cuántas operaciones se borrarán y, si se cancela, no se borra nada.
- [ ] Tras eliminar una empresa no quedan operaciones ni cotizaciones huérfanas.
- [ ] El listado no muestra el precio actual; en modo extendido muestra el precio medio de compra («–» sin acciones) y el precio en vivo.
- [ ] El botón alterna «Lista extendible» / «Lista extendida» para todas las filas a la vez; en modo corto cada fila cabe en una línea y la 4ª cabecera dice «Nº acciones».
- [ ] La tarjeta «Total invertido» suma la inversión de todas las empresas.
- [ ] La ficha de edición muestra el precio medio de compra en solo lectura.
- [ ] No se guarda una empresa sin país; la ficha muestra la divisa del país elegido y los importes llevan su símbolo (SPEC-01-02).
- [ ] Cambiar el país a otra divisa con operaciones se rechaza con un mensaje y sin cambios.
- [ ] «Total invertido» solo suma las empresas de la divisa base y avisa de las excluidas.
- [ ] Las tarjetas «Total empresas», «Sectores únicos» y «Dividend yield promedio» solo cuentan las empresas con inversión > 0; el listado corto solo las muestra y la lista extendida muestra todas; la tarjeta «Rentabilidad media» pondera por lo invertido (SPEC-01-05).
- [ ] La lista extendida muestra el peso de cada empresa con inversión sobre el total invertido, y los pesos suman 100 % (SPEC-01-06).

## 8. Observaciones y limitaciones detectadas

- Las operaciones se vinculan a la empresa por **nombre** (`CompanyName`), no por clave foránea; por eso la baja en cascada se hace desde el servicio. **Renombrar una empresa puede desvincular sus operaciones**: conviene no cambiar el nombre una vez registradas.
- La baja en cascada es definitiva: se recomienda hacer una copia de la base de datos antes de borrar empresas con muchas operaciones (SPEC-19).
- El listado no ofrece buscador ni filtros en pantalla aunque el servicio dispone de métodos de búsqueda por sector, nombre y dividend yield.
- La base de datos se crea con datos de ejemplo (MSFT, AAPL, KO, JNJ, PG) – ver SPEC-19.

## 9. Features relacionadas

- [SPEC-02-06 · Movimientos bancarios al comprar acciones y cobrar dividendos](../spec02-operaciones-de-acciones/spec02-06-trade-bank-movements/spec02-06.md): al eliminar una empresa en cascada se eliminan también los movimientos bancarios de sus compras y dividendos y se recalculan los saldos; el modal indica cuántos desaparecen (estado: implementada y compilada, pendiente de probar a mano).
- [SPEC-01-01 · Precio medio de compra y lista extendida](spec01-01-average-price-extended-list/spec01-01.md): el listado muestra el precio medio de compra en lugar del precio actual, tiene un botón «Lista extendible» / «Lista extendida» que alterna entre una tabla corta y el detalle completo, la tarjeta «Precio promedio» pasa a «Total invertido» y la ficha muestra el precio medio de compra (estado: implementada, pendiente de compilar y probar a mano).
- [SPEC-01-02 · Países y divisa](spec01-02-countries-and-currency/spec01-02.md): catálogo de países (código, nombre, región y divisa) en `/settings/countries`, país obligatorio en la empresa, símbolo de la divisa en los importes, columna de divisa en las operaciones y país base en Configuración; el tipo de cambio queda para SPEC-01-03 (estado: implementada, pendiente de compilar y probar a mano).
- [SPEC-01-03 · Tipo de cambio y totales convertidos](spec01-03-exchange-rate/spec01-03.md): tipos de cambio de Yahoo o manuales en Configuración, tipo guardado en cada operación, totales de Empresas y Dashboard con las empresas en otra divisa convertidas y botón «Detalles» en operaciones, ventas y empresas (estado: implementada, pendiente de compilar y probar a mano).
- [SPEC-01-04 · Catálogo de empresas de mercado](spec01-04-market-catalog/spec01-04.md): base de datos propia con las empresas de IBEX 35, CAC 40, DAX 40, EURO STOXX 50, S&P 500, Dow Jones y Nasdaq-100 (nombre, ticker de Yahoo y país del mercado), buscador en el alta de empresa y pantalla de consulta con comprobación de tickers (estado: implementada, pendiente de compilar y probar a mano).
- [SPEC-01-05 · Resumen y listado de empresas con inversión](spec01-05-invested-companies-summary/spec01-05.md): las tarjetas «Total empresas», «Sectores únicos» y «Dividend yield promedio» y el listado corto solo cuentan las empresas con inversión > 0 (la lista extendida las muestra todas), y nueva tarjeta «Rentabilidad media» ponderada por la inversión (estado: implementada y compilada, pendiente de probar a mano).
- [SPEC-01-06 · Peso de cada empresa en la cartera](spec01-06-portfolio-weight/spec01-06.md): en la lista extendida, cada empresa muestra el porcentaje que pesa lo invertido en ella sobre el total invertido (el mismo peso que usa la rentabilidad media) (estado: implementada y compilada, pendiente de probar a mano).
- [SPEC-05-03 · Volumen, colores y marcas del gráfico](../spec05-grafico-de-cotizacion/spec05-03-chart-volume-and-marks/spec05-03.md): al eliminar una empresa se borran también sus marcas de tipo Empresa del gráfico en la misma transacción; las globales se conservan (estado: implementada y compilada, pendiente de probar a mano).
- [SPEC-21-01 · Calendario de dividendos](../spec21-dividend-calendar/spec21-01-dividend-calendar/spec21-01.md): campo «Próxima fecha de dividendo» en la ficha que crea una línea de dividendo sin alterar las anteriores, botón «Dividendos» en `/companies` hacia `/dividends` y, al eliminar una empresa, borrado de sus líneas de dividendo con aviso en el modal (estado: implementada y compilada, pendiente de probar a mano).
- [SPEC-02-08 · Número de acciones con decimales](../spec02-operaciones-de-acciones/spec02-08-fractional-shares/spec02-08.md): el número de acciones de la empresa (listado, lista extendida y ficha) admite 2 decimales y se muestra con los que tenga (estado: implementada y compilada, pendiente de probar a mano).
- [SPEC-01-07 · Rentabilidad media con dividendos](spec01-07-average-return-with-dividends/spec01-07.md): tarjeta nueva en `/companies` con la media ponderada contando los dividendos de las empresas con inversión (estado: implementada y compilada, pendiente de probar a mano).
- [SPEC-01-08 · Rediseño de las tarjetas de resumen: cartera y rentabilidad](spec01-08-summary-cards-redesign/spec01-08.md) — bloques «Cartera» (Invertido (coste), Valor actual y Resultado con los dividendos debajo) y «Rentabilidad y diversificación» (las dos rentabilidades en una tarjeta, Empresas · sectores y Yield medio), con rótulos de una línea; implementada y compilada, pendiente de probar a mano.
- [SPEC-01-09 · Filtro por periodo e histórico de tipos de cambio](spec01-09-period-summary/spec01-09.md) — filtro Día / Mes / Año / Total en las tarjetas con las vistas «Compras del periodo» y «Cartera al cierre», histórico diario de tipos de cambio y propuesta de los tipos que faltan; implementada y compilada, pendiente de probar a mano.
- [SPEC-01-10 · Listado de empresas por periodo](spec01-10-period-list/spec01-10.md) — con un periodo, el listado corto muestra solo las empresas del periodo y sus cifras. Implementada, pendiente de probar.
- [SPEC-01-11 · Incluir las empresas vendidas](spec01-11-include-sold-companies/spec01-11.md) — selector «Cartera actual / Incluir vendidas» en `/companies`: tarjetas y listado cuentan también lo vendido (coste de las compras cerradas, cobrado neto y resultado realizado con los tipos guardados) dentro del periodo que se mira, con chips «Vendida» / «Parcial»; implementada y compilada, pendiente de probar a mano.
