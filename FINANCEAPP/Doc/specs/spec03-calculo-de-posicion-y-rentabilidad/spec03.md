# SPEC-03 · Cálculo automático de posición, dividendos y rentabilidad

| | |
|---|---|
| **Categoría** | 1. Acciones y empresas |
| **Identificador** | SPEC-03 |
| **Documento** | `spec03-calculo-de-posicion-y-rentabilidad/spec03.md` |
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
- **Rentabilidad media de `/companies`** (SPEC-01-05): (Σ valor actual − Σ invertido) ÷ Σ invertido × 100 de las empresas con inversión, convertidas a la divisa base; es la rentabilidad por precio ponderada por la inversión y no incluye dividendos.
- El recálculo se ejecuta tras cualquier alta, edición o baja de operación y al guardar la empresa; actualiza `UpdatedAt`.

## 4. Datos implicados

- Campos persistidos en `CompanyDetail`: TotalInvestedInitial, NumberOfShares, TotalDividends, PlusvaliaReturnPercent, DividendPlusvaliaReturnPercent.
- Propiedades calculadas no persistidas `TotalInvestedValue` y `AveragePurchasePrice` (total invertido inicial ÷ número de acciones; sin valor si no hay acciones o el invertido es ≤ 0).

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

- [SPEC-02-01 · Histórico de ventas de acciones](../spec02-operaciones-de-acciones/spec02-01-sales-history/spec02-01.md): cambia el cálculo de inversión y acciones, porque una venta cierra las compras que cubre (estado: implementada, pendiente de probar a mano).
- Al deshacer una venta (SPEC-02-03) las compras vuelven a quedar abiertas, se unen las filas partidas y la empresa se recalcula con el mismo método; no cambia ninguna fórmula.
- [SPEC-01-01 · Precio medio de compra y lista extendida](../spec01-gestion-de-empresas/spec01-01-average-price-extended-list/spec01-01.md): añade el valor derivado no persistido «precio medio de compra» = total invertido inicial ÷ número de acciones (sin valor si no hay acciones o el invertido es ≤ 0); no cambia ninguna fórmula existente (estado: implementada, pendiente de compilar y probar a mano).
- [SPEC-17-02 · Informe fiscal anual para la Renta](../spec17-patrimonio-global-y-distribucion/spec17-02-tax-report/spec17-02.md): las retenciones del Dividendo son solo informativas; `TotalDividends` y las rentabilidades siguen usando el importe neto (estado: plan aprobado, pendiente de implementar después de SPEC-17-01).
- [SPEC-21-01 · Calendario de dividendos](../spec21-dividend-calendar/spec21-01-dividend-calendar/spec21-01.md): las líneas de dividendo no cambian ninguna fórmula; `TotalDividends` solo cambia cuando el usuario genera la operación de Dividendo (estado: implementada y compilada, pendiente de probar a mano).
- [SPEC-02-08 · Número de acciones con decimales](../spec02-operaciones-de-acciones/spec02-08-fractional-shares/spec02-08.md): las acciones en cartera son la suma de cantidades con hasta 2 decimales (redondeada a 2); el precio medio y el valor de cartera no cambian de fórmula.
