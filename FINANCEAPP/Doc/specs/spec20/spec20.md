# SPEC-20 · Navegación y patrón de interfaz común

| | |
|---|---|
| **Categoría** | 7. Plataforma y datos |
| **Identificador** | SPEC-20 |
| **Documento** | `spec20/spec20.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Menú lateral, página de inicio y patrón unificado de listado, formulario y operaciones.

## 1. Descripción funcional

Define cómo se navega por la aplicación y el patrón visual y de interacción que comparten todas las categorías, para que el uso sea coherente.

## 2. Pantallas y rutas

- `/` – Home con tarjetas de acceso a los módulos.
- Menú lateral `NavMenu`: Home, Dashboard, Fondos de Inversión, Empresas, Activos Refugio, Propiedades, Cuentas Bancarias y Operaciones Acciones.
- Pantalla no encontrada (`NotFound`) para rutas inexistentes.

## 3. Reglas de negocio

- **Patrón unificado** en todas las categorías: listado de la entidad principal → formulario de alta/edición → operaciones asociadas desplegables bajo cada fila.
- Cada fila ofrece acciones de editar, eliminar y ver detalle; la baja pide confirmación.
- El componente `DataTableBase<TItem>` encapsula una tabla reutilizable (título, botón de alta, plantilla de filas, fila de operaciones, mensaje «No hay registros disponibles» y paginación opcional).
- Interfaz en español, basada en Bootstrap con iconos Bootstrap Icons. El aspecto propio (colores, tipografías y componentes comunes) está en variables CSS de `wwwroot/app.css` que sobrescriben las de Bootstrap.
- Sistema visual «instrumento de precisión» en versión cálida: fondo gris cálido, texto en azul índigo oscuro, menú lateral índigo profundo y un color por clase de activo (acciones índigo, fondos turquesa, refugio ámbar, inmuebles ciruela, efectivo salvia). Verde y rojo se reservan para ganancias y pérdidas (con flecha ▲/▼ además del color). Importes en tipografía monoespaciada con cifras tabulares.
- El menú lateral es índigo oscuro, con el elemento activo marcado en ámbar; ya no hay barra superior con enlaces externos.
- Las tipografías previstas son Bricolage Grotesque, Instrument Sans y Spline Sans Mono desde `wwwroot/fonts`; si faltan los `.woff2` se usa la fuente del sistema (Segoe UI, Consolas).
- La página de host define viewport móvil y una zona de seguridad superior (`status-bar-safe-area`).
- Existe un banner de error no controlado de Blazor con opción de recargar.

## 4. Datos implicados

- No aplica (capa de presentación).

## 5. Flujo principal

1. El usuario entra por Home o por el menú.
2. Elige una categoría y llega al listado.
3. Desde el listado crea, edita o despliega operaciones sin salir de la sección.

## 6. Componentes técnicos

- `MainLayout.razor`, `NavMenu.razor`, `Routes.razor`, `Home.razor`, `NotFound.razor`, `DataTableBase.razor`.
- `wwwroot/index.html`, `wwwroot/app.css`, hojas de estilo por componente.

## 7. Criterios de aceptación

- [ ] Todas las secciones son accesibles desde el menú lateral.
- [ ] El patrón listado → formulario → operaciones se repite en las cinco categorías.
- [ ] Una ruta inexistente muestra la pantalla de no encontrado.

## 8. Observaciones y limitaciones detectadas

- Bootstrap Icons y ApexCharts se cargan desde CDN, por lo que sin conexión faltarían iconos y gráficos.
- Las tablas de acciones y operaciones tienen muchas columnas y requieren adaptación para pantallas pequeñas (uso en móvil).
- Las rutas mezclan inglés y español (`/companies`, `/operations` frente a `/fondos`, `/banco`, `/inmuebles`).
- `DataTableBase` está definido pero las pantallas actuales usan tablas propias; conviene unificarlas.
- Existen páginas de plantilla sin uso funcional (`Counter`, `Weather`).
- El rediseño visual está aplicado a todas las páginas. Cada título lleva la marca de color de su clase y los resúmenes usan tarjetas con banda de color. El listado de Empresas agrupa sus cifras en 7 columnas (empresa, precio, inversión, dividendos, rentabilidad, yield y acciones). Los formularios y la tabla de Operaciones solo heredan colores y estilos globales.
- Inicio muestra el saldo de `IFinancialService` (legado v1), que no coincide con el patrimonio del Dashboard.
