# SPEC-16 · Venta de un inmueble

| | |
|---|---|
| **Categoría** | 5. Inmobiliario |
| **Identificador** | SPEC-16 |
| **Documento** | `spec16-venta-de-inmuebles.md` |
| **Volver al índice** | [README de specs](README.md) |

> Cierre de la inversión en un inmueble registrando la venta y su resultado.

## 1. Descripción funcional

Describe el proceso de venta de un inmueble: registrar el ingreso de la venta, marcar la propiedad como vendida y consolidar sus totales. Incluye la pantalla dedicada de venta, con resumen de la situación del inmueble y resultado estimado.

## 2. Pantallas y rutas

- Botón «Vender» en cada inmueble no vendido de `/inmuebles`, que navega a `/inmuebles/vender/{id}`.
- `/inmuebles/vender/{id}` – pantalla de venta (`PropertySell.razor`): muestra el precio de compra, los gastos y los ingresos acumulados; permite indicar **precio de venta**, **fecha de venta** y **notas**; calcula el resultado estimado y pide confirmación.

## 3. Reglas de negocio

- No se puede vender un inmueble que ya está **Vendida**.
- Al vender, en una única **transacción**: se crea una operación de categoría Ingreso y tipo Venta por el precio de venta; se cambia el estado a **Vendida**; se fija la fecha de venta a hoy; el valor actual pasa a ser el precio de venta.
- Se recalculan: **ingresos totales** = ingresos previos + precio de venta; **total invertido** = precio de compra + gastos.
- El precio de venta debe ser mayor que 0; se propone por defecto el valor actual del inmueble (o, si no lo hay, el precio de compra) y la fecha de hoy.
- **Resultado estimado** mostrado antes de confirmar = ingresos acumulados + precio de venta − (precio de compra + gastos acumulados), la misma fórmula que consolida el servicio.
- Si el inmueble no existe o ya está vendido, la pantalla lo indica y no permite vender.
- Las propiedades vendidas **dejan de contar** en el patrimonio del dashboard (solo cuentan las no vendidas).
- Si algo falla, la transacción se revierte.

## 4. Datos implicados

- `Property` (Status, SaleDate, CurrentValue, TotalIncome, TotalInvested).
- `PropertyOperation` (Venta).

## 5. Flujo principal

1. El usuario pulsa «Vender» en un inmueble.
2. La pantalla muestra la situación actual y propone precio y fecha.
3. El usuario ajusta el precio, la fecha y las notas, y revisa el resultado estimado.
4. Al confirmar, `SellPropertyAsync` registra la operación y actualiza la propiedad.
5. La app vuelve al listado con el inmueble en estado Vendida.

## 6. Componentes técnicos

- `Properties/PropertySell.razor` (nueva).
- `IPropertyService.SellPropertyAsync(propertyId, salePrice, notes, saleDate)` / `PropertyService` (el parámetro opcional `saleDate` permite fijar la fecha; si no se indica se usa la actual).
- `GetPropertyByIdAsync`, `GetTotalIncomeAsync`, `GetTotalExpensesAsync` para el resumen.
- Navegación desde `PropertiesList.razor`.

## 7. Criterios de aceptación

- [ ] El botón «Vender» abre la pantalla de venta sin error.
- [ ] Vender un inmueble lo marca como Vendida con la fecha indicada.
- [ ] Se crea el ingreso de venta con la nota indicada.
- [ ] Un precio de venta ≤ 0 no se acepta.
- [ ] Abrir la venta de un inmueble ya vendido muestra un aviso y no permite repetirla.
- [ ] El patrimonio del dashboard deja de incluir el inmueble.

## 8. Observaciones y limitaciones detectadas

- **Resuelto**: antes el botón navegaba a una ruta sin página; ahora existe `PropertySell.razor`.
- La venta no se puede deshacer desde la app; un error en el precio obligaría a corregir manualmente la operación de venta y el estado del inmueble.
- Los gastos posteriores a la venta siguen sumándose al total invertido si se registran sobre el inmueble vendido.
