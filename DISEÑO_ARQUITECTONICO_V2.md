# 🏗️ REDISEÑO ARQUITECTÓNICO COMPLETO - FINANCEAPP v2.0

## 📅 Fecha de Diseño
**Fecha:** Enero 2025  
**Versión:** 2.0 - Multi-Asset Portfolio Manager  

---

## 🎯 VISIÓN GENERAL

### Objetivo
Transformar FINANCEAPP en una plataforma completa de gestión patrimonial que permita al usuario tracking detallado de:
- ✅ Acciones/Empresas (ya implementado)
- 🆕 Fondos de Inversión
- 🆕 Cuentas Bancarias
- 🆕 Activos Refugio (Oro, Plata, Bitcoin)
- 🆕 Real Estate (Inversión Inmobiliaria)

### Filosofía de Diseño
- **Coherencia:** Todas las categorías siguen el mismo patrón Entidad → Operaciones
- **Simplicidad:** UI intuitiva con edición inline para operaciones
- **Flexibilidad:** Fácil añadir nuevas categorías en el futuro
- **Performance:** Cálculos automáticos con propiedades computadas

---

## 📊 ARQUITECTURA ACTUAL (v1.0)

### Entidades Existentes

#### 1. FinancialRecord (Registro Histórico)
```csharp
// ESTE MODELO QUEDARÁ OBSOLETO Y LO ELIMINAREMOS
// Se reemplazará por un Dashboard que consulta las entidades reales
- Id, Date, Stocks, Dividends, CapitalGains, Profits, Funds, SafeHaven, InitialCapital, BankAccount, Total, Notes
```
**DECISIÓN:** ❌ **ELIMINAR** - Ya no tiene sentido con el nuevo diseño modular

#### 2. CompanyDetail (Empresas) ✅
```csharp
- Id, Name, Ticker, Sector, CurrentPrice, DividendYield, CreatedAt, UpdatedAt
- [Computed] InvestingUrl, FormattedDividendYield
```
**DECISIÓN:** ✅ **MANTENER** y añadir campo `TotalInvested`

#### 3. Operation (Operaciones de Acciones) ✅
```csharp
- Id, CompanyName, Date, CloseDate?, Amount, Type, CreatedAt
- [Computed] IsClosed, Status
```
**DECISIÓN:** ✅ **MANTENER** - Funciona perfectamente

---

## 🆕 NUEVAS ENTIDADES (v2.0)

---

### 🏢 CATEGORÍA 1: ACCIONES/EMPRESAS (Company)

#### Entidad: CompanyDetail (MODIFICADA)
```csharp
public class CompanyDetail
{
	// Campos existentes
	[Key] public int Id { get; set; }

	[Required, MaxLength(200)]
	public string Name { get; set; } = string.Empty;

	[Required, MaxLength(20)]
	public string Ticker { get; set; } = string.Empty;

	[Required, MaxLength(100)]
	public string Sector { get; set; } = string.Empty;

	[Column(TypeName = "decimal(18,2)")]
	public decimal CurrentPrice { get; set; }

	[Column(TypeName = "decimal(18,2)")]
	public decimal DividendYield { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.Now;
	public DateTime UpdatedAt { get; set; } = DateTime.Now;

	// 🆕 NUEVO CAMPO
	/// <summary>
	/// Total invertido calculado desde operaciones
	/// Suma de operaciones tipo "Compra" y resta las "Venta"
	/// Si hay operaciones cerradas, solo suma las posteriores a la última fecha de cierre
	/// </summary>
	[Column(TypeName = "decimal(18,2)")]
	public decimal TotalInvested { get; set; }

	// Propiedades calculadas (no en BD)
	[NotMapped]
	public string InvestingUrl => $"https://www.investing.com/search/?q={Ticker}";

	[NotMapped]
	public string FormattedDividendYield => $"{DividendYield:F2}%";
}
```

#### Entidad: Operation (SIN CAMBIOS)
```csharp
public class Operation
{
	[Key] public int Id { get; set; }

	[Required, MaxLength(200)]
	public string CompanyName { get; set; } = string.Empty;

	[Required]
	public DateTime Date { get; set; }

	public DateTime? CloseDate { get; set; }

	[Column(TypeName = "decimal(18,2)"), Range(0.01, 999999999.99)]
	public decimal Amount { get; set; }

	[Required, MaxLength(50)]
	public string Type { get; set; } = string.Empty; // "Compra", "Venta", "Dividendo", "Traspaso"

	public DateTime CreatedAt { get; set; } = DateTime.Now;

	[NotMapped]
	public bool IsClosed => CloseDate.HasValue;

	[NotMapped]
	public string Status => IsClosed ? "Cerrada" : "Abierta";
}
```

**✅ LÓGICA DE NEGOCIO:**
- Al crear/modificar/eliminar una `Operation`, recalcular `CompanyDetail.TotalInvested`
- Si existe `CloseDate` en alguna operación de la empresa, solo sumar operaciones con `Date > CloseDate`

---

### 💼 CATEGORÍA 2: FONDOS DE INVERSIÓN (Fund)

#### Entidad: Fund (NUEVA)
```csharp
/// <summary>
/// Representa un fondo de inversión en el portafolio
/// </summary>
public class Fund
{
	[Key]
	public int Id { get; set; }

	/// <summary>
	/// Nombre del fondo (ej: "Vanguard S&P 500", "MyInvestor Cartera Permanente")
	/// </summary>
	[Required, MaxLength(200)]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Tipología del fondo: "Indexado", "Renta Fija", "Renta Variable", "Mixto"
	/// </summary>
	[Required, MaxLength(50)]
	public string FundType { get; set; } = string.Empty;

	/// <summary>
	/// Gestora del fondo (ej: "Vanguard", "BlackRock", "MyInvestor")
	/// </summary>
	[MaxLength(100)]
	public string? Manager { get; set; }

	/// <summary>
	/// TER (Total Expense Ratio) - Comisión anual en porcentaje
	/// Ejemplo: 0.15 para 0.15%
	/// </summary>
	[Column(TypeName = "decimal(5,2)")]
	public decimal TER { get; set; }

	/// <summary>
	/// ISIN - Código identificador internacional del fondo
	/// Ejemplo: "IE00B4L5Y983"
	/// </summary>
	[MaxLength(12)]
	public string? ISIN { get; set; }

	/// <summary>
	/// Rentabilidad histórica anual en porcentaje
	/// Ejemplo: 8.5 para 8.5% anual
	/// </summary>
	[Column(TypeName = "decimal(5,2)")]
	public decimal HistoricalReturn { get; set; }

	/// <summary>
	/// Total invertido actualmente en el fondo
	/// Se calcula desde FundOperations
	/// </summary>
	[Column(TypeName = "decimal(18,2)")]
	public decimal TotalInvested { get; set; }

	/// <summary>
	/// Valor actual de la inversión (incluye plusvalías/minusvalías)
	/// Se actualiza mediante operación especial mensual
	/// </summary>
	[Column(TypeName = "decimal(18,2)")]
	public decimal CurrentValue { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.Now;
	public DateTime UpdatedAt { get; set; } = DateTime.Now;

	/// <summary>
	/// Rentabilidad actual calculada: ((CurrentValue - TotalInvested) / TotalInvested) * 100
	/// </summary>
	[NotMapped]
	public decimal CurrentReturn => TotalInvested > 0 
		? ((CurrentValue - TotalInvested) / TotalInvested) * 100 
		: 0;

	[NotMapped]
	public string FormattedCurrentReturn => $"{CurrentReturn:F2}%";
}
```

#### Entidad: FundOperation (NUEVA)
```csharp
/// <summary>
/// Representa una operación sobre un fondo de inversión
/// </summary>
public class FundOperation
{
	[Key]
	public int Id { get; set; }

	/// <summary>
	/// Nombre del fondo al que pertenece la operación
	/// </summary>
	[Required, MaxLength(200)]
	public string FundName { get; set; } = string.Empty;

	/// <summary>
	/// Tipo de operación:
	/// - "Aportación" (compra/entrada de dinero)
	/// - "Reembolso" (venta/salida de dinero)
	/// - "Traspaso Entrada" (desde otro fondo)
	/// - "Traspaso Salida" (hacia otro fondo)
	/// - "Plusvalía" (ajuste automático mensual - solo puede haber uno)
	/// </summary>
	[Required, MaxLength(50)]
	public string Type { get; set; } = string.Empty;

	/// <summary>
	/// Importe de la operación
	/// Positivo para Aportación/Traspaso Entrada
	/// Negativo para Reembolso/Traspaso Salida
	/// Diferencia para Plusvalía
	/// </summary>
	[Column(TypeName = "decimal(18,2)")]
	public decimal Amount { get; set; }

	[Required]
	public DateTime Date { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.Now;

	[NotMapped]
	public bool IsPlusvaliaOperation => Type == "Plusvalía";
}
```

**✅ LÓGICA DE NEGOCIO ESPECIAL - PLUSVALÍAS:**

```
PROCESO MENSUAL DE ACTUALIZACIÓN DE VALOR:
1. Usuario ejecuta acción "Actualizar Valor del Fondo"
2. Introduce valor actual del fondo (ej: 5500€)
3. Sistema busca la última operación tipo "Plusvalía" del fondo
4. Si existe, LA ELIMINA
5. Calcula diferencia: ValorActual - TotalInvested
6. Crea NUEVA operación tipo "Plusvalía" con:
   - Amount = Diferencia (puede ser positivo o negativo)
   - Date = Fecha actual
7. Actualiza Fund.CurrentValue = ValorActual

IMPORTANTE:
- Solo puede haber UNA operación "Plusvalía" por fondo a la vez
- Cada mes se borra la anterior y se crea una nueva
- NO afecta a TotalInvested (que solo se modifica con Aportaciones/Reembolsos)
```

**💡 ALTERNATIVA MÁS SIMPLE (Propuesta):**
En lugar de crear/borrar operaciones "Plusvalía", podríamos simplemente:
- Añadir un botón "Actualizar Valor" en la página de detalle del fondo
- Al hacer click, mostrar modal con input del valor actual
- Actualizar directamente `Fund.CurrentValue` sin crear operación
- Guardar histórico en tabla separada `FundValueHistory(FundId, Date, Value)`

**❓ PREGUNTA:** ¿Prefieres mantener la idea de operación "Plusvalía" o la alternativa más simple?

---

### 🏦 CATEGORÍA 3: BANCO (BankAccount)

#### Entidad: BankAccount (NUEVA)
```csharp
/// <summary>
/// Representa una cuenta bancaria
/// </summary>
public class BankAccount
{
	[Key]
	public int Id { get; set; }

	/// <summary>
	/// Nombre identificativo de la cuenta (ej: "Cuenta Santander", "Cuenta BBVA Nómina")
	/// </summary>
	[Required, MaxLength(200)]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Entidad bancaria (ej: "Santander", "BBVA", "ING")
	/// </summary>
	[Required, MaxLength(100)]
	public string BankName { get; set; } = string.Empty;

	/// <summary>
	/// IBAN de la cuenta (opcional)
	/// </summary>
	[MaxLength(34)]
	public string? IBAN { get; set; }

	/// <summary>
	/// Tipo de cuenta: "Corriente", "Ahorro", "Depósito", "Cuenta Remunerada"
	/// </summary>
	[Required, MaxLength(50)]
	public string AccountType { get; set; } = string.Empty;

	/// <summary>
	/// Saldo actual de la cuenta
	/// Se calcula desde operaciones
	/// </summary>
	[Column(TypeName = "decimal(18,2)")]
	public decimal CurrentBalance { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.Now;
	public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
```

#### Entidad: BankOperation (NUEVA)
```csharp
/// <summary>
/// Representa un movimiento en una cuenta bancaria
/// </summary>
public class BankOperation
{
	[Key]
	public int Id { get; set; }

	/// <summary>
	/// Nombre de la cuenta bancaria
	/// </summary>
	[Required, MaxLength(200)]
	public string AccountName { get; set; } = string.Empty;

	/// <summary>
	/// Tipo de operación:
	/// - "Ingreso" (nómina, transferencia recibida)
	/// - "Gasto" (pago, transferencia enviada)
	/// - "Intereses" (intereses generados por la cuenta)
	/// - "Comisiones" (comisiones bancarias)
	/// - "Traspaso Entrada" (desde otra cuenta propia)
	/// - "Traspaso Salida" (hacia otra cuenta propia)
	/// </summary>
	[Required, MaxLength(50)]
	public string Type { get; set; } = string.Empty;

	/// <summary>
	/// Importe del movimiento
	/// Positivo para Ingreso/Intereses/Traspaso Entrada
	/// Negativo para Gasto/Comisiones/Traspaso Salida
	/// </summary>
	[Column(TypeName = "decimal(18,2)")]
	public decimal Amount { get; set; }

	[Required]
	public DateTime Date { get; set; }

	/// <summary>
	/// Concepto/descripción del movimiento
	/// </summary>
	[MaxLength(500)]
	public string? Description { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.Now;
}
```

**✅ LÓGICA DE NEGOCIO:**
- `BankAccount.CurrentBalance` = SUM(BankOperations.Amount) donde Type = "Ingreso/Intereses/Traspaso Entrada" - SUM donde Type = "Gasto/Comisiones/Traspaso Salida"

---

### 🪙 CATEGORÍA 4: ACTIVOS REFUGIO (SafeHavenAsset)

#### Entidad: SafeHavenAsset (NUEVA)
```csharp
/// <summary>
/// Representa un activo refugio (Oro, Plata, Bitcoin, etc.)
/// </summary>
public class SafeHavenAsset
{
	[Key]
	public int Id { get; set; }

	/// <summary>
	/// Nombre del activo (ej: "Oro Físico", "Bitcoin Core")
	/// </summary>
	[Required, MaxLength(200)]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Tipo de activo: "Oro", "Plata", "Bitcoin", "Ethereum", "Otros"
	/// </summary>
	[Required, MaxLength(50)]
	public string AssetType { get; set; } = string.Empty;

	/// <summary>
	/// Total invertido actualmente en el activo
	/// Se calcula desde operaciones (Compras - Ventas)
	/// </summary>
	[Column(TypeName = "decimal(18,2)")]
	public decimal TotalInvested { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.Now;
	public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
```

#### Entidad: SafeHavenOperation (NUEVA)
```csharp
/// <summary>
/// Representa una operación sobre un activo refugio
/// </summary>
public class SafeHavenOperation
{
	[Key]
	public int Id { get; set; }

	/// <summary>
	/// Nombre del activo
	/// </summary>
	[Required, MaxLength(200)]
	public string AssetName { get; set; } = string.Empty;

	/// <summary>
	/// Tipo de operación:
	/// - "Compra" (adquisición)
	/// - "Venta" (liquidación)
	/// </summary>
	[Required, MaxLength(50)]
	public string Type { get; set; } = string.Empty;

	/// <summary>
	/// Importe de la operación
	/// Positivo para Compra
	/// Negativo para Venta
	/// </summary>
	[Column(TypeName = "decimal(18,2)")]
	public decimal Amount { get; set; }

	[Required]
	public DateTime Date { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.Now;
}
```

**✅ LÓGICA DE NEGOCIO:**
- Muy simple: TotalInvested = SUM(Compras) - SUM(Ventas)
- UI similar a Operaciones de Empresas (inline editing)

---

### 🏠 CATEGORÍA 5: REAL ESTATE (Property)

#### Entidad: Property (NUEVA)
```csharp
/// <summary>
/// Representa una propiedad inmobiliaria
/// </summary>
public class Property
{
	[Key]
	public int Id { get; set; }

	/// <summary>
	/// Nombre identificativo (ej: "Piso Madrid Centro", "Casa Playa Alicante")
	/// </summary>
	[Required, MaxLength(200)]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Tipo de inmueble: "Vivienda", "Local Comercial", "Parking", "Terreno", "Oficina"
	/// </summary>
	[Required, MaxLength(50)]
	public string PropertyType { get; set; } = string.Empty;

	/// <summary>
	/// Dirección completa
	/// </summary>
	[MaxLength(500)]
	public string? Address { get; set; }

	/// <summary>
	/// Precio de compra inicial
	/// </summary>
	[Column(TypeName = "decimal(18,2)")]
	public decimal PurchasePrice { get; set; }

	/// <summary>
	/// Fecha de compra
	/// </summary>
	[Required]
	public DateTime PurchaseDate { get; set; }

	/// <summary>
	/// Valoración actual estimada (se actualiza manualmente)
	/// </summary>
	[Column(TypeName = "decimal(18,2)")]
	public decimal? CurrentValue { get; set; }

	/// <summary>
	/// Estado actual: "Alquilado", "Vacío", "Uso Propio", "En Venta"
	/// </summary>
	[Required, MaxLength(50)]
	public string Status { get; set; } = string.Empty;

	/// <summary>
	/// Alquiler mensual (si está alquilado)
	/// </summary>
	[Column(TypeName = "decimal(18,2)")]
	public decimal? MonthlyRent { get; set; }

	/// <summary>
	/// Total invertido = Precio Compra + Suma de Gastos
	/// </summary>
	[Column(TypeName = "decimal(18,2)")]
	public decimal TotalInvested { get; set; }

	/// <summary>
	/// Total de ingresos = Suma de Alquileres + Venta (si aplica)
	/// </summary>
	[Column(TypeName = "decimal(18,2)")]
	public decimal TotalIncome { get; set; }

	/// <summary>
	/// Resultado neto = TotalIncome - TotalInvested
	/// </summary>
	[NotMapped]
	public decimal NetResult => TotalIncome - TotalInvested;

	public DateTime CreatedAt { get; set; } = DateTime.Now;
	public DateTime UpdatedAt { get; set; } = DateTime.Now;

	[NotMapped]
	public string FormattedNetResult => NetResult >= 0 
		? $"+{NetResult:C}" 
		: $"{NetResult:C}";
}
```

#### Entidad: PropertyOperation (NUEVA)
```csharp
/// <summary>
/// Representa una operación/movimiento sobre una propiedad inmobiliaria
/// </summary>
public class PropertyOperation
{
	[Key]
	public int Id { get; set; }

	/// <summary>
	/// Nombre de la propiedad
	/// </summary>
	[Required, MaxLength(200)]
	public string PropertyName { get; set; } = string.Empty;

	/// <summary>
	/// Categoría de la operación: "Gasto" o "Ingreso"
	/// </summary>
	[Required, MaxLength(20)]
	public string Category { get; set; } = string.Empty;

	/// <summary>
	/// Tipo específico de operación:
	/// 
	/// GASTOS:
	/// - "Compra Inicial"
	/// - "Reforma Baño"
	/// - "Reforma Cocina"
	/// - "Reforma Integral"
	/// - "IBI"
	/// - "Comunidad"
	/// - "Seguro Hogar"
	/// - "Mantenimiento"
	/// - "Reparación"
	/// - "Hipoteca"
	/// - "Notaría/Registro"
	/// - "Otro Gasto"
	/// 
	/// INGRESOS:
	/// - "Alquiler"
	/// - "Venta"
	/// - "Indemnización Seguro"
	/// - "Otro Ingreso"
	/// </summary>
	[Required, MaxLength(50)]
	public string Type { get; set; } = string.Empty;

	/// <summary>
	/// Importe de la operación
	/// Positivo para ingresos, negativo para gastos
	/// </summary>
	[Column(TypeName = "decimal(18,2)")]
	public decimal Amount { get; set; }

	[Required]
	public DateTime Date { get; set; }

	/// <summary>
	/// Descripción adicional (ej: "Arreglo fuga baño", "Alquiler Diciembre 2024")
	/// </summary>
	[MaxLength(500)]
	public string? Description { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.Now;

	[NotMapped]
	public bool IsIncome => Category == "Ingreso";

	[NotMapped]
	public bool IsExpense => Category == "Gasto";
}
```

**✅ LÓGICA DE NEGOCIO:**
```
TotalInvested = PurchasePrice + SUM(PropertyOperations WHERE Category = "Gasto")
TotalIncome = SUM(PropertyOperations WHERE Category = "Ingreso")
NetResult = TotalIncome - TotalInvested
```

---

## 🗂️ RESUMEN DE ENTIDADES

### Entidades Principales (7)
1. ✅ **CompanyDetail** (modificada - añadir TotalInvested)
2. 🆕 **Fund**
3. 🆕 **BankAccount**
4. 🆕 **SafeHavenAsset**
5. 🆕 **Property**

### Entidades de Operaciones (6)
1. ✅ **Operation** (sin cambios)
2. 🆕 **FundOperation**
3. 🆕 **BankOperation**
4. 🆕 **SafeHavenOperation**
5. 🆕 **PropertyOperation**

### Entidad Legacy
- ❌ **FinancialRecord** (ELIMINAR - obsoleta)

**TOTAL: 11 entidades** (eliminando FinancialRecord = 10 activas)

---

## 🎨 NAVEGACIÓN Y UX

### Menú Principal Propuesto

```
📊 Dashboard (Resumen General)
  └─ Métricas globales por categoría
  └─ Gráfico distribución patrimonio

📈 Acciones
  ├─ Lista de Empresas (formulario tipo ficha)
  └─ Operaciones (edición inline)

💼 Fondos
  ├─ Lista de Fondos (formulario tipo ficha)
  └─ Operaciones (edición inline)

🏦 Banco
  ├─ Lista de Cuentas (formulario tipo ficha)
  └─ Movimientos (edición inline)

🪙 Activos Refugio
  ├─ Lista de Activos (formulario tipo ficha)
  └─ Operaciones (edición inline)

🏠 Inmobiliario
  ├─ Lista de Propiedades (formulario tipo ficha)
  └─ Detalle Propiedad
	  └─ Operaciones de la propiedad (edición inline)
```

### Patrón UX Unificado

**TODAS LAS CATEGORÍAS SIGUEN EL MISMO FLUJO:**

#### Entidad Principal:
- Lista con tarjetas de métricas
- Botón "Nueva [Entidad]" → Formulario tipo ficha
- Botón ✏️ Editar → Formulario tipo ficha
- Botón 🗑️ Eliminar → Modal confirmación

#### Operaciones:
- Lista con edición inline
- Botón "Nueva Operación" → Fila editable
- Click ✏️ → Edición inline
- Click ✅ → Guardar
- Click ❌ → Cancelar
- Click 🗑️ → Modal confirmación

---

## 📊 DASHBOARD REDISEÑADO

### Sección 1: Patrimonio Global
```
╔════════════════════════════════════╗
║   PATRIMONIO TOTAL: 125.500,00 €   ║
╚════════════════════════════════════╝
```

### Sección 2: Distribución por Categorías (Tarjetas)

```
┌─────────────────────┐  ┌─────────────────────┐  ┌─────────────────────┐
│   📈 ACCIONES       │  │   💼 FONDOS         │  │   🏦 BANCO          │
│   45.000,00 €       │  │   30.000,00 €       │  │   15.500,00 €       │
│   35.8%             │  │   23.9%             │  │   12.3%             │
└─────────────────────┘  └─────────────────────┘  └─────────────────────┘

┌─────────────────────┐  ┌─────────────────────┐
│   🪙 ACT. REFUGIO   │  │   🏠 INMOBILIARIO   │
│   10.000,00 €       │  │   25.000,00 €       │
│   8.0%              │  │   20.0%             │
└─────────────────────┘  └─────────────────────┘
```

### Sección 3: Métricas Rápidas
- Total de activos por categoría (número)
- Rentabilidad total del mes
- Últimas 5 operaciones

### Sección 4: Gráficos (Futuro)
- Gráfico de tarta: Distribución patrimonio
- Gráfico de línea: Evolución temporal

---

## 🔗 RELACIONES Y CÁLCULOS

### Cálculo de TotalInvested por Categoría

#### Acciones (CompanyDetail.TotalInvested)
```csharp
// Obtener última operación con CloseDate
var lastCloseDate = Operations
	.Where(o => o.CompanyName == company.Name && o.CloseDate.HasValue)
	.Max(o => o.CloseDate);

// Si hay fecha de cierre, solo sumar operaciones posteriores
var operations = lastCloseDate.HasValue
	? Operations.Where(o => o.CompanyName == company.Name && o.Date > lastCloseDate.Value)
	: Operations.Where(o => o.CompanyName == company.Name);

TotalInvested = operations
	.Where(o => o.Type == "Compra")
	.Sum(o => o.Amount)
	- operations
	.Where(o => o.Type == "Venta")
	.Sum(o => o.Amount);
```

#### Fondos (Fund.TotalInvested)
```csharp
TotalInvested = FundOperations
	.Where(o => o.FundName == fund.Name)
	.Where(o => o.Type == "Aportación" || o.Type == "Traspaso Entrada")
	.Sum(o => o.Amount)
	- FundOperations
	.Where(o => o.FundName == fund.Name)
	.Where(o => o.Type == "Reembolso" || o.Type == "Traspaso Salida")
	.Sum(o => Math.Abs(o.Amount));
```

#### Banco (BankAccount.CurrentBalance)
```csharp
CurrentBalance = BankOperations
	.Where(o => o.AccountName == account.Name)
	.Where(o => o.Type == "Ingreso" || o.Type == "Intereses" || o.Type == "Traspaso Entrada")
	.Sum(o => o.Amount)
	- BankOperations
	.Where(o => o.AccountName == account.Name)
	.Where(o => o.Type == "Gasto" || o.Type == "Comisiones" || o.Type == "Traspaso Salida")
	.Sum(o => Math.Abs(o.Amount));
```

#### Activos Refugio (SafeHavenAsset.TotalInvested)
```csharp
TotalInvested = SafeHavenOperations
	.Where(o => o.AssetName == asset.Name && o.Type == "Compra")
	.Sum(o => o.Amount)
	- SafeHavenOperations
	.Where(o => o.AssetName == asset.Name && o.Type == "Venta")
	.Sum(o => Math.Abs(o.Amount));
```

#### Inmobiliario (Property)
```csharp
TotalInvested = PurchasePrice + PropertyOperations
	.Where(o => o.PropertyName == property.Name && o.Category == "Gasto")
	.Sum(o => Math.Abs(o.Amount));

TotalIncome = PropertyOperations
	.Where(o => o.PropertyName == property.Name && o.Category == "Ingreso")
	.Sum(o => o.Amount);

NetResult = TotalIncome - TotalInvested;
```

---

## 📝 SERVICIOS A CREAR

### Servicios de Entidades Principales
1. ✅ `ICompanyService / CompanyService` (ya existe)
2. 🆕 `IFundService / FundService`
3. 🆕 `IBankAccountService / BankAccountService`
4. 🆕 `ISafeHavenAssetService / SafeHavenAssetService`
5. 🆕 `IPropertyService / PropertyService`

### Servicios de Operaciones
1. ✅ `IOperationService / OperationService` (ya existe)
2. 🆕 `IFundOperationService / FundOperationService`
3. 🆕 `IBankOperationService / BankOperationService`
4. 🆕 `ISafeHavenOperationService / SafeHavenOperationService`
5. 🆕 `IPropertyOperationService / PropertyOperationService`

### Servicio Global
🆕 `IDashboardService / DashboardService`
- Método: `GetPortfolioSummary()` → Devuelve métricas globales
- Método: `GetCategoryDistribution()` → Devuelve distribución por categorías
- Método: `GetRecentOperations(int count)` → Últimas N operaciones de todas las categorías

---

## 🎯 PÁGINAS BLAZOR A CREAR

### Dashboard
- ✅ `FinancialDashboard.razor` (modificar completamente)

### Acciones
- ✅ `CompaniesList.razor` (mantener)
- ✅ `CompanyForm.razor` (mantener)
- ✅ `OperationsList.razor` (mantener)

### Fondos (NUEVO)
- 🆕 `FundsList.razor`
- 🆕 `FundForm.razor`
- 🆕 `FundOperationsList.razor`

### Banco (NUEVO)
- 🆕 `BankAccountsList.razor`
- 🆕 `BankAccountForm.razor`
- 🆕 `BankOperationsList.razor`

### Activos Refugio (NUEVO)
- 🆕 `SafeHavenAssetsList.razor`
- 🆕 `SafeHavenAssetForm.razor`
- 🆕 `SafeHavenOperationsList.razor`

### Inmobiliario (NUEVO)
- 🆕 `PropertiesList.razor`
- 🆕 `PropertyForm.razor`
- 🆕 `PropertyDetail.razor` (detalle con operaciones inline)

**TOTAL: 17 páginas** (3 existentes + 14 nuevas)

---

## 📋 PLAN DE IMPLEMENTACIÓN (FASES)

### ✅ FASE 0: Preparación (ANTES DE PROGRAMAR)
- [x] Diseño arquitectónico completo
- [ ] Validación del usuario
- [ ] Refinamiento de campos/lógica

### 🟡 FASE 1: Infraestructura Base
1. Crear todos los modelos (11 entidades)
2. Actualizar AppDbContext con nuevos DbSets
3. Configurar índices y relaciones
4. Añadir seed data de ejemplo
5. Eliminar FinancialRecord legacy

### 🟡 FASE 2: Servicios
1. Crear servicios de entidades principales (5)
2. Crear servicios de operaciones (5)
3. Crear DashboardService
4. Registrar todos en DI

### 🟡 FASE 3: UI - Fondos
1. FundsList.razor
2. FundForm.razor
3. FundOperationsList.razor
4. Implementar lógica de plusvalías mensuales

### 🟡 FASE 4: UI - Banco
1. BankAccountsList.razor
2. BankAccountForm.razor
3. BankOperationsList.razor

### 🟡 FASE 5: UI - Activos Refugio
1. SafeHavenAssetsList.razor
2. SafeHavenAssetForm.razor
3. SafeHavenOperationsList.razor

### 🟡 FASE 6: UI - Inmobiliario
1. PropertiesList.razor
2. PropertyForm.razor
3. PropertyDetail.razor (con operaciones inline)

### 🟡 FASE 7: Dashboard Rediseñado
1. Rediseñar FinancialDashboard.razor
2. Integrar métricas globales
3. Añadir gráficos

### 🟡 FASE 8: Testing y Ajustes
1. Pruebas de CRUD completas
2. Validaciones de datos
3. Ajustes de UI/UX

---

## ❓ PREGUNTAS PENDIENTES PARA VALIDAR

### 1. Plusvalías de Fondos
**¿Preferimos:**
- ❓ Opción A: Crear/borrar operación "Plusvalía" cada mes (lo que propusiste)
- ❓ Opción B: Actualizar solo Fund.CurrentValue + histórico en tabla separada

### 2. Operación de Compra de Propiedad
**¿La compra inicial del inmueble debe:**
- ❓ Estar incluida en Property.PurchasePrice + crear operación "Compra Inicial"?
- ❓ O solo en Property.PurchasePrice sin operación asociada?

### 3. Venta de Propiedad
**¿Cómo gestionamos la venta completa de una propiedad?**
- ❓ ¿Crear operación "Venta" y cambiar Status a "Vendida"?
- ❓ ¿Eliminar la propiedad de la lista activa?
- ❓ ¿Mantenerla en histórico?

### 4. Campos Adicionales
**¿Te gustaría añadir:**
- ❓ Campo "Notas" en todas las entidades principales?
- ❓ Campo "Divisa" (EUR, USD) para operaciones internacionales?
- ❓ Campo "Adjuntos" para subir documentos (futuro)?

---

## 🎯 SIGUIENTE PASO

**Una vez valides este diseño, procederé a:**
1. Crear todos los modelos de datos
2. Actualizar AppDbContext
3. Crear los servicios
4. Implementar la UI página por página

**¿Estás de acuerdo con este diseño? ¿Alguna modificación antes de empezar a programar?** 🚀

---

*Documento de Diseño Arquitectónico - FINANCEAPP v2.0*  
*Pendiente de validación del usuario antes de implementación*
