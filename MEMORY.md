# MEMORY.md — FINANCEAPP

Memoria del proyecto entre sesiones. Máximo ~100 líneas: resume o elimina lo que ya no aporte. Las reglas permanentes están en `AGENTS.md`.

## Estado actual
- App personal de patrimonio, un solo usuario y sin servidor. .NET 10 MAUI Blazor Hybrid + EF Core + SQLite local; páginas `.razor` → `I*Service` → `AppDbContext`.
- Acciones (empresas, operaciones, ventas FIFO con histórico, divisas y tipo de cambio, catálogo de mercado), fondos, banco (saldo, traspasos, movimientos de compras, ventas y dividendos), refugio, inmuebles (con documentos e IA), Dashboard, capturas del patrimonio y logo.
- Gráfico de cotización: precio en vivo y compra/venta (SPEC-05-01), histórico completo por bloques y cinco temporalidades (SPEC-05-02), volumen en dinero, eje por sesiones, colores, leyenda que filtra y marcas Empresa/Global en la tabla `ChartMarks` (SPEC-05-03).
- Docs en `FINANCEAPP/Doc` (README, `constitution.md` y specs `specNN-<nombre>/` con features `specNN-MM-<name>/` de spec, plan y task).
- **Pendiente de probar a mano** (fase 5 de cada `taskNN-MM.md`, sobre una copia de la BD): 01-01 a 01-07, 02-01 a 02-08, 05-01 a 05-04, 21-01, 10-01, 10-02, 14-01, 17-01 y 20-01. Casi todas compiladas; 05-02 y 05-03 con el JS probado aparte con ApexCharts y datos sintéticos.
- Filtro por periodo en `/companies` (SPEC-01-09, 25/10/2026; compilado, sin probar en la app): vistas «Compras del periodo» y «Cartera al cierre», histórico diario de tipos (`CurrencyPairs`, `CurrencyRatePoints`) y «Proponer tipos» en `/settings` para las 5 operaciones USD sin tipo. Backup previo `Backups/BACKUP25102026.db`. Referencia en una copia: compras abiertas de 2025 = 23.461,39 € + 4.903,80 $ (5 empresas; 6.710,35 € + 1.293,56 $ ya vendidas); cartera al 31/12/2025 = 8 empresas, 38.120,74 € + 6.197,36 $ invertidos.
- Incluir vendidas (SPEC-01-11, 25/10/2026; compilado, sin probar en la app; backup `Backups/BACKUP25102026b.db`): selector en `/companies` que suma lo vendido de la vista; comprobado en una copia: 3 ventas (ACS en €, Abcellera en $), coste de las compras cerradas + beneficio = cobrado neto. Arreglado de paso: el filtro por periodo (01-09/01-10) cargaba las empresas sin divisa (USD como EUR). Listado por periodo (SPEC-01-10): filas con cifras del periodo, botones con `RealCompany`.
- Desglose por empresa (SPEC-01-12, 25/10/2026; compilado, sin probar en la app; backup `Backups/BACKUP25102026c.db`): flecha en `/companies` con lotes, ventas y dividendos; comprobado en una copia que la suma de los lotes de las 10 empresas = su fila (coste, acciones y dividendos). `/operations?company=` filtra por empresa. Capturas diarias y JSON para IA local (SPEC-17-03, 25/10/2026; compilado, **sin ejecutar**: la primera vez que se abra la app calculará la serie desde el 20/02/2024 en la BD real, solo en las tablas nuevas; backup `Backups/BACKUP25102026d.db`). Marca «recalcular desde X» en `AppDbContext.SaveChanges`. IA del usuario: Qwen3 4B Instruct 2507 en local (contexto ≥ 16.384).
- Comparación de gráficos (SPEC-05-04, implementada y compilada el 24/10/2026; sin probar a mano en la app): «Comparar» en el gráfico con otra empresa o un índice de `/indices`, base 100, rentabilidades y correlación del tramo visible. Tablas nuevas `BenchmarkIndices` y `BenchmarkPricePoints`; backup previo `Backups/BACKUP24102026e.db`. JS probado aparte con ApexCharts y datos sintéticos; `ChartComparison` con casos conocidos.
- Rentabilidad media con dividendos (SPEC-01-07) y rediseño de las tarjetas de `/companies` (SPEC-01-08, 25/10/2026; compilado, maqueta comprobada en el navegador, sin probar en la app): bloques «Cartera» (invertido, valor actual, resultado) y «Rentabilidad y diversificación». El 20,24 % por precio era correcto (con dividendos 24,35 %).
- Acciones con 2 decimales (SPEC-02-08, implementada y compilada el 24/10/2026; sin probar a mano): `decimal` en modelos y servicios, `ShareQuantity` para redondear, validar y mostrar; columnas `INTEGER` sin reconstruir (SQLite guarda el real). Backup previo: `Backups/BACKUP24102026d.db`.
- Calendario de dividendos (SPEC-21-01, implementada y compilada el 24/10/2026; sin probar a mano): `/dividends`, fecha en la ficha, generar la operación, Inicio, rombos en el gráfico con tramo futuro y navegador, y propuestas (Microsoft, pago 10/12/2026). Backup previo: `Backups/BACKUP24102026c.db`. Diseñada sin implementar: SPEC-17-02 (informe fiscal).

- Icono de la app = símbolo del logo de `/settings` sin texto (25/10/2026), en `Resources/AppIcon/appiconfg.png`; también en la pantalla de carga (`Resources/Splash/splash.png`, fondo `#F5F2ED`); publicado y acceso directo del escritorio actualizado.
- Ideas del proyecto en `FINANCEAPP/Doc/IDEAS.md` (25/10/2026), gestionado con la skill `/idea-app financeapp` (análoga a `/idea`, que usa el global `~/.claude/ideas/IDEAS.md`). Doce ideas, todas sin implementar y sin spec salvo el informe fiscal (SPEC-17-02, ya diseñada): módulos activables en `/settings`, QE e inyecciones monetarias (EE. UU., Europa, Asia, España), exportar listados a Excel, entidades ETF y fondos no indexados, movimientos diarios de patrimonio con detalle listas de seguimiento en Inicio, dibujar líneas en los gráficos, requisitos para comercializar la app app móvil con sincronización, informe fiscal (ID-10, feature SPEC-17-02) importación de CSV de brokers y mapa interactivo de ayuda dentro de la app. Antes de implementar una, retomarla con `/idea-app` y abrir su feature (`/feature-sdd`).

## Decisiones (y por qué)
1. Yahoo `v8/finance/chart` (el de cotizaciones exige token): `User-Agent`, 10 s, sufijos de bolsa para tickers europeos; precio en vivo de la lista manual (consultas lentas y límites).
2. Ventas: solo cuentan operaciones abiertas; las nuevas cierran compras por FIFO y guardan el beneficio en `SalesHistory` (no se recalcula solo); deshacer venta reabre compras.
3. Totales guardados y recalculados tras cada operación (listas rápidas); operaciones enlazadas por nombre (más simple, obliga a borrar en cascada desde el servicio).
4. Esquema solo con `DatabaseSchemaUpgrader` (sin migraciones): no se pierden datos al actualizar.
5. Fondos: valor solo con el historial de valores. Inmuebles en el patrimonio por valor actual (o invertido); vendidos fuera.
6. Gráfico: histórico en caché SQLite hasta la primera cotización, por bloques en segundo plano (el gráfico sale al momento); Horas solo en memoria; cada temporalidad abre con su tramo. Eje por sesiones (sin huecos de fines de semana ni noches, como un bróker). El modal comparte el `DbContext` de la app con un semáforo (uno propio dejó datos desfasados).
7. Volumen en dinero aproximado (acciones × cierre, sumado día a día en periodos agregados), calculado al leer: Yahoo solo da acciones.
8. Marcas del gráfico en tabla propia `ChartMarks` (CompanyId nulo = Global, sin FK; se borran con la empresa salvo las globales); sin fechas futuras (eso será el tipo «Pago de dividendo» de SPEC-21).
9. Capturas del patrimonio: foto que no se recalcula (si no, el histórico cambiaría al corregir operaciones).
10. Divisas: totales de Empresas y Dashboard con el tipo actual; el histórico de ventas con el tipo guardado en cada operación.
11. Dividendos: líneas en `DividendPayments` (no en `ChartMarks`); la operación solo la genera el usuario, con la línea Cobrada en la misma transacción; propuestas solo de dividendos anunciados (Yahoo no da los anunciados).

## Aprendizajes y errores a evitar
1. Todo campo nuevo se persiste: modelo + `DatabaseSchemaUpgrader` + servicio; comprobar cerrando y reabriendo la app.
2. `EnsureCreated` no actualiza una BD existente; cambiar solo el modelo la rompe.
3. Un formulario no debe enlazar la entidad del `DbContext` (es de toda la app): editar una copia.
4. SQLite no admite transacciones anidadas: los recálculos no abren otra. Una página no debe tragarse la excepción: muestra el mensaje.
5. En MAUI Blazor Hybrid los `Scoped` son de toda la app: nunca un segundo `DbContext` en un componente.
6. Tipos, estados y categorías son literales: importan tildes y mayúsculas. Un formulario de edición lejos de su fila parece un botón roto (pasó en `/banco`): abrirlo bajo la fila.
7. Los ficheros usan CRLF (algunos docs LF): respetarlo. Los heredocs de bash con comillas fallan: escribir los scripts en el scratchpad.
8. Al partir un lote no se pierden céntimos: la parte pendiente se queda con el resto.
9. No dar nada por hecho sin compilar; los datos reales solo se tocan con backup previo y las pruebas, sobre una copia.
10. ApexCharts: `updateOptions` copia serie y anotaciones a los gráficos del mismo grupo salvo con el 4.º parámetro a `false`; `updateSeries` puede reiniciar el zoom (guardar y reaplicar el tramo). Un eje de fechas deja huecos de fin de semana: usar eje por índices. El tooltip de la serie tapa el de las anotaciones: el detalle de operaciones y marcas va en el tooltip del día. Los eventos de ratón de las anotaciones no llegan con el ratón real: la línea vertical se gobierna desde el tooltip. Mezclar velas con una serie de línea hace que la escala empiece en 0. Redibujar y volver al tramo desde fechas lo alarga un poco (margen final): si eso dispara un aviso que redibuja, hay bucle; conservar `minX`/`maxX`. El «brush» mueve el gráfico sin «zoomed»: escuchar «updated».

## Próximos pasos
1. Probar a mano las features pendientes (lista de arriba), empezando por 05-02 y 05-03 (Yahoo `period1`/`period2`, `firstTradeDate`, límite de Horas, marcas y volumen).
2. Ingresar en el banco con «Ingresar en banco» las ventas nº 6 (ACS → SANTANDER), nº 4 y nº 2 (Abcellera → REVOLUT; la nº 2 pide tipo de cambio).
3. Implementar SPEC-17-02 (informe fiscal). Actualizar `DividendProposalsData.cs` cuando Mapfre, Enagás, Endesa y Thermo Fisher anuncien sus dividendos.
4. Preparar para móvil: ApexCharts e iconos en local y con versión fija, exportar/importar la BD, tablas para pantalla pequeña; fuentes `.woff2` en `wwwroot/fonts`.
5. Corregir que `RefreshLivePriceAsync` pise la hora al fallar; valoraciones de fondos con fecha pasada; cotización automática de refugio; buscador de empresas.
6. Limpiar código sin uso (plusvalía de fondos, `Counter`, `Weather`, `DataTableBase`, `GetDailyHistoryAsync`) previa consulta, y unificar rutas inglés/español sin romper enlaces.
