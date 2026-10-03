# SPEC-13 · Compras y ventas de activos refugio

| | |
|---|---|
| **Categoría** | 4. Activos refugio |
| **Identificador** | SPEC-13 |
| **Documento** | `spec13-operaciones-de-activos-refugio.md` |
| **Volver al índice** | [README de specs](README.md) |

> Registro de compras y ventas con recálculo del total invertido.

## 1. Descripción funcional

Permite anotar las compras y ventas de cada activo refugio y mantiene actualizado su total invertido a partir de ellas.

## 2. Pantallas y rutas

- Detalle desplegable de cada activo en `/activos-refugio` (componente `SafeHavenOperationsList`).

## 3. Reglas de negocio

- **Tipos de operación**: Compra y Venta.
- **Total invertido** = suma de importes de Compra − suma de importes de Venta (en valor absoluto), recalculado tras cada alta, edición o baja.
- El servicio ofrece el total de compras y el total de ventas de un activo, y el filtrado de operaciones por tipo.

## 4. Datos implicados

- `SafeHavenOperation`: Id, AssetName, Type, Amount, Date, CreatedAt.
- `SafeHavenAsset.TotalInvested`.

## 5. Flujo principal

1. El usuario despliega un activo y registra una operación.
2. El servicio guarda la operación y recalcula el total invertido.
3. El listado se refresca (`OnOperationAdded`).

## 6. Componentes técnicos

- `SafeHaven/SafeHavenOperationsList.razor`.
- `ISafeHavenService`: `GetOperationsByAssetNameAsync`, `GetOperationByIdAsync`, `CreateOperationAsync`, `UpdateOperationAsync`, `DeleteOperationAsync`, `RecalculateAssetTotalAsync`, `GetTotalPurchasesAsync`, `GetTotalSalesAsync`, `GetOperationsByTypeAsync`.

## 7. Criterios de aceptación

- [ ] Una compra incrementa el total invertido y una venta lo reduce.
- [ ] Borrar una operación revierte su efecto.
- [ ] Los totales de compras y ventas coinciden con la suma por tipo.

## 8. Observaciones y limitaciones detectadas

- No se guarda cantidad de unidades (gramos, monedas, bitcoins), solo importes, por lo que no se puede calcular el precio medio de compra.
- Una venta resta el importe de venta del total invertido, no el coste de lo vendido.
