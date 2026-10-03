# SPEC-14 · Gestión de inmuebles

| | |
|---|---|
| **Categoría** | 5. Inmobiliario |
| **Identificador** | SPEC-14 |
| **Documento** | `spec14-gestion-de-inmuebles.md` |
| **Volver al índice** | [README de specs](README.md) |

> Alta, edición, baja y consulta de propiedades con su estado.

## 1. Descripción funcional

Permite mantener las propiedades inmobiliarias del usuario con sus datos de compra, valor actual, estado y alquiler mensual, y consultarlas en un listado con acceso a sus operaciones.

## 2. Pantallas y rutas

- `/inmuebles` – listado.
- `/inmuebles/nueva` y `/inmuebles/editar/{id}` – formulario.
- Detalle desplegable con las operaciones de cada inmueble.

## 3. Reglas de negocio

- Campos obligatorios: nombre, tipo, precio de compra (mayor que 0), fecha de compra y estado.
- **Tipos de inmueble**: Apartamento, Casa, Dúplex, Local Comercial, Oficina, Garaje y Terreno.
- **Estados**: Alquilado, Vacío, Uso Propio, En Venta y Vendida (este último solo lo asigna la venta del inmueble).
- Datos opcionales: dirección, valor actual, alquiler mensual y fecha de venta.
- Si no se indica estado al crear, se asigna «Vacío».
- El servicio ofrece las vistas de propiedades **activas** (no vendidas), **vendidas**, **alquiladas** y por estado.
- Al **eliminar un inmueble** se eliminan también todas sus operaciones.

## 4. Datos implicados

- `Property`: Id, Name, PropertyType, Address, PurchasePrice, PurchaseDate, CurrentValue, Status, MonthlyRent, SaleDate, TotalInvested, TotalIncome, CreatedAt, UpdatedAt.

## 5. Flujo principal

1. El usuario abre `/inmuebles` y ve sus propiedades.
2. Crea o edita una propiedad con el formulario.
3. Despliega el detalle para ver ingresos y gastos.
4. Elimina la propiedad, lo que borra sus operaciones.

## 6. Componentes técnicos

- `Properties/PropertiesList.razor`, `PropertyForm.razor`.
- `IPropertyService`: `GetAllPropertiesAsync`, `GetActivePropertiesAsync`, `GetSoldPropertiesAsync`, `GetPropertyByIdAsync`, `GetPropertyByNameAsync`, `GetPropertiesByStatusAsync`, `GetRentedPropertiesAsync`, `CreatePropertyAsync`, `UpdatePropertyAsync`, `DeletePropertyAsync`.

## 7. Criterios de aceptación

- [ ] Se puede crear un inmueble con los datos obligatorios.
- [ ] Sin estado se guarda como «Vacío».
- [ ] El listado distingue entre propiedades activas y vendidas.
- [ ] Eliminar un inmueble elimina sus operaciones.

## 8. Observaciones y limitaciones detectadas

- La compra inicial se registra solo en `PurchasePrice` (no genera operación), según la decisión «Opción B» del diseño.
- El estado y el tipo se guardan como texto libre.
