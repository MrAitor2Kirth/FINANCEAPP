# SPEC-11 · Totales de ingresos y gastos y consulta por fechas

| | |
|---|---|
| **Categoría** | 3. Banco |
| **Identificador** | SPEC-11 |
| **Documento** | `spec11-totales-y-consultas-bancarias.md` |
| **Volver al índice** | [README de specs](README.md) |

> Agregados de ingresos y gastos por cuenta y filtro por rango de fechas.

## 1. Descripción funcional

Ofrece los datos agregados de cada cuenta (total de ingresos y total de gastos) y la consulta de movimientos en un rango de fechas, como base para el análisis del flujo de caja.

## 2. Pantallas y rutas

- Datos disponibles a través del servicio bancario; el saldo total alimenta el dashboard (SPEC-17).

## 3. Reglas de negocio

- **Total de ingresos** = suma de Ingreso, Intereses y Traspaso Entrada de la cuenta.
- **Total de gastos** = suma de Gasto, Comisiones y Traspaso Salida de la cuenta.
- La consulta por fechas devuelve los movimientos de una cuenta entre dos fechas dadas.
- El **saldo total** de todas las cuentas se muestra como «Efectivo Bancario» en el dashboard.

## 4. Datos implicados

- `BankOperation` (Type, Amount, Date).
- `BankAccount.CurrentBalance`.

## 5. Flujo principal

1. Un componente consulta al servicio los totales de una cuenta.
2. El servicio filtra por nombre de cuenta y tipo y suma los importes.
3. Para una consulta por fechas se aplica además el rango indicado.

## 6. Componentes técnicos

- `IBankService`: `GetTotalIncomesAsync`, `GetTotalExpensesAsync`, `GetOperationsByDateRangeAsync`, `GetTotalBalanceAsync`.

## 7. Criterios de aceptación

- [ ] Los totales de ingresos y gastos coinciden con la suma de los movimientos de cada tipo.
- [ ] La consulta por fechas incluye únicamente movimientos dentro del rango.
- [ ] El saldo total coincide con la suma de los saldos de las cuentas.

## 8. Observaciones y limitaciones detectadas

- Estas consultas existen en el servicio pero **no están expuestas aún con una pantalla propia** (no hay filtro de fechas en la interfaz bancaria); es una funcionalidad lista para usar.
- Los traspasos entre cuentas propias cuentan como ingreso o gasto en estos totales.
