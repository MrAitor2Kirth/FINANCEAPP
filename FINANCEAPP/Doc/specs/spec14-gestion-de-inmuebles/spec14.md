# SPEC-14 · Gestión de inmuebles

| | |
|---|---|
| **Categoría** | 5. Inmobiliario |
| **Identificador** | SPEC-14 |
| **Documento** | `spec14-gestion-de-inmuebles/spec14.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Alta, edición, baja y consulta de propiedades con su estado.

## 1. Descripción funcional

Permite mantener las propiedades inmobiliarias del usuario con sus datos de compra, valor actual, estado y alquiler mensual, y consultarlas en un listado con acceso a sus operaciones.

## 2. Pantallas y rutas

- `/inmuebles` – listado.
- `/inmuebles/nueva` y `/inmuebles/editar/{id}` – formulario.
- Detalle desplegable con las operaciones de cada inmueble y, debajo, la sección «Documentos» (SPEC-14-01).

## 3. Reglas de negocio

- Campos obligatorios: nombre, tipo, precio de compra (mayor que 0), fecha de compra y estado.
- **Tipos de inmueble**: Apartamento, Casa, Dúplex, Local Comercial, Oficina, Garaje y Terreno.
- **Estados**: Alquilado, Vacío, Uso Propio, En Venta y Vendida (este último solo lo asigna la venta del inmueble).
- Datos opcionales: dirección, valor actual, alquiler mensual y fecha de venta.
- Si no se indica estado al crear, se asigna «Vacío».
- El servicio ofrece las vistas de propiedades **activas** (no vendidas), **vendidas**, **alquiladas** y por estado.
- Al **eliminar un inmueble** se eliminan también todas sus operaciones y todos sus documentos adjuntos (la confirmación indica cuántos documentos se perderán).
- **Documentos (SPEC-14-01)**: cada inmueble admite archivos adjuntos (recibos, facturas…) de cualquier tipo hasta 10 MB, sin ejecutables (.exe, .bat, .cmd, .com, .msi, .ps1, .vbs, .vbe, .js, .jse, .wsf, .scr, .lnk, .jar, .reg, .dll). Se guardan dentro de la base de datos, vinculados por `PropertyId` (renombrar el inmueble no los pierde), con descripción opcional. Se pueden abrir (copia temporal que se borra al arrancar), eliminar con confirmación y subir siempre, haya o no clave de IA.
- **Lectura con IA (opcional)**: botón «Leer con IA» en imágenes (JPG, PNG, GIF, WebP hasta 7 MB) y PDF, activo solo con clave de API de Claude guardada en Configuración. Antes de cada lectura se muestra una ventana que avisa de que el documento se envía a Anthropic y se factura a la clave del usuario; si el documento ya tiene datos, avisa de que se sustituirán. Rellena empresa, importe, fecha y tipo, que se pueden corregir a mano («Datos»). Si el documento no es una factura, no se rellena nada.
- **Crear gasto**: desde un documento se abre una ventana (tipo obligatorio entre Comunidad, IBI, Hipoteca, Reforma, Seguro y Servicios, importe mayor que 0, fecha y descripción) que crea un gasto del inmueble y lo enlaza al documento (`PropertyOperationId`). Un documento solo crea un gasto; al borrar ese gasto el enlace se limpia y el documento puede crear otro.

## 4. Datos implicados

- `PropertyDocument` (SPEC-14-01): Id, PropertyId, FileName, ContentType, SizeBytes, Content, Description, UploadedAt, ExtractedCompany, ExtractedAmount, ExtractedDate, ExtractedType, AiReadAt, PropertyOperationId.
- `Property`: Id, Name, PropertyType, Address, PurchasePrice, PurchaseDate, CurrentValue, Status, MonthlyRent, SaleDate, TotalInvested, TotalIncome, CreatedAt, UpdatedAt.

## 5. Flujo principal

1. El usuario abre `/inmuebles` y ve sus propiedades.
2. Crea o edita una propiedad con el formulario.
3. Despliega el detalle para ver ingresos y gastos.
4. Elimina la propiedad, lo que borra sus operaciones.

## 6. Componentes técnicos

- `Properties/PropertiesList.razor`, `PropertyForm.razor`, `PropertyDocumentsPanel.razor`.
- `IPropertyDocumentService`, `IClaudeDocumentReader` (API de mensajes de Anthropic, solo tras confirmar) e `IClaudeApiKeyStore` (la clave vive solo en `SecureStorage`, nunca en la BD, copias ni registros).
- `IPropertyService`: `GetAllPropertiesAsync`, `GetActivePropertiesAsync`, `GetSoldPropertiesAsync`, `GetPropertyByIdAsync`, `GetPropertyByNameAsync`, `GetPropertiesByStatusAsync`, `GetRentedPropertiesAsync`, `CreatePropertyAsync`, `UpdatePropertyAsync`, `DeletePropertyAsync`.

## 7. Criterios de aceptación

- [ ] Se puede crear un inmueble con los datos obligatorios.
- [ ] Sin estado se guarda como «Vacío».
- [ ] El listado distingue entre propiedades activas y vendidas.
- [ ] Eliminar un inmueble elimina sus operaciones.

## 8. Observaciones y limitaciones detectadas

- La compra inicial se registra solo en `PurchasePrice` (no genera operación), según la decisión «Opción B» del diseño.
- El estado y el tipo se guardan como texto libre.
- **Corregido (06/10/2026):** al crear un inmueble, `TotalInvested` quedaba a 0, por lo que el patrimonio (Dashboard e Inicio) no lo sumaba si no tenía valor actual. Ahora el alta guarda `TotalInvested = PurchasePrice` y la edición recalcula los totales (ya no los copia del formulario). Los inmuebles antiguos con total menor que el precio de compra se reparan al arrancar (`DatabaseSchemaUpgrader.RepairPropertyTotals`).
- **Corregido:** el formulario de edición trabajaba sobre la entidad del contexto; al cancelar, los cambios sin guardar se veían en otras pantallas. Ahora edita una copia.
- **Corregido:** borrar un inmueble podía fallar si sus documentos ya estaban cargados en el contexto.
- **Corregido:** en el listado, la columna y la tarjeta «Gastos» mostraban el total invertido; ahora muestran total invertido − precio de compra. El botón «Vender» es un icono (`bi-cash-coin`) como el resto de acciones.
- **Corregido:** Inicio mostraba el «Saldo total» de la tabla antigua `FinancialRecords`; ahora muestra el patrimonio total del Dashboard.

## 9. Features relacionadas

- [SPEC-14-01 · Documentos adjuntos de inmuebles y lectura con IA](spec14-01-property-documents/spec14-01.md): sección «Documentación» en cada inmueble para adjuntar recibos y facturas (cualquier archivo, hasta 10 MB), borrado en cascada con el inmueble y lectura opcional con IA de Claude (estado: implementada, pendiente de compilar y probar a mano).
