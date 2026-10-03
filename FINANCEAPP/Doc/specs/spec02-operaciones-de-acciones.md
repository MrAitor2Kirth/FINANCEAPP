# SPEC-02 · Registro y gestión de operaciones de acciones

| | |
|---|---|
| **Categoría** | 1. Acciones y empresas |
| **Identificador** | SPEC-02 |
| **Documento** | `spec02-operaciones-de-acciones.md` |
| **Volver al índice** | [README de specs](README.md) |

> Histórico de compras, ventas, dividendos y traspasos por empresa.

## 1. Descripción funcional

Permite registrar cada movimiento de la cartera de acciones y consultarlos en una tabla única con edición en línea. Es la fuente de datos de la que se obtienen la inversión, las acciones en cartera y los dividendos de cada empresa.

## 2. Pantallas y rutas

- `/operations` – listado de operaciones con alta, edición en línea y eliminación.
- Menú lateral: «Operaciones Acciones».

## 3. Reglas de negocio

- **Tipos de operación**: Compra, Venta, Dividendo y Traspaso.
- Campos: empresa (desplegable con las empresas existentes), tipo, fecha de apertura, fecha de cierre (opcional), precio por acción, número de acciones, comisiones e importe.
- El **importe se propone automáticamente** como precio × nº de acciones + comisión, pero puede modificarse a mano.
- Validaciones: empresa obligatoria (máx. 200), tipo obligatorio (máx. 50), fecha obligatoria, importe > 0, precio ≥ 0, comisión ≥ 0.
- Una operación está **Abierta** si no tiene fecha de cierre y **Cerrada** si la tiene; el estado se muestra como insignia.
- **Filtro por empresa** desde un menú en la cabecera de la columna Empresa; muestra el aviso «Filtro activo» y un botón para limpiarlo. Los contadores de operaciones abiertas y cerradas respetan el filtro.
- Al seleccionar una empresa en el alta se refresca su precio en vivo y se muestra en la fila.
- Orden por defecto: fecha descendente.
- Cada alta, edición o baja **recalcula automáticamente** la posición de la empresa (SPEC-03); si al editar se cambia de empresa se recalculan ambas.

## 4. Datos implicados

- `Operation`: Id, CompanyName, Date, CloseDate, PricePerShare, NumberOfShares, Commission, Amount, Type, CreatedAt (calculados: IsClosed, Status).
- Índices en CompanyName, Date y Type.

## 5. Flujo principal

1. El usuario pulsa «Nueva operación»; se abre una fila editable en la tabla.
2. Elige empresa y tipo, introduce fecha, precio, acciones y comisión; el importe se calcula.
3. Guarda; la operación se añade y la empresa se recalcula.
4. Para modificar, pulsa el botón de editar de la fila (edición en línea). Para borrar, confirma en el modal.

## 6. Componentes técnicos

- Página `OperationsList.razor`.
- Servicio `IOperationService` / `OperationService`: `GetAllAsync`, `GetByIdAsync`, `GetByCompanyAsync`, `GetByTypeAsync`, `GetOpenOperationsAsync`, `GetClosedOperationsAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `GetTotalOpenAmountAsync`, `GetOperationCountByTypeAsync`.
- Colaboración con `ICompanyService.RecalculateTotalInvestedAsync` y `RefreshLivePriceAsync`.

## 7. Criterios de aceptación

- [ ] Se puede crear una operación de cada tipo y aparece ordenada por fecha.
- [ ] El importe se autocalcula y se puede sobrescribir.
- [ ] Filtrar por empresa reduce la tabla y los contadores; limpiar el filtro la restaura.
- [ ] Tras guardar o borrar, los totales de la empresa se actualizan.
- [ ] No se guarda con importe ≤ 0 ni sin empresa o tipo.

## 8. Observaciones y limitaciones detectadas

- `GetByCompanyAsync` filtra con «contiene», por lo que una empresa cuyo nombre esté contenido en otro puede mezclar resultados.
- El estado Abierta/Cerrada depende solo de la fecha de cierre; el tipo Venta no cierra automáticamente las compras anteriores.
- Los tipos de operación son cadenas de texto libres en base de datos, no una enumeración.
