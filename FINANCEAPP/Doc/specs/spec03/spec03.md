# SPEC-03 · Cálculo automático de posición, dividendos y rentabilidad

| | |
|---|---|
| **Categoría** | 1. Acciones y empresas |
| **Identificador** | SPEC-03 |
| **Documento** | `spec03/spec03.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Recalcula inversión, acciones en cartera, dividendos y rentabilidad de cada empresa.

## 1. Descripción funcional

Mantiene actualizados los totales de cada empresa a partir de sus operaciones, de modo que el listado y el dashboard siempre muestren la inversión, el número de acciones, los dividendos cobrados y la rentabilidad con y sin dividendos.

## 2. Pantallas y rutas

- Resultado visible en `/companies` (columnas de inversión, acciones, dividendos y rentabilidades), en el formulario de edición (solo lectura) y en el dashboard.

## 3. Reglas de negocio

- Solo se consideran las operaciones **abiertas** (sin fecha de cierre) para inversión y acciones. Una Venta nueva (SPEC-02-01) cierra las compras que cubre y se cierra ella misma, así que deja de contar; solo restan las Ventas antiguas que sigan abiertas.
- **Total invertido inicial** = suma de importes de Compra y Traspaso − suma de importes de Venta.
- **Número de acciones** = acciones de Compra y Traspaso − acciones de Venta.
- **Total dividendos** = suma de importes de todas las operaciones de tipo Dividendo de la empresa (abiertas o cerradas).
- **Total valor invertido** = (precio en vivo, o precio actual si no hay precio en vivo) × número de acciones.
- **Rentabilidad plusvalía %** = (valor invertido − invertido inicial) / invertido inicial × 100.
- **Rentabilidad con dividendos %** = (valor invertido + dividendos − invertido inicial) / invertido inicial × 100.
- Si el total invertido inicial es ≤ 0, ambas rentabilidades valen 0.
- El recálculo se ejecuta tras cualquier alta, edición o baja de operación y al guardar la empresa; actualiza `UpdatedAt`.

## 4. Datos implicados

- Campos persistidos en `CompanyDetail`: TotalInvestedInitial, NumberOfShares, TotalDividends, PlusvaliaReturnPercent, DividendPlusvaliaReturnPercent.
- Propiedad calculada no persistida `TotalInvestedValue`.

## 5. Flujo principal

1. Se guarda/edita/elimina una operación.
2. `OperationService` llama a `CompanyService.RecalculateTotalInvestedAsync(nombreEmpresa)`.
3. Se leen las operaciones abiertas y los dividendos de la empresa, se calculan los totales y se guardan.
4. Las rentabilidades se calculan con los métodos `CalculatePlusvaliaReturnPercent` y `CalculateDividendPlusvaliaReturnPercent` del modelo.

## 6. Componentes técnicos

- `CompanyService.RecalculateTotalInvestedAsync`.
- `CompanyDetail.CalculatePlusvaliaReturnPercent` y `CalculateDividendPlusvaliaReturnPercent`.

## 7. Criterios de aceptación

- [ ] Con una compra de 1.000 € y precio actual que vale 1.100 € en total, la rentabilidad es 10 %.
- [ ] Un dividendo suma a la rentabilidad con dividendos pero no a la de plusvalía.
- [ ] Una operación con fecha de cierre deja de contar en inversión y acciones.
- [ ] Cambiar una operación de empresa recalcula las dos empresas implicadas.

## 8. Observaciones y limitaciones detectadas

- Las Ventas antiguas que siguen abiertas restan su **importe completo** de lo invertido (neto de caja). Con las Ventas nuevas, el «total invertido» pasa a ser el coste de las compras que siguen abiertas; ambos criterios conviven hasta que se cierren las ventas antiguas.
- El cálculo depende de que el nombre de empresa de la operación coincida exactamente con el de la empresa.
- Los totales se guardan en base de datos y solo se refrescan al recalcular; si se importan o editan datos fuera de la app pueden quedar desfasados.

## 9. Features relacionadas

- [SPEC-02-01 · Histórico de ventas de acciones](../spec02/spec02-01-sales-history/spec02-01.md): cambia el cálculo de inversión y acciones, porque una venta cierra las compras que cubre (estado: implementada, pendiente de probar a mano).
