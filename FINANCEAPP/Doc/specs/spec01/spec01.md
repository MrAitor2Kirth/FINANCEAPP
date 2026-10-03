# SPEC-01 · Gestión de empresas (alta, edición, baja y consulta)

| | |
|---|---|
| **Categoría** | 1. Acciones y empresas |
| **Identificador** | SPEC-01 |
| **Documento** | `spec01/spec01.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Catálogo de empresas cotizadas que el usuario sigue y en las que invierte.

## 1. Descripción funcional

Permite mantener el catálogo de empresas: darlas de alta con su ticker, sector y precio, editarlas, eliminarlas y consultarlas en un listado con indicadores resumen. Es la entidad principal sobre la que cuelgan las operaciones de acciones, el precio en vivo y el gráfico de cotización.

## 2. Pantallas y rutas

- `/companies` – listado de empresas con tarjetas resumen y tabla.
- `/companies/new` – formulario de alta.
- `/companies/edit/{Id}` – formulario de edición.
- Modal de confirmación al eliminar una empresa.

## 3. Reglas de negocio

- **Nombre** obligatorio, máximo 200 caracteres.
- **Ticker** obligatorio, máximo 20 caracteres y **único** en toda la base de datos (índice único `IX_CompanyDetails_Ticker_Unique`); el servicio ofrece `TickerExistsAsync` para validarlo, excluyendo la propia empresa al editar.
- **Sector** obligatorio, máximo 100; se elige de una lista: Tecnología, Salud, Financiero, Consumo, Energía, Industrial, Telecomunicaciones, Utilidades, Materiales, Inmobiliario y Seguros.
- **Precio actual** obligatorio, mayor que 0.
- **Precio en vivo** opcional; **Dividend yield** entre 0 y 100.
- Los campos Total invertido inicial, Número de acciones, Total valor invertido, Total dividendos y las dos rentabilidades se muestran en el formulario como **solo lectura**: se calculan a partir de las operaciones (ver SPEC-03).
- Al guardar una empresa se recalculan sus rentabilidades.
- Cada empresa ofrece un enlace de información externa a Investing.com construido con su ticker.
- **Eliminación en cascada**: el modal de confirmación avisa del número de operaciones asociadas; al confirmar se eliminan, en una única transacción, la empresa, todas sus operaciones (coincidencia exacta de nombre) y su histórico de cotización guardado.

## 4. Datos implicados

- `CompanyDetail`: Id, Name, Ticker, Sector, CurrentPrice, LivePrice, LivePriceUpdatedAt, DividendYield, TotalInvestedInitial, TotalDividends, PlusvaliaReturnPercent, DividendPlusvaliaReturnPercent, NumberOfShares, CreatedAt, UpdatedAt.
- Índices: Ticker (único) y Name.

## 5. Flujo principal

1. El usuario abre `/companies` y ve las empresas y las tarjetas resumen (sectores únicos, dividend yield medio, precio medio…).
2. Pulsa «Nueva empresa», completa el formulario y guarda; la app valida los campos y vuelve al listado.
3. Desde cada fila puede editar (botón naranja), eliminar (rojo, con confirmación) o abrir el gráfico (gris, ver SPEC-05).
4. Al confirmar la eliminación se borran la empresa, sus operaciones y su histórico de cotización, y se refresca el listado.

## 6. Componentes técnicos

- Páginas `CompaniesList.razor`, `CompanyForm.razor`.
- Servicio `ICompanyService` / `CompanyService`: `GetAllAsync`, `GetByIdAsync`, `GetByTickerAsync`, `GetBySectorAsync`, `SearchByNameAsync`, `GetByMinDividendYieldAsync`, `GetTopByDividendYieldAsync`, `GetAllSectorsAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync` (en cascada), `CountOperationsAsync`, `TickerExistsAsync`.
- Modelo `CompanyDetail` con anotaciones de validación.

## 7. Criterios de aceptación

- [ ] Se puede dar de alta una empresa con datos válidos y aparece en el listado.
- [ ] No se puede guardar sin nombre, ticker, sector o con precio ≤ 0.
- [ ] No se admiten dos empresas con el mismo ticker.
- [ ] Editar una empresa conserva sus operaciones asociadas.
- [ ] Eliminar pide confirmación, avisa de cuántas operaciones se borrarán y, si se cancela, no se borra nada.
- [ ] Tras eliminar una empresa no quedan operaciones ni cotizaciones huérfanas.

## 8. Observaciones y limitaciones detectadas

- Las operaciones se vinculan a la empresa por **nombre** (`CompanyName`), no por clave foránea; por eso la baja en cascada se hace desde el servicio. **Renombrar una empresa puede desvincular sus operaciones**: conviene no cambiar el nombre una vez registradas.
- La baja en cascada es definitiva: se recomienda hacer una copia de la base de datos antes de borrar empresas con muchas operaciones (SPEC-19).
- El listado no ofrece buscador ni filtros en pantalla aunque el servicio dispone de métodos de búsqueda por sector, nombre y dividend yield.
- La base de datos se crea con datos de ejemplo (MSFT, AAPL, KO, JNJ, PG) – ver SPEC-19.
