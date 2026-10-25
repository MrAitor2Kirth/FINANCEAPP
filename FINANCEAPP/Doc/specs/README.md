# Especificaciones

Especificaciones detalladas de las **20 funcionalidades más importantes** de FINANCEAPP, agrupadas en 7 categorías. Cada spec describe la funcionalidad, sus pantallas, reglas de negocio, datos, flujo, componentes técnicos, criterios de aceptación y las observaciones o limitaciones detectadas en el código.

Vista general del proyecto: [`../README.md`](../README.md).

## Organización

Cada spec vive en su propia carpeta: `specs/specNN-<nombre>/specNN.md`. Es la referencia vigente de esa funcionalidad.

Las funcionalidades nuevas o los cambios importantes se documentan como **features** dentro de la carpeta de la spec principal a la que afectan: `specs/specNN-<nombre>/specNN-MM-<english-name>/`. Cada feature contiene tres ficheros:

- `specNN-MM.md`: requisitos (la petición, las decisiones cerradas y los criterios de aceptación).
- `planNN-MM.md`: el plan técnico aprobado.
- `taskNN-MM.md`: la lista de tareas con casillas para seguir el avance.

Cuando una feature se implementa, su resultado se vuelca en la spec base (`specNN.md`), y la carpeta de la feature queda como historial.

## 1. Acciones y empresas

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-01 | Gestión de empresas (alta, edición, baja y consulta) | [spec01-gestion-de-empresas/spec01.md](spec01-gestion-de-empresas/spec01.md) |
| SPEC-02 | Registro y gestión de operaciones de acciones | [spec02-operaciones-de-acciones/spec02.md](spec02-operaciones-de-acciones/spec02.md) |
| SPEC-03 | Cálculo automático de posición, dividendos y rentabilidad | [spec03-calculo-de-posicion-y-rentabilidad/spec03.md](spec03-calculo-de-posicion-y-rentabilidad/spec03.md) |
| SPEC-04 | Precio en vivo desde Yahoo Finance | [spec04-precio-en-vivo/spec04.md](spec04-precio-en-vivo/spec04.md) |
| SPEC-05 | Gráfico de cotización con operaciones superpuestas | [spec05-grafico-de-cotizacion/spec05.md](spec05-grafico-de-cotizacion/spec05.md) |
| SPEC-21 | Calendario de dividendos | [spec21-dividend-calendar/spec21.md](spec21-dividend-calendar/spec21.md) |

## 2. Fondos de inversión

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-06 | Gestión de fondos de inversión | [spec06-gestion-de-fondos/spec06.md](spec06-gestion-de-fondos/spec06.md) |
| SPEC-07 | Aportaciones y reembolsos de fondos | [spec07-aportaciones-y-reembolsos-de-fondos/spec07.md](spec07-aportaciones-y-reembolsos-de-fondos/spec07.md) |
| SPEC-08 | Actualización del valor del fondo e historial de valores | [spec08-actualizacion-de-valor-y-plusvalias-de-fondos/spec08.md](spec08-actualizacion-de-valor-y-plusvalias-de-fondos/spec08.md) |

## 3. Banco

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-09 | Gestión de cuentas bancarias | [spec09-gestion-de-cuentas-bancarias/spec09.md](spec09-gestion-de-cuentas-bancarias/spec09.md) |
| SPEC-10 | Movimientos bancarios y recálculo de saldo | [spec10-movimientos-bancarios-y-saldo/spec10.md](spec10-movimientos-bancarios-y-saldo/spec10.md) |
| SPEC-11 | Totales de ingresos y gastos y consulta por fechas | [spec11-totales-y-consultas-bancarias/spec11.md](spec11-totales-y-consultas-bancarias/spec11.md) |

## 4. Activos refugio

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-12 | Gestión de activos refugio | [spec12-gestion-de-activos-refugio/spec12.md](spec12-gestion-de-activos-refugio/spec12.md) |
| SPEC-13 | Compras y ventas de activos refugio | [spec13-operaciones-de-activos-refugio/spec13.md](spec13-operaciones-de-activos-refugio/spec13.md) |

## 5. Inmobiliario

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-14 | Gestión de inmuebles | [spec14-gestion-de-inmuebles/spec14.md](spec14-gestion-de-inmuebles/spec14.md) |
| SPEC-15 | Ingresos y gastos de inmuebles | [spec15-ingresos-y-gastos-de-inmuebles/spec15.md](spec15-ingresos-y-gastos-de-inmuebles/spec15.md) |
| SPEC-16 | Venta de un inmueble | [spec16-venta-de-inmuebles/spec16.md](spec16-venta-de-inmuebles/spec16.md) |

## 6. Dashboard y análisis

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-17 | Patrimonio global y distribución por clase de activo | [spec17-patrimonio-global-y-distribucion/spec17.md](spec17-patrimonio-global-y-distribucion/spec17.md) |
| SPEC-18 | Rendimiento de cartera, flujo de caja, operaciones recientes y alertas | [spec18-rendimiento-flujo-de-caja-y-alertas/spec18.md](spec18-rendimiento-flujo-de-caja-y-alertas/spec18.md) |

## 7. Plataforma y datos

| Spec | Funcionalidad | Documento |
|---|---|---|
| SPEC-19 | Persistencia, evolución del esquema y copias de seguridad | [spec19-persistencia-esquema-y-copias-de-seguridad/spec19.md](spec19-persistencia-esquema-y-copias-de-seguridad/spec19.md) |
| SPEC-20 | Navegación y patrón de interfaz común | [spec20-navegacion-y-patron-de-interfaz/spec20.md](spec20-navegacion-y-patron-de-interfaz/spec20.md) |

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
| SPEC-01-01 · Precio medio de compra y lista extendida de empresas (`average-price-extended-list`) | SPEC-01 (afecta también a SPEC-03 y SPEC-20) | Implementada, pendiente de compilar y probar a mano | [spec01-gestion-de-empresas/spec01-01-average-price-extended-list/spec01-01.md](spec01-gestion-de-empresas/spec01-01-average-price-extended-list/spec01-01.md) |
| SPEC-01-02 · Países y divisa (`countries-and-currency`) | SPEC-01 (afecta también a SPEC-02, SPEC-02-01, SPEC-17, SPEC-19 y SPEC-20) | Implementada, pendiente de compilar y probar a mano | [spec01-gestion-de-empresas/spec01-02-countries-and-currency/spec01-02.md](spec01-gestion-de-empresas/spec01-02-countries-and-currency/spec01-02.md) |
| SPEC-01-03 · Tipo de cambio y totales convertidos (`exchange-rate`) | SPEC-01 (afecta también a SPEC-02, SPEC-02-01, SPEC-17, SPEC-19 y SPEC-20) | Implementada, pendiente de compilar y probar a mano | [spec01-gestion-de-empresas/spec01-03-exchange-rate/spec01-03.md](spec01-gestion-de-empresas/spec01-03-exchange-rate/spec01-03.md) · [plan](spec01-gestion-de-empresas/spec01-03-exchange-rate/plan01-03.md) · [tareas](spec01-gestion-de-empresas/spec01-03-exchange-rate/task01-03.md) |
| SPEC-01-04 · Catálogo de empresas de mercado (`market-catalog`) | SPEC-01 (afecta también a SPEC-02, SPEC-19 y SPEC-20) | Implementada, pendiente de compilar y probar a mano | [spec01-gestion-de-empresas/spec01-04-market-catalog/spec01-04.md](spec01-gestion-de-empresas/spec01-04-market-catalog/spec01-04.md) · [plan](spec01-gestion-de-empresas/spec01-04-market-catalog/plan01-04.md) · [tareas](spec01-gestion-de-empresas/spec01-04-market-catalog/task01-04.md) |
| SPEC-01-05 · Resumen y listado de empresas con inversión (`invested-companies-summary`) | SPEC-01 (afecta también a SPEC-01-01, SPEC-01-03, SPEC-03 y SPEC-17) | Implementada y compilada, pendiente de probar a mano | [spec01-gestion-de-empresas/spec01-05-invested-companies-summary/spec01-05.md](spec01-gestion-de-empresas/spec01-05-invested-companies-summary/spec01-05.md) · [plan](spec01-gestion-de-empresas/spec01-05-invested-companies-summary/plan01-05.md) · [tareas](spec01-gestion-de-empresas/spec01-05-invested-companies-summary/task01-05.md) |
| SPEC-01-06 · Peso de cada empresa en la cartera (`portfolio-weight`) | SPEC-01 (afecta también a SPEC-01-01, SPEC-01-03 y SPEC-01-05) | Implementada y compilada, pendiente de probar a mano | [spec01-gestion-de-empresas/spec01-06-portfolio-weight/spec01-06.md](spec01-gestion-de-empresas/spec01-06-portfolio-weight/spec01-06.md) · [plan](spec01-gestion-de-empresas/spec01-06-portfolio-weight/plan01-06.md) · [tareas](spec01-gestion-de-empresas/spec01-06-portfolio-weight/task01-06.md) |
| SPEC-01-07 · Rentabilidad media con dividendos (`average-return-with-dividends`) | SPEC-01 (afecta también a SPEC-01-05, SPEC-01-06 y SPEC-03) | Implementada y compilada, pendiente de probar a mano | [spec01-gestion-de-empresas/spec01-07-average-return-with-dividends/spec01-07.md](spec01-gestion-de-empresas/spec01-07-average-return-with-dividends/spec01-07.md) · [plan](spec01-gestion-de-empresas/spec01-07-average-return-with-dividends/plan01-07.md) · [tareas](spec01-gestion-de-empresas/spec01-07-average-return-with-dividends/task01-07.md) |
| SPEC-01-08 · Rediseño de las tarjetas de resumen de empresas: cartera y rentabilidad (`summary-cards-redesign`) | SPEC-01 (afecta también a SPEC-01-05, SPEC-01-07 y SPEC-20) | Implementada y compilada, pendiente de probar a mano | [spec01-gestion-de-empresas/spec01-08-summary-cards-redesign/spec01-08.md](spec01-gestion-de-empresas/spec01-08-summary-cards-redesign/spec01-08.md) · [plan](spec01-gestion-de-empresas/spec01-08-summary-cards-redesign/plan01-08.md) · [tareas](spec01-gestion-de-empresas/spec01-08-summary-cards-redesign/task01-08.md) |
| SPEC-01-09 · Filtro por periodo en las tarjetas de empresas e histórico de tipos de cambio (`period-summary`) | SPEC-01 (afecta también a SPEC-01-03, SPEC-01-08, SPEC-02-05, SPEC-05-04, SPEC-19 y SPEC-20) | Implementada y compilada, pendiente de probar a mano | [spec01-gestion-de-empresas/spec01-09-period-summary/spec01-09.md](spec01-gestion-de-empresas/spec01-09-period-summary/spec01-09.md) · [plan](spec01-gestion-de-empresas/spec01-09-period-summary/plan01-09.md) · [tareas](spec01-gestion-de-empresas/spec01-09-period-summary/task01-09.md) |
| SPEC-01-10 · Listado de empresas filtrado por periodo (`period-list`) | SPEC-01 (afecta a SPEC-01-09) | Implementada y compilada, pendiente de probar a mano |
| SPEC-02-01 · Histórico de ventas de acciones (`sales-history`) | SPEC-02 (afecta también a SPEC-03) | Implementada, pendiente de probar a mano | [spec02-operaciones-de-acciones/spec02-01-sales-history/spec02-01.md](spec02-operaciones-de-acciones/spec02-01-sales-history/spec02-01.md) |
| SPEC-02-02 · Cálculo del importe total de una operación (`total-amount-calculation`) | SPEC-02 | Borrador, en revisión QA | [spec02-operaciones-de-acciones/spec02-02-total-amount-calculation/spec02-02.md](spec02-operaciones-de-acciones/spec02-02-total-amount-calculation/spec02-02.md) |
| SPEC-02-03 · Deshacer venta de acciones (`undo-sale`) | SPEC-02 (afecta también a SPEC-02-01, SPEC-03 y SPEC-19) | Implementada, pendiente de compilar y probar a mano | [spec02-operaciones-de-acciones/spec02-03-undo-sale/spec02-03.md](spec02-operaciones-de-acciones/spec02-03-undo-sale/spec02-03.md) |
| SPEC-02-04 · Ingreso en cuenta bancaria al vender acciones (`sale-bank-deposit`) | SPEC-02 (afecta también a SPEC-02-01, SPEC-02-03, SPEC-05-01, SPEC-01-03, SPEC-10, SPEC-10-01 y SPEC-19) | Implementada y compilada, pendiente de probar a mano | [spec02-operaciones-de-acciones/spec02-04-sale-bank-deposit/spec02-04.md](spec02-operaciones-de-acciones/spec02-04-sale-bank-deposit/spec02-04.md) · [plan](spec02-operaciones-de-acciones/spec02-04-sale-bank-deposit/plan02-04.md) · [tareas](spec02-operaciones-de-acciones/spec02-04-sale-bank-deposit/task02-04.md) |
| SPEC-02-05 · Filtro por periodo en el histórico de ventas (`sales-period-filter`) | SPEC-02 (afecta también a SPEC-02-01, SPEC-01-03 y SPEC-02-04) | Implementada y compilada, pendiente de probar a mano | [spec02-operaciones-de-acciones/spec02-05-sales-period-filter/spec02-05.md](spec02-operaciones-de-acciones/spec02-05-sales-period-filter/spec02-05.md) · [plan](spec02-operaciones-de-acciones/spec02-05-sales-period-filter/plan02-05.md) · [tareas](spec02-operaciones-de-acciones/spec02-05-sales-period-filter/task02-05.md) |
| SPEC-02-06 · Movimientos bancarios al comprar acciones y cobrar dividendos (`trade-bank-movements`) | SPEC-02 (afecta también a SPEC-02-04, SPEC-02-01, SPEC-02-03, SPEC-05-01, SPEC-01, SPEC-01-03, SPEC-10, SPEC-10-01, SPEC-11 y SPEC-19) | Implementada y compilada, pendiente de probar a mano | [spec02-operaciones-de-acciones/spec02-06-trade-bank-movements/spec02-06.md](spec02-operaciones-de-acciones/spec02-06-trade-bank-movements/spec02-06.md) · [plan](spec02-operaciones-de-acciones/spec02-06-trade-bank-movements/plan02-06.md) · [tareas](spec02-operaciones-de-acciones/spec02-06-trade-bank-movements/task02-06.md) |
| SPEC-02-07 · Cuenta bancaria editable de Compras y Dividendos, solo al crear o editar (`trade-bank-account-edit`) | SPEC-02 (afecta también a SPEC-02-06, SPEC-02-04, SPEC-02-03, SPEC-10 y SPEC-20) | Implementada y compilada, pendiente de probar a mano | [spec02-operaciones-de-acciones/spec02-07-trade-bank-account-edit/spec02-07.md](spec02-operaciones-de-acciones/spec02-07-trade-bank-account-edit/spec02-07.md) · [plan](spec02-operaciones-de-acciones/spec02-07-trade-bank-account-edit/plan02-07.md) · [tareas](spec02-operaciones-de-acciones/spec02-07-trade-bank-account-edit/task02-07.md) |
| SPEC-02-08 · Número de acciones con decimales (`fractional-shares`) | SPEC-02 (afecta también a SPEC-01, SPEC-02-01, SPEC-02-02, SPEC-02-04, SPEC-02-06, SPEC-03, SPEC-05-01, SPEC-21-01 y SPEC-19) | Implementada y compilada, pendiente de probar a mano | [spec02-operaciones-de-acciones/spec02-08-fractional-shares/spec02-08.md](spec02-operaciones-de-acciones/spec02-08-fractional-shares/spec02-08.md) · [plan](spec02-operaciones-de-acciones/spec02-08-fractional-shares/plan02-08.md) · [tareas](spec02-operaciones-de-acciones/spec02-08-fractional-shares/task02-08.md) |
| SPEC-05-01 · Gráfico de cotización con precio en vivo y operativa de compra y venta (`chart-live-trading`) | SPEC-05 (afecta también a SPEC-04, SPEC-02, SPEC-02-01, SPEC-02-02, SPEC-01-03 y SPEC-03) | Implementada, pendiente de compilar y probar a mano | [spec05-grafico-de-cotizacion/spec05-01-chart-live-trading/spec05-01.md](spec05-grafico-de-cotizacion/spec05-01-chart-live-trading/spec05-01.md) · [plan](spec05-grafico-de-cotizacion/spec05-01-chart-live-trading/plan05-01.md) · [tareas](spec05-grafico-de-cotizacion/spec05-01-chart-live-trading/task05-01.md) |
| SPEC-05-02 · Histórico completo y temporalidades del gráfico: Horas, Diario, Semanal, Mensual y Anual (`chart-timeframes`) | SPEC-05 (afecta también a SPEC-05-01, SPEC-04 y SPEC-19) | Implementada y compilada, pendiente de probar a mano | [spec05-grafico-de-cotizacion/spec05-02-chart-timeframes/spec05-02.md](spec05-grafico-de-cotizacion/spec05-02-chart-timeframes/spec05-02.md) · [plan](spec05-grafico-de-cotizacion/spec05-02-chart-timeframes/plan05-02.md) · [tareas](spec05-grafico-de-cotizacion/spec05-02-chart-timeframes/task05-02.md) |
| SPEC-05-03 · Volumen en dinero, colores de los marcadores y marcas temporales en el gráfico (`chart-volume-and-marks`) | SPEC-05 (afecta también a SPEC-05-01, SPEC-05-02, SPEC-01 y SPEC-19) | Implementada y compilada, pendiente de probar a mano | [spec05-grafico-de-cotizacion/spec05-03-chart-volume-and-marks/spec05-03.md](spec05-grafico-de-cotizacion/spec05-03-chart-volume-and-marks/spec05-03.md) · [plan](spec05-grafico-de-cotizacion/spec05-03-chart-volume-and-marks/plan05-03.md) · [tareas](spec05-grafico-de-cotizacion/spec05-03-chart-volume-and-marks/task05-03.md) |
| SPEC-05-04 · Comparación de dos gráficos (empresas e índices) y correlación (`chart-comparison`) | SPEC-05 (afecta también a SPEC-05-02, SPEC-05-03, SPEC-01, SPEC-19 y SPEC-20) | Implementada y compilada, pendiente de probar a mano | [spec05-grafico-de-cotizacion/spec05-04-chart-comparison/spec05-04.md](spec05-grafico-de-cotizacion/spec05-04-chart-comparison/spec05-04.md) · [plan](spec05-grafico-de-cotizacion/spec05-04-chart-comparison/plan05-04.md) · [tareas](spec05-grafico-de-cotizacion/spec05-04-chart-comparison/task05-04.md) |
| SPEC-10-01 · Corrección del alta, edición y borrado de movimientos bancarios (`bank-operations-fix`) | SPEC-10 (afecta también a SPEC-09, SPEC-11, SPEC-19 y SPEC-20) | Implementada, compilada, pendiente de probar a mano | [spec10-movimientos-bancarios-y-saldo/spec10-01-bank-operations-fix/spec10-01.md](spec10-movimientos-bancarios-y-saldo/spec10-01-bank-operations-fix/spec10-01.md) · [plan](spec10-movimientos-bancarios-y-saldo/spec10-01-bank-operations-fix/plan10-01.md) · [tareas](spec10-movimientos-bancarios-y-saldo/spec10-01-bank-operations-fix/task10-01.md) |
| SPEC-10-02 · Traspasos entre cuentas bancarias (`account-transfers`) | SPEC-10 (afecta también a SPEC-09, SPEC-10-01, SPEC-11 y SPEC-19) | Implementada y compilada, pendiente de probar a mano | [spec10-movimientos-bancarios-y-saldo/spec10-02-account-transfers/spec10-02.md](spec10-movimientos-bancarios-y-saldo/spec10-02-account-transfers/spec10-02.md) · [plan](spec10-movimientos-bancarios-y-saldo/spec10-02-account-transfers/plan10-02.md) · [tareas](spec10-movimientos-bancarios-y-saldo/spec10-02-account-transfers/task10-02.md) |
| SPEC-14-01 · Documentos adjuntos de inmuebles y lectura con IA (`property-documents`) | SPEC-14 (afecta también a SPEC-15, SPEC-19 y SPEC-20) | Implementada, pendiente de compilar y probar a mano | [spec14-gestion-de-inmuebles/spec14-01-property-documents/spec14-01.md](spec14-gestion-de-inmuebles/spec14-01-property-documents/spec14-01.md) · [plan](spec14-gestion-de-inmuebles/spec14-01-property-documents/plan14-01.md) · [tareas](spec14-gestion-de-inmuebles/spec14-01-property-documents/task14-01.md) |
| SPEC-20-01 · Configuración y logo de la aplicación (`app-logo-settings`) | SPEC-20 (afecta también a SPEC-19 y SPEC-17) | Implementada, pendiente de compilar y probar a mano | [spec20-navegacion-y-patron-de-interfaz/spec20-01-app-logo-settings/spec20-01.md](spec20-navegacion-y-patron-de-interfaz/spec20-01-app-logo-settings/spec20-01.md) |
| SPEC-17-01 · Módulo Histórico y capturas mensuales del patrimonio (`wealth-snapshots`) | SPEC-17 (afecta también a SPEC-18, SPEC-19, SPEC-20 y SPEC-02-01) | Implementada, pendiente de compilar y probar a mano | [spec17-patrimonio-global-y-distribucion/spec17-01-wealth-snapshots/spec17-01.md](spec17-patrimonio-global-y-distribucion/spec17-01-wealth-snapshots/spec17-01.md) · [plan](spec17-patrimonio-global-y-distribucion/spec17-01-wealth-snapshots/plan17-01.md) · [tareas](spec17-patrimonio-global-y-distribucion/spec17-01-wealth-snapshots/task17-01.md) |
| SPEC-17-02 · Informe fiscal anual para la Renta (`tax-report`) | SPEC-17 (afecta también a SPEC-02, SPEC-03, SPEC-07, SPEC-10, SPEC-13, SPEC-15, SPEC-16, SPEC-19 y SPEC-17-01) | Plan aprobado, pendiente de implementar (después de SPEC-17-01) | [spec17-patrimonio-global-y-distribucion/spec17-02-tax-report/spec17-02.md](spec17-patrimonio-global-y-distribucion/spec17-02-tax-report/spec17-02.md) · [plan](spec17-patrimonio-global-y-distribucion/spec17-02-tax-report/plan17-02.md) · [tareas](spec17-patrimonio-global-y-distribucion/spec17-02-tax-report/task17-02.md) |
| SPEC-21-01 · Calendario de dividendos: líneas de dividendo, fecha en la ficha, generar la operación, Inicio y marca en el gráfico (`dividend-calendar`) | SPEC-21 (afecta también a SPEC-01, SPEC-02, SPEC-02-06, SPEC-03, SPEC-04, SPEC-05, SPEC-05-02, SPEC-05-03, SPEC-19 y SPEC-20) | Implementada y compilada, pendiente de probar a mano | [spec21-dividend-calendar/spec21-01-dividend-calendar/spec21-01.md](spec21-dividend-calendar/spec21-01-dividend-calendar/spec21-01.md) · [plan](spec21-dividend-calendar/spec21-01-dividend-calendar/plan21-01.md) · [tareas](spec21-dividend-calendar/spec21-01-dividend-calendar/task21-01.md) |
