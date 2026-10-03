# Especificaciones

Especificaciones detalladas de las **20 funcionalidades más importantes** de FINANCEAPP, agrupadas en 7 categorías. Cada spec describe la funcionalidad, sus pantallas, reglas de negocio, datos, flujo, componentes técnicos, criterios de aceptación y las observaciones o limitaciones detectadas en el código.

Vista general del proyecto: [`../README.md`](../README.md).

## 1. Acciones y empresas

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-01 | Gestión de empresas (alta, edición, baja y consulta) | [spec01-gestion-de-empresas.md](spec01-gestion-de-empresas.md) |
| SPEC-02 | Registro y gestión de operaciones de acciones | [spec02-operaciones-de-acciones.md](spec02-operaciones-de-acciones.md) |
| SPEC-03 | Cálculo automático de posición, dividendos y rentabilidad | [spec03-calculo-de-posicion-y-rentabilidad.md](spec03-calculo-de-posicion-y-rentabilidad.md) |
| SPEC-04 | Precio en vivo desde Yahoo Finance | [spec04-precio-en-vivo.md](spec04-precio-en-vivo.md) |
| SPEC-05 | Gráfico de cotización con operaciones superpuestas | [spec05-grafico-de-cotizacion.md](spec05-grafico-de-cotizacion.md) |

## 2. Fondos de inversión

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-06 | Gestión de fondos de inversión | [spec06-gestion-de-fondos.md](spec06-gestion-de-fondos.md) |
| SPEC-07 | Aportaciones y reembolsos de fondos | [spec07-aportaciones-y-reembolsos-de-fondos.md](spec07-aportaciones-y-reembolsos-de-fondos.md) |
| SPEC-08 | Actualización del valor del fondo e historial de valores | [spec08-actualizacion-de-valor-y-plusvalias-de-fondos.md](spec08-actualizacion-de-valor-y-plusvalias-de-fondos.md) |

## 3. Banco

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-09 | Gestión de cuentas bancarias | [spec09-gestion-de-cuentas-bancarias.md](spec09-gestion-de-cuentas-bancarias.md) |
| SPEC-10 | Movimientos bancarios y recálculo de saldo | [spec10-movimientos-bancarios-y-saldo.md](spec10-movimientos-bancarios-y-saldo.md) |
| SPEC-11 | Totales de ingresos y gastos y consulta por fechas | [spec11-totales-y-consultas-bancarias.md](spec11-totales-y-consultas-bancarias.md) |

## 4. Activos refugio

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-12 | Gestión de activos refugio | [spec12-gestion-de-activos-refugio.md](spec12-gestion-de-activos-refugio.md) |
| SPEC-13 | Compras y ventas de activos refugio | [spec13-operaciones-de-activos-refugio.md](spec13-operaciones-de-activos-refugio.md) |

## 5. Inmobiliario

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-14 | Gestión de inmuebles | [spec14-gestion-de-inmuebles.md](spec14-gestion-de-inmuebles.md) |
| SPEC-15 | Ingresos y gastos de inmuebles | [spec15-ingresos-y-gastos-de-inmuebles.md](spec15-ingresos-y-gastos-de-inmuebles.md) |
| SPEC-16 | Venta de un inmueble | [spec16-venta-de-inmuebles.md](spec16-venta-de-inmuebles.md) |

## 6. Dashboard y análisis

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-17 | Patrimonio global y distribución por clase de activo | [spec17-patrimonio-global-y-distribucion.md](spec17-patrimonio-global-y-distribucion.md) |
| SPEC-18 | Rendimiento de cartera, flujo de caja, operaciones recientes y alertas | [spec18-rendimiento-flujo-de-caja-y-alertas.md](spec18-rendimiento-flujo-de-caja-y-alertas.md) |

## 7. Plataforma y datos

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-19 | Persistencia, evolución del esquema y copias de seguridad | [spec19-persistencia-esquema-y-copias-de-seguridad.md](spec19-persistencia-esquema-y-copias-de-seguridad.md) |
| SPEC-20 | Navegación y patrón de interfaz común | [spec20-navegacion-y-patron-de-interfaz.md](spec20-navegacion-y-patron-de-interfaz.md) |

## Estructura de cada spec

1. Descripción funcional
2. Pantallas y rutas
3. Reglas de negocio
4. Datos implicados
5. Flujo principal
6. Componentes técnicos
7. Criterios de aceptación (casillas verificables)
8. Observaciones y limitaciones detectadas

## Hallazgos detectados al documentar

Estado de los puntos detectados al redactar las specs:

| Hallazgo | Estado |
|---|---|
| **SPEC-16** – El botón «Vender» de los inmuebles llevaba a una página inexistente. | Resuelto: nueva pantalla `PropertySell.razor`. |
| **SPEC-17** – Cifras de Activos Refugio al revés y patrimonio sumando lo invertido. | Resuelto: etiquetas corregidas y patrimonio con valor actual. |
| **SPEC-18** – Rendimiento de acciones siempre 0 % y bloques del dashboard sin pantalla. | Resuelto: cálculo con precio en vivo y bloques añadidos al dashboard. |
| **SPEC-01** – Eliminar una empresa dejaba sus operaciones huérfanas. | Resuelto: baja en cascada con aviso en el modal. |
| **SPEC-08** – Convivían dos mecanismos (opción A y B) para actualizar el valor de un fondo. | Resuelto: solo historial de valores; el botón de eliminar del historial ya funciona. |
| **SPEC-17** – Los inmuebles se valoraban por lo invertido, no por su valor de mercado. | Resuelto: se usa el valor actual si está informado. |
