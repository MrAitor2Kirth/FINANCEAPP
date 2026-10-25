# SPEC-17 · Patrimonio global y distribución por clase de activo

| | |
|---|---|
| **Categoría** | 6. Dashboard y análisis |
| **Identificador** | SPEC-17 |
| **Documento** | `spec17-patrimonio-global-y-distribucion/spec17.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Resumen del valor total del patrimonio y de cada categoría.

## 1. Descripción funcional

Muestra en una sola pantalla el patrimonio total del usuario y el peso de cada categoría de inversión: acciones, fondos, banco, activos refugio e inmuebles.

## 2. Pantallas y rutas

- `/dashboard` – tarjetas de Patrimonio Total, Efectivo Bancario, Acciones, Fondos de Inversión y Activos Refugio, y sección «Acceso Rápido a Módulos».
- `/` – página de inicio con accesos a los módulos.
- `/history` – módulo Histórico (SPEC-17-01): tarjetas de Histórico de ventas, Patrimonio e Informe fiscal (próximamente) y botón «Capturar patrimonio».
- `/history/wealth` – histórico del patrimonio: gráfico de evolución, listado de capturas con variaciones, vista Mensual / Anual, filtro por año y exportación CSV.
- El Dashboard tiene el botón «Capturar patrimonio» y, bajo el total, la variación frente a la última captura de un mes anterior.

## 3. Reglas de negocio

- **Patrimonio total** = acciones (valor invertido actual) + fondos (valor actual) + banco (saldo total) + activos refugio (**valor actual**) + inmuebles activos (**valor actual si está informado; si no, total invertido**).
- **Acciones**: se muestran la inversión inicial y el valor actual (con precio en vivo). Desde SPEC-01-02 los importes usan el símbolo de la divisa base (la del país base de Configuración) y, desde SPEC-01-03, las empresas en otra divisa se convierten a ella con el tipo actual de Configuración (`IExchangeRateService.GetConvertedTotalsAsync`); las que no tienen tipo quedan fuera y el Dashboard muestra una nota que las cuenta (`GetCurrencyInfoAsync`).
- **Fondos**: se muestran total invertido y valor actual.
- **Inmuebles**: solo cuentan los **no vendidos**, valorados por su **valor actual** cuando está informado y, si no, por su total invertido.
- También se informan el número de elementos de cada categoría y la fecha de última actualización.
- **Activos refugio**: se muestran primero lo invertido y después el valor actual, con el mismo criterio que acciones y fondos.
- El reparto por clase de activo usa los mismos valores que el patrimonio total: acciones (valor actual), fondos (valor actual), banco, activos refugio (valor actual) e inmuebles activos (valor actual o, si no hay, total invertido), de modo que la suma del reparto coincide con el patrimonio.
- **Capturas del patrimonio** (SPEC-17-01): una por año y mes (mes en curso o anterior), con los importes de `GetDashboardSummaryAsync` en la divisa base: valor e invertido por clase, saldo bancario (sin invertido), total, total invertido (sin banco), divisa, empresas excluidas y nota. Es una foto: no se recalcula; capturar de nuevo el mismo mes la sustituye con confirmación. Antes de capturar se avisa si hay precios en vivo de más de 24 h o empresas sin tipo de cambio.
- **Variaciones** (se calculan al leer): frente a la captura anterior existente y frente al mismo mes del año anterior; «—» si no hay o la divisa base es distinta. Vista anual = la captura del mes más alto de cada año. Plusvalía latente = total − banco − invertido.

## 4. Datos implicados

- Agregación de `CompanyDetail`, `Fund`, `BankAccount`, `SafeHavenAsset` y `Property`.
- Modelo `DashboardSummary`.
- Tabla `WealthSnapshots` (entidad `WealthSnapshot`, índice único año + mes) y `DashboardSummary.RealEstateInvestedTotal`.

## 5. Flujo principal

1. El usuario abre el dashboard.
2. `DashboardService` obtiene de cada servicio los totales.
3. Se componen las tarjetas con los importes y el patrimonio total.

## 6. Componentes técnicos

- `FinancialDashboard.razor`, `Home.razor`.
- `IDashboardService`: `GetDashboardSummaryAsync`, `GetTotalPatrimonyAsync`, `GetPatrimonyByAssetClassAsync`.
- `IWealthSnapshotService` / `WealthSnapshotService`; páginas `Components/Pages/History/HistoryHub.razor`, `WealthHistoryList.razor` y `WealthCaptureModal.razor`; gráfico `wwwroot/js/wealthChart.js` (ApexCharts).

## 7. Criterios de aceptación

- [ ] El patrimonio total coincide con la suma de las categorías mostradas.
- [ ] La tarjeta de Activos Refugio muestra primero lo invertido y después el valor actual.
- [ ] El patrimonio suma el valor actual de los activos refugio, no lo invertido.
- [ ] Un inmueble con valor actual informado se valora por ese valor; sin él, por su total invertido.
- [ ] El reparto por clase de activo suma lo mismo que el patrimonio total.
- [ ] Un inmueble vendido no suma al patrimonio.
- [ ] Los contadores reflejan el número real de elementos.

## 8. Observaciones y limitaciones detectadas

- Si hay logo de la app (SPEC-20-01), la cabecera del Dashboard lo muestra a la izquierda del título.

- **Resuelto**: la tarjeta de Activos Refugio tenía las cifras al revés (`SafeHavenTotal` mostraba lo invertido y `SafeHavenInitialTotal` el valor actual) y el patrimonio sumaba lo invertido. Ahora `SafeHavenInitialTotal` es lo invertido, `SafeHavenTotal` el valor actual y el patrimonio usa el valor actual.
- El valor actual de los activos refugio es un dato manual (SPEC-12): el patrimonio será tan exacto como esa actualización.
- **Resuelto**: los inmuebles se valoraban siempre por lo invertido; ahora se usa el valor actual si está informado en la ficha del inmueble. Para que el patrimonio refleje revalorizaciones hay que mantener ese campo al día.
- El diseño v2 prevé gráficos en esta pantalla; la evolución del patrimonio se ve en `/history/wealth` (SPEC-17-01).

## 9. Features relacionadas

- [SPEC-01-02 · Países y divisa](../spec01-gestion-de-empresas/spec01-02-countries-and-currency/spec01-02.md): las empresas de la parte de acciones se suman solo si están en la divisa del país base (los importes se muestran con su símbolo) y una nota cuenta las excluidas hasta que exista el tipo de cambio (SPEC-01-03) (estado: plan aprobado, pendiente de implementar).
- [SPEC-01-03 · Tipo de cambio y totales convertidos](../spec01-gestion-de-empresas/spec01-03-exchange-rate/spec01-03.md): las empresas en otra divisa se convierten a la divisa base con el tipo actual y la nota pasa a contar solo las que no tienen tipo (estado: implementada, pendiente de compilar y probar a mano).
- [SPEC-17-01 · Módulo Histórico y capturas mensuales del patrimonio](spec17-01-wealth-snapshots/spec17-01.md): módulo `/history` y capturas mensuales del patrimonio (valor e invertido por clase) con listado, variaciones, gráfico mensual/anual y CSV en `/history/wealth`; botón «Capturar patrimonio» en el Dashboard ([plan](spec17-01-wealth-snapshots/plan17-01.md) · [tareas](spec17-01-wealth-snapshots/task17-01.md)) (estado: implementada, pendiente de compilar y probar a mano).
- [SPEC-17-02 · Informe fiscal anual para la Renta](spec17-02-tax-report/spec17-02.md): informe por ejercicio en `/history/tax-report` con ventas de acciones en EUR, regla de los dos meses, dividendos e intereses con retenciones, ganancias manuales de fondos y refugio, alquileres y venta de inmuebles orientativos, resumen y CSV ([plan](spec17-02-tax-report/plan17-02.md) · [tareas](spec17-02-tax-report/task17-02.md)) (estado: plan aprobado, pendiente de implementar después de SPEC-17-01).
- [SPEC-17-03 · Capturas diarias de la cartera y JSON para IA local](spec17-03-daily-portfolio-snapshots/spec17-03.md): cinco registros diarios (patrimonio, cartera actual, compras del día, con vendidas y solo vendidas) con flujos y detalle por empresa desde la primera operación, datos derivados que se rehacen desde la fecha más antigua cambiada, y un JSON plano y precalculado (alertas, variaciones, serie) para una IA local; implementada y compilada, pendiente de probar a mano.
