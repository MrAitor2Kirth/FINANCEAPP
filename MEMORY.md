# MEMORY.md — FINANCEAPP

Memoria del proyecto entre sesiones. Máximo ~50 líneas: resume o elimina lo que ya no aporte.

## Estado actual
- App personal de patrimonio, un solo usuario y sin servidor. .NET 10 MAUI Blazor Hybrid + EF Core + SQLite local; páginas `.razor` → `I*Service` → `AppDbContext`. Cambios recientes sin compilar ni probar.
- Acciones: empresas y operaciones (Compra, Venta, Dividendo, Traspaso), recálculo de inversión y rentabilidad, precio en vivo manual (Yahoo) y gráfico diario/mensual/anual.
- Fondos (aportaciones, reembolsos, historial de valores), banco (saldo recalculado), activos refugio (compras y ventas) e inmuebles (ingresos, gastos y venta en `/inmuebles/vender/{id}`).
- Rediseño visual (sin compilar ni probar): tokens y sobrescritura de Bootstrap en `wwwroot/app.css`. Un color por clase de activo (acciones índigo, fondos turquesa, refugio ámbar, inmuebles ciruela, efectivo salvia) en menú, Inicio, Dashboard, títulos y tarjetas de resumen. Aplicado a todas las páginas; Empresas pasó de 15 a 7 columnas.
- Dashboard: patrimonio total y por clase, rendimiento, flujo de caja, operaciones recientes y alertas.
- Histórico (SPEC-17-01, implementada sin compilar): hub `/history`, capturas mensuales del patrimonio en `/history/wealth` (listado con variaciones, gráfico Mensual/Anual, nota, CSV) y botón «Capturar patrimonio» en el Dashboard. SPEC-17-02 (informe fiscal anual) con spec, plan y tareas aprobados, pendiente de implementar.
- Docs en `FINANCEAPP/doc` (README + 20 specs en `specs/specNN-<nombre>/specNN.md`; features en `specNN-MM-<name>/` con spec, plan y task) y `AGENTS.md` en la raíz. Feature implementada y pendiente de probar a mano: `spec02-01-sales-history` (histórico en `/sales-history`), `spec02-02-total-amount-calculation` (importe de operaciones) `spec02-03-undo-sale` (deshacer venta) `spec20-01-app-logo-settings` (logo en `/settings`) y `spec01-01-average-price-extended-list` (precio medio de compra y lista extendida en `/companies`), todas sin compilar.

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
16. Histórico de ventas: el beneficio se guarda (no se recalcula solo). Deshacer venta reabre las compras y borra la Venta y el registro; las operaciones cerradas por una venta están bloqueadas en `/operations`.
17. Importe de Compra y Venta calculado en `OperationAmountCalculator`: manda el precio por acción, si no el precio en vivo; Traspaso y Dividendo, importe manual.
18. Logo de la app en la BD (`AppSettings`), recortado en el navegador al subirlo y pintado solo con `AppLogo` sobre una placa blanca; sin logo se ve la marca de siempre.
19. Divisas (SPEC-01-02): país con divisa en el catálogo `Countries`; la empresa tiene país (obligatorio al guardar) y la operación guarda `CurrencyCode`. Totales solo con la divisa base (país base en Configuración); el tipo de cambio queda para SPEC-01-03.
20. Catálogo de mercado (SPEC-01-04): tabla `MarketCompanies` (647 empresas de IBEX 35, CAC 40, DAX 40, EURO STOXX 50, S&P 500, Dow Jones y Nasdaq-100) generada una vez y cargada por `EnsureMarketCatalogTable` según `MarketCatalogVersion`; solo lectura, rellena el alta de empresa. FTSE 100 excluido (Yahoo cotiza Londres en peniques). Yahoo no es accesible desde la nube: los tickers se comprueban en la app con «Comprobar». Pendiente: compilar y probar a mano (task01-04 fase 5).
21. Tipo de cambio (SPEC-01-03): tabla `ExchangeRates` (divisa + divisa base) con tipo actual de Yahoo (par directo o inverso) o manual, actualizado solo con botón en Configuración. Totales de Empresas y Dashboard convertidos con el tipo actual; sin tipo, la empresa queda fuera con nota. Cada operación guarda `ExchangeRate`/`ExchangeRateTo` solo para «Detalles» (operaciones, ventas y empresas). Pendiente: compilar y probar a mano (task01-03 fase 5).
22. Documentos de inmuebles (SPEC-14-01): archivos dentro de la BD (`PropertyDocuments`, enlace por `PropertyId`, borrado en cascada desde `PropertyService`). Lectura con IA opcional vía clave de API de Claude en `SecureStorage` (no hay login con cuenta Claude); siempre con confirmación previa porque el documento sale a Anthropic. Pendiente: compilar y probar (task14-01 fase 5).

23. Capturas del patrimonio (SPEC-17-01): tabla `WealthSnapshots`, una por año y mes, foto de `GetDashboardSummaryAsync` que no se recalcula (si se recalculara, el histórico cambiaría al corregir operaciones). Banco sin invertido; variaciones al leer y nunca entre divisas distintas. CSV con `Share` de MAUI (sin paquetes).

24. Gráfico con precio en vivo (SPEC-05-01, implementada sin compilar): precio único de referencia (Yahoo v8 no da bid/ask), refresco cada 30 s guardado en la empresa, compra/venta con mini-formulario vía `RegisterQuickTradeAsync`; bloqueo solo con fecha de hoy y mercado cerrado; capas (velas, volumen, precio medio) en `AppSettings`. El modal comparte el `DbContext` de la app (semáforo + espera al cerrar); tuvo uno propio y causó datos desfasados (16/10/2026).

## Aprendizajes y errores a evitar
- Un formulario de edición no debe enlazar la entidad del `DbContext` (es de toda la app): editar una copia, o al cancelar los cambios quedan en memoria. Pasó en inmuebles (06/10/2026), junto con el alta que dejaba `TotalInvested` a 0.
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
13. En MAUI Blazor Hybrid los servicios `Scoped` son de toda la app. No crear un segundo `DbContext` en un componente: el principal se queda con entidades desfasadas y EF no guarda cambios sobre ellas (una venta deshecha no reabrió las compras de ACS). Serializar con semáforo en su lugar. Al arrancar, `RepairOrphanSaleLinksAsync` reabre compras ligadas a ventas que ya no existen.

## Próximos pasos
- Compilar y probar SPEC-05-01 (fase 5 de `task05-01.md`) sobre una copia de la BD. Pendiente: ApexCharts en local y con versión fija; corregir que `RefreshLivePriceAsync` pise la hora al fallar.
0. Compilar y probar SPEC-17-01 (fase 5 de `task17-01.md`) sobre una copia de la BD; después implementar SPEC-17-02 (informe fiscal).
1. Compilar y probar a mano SPEC-01-02 (países y divisa, fase 5 de `task01-02.md`) sobre una copia de la BD; después SPEC-01-03 (tipo de cambio). Probar a mano el logo y la configuración (fase 5 de `task20-01.md`), el histórico de ventas y el nuevo cálculo del importe (fase 5 de `task02-01.md` y `task02-02.md`) sobre una copia de la BD. Compilar y probar los cambios recientes (incluido el rediseño visual): venta de inmuebles, historial de fondos, borrado de empresa y dashboard.
2. Preparar para móvil: ApexCharts e iconos en local, exportar/importar la BD y tablas para pantalla pequeña.
3. Valoraciones de fondos con fecha pasada (hoy siempre la de hoy).
4. Buscador y filtros en el listado de empresas, con los métodos ya existentes.
5. Cotización automática de activos refugio (hoy manual).
6. Limpiar código sin uso: métodos de plusvalía de fondos, `Counter`, `Weather` y `DataTableBase`.
7. Unificar las rutas que mezclan inglés y español, sin romper enlaces.
8. Probar a mano «Deshacer venta» (fase 5 de `task02-03.md`) y generar histórico retroactivo de ventas antiguas (hoy se eliminan a mano).
9. Copiar las fuentes `.woff2` a `wwwroot/fonts` (hoy cae a la fuente del sistema) y revisar a ojo formularios y Operaciones tras el rediseño.
