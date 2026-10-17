# SPEC-07 · Aportaciones y reembolsos de fondos

| | |
|---|---|
| **Categoría** | 2. Fondos de inversión |
| **Identificador** | SPEC-07 |
| **Documento** | `spec07-aportaciones-y-reembolsos-de-fondos/spec07.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Registro de las entradas y salidas de dinero de cada fondo.

## 1. Descripción funcional

Permite anotar las aportaciones y los reembolsos de cada fondo desde su propio detalle, y consultar los totales aportados y reembolsados.

## 2. Pantallas y rutas

- Detalle desplegable de cada fondo en `/fondos`, sección «Aportaciones y reembolsos»: componente de operaciones (`OperationsList`) con tabla de Tipo, Cantidad, Fecha y Acciones, y formulario de alta.

## 3. Reglas de negocio

- **Tipos de operación que se pueden crear**: Aportación, Reembolso, Traspaso Entrada y Traspaso Salida. El tipo Plusvalía ya no se ofrece (el valor del fondo se actualiza con el historial de valores, SPEC-08), aunque las operaciones de plusvalía antiguas se siguen mostrando.
- Las operaciones se pueden crear, editar y eliminar desde la tabla.
- El servicio calcula `GetTotalContributionsAsync` (solo tipo Aportación) y `GetTotalRedemptionsAsync` (solo tipo Reembolso) por fondo.
- Tras añadir una operación se notifica al listado (`OnOperationAdded`) para que se refresque.

## 4. Datos implicados

- `FundOperation`: Id, FundName, Type, Amount, Date, CreatedAt.

## 5. Flujo principal

1. El usuario despliega el detalle de un fondo.
2. Elige el tipo, la cantidad y la fecha y guarda.
3. La operación aparece en la tabla y el listado de fondos se refresca.
4. Puede eliminar una operación con su botón.

## 6. Componentes técnicos

- `Funds/OperationsList.razor`.
- `IFundService`: `GetOperationsByFundNameAsync`, `GetOperationByIdAsync`, `CreateOperationAsync`, `UpdateOperationAsync`, `DeleteOperationAsync`, `GetTotalContributionsAsync`, `GetTotalRedemptionsAsync`.

## 7. Criterios de aceptación

- [ ] Se puede registrar una aportación y un reembolso con fecha.
- [ ] Los totales de aportaciones y reembolsos se calculan por separado.
- [ ] Eliminar una operación la quita de la tabla.

## 8. Observaciones y limitaciones detectadas

- A diferencia del banco, el alta de una aportación o reembolso **no recalcula automáticamente** `Fund.TotalInvested`; este valor se mantiene desde el formulario del fondo.
- La cantidad se introduce como importe; no hay participaciones ni valor liquidativo.

## 9. Features relacionadas

- [SPEC-17-02 · Informe fiscal anual para la Renta](../spec17-patrimonio-global-y-distribucion/spec17-02-tax-report/spec17-02.md): el Reembolso añade «Ganancia/pérdida fiscal» y «Retención» (opcionales, a mano desde la información fiscal de la gestora); los traspasos salen como informativos en el informe (estado: plan aprobado, pendiente de implementar después de SPEC-17-01).
