# SPEC-08 · Actualización del valor del fondo e historial de valores

| | |
|---|---|
| **Categoría** | 2. Fondos de inversión |
| **Identificador** | SPEC-08 |
| **Documento** | `spec08-actualizacion-de-valor-y-plusvalias-de-fondos.md` |
| **Volver al índice** | [README de specs](README.md) |

> Registro de las valoraciones del fondo en un historial; la última valoración es el valor actual.

## 1. Descripción funcional

Permite poner al día el valor actual de un fondo. Cada valoración se guarda en un historial con su fecha y su rentabilidad, y la última pasa a ser el valor actual del fondo. Este es el **único mecanismo** de actualización de valor: el anterior sistema de operaciones de plusvalía (opción A) se retiró de la interfaz.

## 2. Pantallas y rutas

- Detalle desplegable de cada fondo en `/fondos`, sección «Historial de valores» (componente `ValueHistoryList`), situada bajo «Aportaciones y reembolsos» (SPEC-07).

## 3. Reglas de negocio

- El usuario introduce el **nuevo valor** del fondo y, opcionalmente, una **nota**; el registro se guarda con la **fecha de hoy**.
- **Rentabilidad a esa fecha** = (nuevo valor − total invertido) / total invertido × 100 (0 si no hay inversión); se calcula automáticamente, no se introduce.
- Si no se indica nota se guarda una automática («Actualización automática del dd/mm/aaaa»).
- El valor introducido debe ser mayor que 0.
- Al registrar, se actualizan `Fund.CurrentValue` y `Fund.UpdatedAt`; la operación se ejecuta en una **transacción** que se revierte si algo falla.
- **Eliminar un registro** del historial lo borra; si era el más reciente, el valor actual del fondo vuelve al del registro anterior (si no quedan registros, se conserva el valor actual).
- El historial se muestra en una tabla con fecha, valor, rentabilidad (insignia verde o roja), notas y botón de eliminar.
- Si el fondo no existe se lanza un error controlado.

## 4. Datos implicados

- `FundValueHistory`: Id, FundName, Value, Date, ReturnAtDate, Notes, CreatedAt.
- `Fund.CurrentValue`, `Fund.UpdatedAt`.

## 5. Flujo principal

1. El usuario despliega un fondo y baja a «Historial de valores».
2. Introduce el nuevo valor (y una nota si quiere) y pulsa «Registrar».
3. El servicio guarda el registro y actualiza el valor actual en una transacción.
4. El listado de fondos se refresca con el nuevo valor y rentabilidad, y la tabla muestra la nueva fila.

## 6. Componentes técnicos

- `FundsList.razor`, `Funds/ValueHistoryList.razor`.
- `IFundService`: `UpdateFundValueWithHistoryAsync(fundName, newValue, notes)`, `GetFundValueHistoryAsync`, `DeleteFundValueHistoryAsync(historyId)`.

## 7. Criterios de aceptación

- [ ] Registrar un valor añade una fila al historial y actualiza el valor actual del fondo.
- [ ] La rentabilidad de la fila se calcula sobre el total invertido.
- [ ] Un valor ≤ 0 no se registra.
- [ ] Eliminar el último registro devuelve el valor actual al anterior.
- [ ] Si falla el guardado no queda ningún cambio parcial.

## 8. Observaciones y limitaciones detectadas

- **Resuelto**: antes convivían dos mecanismos (opción A con operación de plusvalía y opción B con historial). Se conserva solo el historial, que mantiene toda la evolución y no mezcla plusvalías con aportaciones y reembolsos.
- El formulario antiguo mostraba campos de fecha y rentabilidad que el servicio ignoraba, y el botón de eliminar no hacía nada; ambos puntos están corregidos.
- Los métodos de la opción A (`UpdateFundValueWithPlusvaliaOperationAsync`, `GetLatestPlusvaliOperationAsync`) siguen en el servicio sin uso en la interfaz, y pueden existir operaciones de tipo Plusvalía creadas antes del cambio: se siguen mostrando pero ya no se pueden crear.
- La fecha del registro es siempre la de hoy; no se pueden anotar valoraciones con fecha pasada.
