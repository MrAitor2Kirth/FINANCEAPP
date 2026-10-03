# SPEC-06 · Gestión de fondos de inversión

| | |
|---|---|
| **Categoría** | 2. Fondos de inversión |
| **Identificador** | SPEC-06 |
| **Documento** | `spec06/spec06.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Alta, edición, baja y consulta de fondos con su rentabilidad.

## 1. Descripción funcional

Permite mantener el catálogo de fondos de inversión del usuario con sus datos básicos, el total invertido, el valor actual y la rentabilidad resultante, y consultarlos en un listado con acceso a sus operaciones e historial.

## 2. Pantallas y rutas

- `/fondos` – listado de fondos.
- `/fondos/nuevo` y `/fondos/editar/{id}` – formulario de alta y edición.
- Detalle desplegable bajo cada fondo (botón de detalle) con operaciones, plusvalías e historial.

## 3. Reglas de negocio

- El modelo exige **nombre** y **tipo de fondo**; el formulario marca además ISIN, gestor/entidad, TER y rentabilidad histórica como campos obligatorios (*).
- **Tipos de fondo**: Renta Variable, Renta Fija, Mixto, Monetario, Índice y Temático.
- **TER** entre 0 y 99,99 %; **rentabilidad histórica** entre −99,99 y 99,99 %.
- **Total invertido** y **Valor actual** se introducen a mano en el formulario («Información de Inversión»); el **% de rentabilidad** se calcula automáticamente y es de solo lectura.
- Rentabilidad del fondo = (valor actual − total invertido) / total invertido × 100.
- El servicio calcula totales agregados: total invertido, valor actual total y rentabilidad agregada de todos los fondos.
- Al **eliminar un fondo** se eliminan también sus operaciones y su historial de valores.

## 4. Datos implicados

- `Fund`: Id, Name, FundType, Manager, TER, ISIN, HistoricalReturn, TotalInvested, CurrentValue, CreatedAt, UpdatedAt.

## 5. Flujo principal

1. El usuario abre `/fondos` y ve la tabla (nombre, tipo, gestor, TER, total invertido, valor actual, rentabilidad).
2. Crea un fondo con «Nuevo» o edita uno existente.
3. Pulsa el botón de detalle para desplegar operaciones y valor.
4. Elimina el fondo; sus datos asociados se borran con él.

## 6. Componentes técnicos

- `FundsList.razor`, `FundForm.razor`.
- `IFundService` / `FundService`: `GetAllFundsAsync`, `GetFundByIdAsync`, `GetFundByNameAsync`, `CreateFundAsync`, `UpdateFundAsync`, `DeleteFundAsync`, `GetTotalInvestedAsync`, `GetTotalCurrentValueAsync`, `GetAggregateReturnAsync`.

## 7. Criterios de aceptación

- [ ] Se puede crear y editar un fondo con datos válidos.
- [ ] No se guarda sin nombre o tipo, ni con TER o rentabilidad fuera de rango.
- [ ] El listado muestra la rentabilidad calculada.
- [ ] Eliminar un fondo borra sus operaciones e historial.

## 8. Observaciones y limitaciones detectadas

- Las operaciones se vinculan al fondo por **nombre** (`FundName`).
- La rentabilidad histórica del fondo es un dato introducido a mano, distinto de la rentabilidad calculada.
- El total invertido no se deriva de las aportaciones y reembolsos (ver SPEC-07): se mantiene a mano.
