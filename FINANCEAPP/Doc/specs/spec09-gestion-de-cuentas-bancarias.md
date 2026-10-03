# SPEC-09 · Gestión de cuentas bancarias

| | |
|---|---|
| **Categoría** | 3. Banco |
| **Identificador** | SPEC-09 |
| **Documento** | `spec09-gestion-de-cuentas-bancarias.md` |
| **Volver al índice** | [README de specs](README.md) |

> Alta, edición, baja y consulta de cuentas con su saldo.

## 1. Descripción funcional

Permite mantener las cuentas bancarias del usuario y ver su saldo actual y el total de efectivo. Cada cuenta se puede desplegar para ver y registrar sus movimientos.

## 2. Pantallas y rutas

- `/banco` – listado de cuentas.
- `/banco/nueva` y `/banco/editar/{Id}` – formulario.
- Detalle desplegable de cada cuenta con sus movimientos.

## 3. Reglas de negocio

- Campos obligatorios: nombre, banco y tipo de cuenta; **IBAN** opcional, máximo 34 caracteres.
- **Tipos de cuenta**: Corriente, Ahorro, Depósito y Cuenta Remunerada.
- El servicio calcula el **saldo total** sumando todas las cuentas.
- Al **eliminar una cuenta** se eliminan también todos sus movimientos.

## 4. Datos implicados

- `BankAccount`: Id, Name, BankName, IBAN, AccountType, CurrentBalance, CreatedAt, UpdatedAt.

## 5. Flujo principal

1. El usuario abre `/banco` y ve las cuentas con su saldo.
2. Crea o edita una cuenta con el formulario.
3. Despliega una cuenta para ver sus movimientos.
4. Elimina la cuenta, lo que borra sus movimientos.

## 6. Componentes técnicos

- `Bank/BankAccountsList.razor`, `BankAccountForm.razor`.
- `IBankService`: `GetAllBankAccountsAsync`, `GetBankAccountByIdAsync`, `GetBankAccountByNameAsync`, `CreateBankAccountAsync`, `UpdateBankAccountAsync`, `DeleteBankAccountAsync`, `GetTotalBalanceAsync`.

## 7. Criterios de aceptación

- [ ] Se puede crear una cuenta con nombre, banco y tipo.
- [ ] El listado muestra el saldo de cada cuenta y el total.
- [ ] Eliminar una cuenta elimina sus movimientos.

## 8. Observaciones y limitaciones detectadas

- El IBAN solo se limita en longitud; no se valida su formato ni su dígito de control.
- Al editar una cuenta se puede modificar el saldo directamente, lo que puede dejarlo distinto del que resultaría de sus movimientos hasta el siguiente recálculo.
