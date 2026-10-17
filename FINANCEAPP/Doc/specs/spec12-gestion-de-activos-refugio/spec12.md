# SPEC-12 · Gestión de activos refugio

| | |
|---|---|
| **Categoría** | 4. Activos refugio |
| **Identificador** | SPEC-12 |
| **Documento** | `spec12-gestion-de-activos-refugio/spec12.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Alta, edición, baja y consulta de metales, criptomonedas y bonos.

## 1. Descripción funcional

Permite mantener el catálogo de activos refugio (oro, plata, criptomonedas, bonos…) con su total invertido y su valor actual, y consultarlos en un listado con acceso a sus operaciones.

## 2. Pantallas y rutas

- `/activos-refugio` – listado.
- `/activos-refugio/nuevo` y `/activos-refugio/editar/{id}` – formulario.
- Detalle desplegable con las operaciones de cada activo.

## 3. Reglas de negocio

- Campos obligatorios: nombre y tipo de activo.
- **Tipos de activo**: Oro, Plata, Platino, Paladio, Bitcoin, Ethereum, Otras Criptomonedas y Bonos del Tesoro.
- Se registran el **total invertido** y el **valor actual**; el servicio calcula el total invertido global, el valor actual global y el desglose de inversión por tipo de activo.
- Al **eliminar un activo** se eliminan también todas sus operaciones.

## 4. Datos implicados

- `SafeHavenAsset`: Id, Name, AssetType, TotalInvested, CurrentValue, CreatedAt, UpdatedAt.

## 5. Flujo principal

1. El usuario abre `/activos-refugio` y ve los activos con inversión y valor.
2. Crea o edita un activo con el formulario.
3. Despliega el detalle para ver sus operaciones.
4. Elimina el activo, lo que borra sus operaciones.

## 6. Componentes técnicos

- `SafeHaven/SafeHavenAssetsList.razor`, `SafeHavenAssetForm.razor`.
- `ISafeHavenService`: `GetAllAssetsAsync`, `GetAssetByIdAsync`, `GetAssetByNameAsync`, `CreateAssetAsync`, `UpdateAssetAsync`, `DeleteAssetAsync`, `GetTotalInvestedAsync`, `GetTotalCurrentValueAsync`, `GetInvestmentByAssetTypeAsync`.

## 7. Criterios de aceptación

- [ ] Se puede crear un activo de cada tipo.
- [ ] El listado muestra inversión y valor actual.
- [ ] El desglose por tipo suma correctamente.
- [ ] Eliminar un activo elimina sus operaciones.

## 8. Observaciones y limitaciones detectadas

- El valor actual se introduce **a mano**; no hay cotización automática para metales ni criptomonedas.
- El formulario pide el total invertido, pero al registrar operaciones ese valor se **sobrescribe** con el recalculado desde las compras y ventas (SPEC-13).
- Las operaciones se vinculan por nombre (`AssetName`).
