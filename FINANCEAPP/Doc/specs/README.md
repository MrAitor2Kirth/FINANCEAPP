# Especificaciones

Especificaciones detalladas de las **20 funcionalidades más importantes** de FINANCEAPP, agrupadas en 7 categorías. Cada spec describe la funcionalidad, sus pantallas, reglas de negocio, datos, flujo, componentes técnicos, criterios de aceptación y las observaciones o limitaciones detectadas en el código.

Vista general del proyecto: [`../README.md`](../README.md).

## Organización

Cada spec vive en su propia carpeta: `specs/specNN/specNN.md`. Es la referencia vigente de esa funcionalidad.

Las funcionalidades nuevas o los cambios importantes se documentan como **features** dentro de la carpeta de la spec principal a la que afectan: `specs/specNN/specNN-MM-<english-name>/`. Cada feature contiene tres ficheros:

- `specNN-MM.md`: requisitos (la petición, las decisiones cerradas y los criterios de aceptación).
- `planNN-MM.md`: el plan técnico aprobado.
- `taskNN-MM.md`: la lista de tareas con casillas para seguir el avance.

Cuando una feature se implementa, su resultado se vuelca en la spec base (`specNN.md`), y la carpeta de la feature queda como historial.

## 1. Acciones y empresas

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-01 | Gestión de empresas (alta, edición, baja y consulta) | [spec01/spec01.md](spec01/spec01.md) |
| SPEC-02 | Registro y gestión de operaciones de acciones | [spec02/spec02.md](spec02/spec02.md) |
| SPEC-03 | Cálculo automático de posición, dividendos y rentabilidad | [spec03/spec03.md](spec03/spec03.md) |
| SPEC-04 | Precio en vivo desde Yahoo Finance | [spec04/spec04.md](spec04/spec04.md) |
| SPEC-05 | Gráfico de cotización con operaciones superpuestas | [spec05/spec05.md](spec05/spec05.md) |

## 2. Fondos de inversión

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-06 | Gestión de fondos de inversión | [spec06/spec06.md](spec06/spec06.md) |
| SPEC-07 | Aportaciones y reembolsos de fondos | [spec07/spec07.md](spec07/spec07.md) |
| SPEC-08 | Actualización del valor del fondo e historial de valores | [spec08/spec08.md](spec08/spec08.md) |

## 3. Banco

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-09 | Gestión de cuentas bancarias | [spec09/spec09.md](spec09/spec09.md) |
| SPEC-10 | Movimientos bancarios y recálculo de saldo | [spec10/spec10.md](spec10/spec10.md) |
| SPEC-11 | Totales de ingresos y gastos y consulta por fechas | [spec11/spec11.md](spec11/spec11.md) |

## 4. Activos refugio

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-12 | Gestión de activos refugio | [spec12/spec12.md](spec12/spec12.md) |
| SPEC-13 | Compras y ventas de activos refugio | [spec13/spec13.md](spec13/spec13.md) |

## 5. Inmobiliario

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-14 | Gestión de inmuebles | [spec14/spec14.md](spec14/spec14.md) |
| SPEC-15 | Ingresos y gastos de inmuebles | [spec15/spec15.md](spec15/spec15.md) |
| SPEC-16 | Venta de un inmueble | [spec16/spec16.md](spec16/spec16.md) |

## 6. Dashboard y análisis

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-17 | Patrimonio global y distribución por clase de activo | [spec17/spec17.md](spec17/spec17.md) |
| SPEC-18 | Rendimiento de cartera, flujo de caja, operaciones recientes y alertas | [spec18/spec18.md](spec18/spec18.md) |

## 7. Plataforma y datos

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-19 | Persistencia, evolución del esquema y copias de seguridad | [spec19/spec19.md](spec19/spec19.md) |
| SPEC-20 | Navegación y patrón de interfaz común | [spec20/spec20.md](spec20/spec20.md) |

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

## Features

| Feature | Spec principal | Estado | Documento |
|---|---|---|---|
| SPEC-02-01 · Histórico de ventas de acciones (`sales-history`) | SPEC-02 (afecta también a SPEC-03) | Implementada, pendiente de probar a mano | [spec02/spec02-01-sales-history/spec02-01.md](spec02/spec02-01-sales-history/spec02-01.md) |
| SPEC-02-02 · Cálculo del importe total de una operación (`total-amount-calculation`) | SPEC-02 | Borrador, en revisión QA | [spec02/spec02-02-total-amount-calculation/spec02-02.md](spec02/spec02-02-total-amount-calculation/spec02-02.md) |
