# SPEC-21 · Calendario de dividendos

| | |
|---|---|
| **Categoría** | 1. Acciones y empresas |
| **Identificador** | SPEC-21 |
| **Documento** | `spec21-dividend-calendar/spec21.md` |
| **Estado** | Implementada y compilada, pendiente de probar a mano ([SPEC-21-01](spec21-01-dividend-calendar/spec21-01.md)) |
| **Volver al índice** | [README de specs](../README.md) |

> Calendario propio de dividendos por empresa (fecha de pago, ex-dividendo, importe por acción, acciones y % dividendo), con generación de la operación de Dividendo, próximos dividendos en Inicio y marca «Pago de dividendo» en el gráfico, con tramo futuro y minigráfico navegador.

## 1. Descripción funcional

Vista `/dividends`, accesible desde el botón «Dividendos» de `/companies`, con las líneas de dividendo de cada empresa. Las líneas se crean a mano («Nuevo dividendo»), desde la «Próxima fecha de dividendo» de la ficha de empresa (sin alterar las anteriores) o desde «Propuestas» de dividendos anunciados. Desde una línea pendiente cuya fecha de pago ya llegó se genera la operación de Dividendo (acciones × importe por acción, editable) con su movimiento bancario. Inicio muestra los dividendos pendientes de los próximos 90 días de las empresas con acciones, y el gráfico de cotización dibuja cada pago como un rombo azul, también en el futuro, con un minigráfico navegador para moverse entre pasado y futuro.

## 2. Pantallas y rutas

- `/dividends` – vista de dividendos (`DividendsList.razor`), desde el botón «Dividendos» de `/companies`.
- `/companies/edit/{id}` y alta – campo «Próxima fecha de dividendo» y estado de su línea (SPEC-01).
- `/` – bloque «Próximos dividendos» (SPEC-20).
- Modal del gráfico de cotización – marca «Pago de dividendo», tramo futuro y navegador (SPEC-05).

## 3. Reglas de negocio

### Líneas de dividendo

- Cada línea guarda empresa (`CompanyId`), fecha de pago (obligatoria, pasada o futura), fecha ex-dividendo (opcional, no posterior a la de pago), importe por acción (opcional al crear, > 0, **hasta 4 decimales**), número de acciones (≥ 0, hasta 2 decimales; se propone con las de la cartera), % dividendo (opcional, 0–100), estado (`"Pendiente"` / `"Cobrado"`), operación generada, origen (`"Manual"`, `"Ficha"`, `"Búsqueda"`) y notas (≤ 200).
- **Importe total** = acciones × importe por acción, redondeado a 2 decimales (`DividendPaymentRules.CalculateTotal`); se calcula al leer, no se guarda.
- No puede haber dos líneas de la misma empresa con la misma fecha de pago (servicio + índice único).
- Una línea **Pendiente** se edita (sobre una copia) y se borra con confirmación; una **Cobrada** no («Cobrado: para cambiarlo, borra la operación en Operaciones»).
- Al arrancar, una línea Cobrada cuya operación ya no existe vuelve a Pendiente (`RepairOrphanLinksAsync`).

### Vista `/dividends`

- Resumen en divisa base con el tipo actual: «Pendiente 90 días» (pendientes con importe y pago de hoy a 90 días) y «Cobrado este año» (todas las operaciones Dividendo del año natural). Las empresas en otra divisa sin tipo quedan fuera con nota.
- Filtro Pendientes (por defecto, de la más cercana a la más lejana) / Cobrados / Todos (descendente) y filtro por empresa. Las pendientes con fecha de pago pasada se resaltan: «Pago vencido, sin cobrar».
- «Último dividendo (Yahoo)» en el formulario: último pago según Yahoo (`events=div`) con «Usar importe»; no crea líneas ni fechas.

### Ficha de empresa

- Guardar la ficha con una «Próxima fecha de dividendo» nueva o distinta crea, en el mismo guardado (en el alta, en la misma transacción), una línea Pendiente de origen `"Ficha"` con las acciones en cartera, salvo que ya exista una con esa fecha. Cambiar la fecha después crea otra y no toca la anterior; vaciarla no borra nada. La ficha muestra el estado de la línea de su fecha.

### Generar la operación

- «Generar operación» aparece solo en líneas Pendientes con importe por acción, acciones > 0 y fecha de pago ≤ hoy; si no, la fila dice el motivo («Falta el importe por acción», «Disponible el dd/mm/aaaa»).
- Mini-formulario: fecha = fecha de pago; importe propuesto = importe total, **editable** (p. ej. el neto); cuenta bancaria (la más antigua por defecto o «No registrar en banco»); tipo de cambio si la empresa está en otra divisa.
- La operación `"Dividendo"` se crea con `OperationService.AddAsync(operation, cuenta, dividendPaymentId)`: crea el Ingreso del banco, recalcula la empresa y, **en la misma transacción**, marca la línea Cobrada con `OperationId` (si ya estaba cobrada, se deshace todo). La app nunca genera operaciones sola.
- Borrar en `/operations` la operación generada devuelve la línea a Pendiente en la misma transacción; editarla no cambia la línea.

### Inicio

- «Próximos dividendos»: líneas Pendientes de empresas con acciones y pago de hoy a 90 días, con su importe o «Importe por confirmar»; sin líneas, «No hay dividendos en los próximos 90 días».

### Gráfico de cotización

- Cada línea de la empresa es un **rombo azul** (hueco Pendiente, relleno Cobrado) en su fecha de pago: en el pasado sobre el cierre del periodo; en el futuro a la altura del último precio. Se oculta con el interruptor «Marcas»; en Horas solo las pasadas.
- El eje se prolonga con **sesiones futuras vacías** (Diario: lunes a viernes; Semanal, Mensual y Anual: sus periodos) hasta lo más lejano de hoy + 1 año o el pago pendiente más lejano; en Horas no hay tramo futuro. La vista inicial sigue acabando en el último precio.
- Al pasar el ratón por un periodo con pago, el tooltip del día añade «Pago de dividendo» (fechas, estado e importe) y aparece una línea vertical azul discontinua; en el futuro el tooltip dice «Sin cotización (fecha futura)».
- **Minigráfico navegador** bajo el gráfico: todo el histórico y el tramo futuro sombreado, con una ventana que se arrastra o se estira («brush» de ApexCharts hacia el gráfico y el volumen) y que sigue al zoom, al desplazamiento, a «Vista inicial» y a los bloques antiguos en segundo plano.

### Propuestas

- `Data/DividendProposalsData.cs` guarda dividendos **anunciados** (nunca estimaciones) investigados en la web. «Propuestas (N)» los enlaza con las empresas por ticker sin sufijo de bolsa, marca «Ya existe» los que ya tienen línea y crea las marcadas como líneas Pendientes de origen `"Búsqueda"` con la fuente en notas. Nunca crea operaciones.

### Baja de empresa

- Al eliminar una empresa se borran sus líneas en la misma transacción; el modal avisa de cuántas.

## 4. Datos implicados

- Tabla `DividendPayments` (`EnsureDividendPaymentsTable`): Id, CompanyId, PaymentDate, ExDividendDate, AmountPerShare (`decimal(18,4)`), NumberOfShares, DividendPercent, Status, OperationId, Source, Notes, CreatedAt, UpdatedAt; índice único (CompanyId, PaymentDate) e índice OperationId. Sin claves foráneas.
- Columna `CompanyDetails.NextDividendDate` (`EnsureCompanyNextDividendDateColumn`).
- `Operations` (tipo `"Dividendo"`) y `BankOperations` (SPEC-02-06), sin cambios.
- `Data/DividendProposalsData.cs`: propuestas en memoria. Búsqueda del 24/10/2026: Microsoft, 0,98 $ por acción, ex 19/11/2026, pago 10/12/2026 (comunicado del 15/09/2026); el resto de empresas en cartera no había anunciado su próximo dividendo.

## 5. Flujo principal

1. El usuario apunta el próximo dividendo (a mano, desde la ficha o desde una propuesta).
2. Lo ve en Inicio y como rombo en el gráfico.
3. Llegada la fecha de pago, pulsa «Generar operación»: se crean el Dividendo y el ingreso en el banco y la línea queda Cobrada.

## 6. Componentes técnicos

- `Models/DividendPayment.cs`, `Data/DividendProposalsData.cs`.
- `Services/DividendPaymentRules.cs` (reglas puras: literales, validación, total, cuándo se puede generar, ticker normalizado).
- `IDividendService` / `DividendService`: `GetAsync`, `GetSummaryAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `GetDefaultSharesAsync`, `GenerateOperationAsync`, `RepairOrphanLinksAsync`, `GetUpcomingAsync`, `GetProposalsAsync`, `CreateFromProposalsAsync`, `GetLastDividendAsync`, `GetChartMarksAsync`.
- `OperationService.AddAsync(..., dividendPaymentId)` y `DeleteAsync` (reapertura); `CompanyService` (línea desde la ficha, `GetNextDividendStatusAsync`, `CountDividendPaymentsAsync`, baja); `StockPriceService.GetLastDividendAsync`; `PriceHistoryService.BuildFutureSessions` y `AlignDividendMarks`.
- `DividendsList.razor`, `CompaniesList.razor`, `CompanyForm.razor`, `Home.razor`, `CompanyChartModal.razor` y `companyChart.js` (sesiones futuras, serie auxiliar «Referencia» oculta que sostiene la escala en el futuro, rombos, línea vertical desde el tooltip y navegador).

## 7. Criterios de aceptación

- [ ] Se crean líneas a mano, desde la ficha y desde propuestas; los duplicados empresa + fecha de pago se rechazan.
- [ ] «Generar operación» crea el Dividendo y el ingreso en el banco y deja la línea Cobrada; borrar la operación la reabre.
- [ ] Inicio muestra los pendientes a 90 días de las empresas con acciones.
- [ ] El gráfico muestra los pagos como rombos (también en el futuro) y el navegador mueve el gráfico y el volumen.

Criterios detallados en [SPEC-21-01 §4](spec21-01-dividend-calendar/spec21-01.md#4-requisitos-funcionales).

## 8. Observaciones y limitaciones detectadas

- Yahoo (`v8/finance/chart`) solo da dividendos ya pagados, no los anunciados: las fechas confirmadas llegan como propuestas investigadas a mano.
- Las propuestas son una foto del 24/10/2026: no hay búsqueda automática; para añadir nuevas se edita `DividendProposalsData.cs`.
- El número de acciones de una línea no se recalcula solo si compras o vendes después de crearla: se edita a mano.
- Las sesiones futuras de Diario son días laborables (lunes a viernes); no tienen en cuenta festivos de cada bolsa.

## 9. Features relacionadas

- [SPEC-21-01 · Calendario de dividendos](spec21-01-dividend-calendar/spec21-01.md): tabla de dividendos, ficha de empresa, generar la operación, Inicio, marca «Pago de dividendo» con tramo futuro y navegador, y propuestas confirmadas (estado: implementada y compilada, pendiente de probar a mano).
