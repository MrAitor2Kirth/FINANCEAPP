# Guía de Migración de Base de Datos

## 📋 Resumen

Se han añadido dos nuevas tablas a la base de datos:
- **Operations**: Gestión de operaciones financieras (compra/venta de acciones)
- **CompanyDetails**: Catálogo de empresas con información de inversión

## 🔄 Opciones de Migración

### Opción 1: Migración Automática (Recomendada para Desarrollo)

El proyecto está configurado para usar `Database.EnsureCreated()` que automáticamente creará las nuevas tablas cuando ejecutes la aplicación por primera vez después de estos cambios.

**Pasos:**
1. Elimina la base de datos existente (opcional, si quieres empezar desde cero)
2. Ejecuta la aplicación
3. Las nuevas tablas se crearán automáticamente

**Ubicación de la base de datos:**
- La base de datos SQLite se encuentra en: `FileSystem.AppDataDirectory/financeapp.db`
- En Windows: `%LOCALAPPDATA%\Packages\[PackageName]\LocalState\financeapp.db`

### Opción 2: Migraciones de Entity Framework Core (Recomendada para Producción)

Si prefieres usar migraciones formales de EF Core:

#### Paso 1: Detener la aplicación
```powershell
# Asegúrate de que no hay procesos de FINANCEAPP en ejecución
taskkill /IM FINANCEAPP.exe /F
```

#### Paso 2: Crear la migración
```powershell
cd "C:\Users\Mr X\source\repos\FINANCEAPP\FINANCEAPP"
dotnet ef migrations add AddOperationsAndCompanyDetails --framework net10.0-android
```

#### Paso 3: Aplicar la migración
```powershell
dotnet ef database update --framework net10.0-android
```

### Opción 3: Script SQL Manual

Si prefieres ejecutar SQL directamente, aquí está el esquema:

```sql
-- Tabla Operations
CREATE TABLE Operations (
	Id INTEGER PRIMARY KEY AUTOINCREMENT,
	CompanyName TEXT NOT NULL,
	Date TEXT NOT NULL,
	CloseDate TEXT,
	Amount REAL NOT NULL,
	Type TEXT NOT NULL,
	CreatedAt TEXT NOT NULL
);

CREATE INDEX IX_Operations_CompanyName ON Operations(CompanyName);
CREATE INDEX IX_Operations_Date ON Operations(Date);
CREATE INDEX IX_Operations_Type ON Operations(Type);

-- Tabla CompanyDetails
CREATE TABLE CompanyDetails (
	Id INTEGER PRIMARY KEY AUTOINCREMENT,
	Name TEXT NOT NULL,
	Ticker TEXT NOT NULL UNIQUE,
	Sector TEXT NOT NULL,
	CurrentPrice REAL NOT NULL,
	DividendYield REAL NOT NULL,
	CreatedAt TEXT NOT NULL,
	UpdatedAt TEXT NOT NULL
);

CREATE UNIQUE INDEX IX_CompanyDetails_Ticker_Unique ON CompanyDetails(Ticker);
CREATE INDEX IX_CompanyDetails_Name ON CompanyDetails(Name);
CREATE INDEX IX_CompanyDetails_Sector ON CompanyDetails(Sector);
```

## 🧪 Datos de Prueba (Opcional)

Si quieres poblar las tablas con datos de ejemplo para pruebas, puedes:

### Operations de Ejemplo:
```csharp
var operations = new List<Operation>
{
	new Operation { CompanyName = "Microsoft", Type = "Compra", Date = DateTime.Now.AddMonths(-3), Amount = 5000.00m },
	new Operation { CompanyName = "Apple", Type = "Compra", Date = DateTime.Now.AddMonths(-2), Amount = 3000.00m },
	new Operation { CompanyName = "Tesla", Type = "Venta", Date = DateTime.Now.AddMonths(-1), CloseDate = DateTime.Now, Amount = 2000.00m }
};
```

### CompanyDetails de Ejemplo:
```csharp
var companies = new List<CompanyDetail>
{
	new CompanyDetail { Name = "Microsoft Corporation", Ticker = "MSFT", Sector = "Tecnología", CurrentPrice = 415.50m, DividendYield = 0.75m },
	new CompanyDetail { Name = "Apple Inc.", Ticker = "AAPL", Sector = "Tecnología", CurrentPrice = 225.30m, DividendYield = 0.45m },
	new CompanyDetail { Name = "Coca-Cola Company", Ticker = "KO", Sector = "Consumo", CurrentPrice = 63.20m, DividendYield = 3.10m },
	new CompanyDetail { Name = "Johnson & Johnson", Ticker = "JNJ", Sector = "Salud", CurrentPrice = 158.75m, DividendYield = 3.05m }
};
```

## 📱 Acceso a las Nuevas Páginas

Una vez que la migración esté completa, puedes acceder a las nuevas páginas:

- **Operaciones**: `/operations` - Lista todas las operaciones financieras
- **Empresas**: `/companies` - Catálogo de empresas con enlaces a Investing.com

## ⚙️ Configuración del Menú de Navegación

Para añadir estos enlaces al menú de navegación, actualiza el archivo `Components/Layout/NavMenu.razor`:

```razor
<div class="nav-item px-3">
	<NavLink class="nav-link" href="operations">
		<span class="bi bi-graph-up-arrow" aria-hidden="true"></span> Operaciones
	</NavLink>
</div>

<div class="nav-item px-3">
	<NavLink class="nav-link" href="companies">
		<span class="bi bi-building" aria-hidden="true"></span> Empresas
	</NavLink>
</div>
```

## 🔍 Verificación

Para verificar que todo funciona correctamente:

1. Ejecuta la aplicación
2. Navega a `/operations` - deberías ver la página de operaciones (vacía inicialmente)
3. Navega a `/companies` - deberías ver la página de empresas (vacía inicialmente)
4. Utiliza los servicios para añadir datos de prueba

## 📚 Servicios Disponibles

### IOperationService
- `GetAllAsync()` - Obtiene todas las operaciones
- `GetOpenOperationsAsync()` - Operaciones sin cerrar
- `AddAsync(operation)` - Añadir nueva operación
- Y más...

### ICompanyService
- `GetAllAsync()` - Obtiene todas las empresas
- `GetByTickerAsync(ticker)` - Buscar por ticker
- `GetTopByDividendYieldAsync()` - Top empresas por dividendo
- Y más...

## 🛠️ Soporte

Si encuentras problemas:
1. Verifica que la aplicación no esté en ejecución
2. Elimina la base de datos existente y deja que se recree
3. Revisa los logs de la consola para errores específicos
4. Asegúrate de que todos los servicios estén registrados en `MauiProgram.cs`

---

**Fecha de Creación**: 2025
**Arquitectura**: .NET MAUI 10 + Blazor Hybrid + Entity Framework Core + SQLite
