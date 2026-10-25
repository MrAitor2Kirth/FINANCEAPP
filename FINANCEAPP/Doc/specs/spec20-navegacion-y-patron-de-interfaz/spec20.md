# SPEC-20 · Navegación y patrón de interfaz común

| | |
|---|---|
| **Categoría** | 7. Plataforma y datos |
| **Identificador** | SPEC-20 |
| **Documento** | `spec20-navegacion-y-patron-de-interfaz/spec20.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Menú lateral, página de inicio y patrón unificado de listado, formulario y operaciones.

## 1. Descripción funcional

Define cómo se navega por la aplicación y el patrón visual y de interacción que comparten todas las categorías, para que el uso sea coherente.

## 2. Pantallas y rutas

- `/` – Home con tarjetas de acceso a los módulos.
- Menú lateral `NavMenu`: Home, Dashboard, Fondos de Inversión, Empresas, Activos Refugio, Propiedades, Cuentas Bancarias, Operaciones Acciones y Configuración.
- `/settings` – Configuración: sube el logo de la app (SPEC-20-01). Entrada «Configuración» en la sección «Sistema» del menú. El icono del programa (`.exe`, acceso directo, barra de tareas y, en móvil, el de la app) es el símbolo de ese logo sin texto: `Resources/AppIcon/appiconfg.png`, configurado por plataforma en el `.csproj`, y la pantalla de carga de Android usa el mismo símbolo (`Resources/Splash/splash.png`) sobre el color de fondo de la app (no se actualizan solos al subir otro logo).
- `/settings/countries` – catálogo de países con código, nombre, región y divisa (SPEC-01-02). Se llega desde la tarjeta «Países» de Configuración, que también incluye la sección «País base» (clave `BaseCountryCode`, España por defecto) cuya divisa es la de los totales.
- Tarjeta «Lectura de facturas con IA» en `/settings` (SPEC-14-01): clave de API de Claude (campo enmascarado, guardar y quitar; solo en el almacén seguro del dispositivo), modelo de IA (por defecto `claude-haiku-4-5-20251001`) y aviso fijo de que los documentos se envían a Anthropic y se facturan a la clave. Sin clave, la lectura con IA queda desactivada y todo lo demás funciona.
- Tarjeta «Tipos de cambio» en `/settings` (SPEC-01-03): una fila por divisa en uso con el tipo, su origen y fecha, «Actualizar», escritura a mano y «Actualizar todos». El botón «Detalles» (icono de cambio de divisa) abre la ventana compartida `ExchangeDetailsModal` en operaciones, histórico de ventas y empresas de otra divisa.
- `/indices` – índices para comparar en el gráfico de cotización (SPEC-05-04); se llega con el botón «Índices» de `/companies` (no tiene entrada en el menú).
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
- **Logo de la app** (SPEC-20-01): se sube en `/settings` (PNG, JPG o WebP de hasta 2 MB, máx. 6000 px por lado). Un script propio (`wwwroot/js/logoTools.js`) recorta los márgenes, lleva el fondo claro a blanco y guarda un PNG de 640 px como máximo. Se guarda en la tabla `AppSettings` (clave `AppLogo`) y se pinta con el componente `AppLogo` sobre una placa blanca redondeada (con `mix-blend-mode: multiply`) en cuatro sitios: cabecera del menú lateral, bloque principal de Inicio, cabecera del Dashboard y vista previa de Configuración. Sin logo se ve el rombo y el texto de siempre. «Quitar logo» pide confirmación.

## 4. Datos implicados

- Tabla `AppSettings` (entidad `AppSetting`: Key, TextValue, BinaryValue, UpdatedAt), creada desde `DatabaseSchemaUpgrader.EnsureAppSettingsTable`; la fila `AppLogo` guarda el tipo de contenido y la imagen. El resto de la capa es de presentación.

## 5. Flujo principal

1. El usuario entra por Home o por el menú.
2. Elige una categoría y llega al listado.
3. Desde el listado crea, edita o despliega operaciones sin salir de la sección.

## 6. Componentes técnicos

- `MainLayout.razor`, `NavMenu.razor`, `Routes.razor`, `Home.razor`, `NotFound.razor`, `DataTableBase.razor`.
- Logo: `Settings.razor`, `Components/Layout/AppLogo.razor`, `IAppSettingsService` / `AppSettingsService` (`GetLogoAsync`, `SaveLogoAsync`, `RemoveLogoAsync`, evento `LogoChanged`, caché en memoria) y `wwwroot/js/logoTools.js` (`processLogo`).
- `wwwroot/index.html`, `wwwroot/app.css`, hojas de estilo por componente.

## 7. Criterios de aceptación

- [ ] Todas las secciones son accesibles desde el menú lateral.
- [ ] El patrón listado → formulario → operaciones se repite en las cinco categorías.
- [ ] Una ruta inexistente muestra la pantalla de no encontrado.
- [ ] En `/settings` se sube un logo, se recorta, se guarda y aparece en el menú, Inicio, Dashboard y la vista previa; sin reiniciar al cambiarlo y tras reiniciar sigue ahí (SPEC-20-01).
- [ ] Un fichero que no es imagen, un SVG, más de 2 MB o más de 6000 px se rechazan con mensaje; «Quitar logo» devuelve la interfaz a su aspecto original.

## 8. Observaciones y limitaciones detectadas

- Bootstrap Icons y ApexCharts se cargan desde CDN, por lo que sin conexión faltarían iconos y gráficos.
- Las tablas de acciones y operaciones tienen muchas columnas y requieren adaptación para pantallas pequeñas (uso en móvil).
- Las rutas mezclan inglés y español (`/companies`, `/operations` frente a `/fondos`, `/banco`, `/inmuebles`).
- `DataTableBase` está definido pero las pantallas actuales usan tablas propias; conviene unificarlas.
- Existen páginas de plantilla sin uso funcional (`Counter`, `Weather`).
- El rediseño visual está aplicado a todas las páginas. Cada título lleva la marca de color de su clase y los resúmenes usan tarjetas con banda de color. El listado de Empresas agrupa sus cifras en 7 columnas (empresa, precio, inversión, dividendos, rentabilidad, yield y acciones). Los formularios y la tabla de Operaciones solo heredan colores y estilos globales.
- Inicio muestra el saldo de `IFinancialService` (legado v1), que no coincide con el patrimonio del Dashboard.

## 9. Features relacionadas

| Feature | Estado | Documentos |
|---|---|---|
| [SPEC-20-01 · Configuración y logo de la aplicación](spec20-01-app-logo-settings/spec20-01.md) | Implementada, pendiente de compilar y probar a mano | [spec](spec20-01-app-logo-settings/spec20-01.md) · [plan](spec20-01-app-logo-settings/plan20-01.md) · [tareas](spec20-01-app-logo-settings/task20-01.md) |
| [SPEC-01-02 · Países y divisa](../spec01-gestion-de-empresas/spec01-02-countries-and-currency/spec01-02.md) | Implementada, pendiente de compilar y probar a mano | [spec](../spec01-gestion-de-empresas/spec01-02-countries-and-currency/spec01-02.md) · [plan](../spec01-gestion-de-empresas/spec01-02-countries-and-currency/plan01-02.md) · [tareas](../spec01-gestion-de-empresas/spec01-02-countries-and-currency/task01-02.md) |
| [SPEC-01-03 · Tipo de cambio y totales convertidos](../spec01-gestion-de-empresas/spec01-03-exchange-rate/spec01-03.md) | Implementada, pendiente de compilar y probar a mano | [spec](../spec01-gestion-de-empresas/spec01-03-exchange-rate/spec01-03.md) · [plan](../spec01-gestion-de-empresas/spec01-03-exchange-rate/plan01-03.md) · [tareas](../spec01-gestion-de-empresas/spec01-03-exchange-rate/task01-03.md) |
| [SPEC-01-04 · Catálogo de empresas de mercado](../spec01-gestion-de-empresas/spec01-04-market-catalog/spec01-04.md) | Implementada, pendiente de compilar y probar a mano | [spec](../spec01-gestion-de-empresas/spec01-04-market-catalog/spec01-04.md) · [plan](../spec01-gestion-de-empresas/spec01-04-market-catalog/plan01-04.md) · [tareas](../spec01-gestion-de-empresas/spec01-04-market-catalog/task01-04.md) |
| [SPEC-14-01 · Documentos adjuntos de inmuebles y lectura con IA](../spec14-gestion-de-inmuebles/spec14-01-property-documents/spec14-01.md) | Spec cerrada, plan pendiente de aprobar | [spec](../spec14-gestion-de-inmuebles/spec14-01-property-documents/spec14-01.md) · [plan](../spec14-gestion-de-inmuebles/spec14-01-property-documents/plan14-01.md) · [tareas](../spec14-gestion-de-inmuebles/spec14-01-property-documents/task14-01.md) |
| [SPEC-17-01 · Módulo Histórico y capturas mensuales del patrimonio](../spec17-patrimonio-global-y-distribucion/spec17-01-wealth-snapshots/spec17-01.md) — entrada «Histórico» en el menú (sección «Análisis»), hub `/history` y «Volver» de `/sales-history` según origen | Implementada, pendiente de compilar y probar a mano | [spec](../spec17-patrimonio-global-y-distribucion/spec17-01-wealth-snapshots/spec17-01.md) · [plan](../spec17-patrimonio-global-y-distribucion/spec17-01-wealth-snapshots/plan17-01.md) · [tareas](../spec17-patrimonio-global-y-distribucion/spec17-01-wealth-snapshots/task17-01.md) |
| [SPEC-05-04 · Comparación de dos gráficos](../spec05-grafico-de-cotizacion/spec05-04-chart-comparison/spec05-04.md) — ruta nueva `/indices` con botón «Índices» en `/companies` | Implementada y compilada, pendiente de probar a mano | [spec](../spec05-grafico-de-cotizacion/spec05-04-chart-comparison/spec05-04.md) · [plan](../spec05-grafico-de-cotizacion/spec05-04-chart-comparison/plan05-04.md) · [tareas](../spec05-grafico-de-cotizacion/spec05-04-chart-comparison/task05-04.md) |
| [SPEC-21-01 · Calendario de dividendos](../spec21-dividend-calendar/spec21-01-dividend-calendar/spec21-01.md) | Implementada y compilada, pendiente de probar a mano | [spec](../spec21-dividend-calendar/spec21-01-dividend-calendar/spec21-01.md) · [plan](../spec21-dividend-calendar/spec21-01-dividend-calendar/plan21-01.md) · [tareas](../spec21-dividend-calendar/spec21-01-dividend-calendar/task21-01.md) |
