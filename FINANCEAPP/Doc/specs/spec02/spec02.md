# SPEC-02 · Registro y gestión de operaciones de acciones

| | |
|---|---|
| **Categoría** | 1. Acciones y empresas |
| **Identificador** | SPEC-02 |
| **Documento** | `spec02/spec02.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Histórico de compras, ventas, dividendos y traspasos por empresa.

## 1. Descripción funcional

Permite registrar cada movimiento de la cartera de acciones y consultarlos en una tabla única con edición en línea. Es la fuente de datos de la que se obtienen la inversión, las acciones en cartera y los dividendos de cada empresa.

## 2. Pantallas y rutas

- `/operations` – listado de operaciones con alta, edición en línea y eliminación. Incluye el botón «Histórico de ventas».
- `/sales-history` – histórico de ventas de acciones con el beneficio de cada una (SPEC-02-01).
- Menú lateral: «Operaciones Acciones».

## 3. Reglas de negocio

- **Tipos de operación**: Compra, Venta, Dividendo y Traspaso.
- Campos: empresa (desplegable con las empresas existentes), tipo, fecha de apertura, fecha de cierre (opcional), precio por acción, número de acciones, comisiones e importe.
- **Importe de Compra y Venta** (SPEC-02-02): se calcula con `OperationAmountCalculator` como precio base × nº de acciones + comisión, donde el precio base es el precio por acción si está informado (manda) y, si no, el precio en vivo. Se recalcula al cambiar precio, precio en vivo, acciones o comisión (también si se había escrito a mano) y siempre es editable. Si solo hay precio en vivo, se copia al precio por acción al guardar. En **Dividendo y Traspaso** el importe se informa siempre a mano.
- Validaciones: empresa obligatoria (máx. 200), tipo obligatorio (máx. 50), fecha obligatoria, comisión ≥ 0 e importe entre 0,01 y 999.999.999,99. Compra y Venta exigen además precio base > 0 y acciones enteras ≥ 1.
- Una operación está **Abierta** si no tiene fecha de cierre y **Cerrada** si la tiene; el estado se muestra como insignia.
- **Filtro por empresa** desde un menú en la cabecera de la columna Empresa; muestra el aviso «Filtro activo» y un botón para limpiarlo. Los contadores de operaciones abiertas y cerradas respetan el filtro.
- Al seleccionar una empresa en el alta se refresca su precio en vivo y se muestra en la fila.
- Orden por defecto: fecha descendente.
- **Una Venta nueva se registra con `RegisterSaleAsync`** (SPEC-02-01): exige empresa existente, precio por acción > 0 y acciones > 0, y cierra por **FIFO** (fecha e Id) las compras y traspasos abiertos de la empresa con la fecha de la venta. La propia Venta también queda cerrada.
- Si una compra se cierra solo en parte, se **divide en dos filas**: una cerrada con las acciones vendidas y otra abierta duplicada con las pendientes. Importe y comisión se reparten proporcionalmente y la parte pendiente se queda con el resto del redondeo.
- Se rechaza la venta, sin cambiar nada, si las acciones a vender superan las disponibles (compras abiertas menos acciones de ventas antiguas que sigan abiertas). Todo ocurre en una transacción.
- **Histórico de ventas** (`SalesHistory`): por cada venta se guarda el ticker, las acciones, el precio medio ponderado de compra, el precio de venta, el importe cobrado, las comisiones (la de la venta más la parte proporcional de las compras cerradas) y el beneficio = importe − coste medio × acciones − comisiones. El precio de venta es el precio por acción de la Venta si está informado y, si no, el «Precio en vivo» de la fila.
- El precio de venta del histórico es editable y recalcula importe y beneficio; el resto del beneficio no se recalcula solo. Borrar un registro del histórico o una Venta **no reabre** las compras cerradas. Las ventas anteriores a la feature no se tocan y no tienen registro.
- Los errores de validación y de acciones insuficientes se muestran en pantalla.
- Cada alta, edición o baja **recalcula automáticamente** la posición de la empresa (SPEC-03); si al editar se cambia de empresa se recalculan ambas.

## 4. Datos implicados

- `SaleHistory` (tabla `SalesHistory`, creada solo desde `DatabaseSchemaUpgrader`): Id, OperationId (sin clave foránea), CompanyName, Ticker, SaleDate, NumberOfShares, AverageBuyPrice, SellPrice, Amount, Commission, Profit, CreatedAt, UpdatedAt. Índices por empresa y fecha.
- `Operation`: Id, CompanyName, Date, CloseDate, PricePerShare, NumberOfShares, Commission, Amount, Type, CreatedAt (calculados: IsClosed, Status).
- Índices en CompanyName, Date y Type.

## 5. Flujo principal

1. El usuario pulsa «Nueva operación»; se abre una fila editable en la tabla.
2. Elige empresa y tipo, introduce fecha, precio, acciones y comisión; el importe se calcula.
3. Guarda; la operación se añade y la empresa se recalcula.
4. Para modificar, pulsa el botón de editar de la fila (edición en línea). Para borrar, confirma en el modal.

## 6. Componentes técnicos

- Páginas `OperationsList.razor` y `SalesHistoryList.razor`.
- `OperationAmountCalculator` (clase estática en `Services/`): `IsAutoCalculated`, `ResolveBasePrice`, `Calculate` y `Validate`.
- Servicio `ISaleHistoryService` / `SaleHistoryService`: `GetAllAsync`, `GetByIdAsync`, `UpdateSellPriceAsync`, `DeleteAsync`.
- `IOperationService.RegisterSaleAsync` y `SaleRegistrationResult`.
- Servicio `IOperationService` / `OperationService`: `GetAllAsync`, `GetByIdAsync`, `GetByCompanyAsync`, `GetByTypeAsync`, `GetOpenOperationsAsync`, `GetClosedOperationsAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `GetTotalOpenAmountAsync`, `GetOperationCountByTypeAsync`.
- Colaboración con `ICompanyService.RecalculateTotalInvestedAsync` y `RefreshLivePriceAsync`.

## 7. Criterios de aceptación

- [ ] Se puede crear una operación de cada tipo y aparece ordenada por fecha.
- [ ] El importe de Compra y Venta se autocalcula (manda el precio por acción sobre el precio en vivo) y se puede sobrescribir; al cambiar precio, acciones o comisión se vuelve a calcular.
- [ ] Dividendo y Traspaso no recalculan el importe.
- [ ] Filtrar por empresa reduce la tabla y los contadores; limpiar el filtro la restaura.
- [ ] Tras guardar o borrar, los totales de la empresa se actualizan.
- [ ] No se guarda con importe fuera de rango ni sin empresa o tipo, ni una Compra o Venta sin precio base o acciones.
- [ ] Una Venta nueva cierra las compras por FIFO, se cierra ella misma y crea un registro en `/sales-history`.
- [ ] En una venta parcial, la última compra se divide en cerrada y abierta y la suma de importes y comisiones coincide con la original.
- [ ] Vender más acciones de las abiertas se rechaza con un mensaje y sin cambios.
- [ ] Editar el precio de venta en el histórico recalcula el beneficio.
- [ ] Borrar una Venta o un registro del histórico avisa de que las compras no se reabren.

## 8. Observaciones y limitaciones detectadas

- `GetByCompanyAsync` filtra con «contiene», por lo que una empresa cuyo nombre esté contenido en otro puede mezclar resultados.
- El estado Abierta/Cerrada depende de la fecha de cierre. Las Ventas nuevas cierran las compras por FIFO, pero las anteriores a SPEC-02-01 siguen abiertas.
- No hay forma de deshacer una venta registrada (reabrir compras): queda como mejora futura.
- Los tipos de operación son cadenas de texto libres en base de datos, no una enumeración.

## 9. Features relacionadas

| Feature | Estado | Documentos |
|---|---|---|
| [SPEC-02-01 · Histórico de ventas de acciones](spec02-01-sales-history/spec02-01.md) | Implementada, pendiente de probar a mano | [spec](spec02-01-sales-history/spec02-01.md) · [plan](spec02-01-sales-history/plan02-01.md) · [tareas](spec02-01-sales-history/task02-01.md) |
| [SPEC-02-02 · Cálculo del importe total de una operación](spec02-02-total-amount-calculation/spec02-02.md) | Implementada, pendiente de probar a mano | [spec](spec02-02-total-amount-calculation/spec02-02.md) |
