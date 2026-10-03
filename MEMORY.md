# MEMORY.md — FINANCEAPP

Memoria del proyecto entre sesiones. Máximo ~50 líneas: resume o elimina lo que ya no aporte.

## Estado actual
- App personal de patrimonio, un solo usuario y sin servidor. .NET 10 MAUI Blazor Hybrid + EF Core + SQLite local; páginas `.razor` → `I*Service` → `AppDbContext`. Cambios recientes sin compilar ni probar.
- Acciones: empresas y operaciones (Compra, Venta, Dividendo, Traspaso), recálculo de inversión y rentabilidad, precio en vivo manual (Yahoo) y gráfico diario/mensual/anual.
- Fondos (aportaciones, reembolsos, historial de valores), banco (saldo recalculado), activos refugio (compras y ventas) e inmuebles (ingresos, gastos y venta en `/inmuebles/vender/{id}`).
- Rediseño visual (sin compilar ni probar): tokens y sobrescritura de Bootstrap en `wwwroot/app.css`. Un color por clase de activo (acciones índigo, fondos turquesa, refugio ámbar, inmuebles ciruela, efectivo salvia) en menú, Inicio, Dashboard, títulos y tarjetas de resumen. Aplicado a todas las páginas; Empresas pasó de 15 a 7 columnas.
- Dashboard: patrimonio total y por clase, rendimiento, flujo de caja, operaciones recientes y alertas.
- Docs en `FINANCEAPP/doc` (README + 20 specs en `specs/specNN/specNN.md`; features en `specNN-MM-<name>/` con spec, plan y task) y `AGENTS.md` en la raíz. Feature implementada y pendiente de probar a mano: `spec02-01-sales-history` (histórico en `/sales-history`) y `spec02-02-total-amount-calculation` (importe de operaciones).

## Decisiones (y por qué)
1. Precios con el endpoint `v8/finance/chart` de Yahoo: el de cotizaciones exige token y cookies; se envía `User-Agent`, timeout 10 s.
2. Tickers europeos: se prueban sufijos de bolsa (`.MC`, `.PA`, `.MI`, `.DE`…) para que ACS funcione sin escribir `ACS.MC`.
3. Precio en vivo manual (botón), no automático: consultas secuenciales lentas y posible límite de Yahoo.
4. Ventas de acciones: solo cuentan operaciones abiertas. Las Ventas nuevas cierran compras por FIFO, se cierran ellas mismas y guardan el beneficio en `SalesHistory`; las antiguas abiertas siguen restando su importe completo.
5. Valor de cartera = precio en vivo, o precio manual si no hay.
6. Histórico de cotización en caché SQLite (fresco 12 h, 5 años); mensual y anual se agregan en servidor.
7. SQLite con `EnsureCreated` y `DatabaseSchemaUpgrader`, sin migraciones de EF: no se pierden datos al actualizar.
8. Operaciones enlazadas por nombre, no por clave foránea: más simple, pero obliga a borrar en cascada desde el servicio.
9. Totales guardados y recalculados tras cada operación, no al leer: listas rápidas.
10. Fondos: el valor se actualiza solo con el historial de valores; se retiró la operación de plusvalía.
11. Inmuebles en el patrimonio por valor actual, o por lo invertido si no lo hay; los vendidos no cuentan.
12. Borrar empresa es en cascada y transaccional (operaciones + histórico); el modal avisa de cuántas se pierden.
13. MAUI Blazor Hybrid: una base de código para Windows, Android e iOS.
14. ApexCharts por puente JS: superpone compras y cambia de granularidad; va por CDN.
15. Venta de inmuebles en una sola transacción (ingreso, estado Vendida, fecha y totales); el resultado previo usa la misma fórmula.
16. Histórico de ventas: el beneficio se guarda (no se recalcula solo) y borrar una Venta no reabre compras.
17. Importe de Compra y Venta calculado en `OperationAmountCalculator`: manda el precio por acción, si no el precio en vivo; Traspaso y Dividendo, importe manual.

## Aprendizajes y errores a evitar
1. Todo campo nuevo se persiste: modelo + columna en `DatabaseSchemaUpgrader` + guardado y lectura en el servicio; comprobar cerrando y reabriendo la app.
2. Las operaciones se enlazan por nombre: renombrar una entidad desvincula sus operaciones.
3. `EnsureCreated` no actualiza una BD existente; cambiar solo el modelo la rompe.
4. Los totales se guardan: recalcular tras crear, editar o borrar operaciones o quedan desfasados.
5. No dar por terminado un cambio sin compilarlo: en la nube no hay .NET; probar en Visual Studio sobre una copia.
6. Un control visible debe hacer algo: hubo un botón sin página, uno que no borraba y campos ignorados.
7. SQLite no admite transacciones anidadas: los recálculos van dentro de transacciones ya abiertas.
8. Tipos, estados y categorías son literales (`"Compra"`, `"Aportación"`): importan tildes y mayúsculas.
9. Los ficheros usan CRLF: respetarlo al editar para no ensuciar git con cambios de fin de línea.
10. Al dividir un lote de compra, el reparto de importe y comisión no debe perder céntimos: la parte pendiente se queda con el resto.
11. El importe manual se pierde al cambiar precio, acciones o comisión: es decisión del usuario, no un fallo.
12. Los datos reales son sagrados: probar sobre una copia y hacer backup antes de cambiar el esquema o borrar.

## Próximos pasos
1. Probar a mano el histórico de ventas y el nuevo cálculo del importe (fase 5 de `task02-01.md` y `task02-02.md`) sobre una copia de la BD. Compilar y probar los cambios recientes (incluido el rediseño visual): venta de inmuebles, historial de fondos, borrado de empresa y dashboard.
2. Preparar para móvil: ApexCharts e iconos en local, exportar/importar la BD y tablas para pantalla pequeña.
3. Valoraciones de fondos con fecha pasada (hoy siempre la de hoy).
4. Buscador y filtros en el listado de empresas, con los métodos ya existentes.
5. Cotización automática de activos refugio (hoy manual).
6. Limpiar código sin uso: métodos de plusvalía de fondos, `Counter`, `Weather` y `DataTableBase`.
7. Unificar las rutas que mezclan inglés y español, sin romper enlaces.
8. Deshacer ventas (reabrir compras) y histórico retroactivo de ventas antiguas.
9. Copiar las fuentes `.woff2` a `wwwroot/fonts` (hoy cae a la fuente del sistema) y revisar a ojo formularios y Operaciones tras el rediseño.
