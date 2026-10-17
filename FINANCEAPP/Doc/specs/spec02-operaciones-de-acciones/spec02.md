# SPEC-02 · Registro y gestión de operaciones de acciones

| | |
|---|---|
| **Categoría** | 1. Acciones y empresas |
| **Identificador** | SPEC-02 |
| **Documento** | `spec02-operaciones-de-acciones/spec02.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Histórico de compras, ventas, dividendos y traspasos por empresa.

## 1. Descripción funcional

Permite registrar cada movimiento de la cartera de acciones y consultarlos en una tabla única con edición en línea. Es la fuente de datos de la que se obtienen la inversión, las acciones en cartera y los dividendos de cada empresa.

## 2. Pantallas y rutas

- `/operations` – listado de operaciones con alta, edición en línea y eliminación. Incluye el botón «Histórico de ventas».
- `/sales-history` – histórico de ventas de acciones con el beneficio de cada una (SPEC-02-01).
- Menú lateral: «Operaciones Acciones».
- Gráfico de cotización de `/companies` (SPEC-05-01): botones Comprar y Vender con un mini-formulario.

## 3. Reglas de negocio

- **Tipos de operación**: Compra, Venta, Dividendo y Traspaso.
- Campos: empresa (desplegable con las empresas existentes), tipo, fecha de apertura, fecha de cierre (opcional), precio por acción, número de acciones, comisiones e importe.
- **Importe de Compra y Venta** (SPEC-02-02): se calcula con `OperationAmountCalculator` como precio base × nº de acciones + comisión, donde el precio base es el precio por acción si está informado (manda) y, si no, el precio en vivo. Se recalcula al cambiar precio, precio en vivo, acciones o comisión (también si se había escrito a mano) y siempre es editable. Si solo hay precio en vivo, se copia al precio por acción al guardar. En **Dividendo y Traspaso** el importe se informa siempre a mano.
- Validaciones: empresa obligatoria (máx. 200), tipo obligatorio (máx. 50), fecha obligatoria, comisión ≥ 0 e importe entre 0,01 y 999.999.999,99. Compra y Venta exigen además precio base > 0 y acciones enteras ≥ 1.
- Una operación está **Abierta** si no tiene fecha de cierre y **Cerrada** si la tiene; el estado se muestra como insignia. En modo edición la columna Estado es un desplegable: «Cerrada» pone la fecha de cierre de hoy si no había ninguna (editable en «Fecha Cierre») y «Abierta» la borra. Al guardar se recalcula la empresa. Las operaciones ligadas a una venta del histórico (compras o traspasos que cerró, y la propia Venta) **se pueden editar con aviso**: el beneficio guardado de la venta no cambia y, si se reabre una compra o traspaso, se desvincula de la venta (`SaleHistoryId` vacío) y «Deshacer venta» ya no la toca. **Eliminarlas** sigue bloqueado: hay que deshacer la venta.
- **Filtro por empresa** desde un menú en la cabecera de la columna Empresa; muestra el aviso «Filtro activo» y un botón para limpiarlo. Los contadores de operaciones abiertas y cerradas respetan el filtro.
- Al seleccionar una empresa en el alta se refresca su precio en vivo y se muestra en la fila.
- **Compra y Venta desde el gráfico** (SPEC-05-01): pasan por `OperationService.RegisterQuickTradeAsync`, que revalida el bloqueo (fecha futura, fecha de hoy con mercado cerrado, venta sin acciones), calcula el importe con `OperationAmountCalculator` y guarda con `AddAsync` (Compra) o `RegisterSaleAsync` (Venta).
- Orden por defecto: fecha descendente.
- **Una Venta nueva se registra con `RegisterSaleAsync`** (SPEC-02-01): exige empresa existente, precio por acción > 0 y acciones > 0, y cierra por **FIFO** (fecha e Id) las compras y traspasos abiertos de la empresa con la fecha de la venta. La propia Venta también queda cerrada.
- Si una compra se cierra solo en parte, se **divide en dos filas**: una cerrada con las acciones vendidas y otra abierta duplicada con las pendientes. Importe y comisión se reparten proporcionalmente y la parte pendiente se queda con el resto del redondeo.
- Se rechaza la venta, sin cambiar nada, si las acciones a vender superan las disponibles (compras abiertas menos acciones de ventas antiguas que sigan abiertas). Todo ocurre en una transacción.
- **Histórico de ventas** (`SalesHistory`): por cada venta se guarda el ticker, las acciones, el precio medio ponderado de compra, el precio de venta, el importe cobrado, las comisiones (la de la venta más la parte proporcional de las compras cerradas) y el beneficio = importe − coste medio × acciones − comisiones. El precio de venta es el precio por acción de la Venta si está informado y, si no, el «Precio en vivo» de la fila.
- El precio de venta del histórico es editable y recalcula importe y beneficio; el resto del beneficio no se recalcula solo. **Deshacer una venta** (SPEC-02-03) desde `/sales-history`: reabre las compras que cerró (usan `Operation.SaleHistoryId`, que se limpia junto con la fecha de cierre), vuelve a unir las filas de un lote partido (`OriginOperationId`) con importe y comisión exactos, elimina la operación Venta y el registro, y recalcula la empresa, todo en una transacción y en cualquier orden aunque haya ventas posteriores.
- Las operaciones **cerradas por una venta** (compras y traspasos con `SaleHistoryId` y Ventas con registro) no se pueden editar ni eliminar desde `/operations`: se muestra un aviso que remite al histórico. Las ventas anteriores a la feature no se tocan y no tienen registro.
- **Divisa** (SPEC-01-02): cada operación guarda `CurrencyCode`, la divisa de su empresa en el momento de registrarla (nulo = EUR en las antiguas y en empresas sin país). La tabla tiene la columna «Cód. divisa» (por ejemplo «USD $»), de solo lectura también al editar; las ventas parciales copian la divisa a la fila pendiente y, al editar una operación y cambiar de empresa, pasa a la de la empresa nueva. Los importes llevan el símbolo de su divisa y la tarjeta «Total invertido» suma solo operaciones en la divisa base con una nota de las excluidas (las operaciones en otra divisa siguen sin sumarse en esta tarjeta).
- **Tipo de cambio** (SPEC-01-03): en empresas de otra divisa, la fila en edición muestra «Tipo de cambio», precargado con el tipo actual (se conserva el guardado al editar), editable y opcional; se guarda en `ExchangeRate` y `ExchangeRateTo`. La venta parcial lo copia a la fila pendiente. Un botón «Detalles», junto a editar y eliminar en todas las operaciones, muestra importe y comisión; en otra divisa, originales y convertidos con el tipo de la operación (o el actual, rotulado como orientativo), y en la divisa base una sola columna.
- Los errores de validación y de acciones insuficientes se muestran en pantalla.
- Cada alta, edición o baja **recalcula automáticamente** la posición de la empresa (SPEC-03); si al editar se cambia de empresa se recalculan ambas.

## 4. Datos implicados

- `Operation.SaleHistoryId` (Id del registro que cerró la compra) y `Operation.OriginOperationId` (origen común de las filas de un lote partido), ambos nulos y sin clave foránea, añadidos desde `DatabaseSchemaUpgrader.EnsureOperationSaleColumns`.
- `SaleHistory` (tabla `SalesHistory`, creada solo desde `DatabaseSchemaUpgrader`): Id, OperationId (sin clave foránea), CompanyName, Ticker, SaleDate, NumberOfShares, AverageBuyPrice, SellPrice, Amount, Commission, Profit, CreatedAt, UpdatedAt. Índices por empresa y fecha.
- `Operation`: Id, CompanyName, Date, CloseDate, PricePerShare, NumberOfShares, Commission, Amount, Type, CurrencyCode (nulo = EUR), CreatedAt (calculados: IsClosed, Status, EffectiveCurrencyCode).
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
- `IOperationService.RegisterSaleAsync` y `SaleRegistrationResult`; `UndoSaleAsync` y `SaleUndoResult`; `GetSaleLockMessageAsync` (bloqueo de operaciones cerradas por una venta, aplicado solo en `DeleteAsync`); `GetSaleEditWarningAsync` (aviso al editarlas).
- Servicio `IOperationService` / `OperationService`: `GetAllAsync`, `GetByIdAsync`, `GetByCompanyAsync`, `GetByTypeAsync`, `GetOpenOperationsAsync`, `GetClosedOperationsAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `GetTotalOpenAmountAsync`, `GetOperationCountByTypeAsync`.
- Colaboración con `ICompanyService.RecalculateTotalInvestedAsync` y `RefreshLivePriceAsync`.
- SPEC-05-01: `GetAvailableSharesAsync`, `GetQuickTradeBlockReason`, `RegisterQuickTradeAsync`, `GetChartMarkersAsync` (tipos en `Services/QuickTrade.cs`).

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
- [ ] Deshacer una venta reabre sus compras, une las filas partidas, elimina la Venta y el registro y recalcula la empresa (SPEC-02-03).
- [ ] Eliminar desde `/operations` una operación cerrada por una venta muestra el aviso y no cambia nada; editarla muestra el aviso amarillo y deja guardar.
- [ ] Reabrir una compra cerrada por una venta la desvincula: al deshacer esa venta, la compra no cambia.
- [ ] La columna «Cód. divisa» muestra código y símbolo de cada operación; una operación nueva toma la divisa de su empresa y la parcial conserva la divisa (SPEC-01-02).
- [ ] En modo edición, cambiar Estado a Cerrada pone la fecha de hoy y a Abierta la borra; al guardar cambian las acciones y el total invertido de la empresa.

## 8. Observaciones y limitaciones detectadas

- `GetByCompanyAsync` filtra con «contiene», por lo que una empresa cuyo nombre esté contenido en otro puede mezclar resultados.
- El estado Abierta/Cerrada depende de la fecha de cierre. Las Ventas nuevas cierran las compras por FIFO, pero las anteriores a SPEC-02-01 siguen abiertas.
- Las ventas registradas antes de SPEC-02-03 no tienen `SaleHistoryId`: al deshacerlas solo se eliminan la Venta y el registro, sin reabrir compras (se limpian a mano).
- Los tipos de operación son cadenas de texto libres en base de datos, no una enumeración.

## 9. Features relacionadas

| Feature | Estado | Documentos |
|---|---|---|
| [SPEC-02-01 · Histórico de ventas de acciones](spec02-01-sales-history/spec02-01.md) | Implementada, pendiente de probar a mano | [spec](spec02-01-sales-history/spec02-01.md) · [plan](spec02-01-sales-history/plan02-01.md) · [tareas](spec02-01-sales-history/task02-01.md) |
| [SPEC-02-02 · Cálculo del importe total de una operación](spec02-02-total-amount-calculation/spec02-02.md) | Implementada, pendiente de probar a mano | [spec](spec02-02-total-amount-calculation/spec02-02.md) |
| [SPEC-02-03 · Deshacer venta de acciones](spec02-03-undo-sale/spec02-03.md) | Implementada, pendiente de compilar y probar a mano | [spec](spec02-03-undo-sale/spec02-03.md) · [plan](spec02-03-undo-sale/plan02-03.md) · [tareas](spec02-03-undo-sale/task02-03.md) |
| [SPEC-01-02 · Países y divisa](../spec01-gestion-de-empresas/spec01-02-countries-and-currency/spec01-02.md) | Implementada, pendiente de compilar y probar a mano | [spec](../spec01-gestion-de-empresas/spec01-02-countries-and-currency/spec01-02.md) · [plan](../spec01-gestion-de-empresas/spec01-02-countries-and-currency/plan01-02.md) · [tareas](../spec01-gestion-de-empresas/spec01-02-countries-and-currency/task01-02.md) |
| [SPEC-01-03 · Tipo de cambio y totales convertidos](../spec01-gestion-de-empresas/spec01-03-exchange-rate/spec01-03.md) | Implementada, pendiente de compilar y probar a mano | [spec](../spec01-gestion-de-empresas/spec01-03-exchange-rate/spec01-03.md) · [plan](../spec01-gestion-de-empresas/spec01-03-exchange-rate/plan01-03.md) · [tareas](../spec01-gestion-de-empresas/spec01-03-exchange-rate/task01-03.md) |
| [SPEC-01-04 · Catálogo de empresas de mercado](../spec01-gestion-de-empresas/spec01-04-market-catalog/spec01-04.md) | Implementada, pendiente de compilar y probar a mano | [spec](../spec01-gestion-de-empresas/spec01-04-market-catalog/spec01-04.md) · [plan](../spec01-gestion-de-empresas/spec01-04-market-catalog/plan01-04.md) · [tareas](../spec01-gestion-de-empresas/spec01-04-market-catalog/task01-04.md) |
- [SPEC-05-01 · Gráfico con precio en vivo y compra/venta](../spec05-grafico-de-cotizacion/spec05-01-chart-live-trading/spec05-01.md): crea Compras y Ventas desde el gráfico de cotización con las mismas reglas que `/operations` (estado: implementada, pendiente de compilar y probar a mano).
- [SPEC-17-02 · Informe fiscal anual para la Renta](../spec17-patrimonio-global-y-distribucion/spec17-02-tax-report/spec17-02.md): el Dividendo añade «Retención en origen» y «Retención en España» (opcionales); el importe sigue siendo el neto cobrado y bruto = neto + retenciones. Las ventas del histórico alimentan el informe fiscal (estado: plan aprobado, pendiente de implementar después de SPEC-17-01).
