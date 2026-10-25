# SPEC-10 · Movimientos bancarios y recálculo de saldo

| | |
|---|---|
| **Categoría** | 3. Banco |
| **Identificador** | SPEC-10 |
| **Documento** | `spec10-movimientos-bancarios-y-saldo/spec10.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Registro de ingresos, gastos y traspasos con actualización automática del saldo.

## 1. Descripción funcional

Permite registrar los movimientos de cada cuenta. Tras cada alta, edición o baja se recalcula el saldo de la cuenta a partir de todos sus movimientos.

## 2. Pantallas y rutas

- Detalle desplegable de cada cuenta en `/banco` (componente `BankOperationsList`), con tabla de movimientos con «Editar» y «Eliminar» (con confirmación) y formulario de alta al final (fecha inicial de hoy, mensajes de éxito y error bajo el formulario). «Editar» abre el formulario **justo debajo de la fila** (antes se abría al final de la lista, fuera de la vista, y parecía que el botón no hacía nada). Los traspasos se editan ahí (importe, fecha y descripción, en las dos patas). En un ingreso de venta o en el movimiento de una compra o dividendo cuya operación existe, «Editar» muestra bajo la fila el motivo y dónde cambiarlo (`BankService.GetEditLockMessageAsync`, la misma regla que al guardar).

## 3. Reglas de negocio

- **Tipos de movimiento**: Ingreso, Gasto, Intereses, Comisiones, Traspaso Entrada y Traspaso Salida.
- **Suman** al saldo: Ingreso, Intereses y Traspaso Entrada. **Restan**: Gasto, Comisiones y Traspaso Salida.
- El usuario escribe el importe **siempre en positivo**; `BankService` lo guarda con el signo del tipo (negativo en gastos), redondeado a 2 decimales (`AwayFromZero`).
- Saldo = `OpeningBalance` (saldo inicial de la cuenta) + efecto de los movimientos, con valor absoluto y tipo. Se recalcula tras cada alta, edición o baja (`RecalculateAccountBalanceAsync`), que **no abre transacción propia** (SQLite no admite anidadas); la abre el método que la llama.
- Validación solo en el servicio, con mensajes en español: tipo válido, importe > 0 (máximo 999.999.999,99), fecha obligatoria, descripción de hasta 500 caracteres.
- Campos: tipo, importe, fecha y descripción opcional. Un movimiento no cambia de cuenta.
- **Ingresos de ventas de acciones** (SPEC-02-04): los movimientos con `SaleHistoryId` los crea la venta y no se editan ni se borran desde `/banco` (se muestran con la insignia «Venta nº X»); se cambian desde «Histórico de ventas». Borrar la cuenta sí los elimina.
- **Movimientos de compras y dividendos** (SPEC-02-06): los movimientos con `OperationId` (Gasto de una Compra, Ingreso de un Dividendo) los crea la operación y no se editan ni se borran desde `/banco` mientras la operación exista; se cambian editándola o borrándola en `/operations`. Si la operación ya no existe, se tratan como normales.
- **Cambio de cuenta** (SPEC-02-07): la cuenta de un movimiento de compra o dividendo solo cambia al editar su operación en `/operations`; el movimiento pasa de una cuenta a otra y se recalculan las dos.
- **Traspasos entre cuentas** (SPEC-10-02): la acción «Traspaso» de `/banco` crea un Traspaso Salida y un Traspaso Entrada enlazados por `TransferId`; se borran juntos y su importe y fecha se editan en las dos patas (el tipo no cambia). Los traspasos manuales anteriores siguen siendo movimientos independientes.

## 4. Datos implicados

- `BankOperation`: Id, AccountName, Type, Amount, Date, Description, CreatedAt.
- `BankAccount.CurrentBalance` y `BankAccount.OpeningBalance` (columna añadida por `EnsureBankAccountOpeningBalanceColumn`).

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
- [ ] Un Gasto escrito en positivo se guarda negativo y reduce el saldo.
- [ ] Un movimiento no válido muestra el motivo en el formulario y no se guarda.
- [ ] Una cuenta con saldo escrito a mano conserva ese saldo como inicial al añadir movimientos.
- [ ] Tras guardar, el desplegable sigue abierto y el saldo del listado se actualiza.

## 8. Observaciones y limitaciones detectadas

- Un traspaso entre cuentas exige registrar manualmente la salida en una cuenta y la entrada en la otra; no hay una operación de traspaso enlazada.
- Al editar el saldo de una cuenta con movimientos, el saldo inicial se reajusta para que se vea el saldo escrito.
- Los movimientos siguen ligados a la cuenta por nombre: renombrar la cuenta los desvincula.
- Alta, edición y baja fallaban porque el recálculo abría una transacción anidada (corregido en SPEC-10-01, pendiente de probar a mano).

## 9. Features relacionadas

- [SPEC-02-04 · Ingreso en cuenta bancaria al vender acciones](../spec02-operaciones-de-acciones/spec02-04-sale-bank-deposit/spec02-04.md): columna `SaleHistoryId` e ingresos de ventas protegidos (estado: implementada y compilada, pendiente de probar a mano).
- [SPEC-10-01 · Corrección del alta, edición y borrado de movimientos](spec10-01-bank-operations-fix/spec10-01.md): arregla el fallo de transacciones anidadas, fija el signo según el tipo, añade `OpeningBalance` a la cuenta, mensajes de error y edición de movimientos (estado: implementada, compilada, pendiente de probar a mano).
- [SPEC-10-02 · Traspasos entre cuentas bancarias](spec10-02-account-transfers/spec10-02.md): acción «Traspaso» en `/banco` que crea, enlazados por `TransferId`, un Traspaso Salida y un Traspaso Entrada; editar o borrar una pata cambia las dos (estado: implementada y compilada, pendiente de probar a mano).
- [SPEC-17-02 · Informe fiscal anual para la Renta](../spec17-patrimonio-global-y-distribucion/spec17-02-tax-report/spec17-02.md): el movimiento Intereses añade «Retención» (opcional); el importe es lo abonado (neto) y bruto = neto + retención (estado: plan aprobado, pendiente de implementar después de SPEC-17-01).
