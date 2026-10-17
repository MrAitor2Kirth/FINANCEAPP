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

## 8. Observaciones y limitaciones detectadas

- Las operaciones se vinculan a la empresa por **nombre** (`CompanyName`), no por clave foránea; por eso la baja en cascada se hace desde el servicio. **Renombrar una empresa puede desvincular sus operaciones**: conviene no cambiar el nombre una vez registradas.
- La baja en cascada es definitiva: se recomienda hacer una copia de la base de datos antes de borrar empresas con muchas operaciones (SPEC-19).
- El listado no ofrece buscador ni filtros en pantalla aunque el servicio dispone de métodos de búsqueda por sector, nombre y dividend yield.
- La base de datos se crea con datos de ejemplo (MSFT, AAPL, KO, JNJ, PG) – ver SPEC-19.

## 9. Features relacionadas

- [SPEC-01-01 · Precio medio de compra y lista extendida](spec01-01-average-price-extended-list/spec01-01.md): el listado muestra el precio medio de compra en lugar del precio actual, tiene un botón «Lista extendible» / «Lista extendida» que alterna entre una tabla corta y el detalle completo, la tarjeta «Precio promedio» pasa a «Total invertido» y la ficha muestra el precio medio de compra (estado: implementada, pendiente de compilar y probar a mano).
- [SPEC-01-02 · Países y divisa](spec01-02-countries-and-currency/spec01-02.md): catálogo de países (código, nombre, región y divisa) en `/settings/countries`, país obligatorio en la empresa, símbolo de la divisa en los importes, columna de divisa en las operaciones y país base en Configuración; el tipo de cambio queda para SPEC-01-03 (estado: implementada, pendiente de compilar y probar a mano).
- [SPEC-01-03 · Tipo de cambio y totales convertidos](spec01-03-exchange-rate/spec01-03.md): tipos de cambio de Yahoo o manuales en Configuración, tipo guardado en cada operación, totales de Empresas y Dashboard con las empresas en otra divisa convertidas y botón «Detalles» en operaciones, ventas y empresas (estado: implementada, pendiente de compilar y probar a mano).
- [SPEC-01-04 · Catálogo de empresas de mercado](spec01-04-market-catalog/spec01-04.md): base de datos propia con las empresas de IBEX 35, CAC 40, DAX 40, EURO STOXX 50, S&P 500, Dow Jones y Nasdaq-100 (nombre, ticker de Yahoo y país del mercado), buscador en el alta de empresa y pantalla de consulta con comprobación de tickers (estado: implementada, pendiente de compilar y probar a mano).
