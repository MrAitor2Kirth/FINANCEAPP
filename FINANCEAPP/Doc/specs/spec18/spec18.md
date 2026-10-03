# SPEC-18 · Rendimiento de cartera, flujo de caja, operaciones recientes y alertas

| | |
|---|---|
| **Categoría** | 6. Dashboard y análisis |
| **Identificador** | SPEC-18 |
| **Documento** | `spec18/spec18.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Indicadores analíticos del dashboard: rentabilidad, ingresos/gastos, últimas operaciones y avisos.

## 1. Descripción funcional

Completa el dashboard con indicadores analíticos: rendimiento de la cartera de inversión, estadísticas de ingresos y gastos, las operaciones más recientes de todas las categorías y alertas automáticas sobre situaciones a vigilar.

## 2. Pantallas y rutas

- `/dashboard` – debajo de las tarjetas de patrimonio, el dashboard muestra: **Alertas**, tabla de **Rendimiento de la cartera**, **Flujo de caja** y tabla de **Operaciones recientes**.

## 3. Reglas de negocio

- **Rendimiento de cartera**: agrega fondos y acciones. Para fondos usa invertido y valor actual; para acciones usa el **valor actual real** (precio en vivo, o precio actual si no hay, × acciones en cartera) frente a la inversión inicial.
- Rentabilidad total = valor actual − invertido; porcentaje = rentabilidad / invertido × 100 (0 si no hay inversión). Se detalla por clase de activo.
- **Ingresos y gastos**: ingresos = alquileres cobrados; gastos = gastos de inmuebles no vendidos; flujo neto = ingresos − gastos.
- **Operaciones recientes**: reúne las 3 últimas operaciones de cada fondo, cuenta bancaria e inmueble, las ordena por fecha y devuelve las más recientes hasta un límite (10 por defecto). No incluye acciones ni activos refugio.
- **Alertas** (nivel advertencia): propiedades vacías sin ingresos; cuentas con saldo inferior a **1.000 €**; fondos con rentabilidad inferior a **−5 %**. Siempre se añade al final una alerta informativa «Dashboard actualizado».

## 4. Datos implicados

- Agregación de operaciones de fondos, banco, refugio, inmuebles y acciones.
- Modelos `PortfolioPerformance`, `AssetClassPerformance`, `IncomeExpenseStats`, `RecentOperation`, `DashboardAlert`.

## 5. Flujo principal

1. `DashboardService` consulta cada servicio.
2. Calcula rendimientos, flujo de caja y alertas con los umbrales definidos.
3. Devuelve los modelos a la página para su presentación.

## 6. Componentes técnicos

- `IDashboardService`: `GetPortfolioPerformanceAsync`, `GetIncomeExpenseStatsAsync`, `GetRecentOperationsAsync(limit)`, `GetDashboardAlertsAsync`.

## 7. Criterios de aceptación

- [ ] Una cuenta con menos de 1.000 € genera una alerta de saldo bajo.
- [ ] Un fondo por debajo de −5 % genera una alerta de bajo rendimiento.
- [ ] El flujo neto es ingresos de alquiler menos gastos de inmuebles.
- [ ] El rendimiento de las acciones refleja la diferencia entre el valor actual y lo invertido (ya no sale siempre 0 %).
- [ ] El dashboard muestra los cuatro bloques (alertas, rendimiento, flujo de caja y operaciones recientes).
- [ ] La alerta informativa «Dashboard actualizado» se añade siempre.

## 8. Observaciones y limitaciones detectadas

- **Resuelto**: el rendimiento de acciones valoraba las acciones por su inversión inicial y salía siempre 0 %; ahora usa `TotalInvestedValue` (precio en vivo × acciones). Para ver cifras actuales conviene pulsar antes «Actualizar Precios en Vivo» en `/companies` (SPEC-04).
- El flujo de caja solo contempla el inmobiliario; no incluye dividendos, intereses bancarios ni otros ingresos.
- Los umbrales de las alertas (1.000 € y −5 %) están fijados en el código.
- El dashboard carga todos los bloques en la misma operación: si uno falla, los siguientes no se muestran.
- Las alertas de nivel informativo se muestran siempre, incluso cuando hay avisos de advertencia.
