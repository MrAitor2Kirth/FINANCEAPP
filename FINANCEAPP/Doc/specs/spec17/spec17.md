# SPEC-17 · Patrimonio global y distribución por clase de activo

| | |
|---|---|
| **Categoría** | 6. Dashboard y análisis |
| **Identificador** | SPEC-17 |
| **Documento** | `spec17/spec17.md` |
| **Volver al índice** | [README de specs](../README.md) |

> Resumen del valor total del patrimonio y de cada categoría.

## 1. Descripción funcional

Muestra en una sola pantalla el patrimonio total del usuario y el peso de cada categoría de inversión: acciones, fondos, banco, activos refugio e inmuebles.

## 2. Pantallas y rutas

- `/dashboard` – tarjetas de Patrimonio Total, Efectivo Bancario, Acciones, Fondos de Inversión y Activos Refugio, y sección «Acceso Rápido a Módulos».
- `/` – página de inicio con accesos a los módulos.

## 3. Reglas de negocio

- **Patrimonio total** = acciones (valor invertido actual) + fondos (valor actual) + banco (saldo total) + activos refugio (**valor actual**) + inmuebles activos (**valor actual si está informado; si no, total invertido**).
- **Acciones**: se muestran la inversión inicial y el valor actual (con precio en vivo).
- **Fondos**: se muestran total invertido y valor actual.
- **Inmuebles**: solo cuentan los **no vendidos**, valorados por su **valor actual** cuando está informado y, si no, por su total invertido.
- También se informan el número de elementos de cada categoría y la fecha de última actualización.
- **Activos refugio**: se muestran primero lo invertido y después el valor actual, con el mismo criterio que acciones y fondos.
- El reparto por clase de activo usa los mismos valores que el patrimonio total: acciones (valor actual), fondos (valor actual), banco, activos refugio (valor actual) e inmuebles activos (valor actual o, si no hay, total invertido), de modo que la suma del reparto coincide con el patrimonio.

## 4. Datos implicados

- Agregación de `CompanyDetail`, `Fund`, `BankAccount`, `SafeHavenAsset` y `Property`.
- Modelo `DashboardSummary`.

## 5. Flujo principal

1. El usuario abre el dashboard.
2. `DashboardService` obtiene de cada servicio los totales.
3. Se componen las tarjetas con los importes y el patrimonio total.

## 6. Componentes técnicos

- `FinancialDashboard.razor`, `Home.razor`.
- `IDashboardService`: `GetDashboardSummaryAsync`, `GetTotalPatrimonyAsync`, `GetPatrimonyByAssetClassAsync`.

## 7. Criterios de aceptación

- [ ] El patrimonio total coincide con la suma de las categorías mostradas.
- [ ] La tarjeta de Activos Refugio muestra primero lo invertido y después el valor actual.
- [ ] El patrimonio suma el valor actual de los activos refugio, no lo invertido.
- [ ] Un inmueble con valor actual informado se valora por ese valor; sin él, por su total invertido.
- [ ] El reparto por clase de activo suma lo mismo que el patrimonio total.
- [ ] Un inmueble vendido no suma al patrimonio.
- [ ] Los contadores reflejan el número real de elementos.

## 8. Observaciones y limitaciones detectadas

- **Resuelto**: la tarjeta de Activos Refugio tenía las cifras al revés (`SafeHavenTotal` mostraba lo invertido y `SafeHavenInitialTotal` el valor actual) y el patrimonio sumaba lo invertido. Ahora `SafeHavenInitialTotal` es lo invertido, `SafeHavenTotal` el valor actual y el patrimonio usa el valor actual.
- El valor actual de los activos refugio es un dato manual (SPEC-12): el patrimonio será tan exacto como esa actualización.
- **Resuelto**: los inmuebles se valoraban siempre por lo invertido; ahora se usa el valor actual si está informado en la ficha del inmueble. Para que el patrimonio refleje revalorizaciones hay que mantener ese campo al día.
- El diseño v2 prevé gráficos en esta pantalla; todavía no existen.
