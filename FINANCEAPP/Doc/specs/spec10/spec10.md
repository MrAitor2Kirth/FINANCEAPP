# SPEC-10 · Movimientos bancarios y recálculo de saldo

| | |
|---|---|
| **Categoría** | 3. Banco |
| **Identificador** | SPEC-10 |
| **Documento** | `spec10/spec10.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Registro de ingresos, gastos y traspasos con actualización automática del saldo.

## 1. Descripción funcional

Permite registrar los movimientos de cada cuenta. Tras cada alta, edición o baja se recalcula el saldo de la cuenta a partir de todos sus movimientos.

## 2. Pantallas y rutas

- Detalle desplegable de cada cuenta en `/banco` (componente `BankOperationsList`), con formulario de alta y tabla de movimientos.

## 3. Reglas de negocio

- **Tipos de movimiento**: Ingreso, Gasto, Intereses, Comisiones, Traspaso Entrada y Traspaso Salida.
- **Suman** al saldo: Ingreso, Intereses y Traspaso Entrada. **Restan**: Gasto, Comisiones y Traspaso Salida.
- El saldo se recalcula desde cero a partir de todos los movimientos de la cuenta tras cada alta, edición o baja (`RecalculateAccountBalanceAsync`).
- Campos: tipo, importe, fecha y descripción opcional.

## 4. Datos implicados

- `BankOperation`: Id, AccountName, Type, Amount, Date, Description, CreatedAt.
- `BankAccount.CurrentBalance`.

## 5. Flujo principal

1. El usuario despliega una cuenta y rellena el formulario de movimiento.
2. Guarda; el movimiento aparece en la tabla.
3. El servicio recalcula el saldo y notifica al listado, que se refresca.

## 6. Componentes técnicos

- `Bank/BankOperationsList.razor`.
- `IBankService`: `GetOperationsByAccountNameAsync`, `GetOperationByIdAsync`, `CreateOperationAsync`, `UpdateOperationAsync`, `DeleteOperationAsync`, `RecalculateAccountBalanceAsync`.

## 7. Criterios de aceptación

- [ ] Un ingreso aumenta el saldo y un gasto lo reduce.
- [ ] Editar el importe de un movimiento corrige el saldo.
- [ ] Borrar un movimiento revierte su efecto en el saldo.

## 8. Observaciones y limitaciones detectadas

- Un traspaso entre cuentas exige registrar manualmente la salida en una cuenta y la entrada en la otra; no hay una operación de traspaso enlazada.
- El recálculo parte de cero, por lo que un saldo inicial introducido en la cuenta no se conserva salvo que se registre como movimiento.
