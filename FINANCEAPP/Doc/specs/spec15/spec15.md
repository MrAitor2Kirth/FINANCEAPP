# SPEC-15 · Ingresos y gastos de inmuebles

| | |
|---|---|
| **Categoría** | 5. Inmobiliario |
| **Identificador** | SPEC-15 |
| **Documento** | `spec15/spec15.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Operaciones de alquiler, impuestos, reformas y otros con resultado neto.

## 1. Descripción funcional

Permite registrar los ingresos y gastos de cada inmueble y calcula automáticamente el total invertido, los ingresos acumulados y el resultado neto.

## 2. Pantallas y rutas

- Detalle desplegable de cada inmueble en `/inmuebles` (componente `PropertyOperationsList`).

## 3. Reglas de negocio

- **Categorías**: Ingreso y Gasto.
- **Tipos**: Alquiler, Venta, Comunidad, IBI, Hipoteca, Reforma, Seguro y Servicios (agua, luz, gas).
- **Total ingresos** = suma de operaciones de categoría Ingreso; **total gastos** = suma de categoría Gasto.
- **Total invertido** = precio de compra + valor absoluto de los gastos.
- **Resultado neto** = ingresos − gastos.
- Tras cada alta, edición o baja se recalculan los totales del inmueble.
- El **ingreso por alquiler** global suma las operaciones de tipo Alquiler y categoría Ingreso.
- Se pueden filtrar operaciones por categoría, por tipo y por rango de fechas.

## 4. Datos implicados

- `PropertyOperation`: Id, PropertyName, Category, Type, Amount, Date, Description, CreatedAt.
- `Property.TotalInvested`, `TotalIncome`.

## 5. Flujo principal

1. El usuario despliega un inmueble y registra una operación.
2. El servicio guarda la operación dentro de una transacción y recalcula los totales.
3. La tabla y el listado se refrescan.

## 6. Componentes técnicos

- `Properties/PropertyOperationsList.razor`.
- `IPropertyService`: `CreateOperationAsync`, `UpdateOperationAsync`, `DeleteOperationAsync`, `RecalculatePropertyTotalsAsync`, `GetTotalIncomeAsync`, `GetTotalExpensesAsync`, `GetPropertyNetResultAsync`, `GetTotalRentalIncomeAsync`, `GetOperationsByCategoryAsync`, `GetOperationsByTypeAsync`, `GetOperationsByDateRangeAsync`.

## 7. Criterios de aceptación

- [ ] Un gasto incrementa el total invertido y un ingreso incrementa los ingresos.
- [ ] El resultado neto = ingresos − gastos.
- [ ] Borrar una operación recalcula los totales.
- [ ] El ingreso de alquiler global suma solo alquileres.

## 8. Observaciones y limitaciones detectadas

- El recálculo se invoca dentro de transacciones abiertas; el código advierte de que SQLite no admite transacciones anidadas, por lo que no debe abrirse otra dentro.
- Las hipotecas se tratan como gasto completo y no se separa capital de intereses.
