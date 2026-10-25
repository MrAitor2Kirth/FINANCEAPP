# Ideas de FINANCEAPP

Documento vivo con las ideas para la aplicación, de la primera ocurrencia a su implementación. Se gestiona con la skill `/idea-app financeapp`. Cuando una idea se implementa, su detalle pasa a una spec en `Doc/specs/` y aquí queda el enlace.

## Índice
| # | Idea | Estado | Fase | Actualizada |
|---|------|--------|------|-------------|
| 1 | [Activar o desactivar módulos desde configuración](#id-1--activar-o-desactivar-módulos-desde-configuración) | nueva | idea | 2026-10-25 |
| 2 | [Información de QE e inyecciones monetarias](#id-2--información-de-qe-e-inyecciones-monetarias) | nueva | idea | 2026-10-25 |
| 3 | [Exportar listados a Excel](#id-3--exportar-listados-a-excel) | nueva | idea | 2026-10-25 |
| 4 | [Nuevas entidades: ETF y fondos no indexados](#id-4--nuevas-entidades-etf-y-fondos-no-indexados) | nueva | idea | 2026-10-25 |
| 5 | [Movimientos diarios de patrimonio con movimientos detalle](#id-5--movimientos-diarios-de-patrimonio-con-movimientos-detalle) | nueva | idea | 2026-10-25 |
| 6 | [Listas de seguimiento en la vista de inicio](#id-6--listas-de-seguimiento-en-la-vista-de-inicio) | nueva | idea | 2026-10-25 |
| 7 | [Dibujar líneas y copiar/pegar sobre los gráficos](#id-7--dibujar-líneas-y-copiarpegar-sobre-los-gráficos) | nueva | idea | 2026-10-25 |
| 8 | [Requisitos para comercializar la app](#id-8--requisitos-para-comercializar-la-app) | nueva | idea | 2026-10-25 |
| 9 | [App móvil (Android e iOS) y sincronización de datos](#id-9--app-móvil-android-e-ios-y-sincronización-de-datos) | nueva | idea | 2026-10-25 |
| 10 | [Informe fiscal anual para la Renta](#id-10--informe-fiscal-anual-para-la-renta) | nueva | planificada | 2026-10-25 |
| 11 | [Importación de CSV de brokers](#id-11--importación-de-csv-de-brokers) | nueva | idea | 2026-10-25 |
| 12 | [Mapa interactivo de ayuda dentro de la app](#id-12--mapa-interactivo-de-ayuda-dentro-de-la-app) | nueva | idea | 2026-10-25 |

---

## ID-1 · Activar o desactivar módulos desde configuración
**Estado:** nueva · **Fase:** idea

| Creada | Actualizada | Inicio | Fecha objetivo | Finalizada |
|--------|-------------|--------|----------------|------------|
| 2026-10-25 | 2026-10-25 | — | — | — |

### Descripción
Tener en la configuración (`/settings`) un botón para activar o desactivar módulos de la aplicación.

### Objetivo
Que el usuario pueda ocultar los módulos que no usa y ver solo los que le interesan. Cómo se sabrá que está logrado: pendiente de definir.

### Requerimientos
- RQ-1: un control en Configuración para activar o desactivar cada módulo (funcional).
- RQ-2: pendiente de definir qué módulos se pueden desactivar y qué efecto tiene (menú, Inicio, Dashboard, totales).

### Estado actual
Solo está la idea enunciada. Falta decidir qué es un «módulo» y qué ocurre con sus datos y con los totales del patrimonio al desactivarlo.

### Planificación
- [ ] Definir la lista de módulos y el efecto de desactivarlos

### Preguntas abiertas
- ¿Qué se considera un módulo? ¿Acciones, Fondos, Banco, Activos refugio, Inmuebles, Histórico, Dividendos, Índices…?
- ¿Desactivar un módulo oculta solo el menú, o también lo excluye del Dashboard y del patrimonio total?
- ¿Se conservan los datos del módulo desactivado? (se supone que sí, sin borrar nada)

### Bitácora
- 2026-10-25: idea creada.

---

## ID-2 · Información de QE e inyecciones monetarias
**Estado:** nueva · **Fase:** idea

| Creada | Actualizada | Inicio | Fecha objetivo | Finalizada |
|--------|-------------|--------|----------------|------------|
| 2026-10-25 | 2026-10-25 | — | — | — |

### Descripción
Tener información sobre los QE (expansión cuantitativa) y las inyecciones monetarias que se hacen a nivel de EE. UU., Europa, Asia y España, que son los mercados que más afectan al usuario por ahora.

### Objetivo
Contar con contexto macroeconómico monetario dentro de la app para interpretar la evolución de los mercados en los que se invierte. Cómo se sabrá que está logrado: pendiente de definir.

### Requerimientos
- RQ-1: cubrir al menos EE. UU., Europa, Asia y España (funcional).
- RQ-2: pendiente de definir qué datos se muestran, de dónde salen y con qué frecuencia se actualizan.

### Estado actual
Solo está la idea enunciada. Falta decidir el alcance (datos numéricos, gráficos o noticias) y la fuente de datos.

### Planificación
- [ ] Decidir el tipo de información y las fuentes disponibles

### Preguntas abiertas
- ¿Series numéricas (balance de bancos centrales, M2, tipos de interés), gráficos, o resumen/noticias?
- ¿Fuente de datos? Hay que comprobar que sea gratuita y accesible desde la app.
- ¿Dónde se ve: página propia, Dashboard o junto al gráfico de cotización?
- ¿Se superpone con el gráfico de los activos para comparar?
- ¿«Asia» qué incluye? (Japón, China, otros)

### Bitácora
- 2026-10-25: idea creada.

---

## ID-3 · Exportar listados a Excel
**Estado:** nueva · **Fase:** idea

| Creada | Actualizada | Inicio | Fecha objetivo | Finalizada |
|--------|-------------|--------|----------------|------------|
| 2026-10-25 | 2026-10-25 | — | — | — |

### Descripción
Tener la posibilidad de exportar a Excel ciertos listados.

### Objetivo
Poder sacar datos de la app a una hoja de cálculo para analizarlos o archivarlos fuera. Cómo se sabrá que está logrado: pendiente de definir.

### Requerimientos
- RQ-1: exportar a Excel (funcional).
- RQ-2: pendiente de definir qué listados («ciertos listados») y qué columnas.

### Estado actual
Solo está la idea enunciada. Falta elegir qué listados se exportan y cómo se genera el fichero (librería nueva o formato alternativo como CSV).

### Planificación
- [ ] Elegir los listados a exportar

### Preguntas abiertas
- ¿Qué listados? (empresas, operaciones, histórico de ventas, movimientos bancarios, dividendos, capturas…)
- ¿Exporta lo que se ve con los filtros aplicados, o todo?
- ¿Formato `.xlsx` real o basta CSV? `.xlsx` obliga a añadir una librería NuGet (el proyecto pide confirmar antes de añadir paquetes).
- ¿Dónde se guarda el fichero en escritorio y, a futuro, en móvil?

### Bitácora
- 2026-10-25: idea creada.

---

## ID-4 · Nuevas entidades: ETF y fondos no indexados
**Estado:** nueva · **Fase:** idea

| Creada | Actualizada | Inicio | Fecha objetivo | Finalizada |
|--------|-------------|--------|----------------|------------|
| 2026-10-25 | 2026-10-25 | — | — | — |

### Descripción
Crear una nueva entidad de ETF y otra de fondos que no sean indexados.

### Objetivo
Representar por separado los ETF y los fondos no indexados, en lugar de mezclarlos con las acciones y con los fondos actuales. Cómo se sabrá que está logrado: pendiente de definir.

### Requerimientos
- RQ-1: entidad y gestión propia para ETF (funcional).
- RQ-2: entidad y gestión propia para fondos no indexados (funcional).
- RQ-3: pendiente de definir su efecto en el Dashboard y en el patrimonio total.

### Estado actual
Solo está la idea enunciada. Falta aclarar cómo encaja con lo que ya existe (empresas con precio en vivo y fondos con historial de valores).

### Planificación
- [ ] Aclarar el modelo y qué se reutiliza de Acciones y de Fondos

### Preguntas abiertas
- ¿Los ETF cotizan en bolsa con precio en vivo y gráfico, como las acciones? Si es así, ¿se reutiliza el flujo de empresas?
- ¿Los fondos no indexados son lo que hoy se registra como «Fondos», y los indexados pasarían a la entidad nueva de ETF/indexados, o se mantienen los dos tipos?
- ¿Qué operaciones tienen cada una (compra, venta, dividendos, aportación, reembolso)?
- Requerirá tablas nuevas: esquema vía `DatabaseSchemaUpgrader`, nombres en inglés, y preguntar antes de cambiar el esquema.

### Bitácora
- 2026-10-25: idea creada.

---

## ID-5 · Movimientos diarios de patrimonio con movimientos detalle
**Estado:** nueva · **Fase:** idea

| Creada | Actualizada | Inicio | Fecha objetivo | Finalizada |
|--------|-------------|--------|----------------|------------|
| 2026-10-25 | 2026-10-25 | — | — | — |

### Descripción
Revisar la forma en la que se hacen las capturas de patrimonio y ver si sería posible crear **movimientos diarios** con esos mismos campos. Cada uno de estos movimientos tendría **movimientos detalle** que alteran los campos según filtros que el usuario ya tiene en las listas: compras del periodo, compras al cierre (cartera al cierre), incluir ventas y cartera actual.

### Objetivo
Pasar de capturas mensuales a una serie diaria del patrimonio, explicable mediante movimientos de detalle que muestren qué ha cambiado cada día y por qué. Cómo se sabrá que está logrado: pendiente de definir.

### Requerimientos
- RQ-1: movimientos diarios con los mismos campos que las capturas actuales (funcional).
- RQ-2: cada movimiento diario tiene movimientos detalle que alteran esos campos (funcional).
- RQ-3: los detalles se basan en los filtros existentes de las listas: compras del periodo, cartera al cierre, incluir ventas, cartera actual (funcional).
- RQ-4: pendiente de definir si esto sustituye o complementa a las capturas actuales.

### Estado actual
Solo está la idea enunciada, y el propio usuario la plantea como «ver si sería posible». Hay que analizar la viabilidad antes de definir requisitos.

### Planificación
- [ ] Analizar cómo funcionan hoy las capturas y los filtros por periodo, y si pueden reutilizarse
- [ ] Decidir si se pueden reconstruir los días pasados o solo se registra desde ahora

### Diseño
Contexto actual relevante: las capturas (`WealthSnapshots`, SPEC-17-01) son una por año y mes, una foto que no se recalcula nunca, y solo permiten el mes en curso o el anterior. Los filtros por periodo (SPEC-01-09) ya calculan «Compras del periodo» y «Cartera al cierre» con operaciones, precios de cierre y tipos de cambio históricos. Habrá que decidir cómo se combina con el principio de que las capturas son fotos.

### Preguntas abiertas
- ¿«Movimiento diario» = una captura por día, o el cambio de patrimonio de un día a otro?
- ¿Los movimientos detalle son por operación (cada compra, venta, dividendo) o por filtro (periodo, cierre, ventas, cartera actual)?
- ¿Se pueden reconstruir días pasados a partir de las operaciones y los precios históricos, o solo hacia delante?
- ¿Se calcula al leer, a diferencia de las capturas actuales que son foto fija, o se guarda cada día?
- ¿Cuánto crece la BD con un registro diario?
- ¿Qué pasa con el banco, fondos, refugio e inmuebles (valores sin cotización diaria)?

### Bitácora
- 2026-10-25: idea creada.

---

## ID-6 · Listas de seguimiento en la vista de inicio
**Estado:** nueva · **Fase:** idea

| Creada | Actualizada | Inicio | Fecha objetivo | Finalizada |
|--------|-------------|--------|----------------|------------|
| 2026-10-25 | 2026-10-25 | — | — | — |

### Descripción
Poder crear listas de seguimiento de ciertos valores y que se mostraran en la vista de inicio.

### Objetivo
Seguir de un vistazo los valores que interesan al usuario, aunque no los tenga en cartera. Cómo se sabrá que está logrado: pendiente de definir.

### Requerimientos
- RQ-1: crear listas de seguimiento con valores (funcional).
- RQ-2: mostrarlas en la vista de inicio (funcional).
- RQ-3: pendiente de definir qué datos se muestran de cada valor.

### Estado actual
Solo está la idea enunciada. Falta definir qué es un valor seguido y qué datos se enseñan.

### Planificación
- [ ] Definir el modelo de lista y qué se muestra en Inicio

### Preguntas abiertas
- ¿Un valor seguido es una empresa del catálogo de mercado, un ticker libre, un índice o un fondo?
- ¿Cuántas listas, con nombre propio? ¿Un valor puede estar en varias?
- ¿Qué se muestra: precio en vivo, variación del día, mini gráfico, alertas de precio?
- ¿Se relaciona con las empresas que ya existen sin inversión?

### Bitácora
- 2026-10-25: idea creada.

---

## ID-7 · Dibujar líneas y copiar/pegar sobre los gráficos
**Estado:** nueva · **Fase:** idea

| Creada | Actualizada | Inicio | Fecha objetivo | Finalizada |
|--------|-------------|--------|----------------|------------|
| 2026-10-25 | 2026-10-25 | — | — | — |

### Descripción
Poder dibujar líneas y hacer copiar y pegar de esas líneas sobre los gráficos de la app.

### Objetivo
Que el usuario pueda trazar líneas propias (p. ej. soportes, resistencias, tendencias) sobre los gráficos y reutilizarlas copiándolas y pegándolas, sin tener que dibujarlas de nuevo. Cómo se sabrá que está logrado: pendiente de definir.

### Requerimientos
- RQ-1: dibujar líneas sobre los gráficos (funcional).
- RQ-2: copiar y pegar esas líneas (funcional).
- RQ-3: pendiente de definir en qué gráficos (cotización, evolución del patrimonio, otros) y si las líneas se guardan.

### Estado actual
Solo está la idea enunciada. Falta definir en qué gráficos aplica, qué tipos de línea hay y si se persisten.

### Planificación
- [ ] Definir gráficos afectados, tipos de línea y si se guardan

### Preguntas abiertas
- ¿En qué gráficos? Cotización (`companyChart.js`), evolución del patrimonio (`wealthChart.js`), comparación con índices…
- ¿Qué tipos de línea: libre, horizontal, vertical, tendencia entre dos puntos?
- ¿Copiar y pegar entre empresas/gráficos distintos, o dentro del mismo gráfico? ¿Se pega en la misma posición (fecha y precio) o desplazada?
- ¿Las líneas se guardan en la BD (como las marcas, `ChartMarks`) o solo viven mientras el gráfico está abierto?
- ¿Se pueden editar, mover, borrar, cambiar color y grosor?
- Con el eje por sesiones (SPEC-05-03), las líneas deberán traducirse entre fechas e índices.
- ¿Se solapa con las marcas existentes del gráfico?

### Bitácora
- 2026-10-25: idea creada.

---
## ID-8 · Requisitos para comercializar la app
**Estado:** nueva · **Fase:** idea

| Creada | Actualizada | Inicio | Fecha objetivo | Finalizada |
|--------|-------------|--------|----------------|------------|
| 2026-10-25 | 2026-10-25 | — | — | — |

### Descripción
Si la app se transformase en una app de venta al público, lo que haría falta construir o cambiar, según el usuario: informe fiscal, importación de CSV, copias de seguridad y exportar/importar la BD, fuente de datos con licencia, móvil, pruebas automáticas y claves reales.

### Objetivo
Tener la lista de requisitos técnicos y de producto para que FINANCEAPP pudiera venderse a otros inversores, y poder priorizarlos. Cómo se sabrá que está logrado: pendiente de definir (primero habría que validar que hay quien pague).

### Requerimientos
- RQ-1: **informe fiscal** para inversores españoles (funcional). Detalle en la idea ID-10 (feature SPEC-17-02 ya diseñada, sin implementar).
- RQ-2: **importación de CSV** de brokers habituales para no teclear operaciones a mano (funcional). Detalle en la idea ID-11.
- RQ-3: **copias de seguridad y exportar/importar la BD**, fáciles para el usuario final (funcional).
- RQ-4: **fuente de datos de mercado con licencia**, en lugar del endpoint no oficial de Yahoo Finance (restricción).
- RQ-5: **versión móvil** (funcional); hoy el destino principal es Windows. Detalle y opciones de sincronización en la idea ID-9.
- RQ-6: **pruebas automáticas** de los cálculos de dinero (no funcional); hoy no hay proyecto de pruebas.
- RQ-7: **claves reales** entre entidades y operaciones, hoy enlazadas por nombre (no funcional; ver Preguntas abiertas).

### Estado actual
Solo está la lista de requisitos. Falta decidir si se quiere comercializar y validarlo antes de construir nada. Lo único con feature ya diseñada es el informe fiscal.

### Planificación
- [ ] Validar el interés antes de construir: página sencilla que explique informe fiscal y privacidad local, y hablar con 10–20 inversores particulares
- [ ] Decidir público, precio y modelo (licencia única o suscripción)
- [ ] Ordenar los requisitos por prioridad y por dependencia

### Diseño
Análisis de viabilidad (25/10/2026, opinión del asistente, sin datos de mercado):
- **Valor diferencial**: patrimonio global (acciones, fondos, banco, refugio e inmuebles), datos solo en el equipo del usuario, ventas FIFO con histórico, multidivisa con tipo guardado, calendario de dividendos y, cuando exista, el informe fiscal.
- **Competencia**: Portfolio Performance es gratuito y completo; Finary, Getquin, Sharesight y Delta cubren el resto. Eso limita el precio.
- **Público posible**: inversores particulares hispanohablantes que quieren privacidad y control del cálculo fiscal. Nicho pequeño pero con disposición a pagar.
- **Precio orientativo**: licencia única de 30–60 € o suscripción de 3–8 € al mes.
- **Funciones ya en la lista de ideas** (Excel, ETF, listas de seguimiento) son habituales en la competencia: no diferencian.
- **Aspectos legales**: aviso de que no es asesoramiento financiero y, si hubiera nube, RGPD.

### Preguntas abiertas
- ¿«Claves reales» significa claves foráneas en lugar de enlazar por nombre (`CompanyName`, `FundName`…)? Se supone que sí. Cambiarlo exige preguntar antes y migrar con `DatabaseSchemaUpgrader`.
- ¿Qué brokers importar primero (DEGIRO, Interactive Brokers, MyInvestor, Trade Republic…)?
- ¿Qué proveedor de datos con licencia y a qué coste por usuario?
- ¿Solo Windows y Android, o también iOS/Mac (obliga a compilar en macOS)?
- ¿Sincronización entre dispositivos o solo exportar/importar?
- ¿Cuántos clientes se necesitan para que compense el coste de datos y de soporte?
- ¿Se quiere realmente vender, o es solo un ejercicio para decidir qué mejorar?

### Bitácora
- 2026-10-25: idea creada a partir de una valoración de viabilidad comercial de la app.
- 2026-10-25: el móvil y la sincronización pasan a su propia idea (ID-9), porque son útiles aunque no se comercialice.
- 2026-10-25: el informe fiscal (ID-10) y la importación de CSV (ID-11) pasan a ideas propias; aquí quedan como requisitos que apuntan a ellas.

---

## ID-9 · App móvil (Android e iOS) y sincronización de datos
**Estado:** nueva · **Fase:** idea

| Creada | Actualizada | Inicio | Fecha objetivo | Finalizada |
|--------|-------------|--------|----------------|------------|
| 2026-10-25 | 2026-10-25 | — | — | — |

### Descripción
Llevar la app a Android e iOS, además de escritorio. Es un plan que ya figuraba en el proyecto (los destinos están configurados en el `.csproj`). Al comentar la viabilidad comercial (ID-8) surgió el problema de cómo comparten datos el PC y el móvil sin perder la privacidad de tener los datos solo en el equipo del usuario.

### Objetivo
Poder consultar y gestionar el patrimonio desde el móvil con los mismos datos que en el PC, sin subirlos a un servidor ajeno. Cómo se sabrá que está logrado: pendiente de definir.

### Requerimientos
- RQ-1: la app compila y funciona en Android (funcional).
- RQ-2: la app compila y funciona en iOS (funcional; solo se puede compilar desde un Mac).
- RQ-3: los datos del móvil y del PC coinciden, con un método de sincronización o de traspaso (funcional; ver Diseño).
- RQ-4: gráficos e iconos funcionan sin conexión, en local y con versión fija (funcional; hoy van por CDN).
- RQ-5: tablas y pantallas adaptadas a pantalla pequeña (funcional).
- RQ-6: mantener la privacidad: los datos no pasan por un servidor del proyecto (restricción).

### Estado actual
Solo está la idea enunciada y el análisis de opciones de sincronización. Falta elegir el método y el orden (Android primero o los dos).

### Planificación
- [ ] Elegir el método de sincronización entre dispositivos
- [ ] Preparar la app para móvil: ApexCharts e iconos en local, tablas para pantalla pequeña, fuentes `.woff2` en `wwwroot/fonts` (ya anotado en `MEMORY.md`)
- [ ] Compilar y probar primero en Android (no necesita Mac)
- [ ] Preparar el entorno para iOS (Mac y cuenta de desarrollador de Apple)

### Diseño
Opciones de sincronización, de menos a más esfuerzo (25/10/2026, opinión del asistente):
1. **Exportar/importar la BD a mano:** se copia un fichero entre dispositivos. Es lo más simple y mantiene la privacidad, pero obliga a recordar traspasar y a no editar en los dos sitios a la vez. Encaja con la idea de copias y exportar/importar de ID-8.
2. **Fichero en la nube del propio usuario** (OneDrive, iCloud, Google Drive), cifrado: cómodo y sigue sin pasar por un servidor del proyecto. Hay que resolver conflictos si se edita en los dos lados.
3. **Servidor propio con sincronización:** es lo más cómodo, pero rompe la ventaja de privacidad y añade coste y obligaciones (RGPD, seguridad, soporte). No recomendado salvo que se comercialice con ese modelo.
4. **Móvil de solo lectura:** el PC es la fuente de datos y el móvil muestra una copia exportada. Reduce el problema de conflictos y puede ser un primer paso útil.

Limitaciones de las plataformas: iOS exige un Mac para compilar y cuenta de desarrollador de Apple de pago anual; Android tiene una tarifa única de publicación en Google Play.

### Preguntas abiertas
- ¿Android e iOS a la vez o primero Android?
- ¿Se necesita editar desde el móvil o basta con consultar?
- ¿Qué método de sincronización se prefiere (1 a 4)?
- ¿Se dispone de un Mac para iOS?
- ¿Se publicará en las tiendas o solo se instalará para uso propio?
- ¿Cómo se guarda la clave de API de IA en móvil (`SecureStorage`)?

### Bitácora
- 2026-10-25: idea creada a raíz de la valoración comercial (ID-8).

---

## ID-10 · Informe fiscal anual para la Renta
**Estado:** nueva · **Fase:** planificada

| Creada | Actualizada | Inicio | Fecha objetivo | Finalizada |
|--------|-------------|--------|----------------|------------|
| 2026-10-25 | 2026-10-25 | — | — | — |

### Descripción
Informe por año natural con lo que hay que llevar a la declaración de la Renta en España: ganancias y pérdidas por ventas, dividendos e intereses con sus retenciones, rendimientos de alquiler y avisos fiscales. Salió como el mejor diferencial para una versión comercial (ver ID-8): las apps grandes no resuelven bien el caso español.

### Objetivo
Que el usuario pueda copiar sus cifras en Renta Web sin reunirlas a mano. Es orientativo: no calcula la cuota ni sustituye los datos fiscales de la AEAT ni los certificados de bancos y brókers.

### Requerimientos
- RQ-1: los requisitos funcionales completos están en la feature ya diseñada `specs/spec17-patrimonio-global-y-distribucion/spec17-02-tax-report/` (spec, plan y tareas). No se duplican aquí.
- RQ-2: pendiente de decidir cómo se amplía para otros usuarios (otros brokers, otras divisas, otras situaciones) si se comercializa.

### Estado actual
La feature SPEC-17-02 tiene el plan aprobado y está pendiente de implementar, después de SPEC-17-01. Las tareas están sin marcar.

### Planificación
- [ ] Implementar SPEC-17-02 según su `task17-02.md`
- [ ] Revisar con casos reales de otras personas antes de ofrecerlo a terceros
- [ ] Decidir si se añade el cálculo de la cuota o se mantiene orientativo

### Implementación
Feature en `FINANCEAPP/Doc/specs/spec17-patrimonio-global-y-distribucion/spec17-02-tax-report/` (spec17-02.md, plan17-02.md, task17-02.md). Ruta prevista `/history/tax-report`.

### Preguntas abiertas
- ¿Hay que validar las reglas fiscales con un asesor antes de ofrecerlo a otros usuarios?
- ¿Cómo se mantendrá al día con los cambios de la normativa cada ejercicio?
- ¿Qué pasa con ventas anteriores a SPEC-02-01, que no tienen histórico?
- ¿Cubrirá más casos (criptomonedas, ETF, modelo 720 por activos en el extranjero)?

### Bitácora
- 2026-10-25: idea creada al separarla de ID-8; la feature ya estaba diseñada.

---

## ID-11 · Importación de CSV de brokers
**Estado:** nueva · **Fase:** idea

| Creada | Actualizada | Inicio | Fecha objetivo | Finalizada |
|--------|-------------|--------|----------------|------------|
| 2026-10-25 | 2026-10-25 | — | — | — |

### Descripción
Poder importar a la app las operaciones desde los ficheros CSV de los brokers habituales, para no teclearlas a mano. Salió como requisito necesario para una versión comercial (ver ID-8): casi nadie teclea cientos de operaciones.

### Objetivo
Que un usuario nuevo pueda cargar su historial de operaciones desde su broker en pocos pasos y con control sobre lo que se importa. Cómo se sabrá que está logrado: pendiente de definir.

### Requerimientos
- RQ-1: importar operaciones desde CSV de brokers (funcional).
- RQ-2: pendiente de definir qué brokers y qué tipos de operación (Compra, Venta, Dividendo, Traspaso).
- RQ-3: las importaciones deben respetar las reglas de la app: ventas con `RegisterSaleAsync` (FIFO), recálculo de totales, tipo de cambio, movimientos bancarios (restricción).

### Estado actual
Solo está la idea enunciada. Falta decidir los brokers, el flujo de importación y cómo evitar duplicados.

### Planificación
- [ ] Elegir los primeros brokers con ficheros de ejemplo reales
- [ ] Definir el flujo: elegir fichero, ver una vista previa, confirmar, importar

### Preguntas abiertas
- ¿Qué brokers primero (DEGIRO, Interactive Brokers, MyInvestor, Trade Republic…)?
- ¿Cómo se evitan duplicados si se importa dos veces el mismo fichero?
- ¿Cómo se asocia cada fila a una empresa existente (hoy se enlaza por nombre) y qué pasa si no existe?
- ¿Se crean los movimientos bancarios al importar compras y dividendos (SPEC-02-06)?
- ¿Cómo se importan las ventas respetando FIFO y las compras abiertas?
- ¿Cada broker lleva su propio formato (columnas, idioma, decimales, divisas)? ¿Un lector por broker?
- ¿Se necesita ejemplo real de cada broker? El usuario tendría que aportar ficheros sin datos sensibles.
- ¿Relación con la exportación a Excel (ID-3)?

### Bitácora
- 2026-10-25: idea creada al separarla de ID-8.

---

## ID-12 · Mapa interactivo de ayuda dentro de la app
**Estado:** nueva · **Fase:** idea

| Creada | Actualizada | Inicio | Fecha objetivo | Finalizada |
|--------|-------------|--------|----------------|------------|
| 2026-10-25 | 2026-10-25 | — | — | — |

### Descripción
Un mapa interactivo dentro de la app para que el usuario sepa cómo hacer las cosas. Se generaría con un aspecto parecido al de la skill `/app-map`, pero se quedaría dentro de la app: no habría que volver a generarlo y funcionaría como una guía interactiva moderna.

### Objetivo
Que un usuario, sobre todo uno nuevo, descubra por sí mismo qué puede hacer la app y cómo hacerlo (dar de alta una empresa, registrar una venta, ver el informe fiscal…), sin leer la documentación. Cómo se sabrá que está logrado: pendiente de definir.

### Requerimientos
- RQ-1: un mapa o guía interactiva accesible desde la app (funcional).
- RQ-2: forma parte de la app, no se regenera cada vez que se usa (restricción).
- RQ-3: pendiente de definir su contenido, su navegación y cómo se mantiene al día cuando cambia la app.

### Estado actual
Decididos el enfoque (tareas y módulos, cada nodo lleva a la pantalla real) y el ciclo de vida (un mapa definitivo que se amplía en cada versión). Falta elegir cómo se alimenta su contenido y cómo se evita que quede desactualizado cuando cambia una pantalla ya existente.

### Planificación
- [ ] Elegir cómo se alimenta su contenido (fichero de datos a mano o generado en la compilación)
- [ ] Definir cómo se detecta que un nodo apunta a una pantalla que ya no existe
- [ ] Incluir en el flujo de cada feature un paso «actualizar el mapa»

### Diseño
Observaciones (25/10/2026, opinión del asistente): la skill `/app-map` dibuja el código para un desarrollador (ficheros, dependencias, reglas internas). Un usuario final no necesita eso: necesita saber «cómo hago X». Por eso convendría que el mapa se organizara por **tareas y módulos de la app** y no por ficheros, y que cada nodo llevara a la pantalla real. Opciones de contenido, de menos a más esfuerzo de mantenimiento:
1. Contenido escrito a mano en un fichero de datos (lista de tareas, textos, rutas): sencillo y controlable, pero se desactualiza si no se revisa.
2. Contenido generado en la compilación a partir de las specs y las rutas: se mantiene mejor, pero exige una herramienta de generación.
3. Guía paso a paso sobre la pantalla real (recorridos con resaltado de botones) además del mapa.

### Preguntas abiertas
- ¿Cómo se mantiene al día cuando cambia una pantalla que ya estaba en el mapa? Ampliarlo con funciones nuevas no basta: un cambio en lo existente también obliga a revisarlo.
- ¿Debe funcionar sin conexión (hoy los gráficos y los iconos van por CDN)?
- ¿Se enlaza cada nodo con la pantalla real, o con un recorrido guiado?
- ¿Con buscador tipo «¿cómo hago…?»?
- ¿Encaja con el activador de módulos (ID-1): oculta lo que está desactivado?
- ¿Es útil solo para usuarios nuevos o también para ti?

### Decisiones tomadas
- El mapa se organiza por tareas y módulos, y cada nodo lleva a la pantalla real — es lo natural para un usuario final, que busca «cómo hago X» y no la estructura del código.
- Se crea un mapa definitivo y se amplía en cada versión nueva, sin regenerarlo — así el esfuerzo se reparte con cada función en vez de rehacerlo entero. Condición: cuando una versión cambie una pantalla que ya estaba en el mapa, también hay que corregir su nodo.
- No se implementa ahora, solo se prepara: cada pantalla nueva debe poder describirse como una tarea del usuario con su ruta, y cada feature nueva lleva un paso «Mapa de ayuda» en su `taskNN-MM.md` — así, cuando llegue el momento, el mapa sale casi de las propias specs. Mientras no exista, el paso es anotar la tarea y su ruta aquí.

#### Tareas anotadas para el mapa
- Ver el informe fiscal — `/history/tax-report` (SPEC-17-02).

### Bitácora
- 2026-10-25: idea creada.
- 2026-10-25: decidido el enfoque por tareas y que el mapa sea definitivo y se amplíe por versiones.
- 2026-10-25: decidido no implementarla todavía (las pantallas aún cambian; antes, probar a mano, informe fiscal, importación y móvil) y mantener un criterio de diseño y un paso «Mapa de ayuda» en las tareas de las features nuevas (regla en `AGENTS.md`, paso T4.6 en `task17-02.md`).

---
