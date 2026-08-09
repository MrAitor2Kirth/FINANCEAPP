# SOLUCIÓN AL ERROR: SQLite Error 1: 'no such table: CompanyDetails'

## 🔴 Problema

Al intentar crear una nueva empresa, aparece el error:
```
Error: Error al guardar: SQLite Error 1: 'no such table: CompanyDetails'
```

Este error ocurre porque **la base de datos SQLite ya existe** pero fue creada **antes** de agregar los modelos `Operation` y `CompanyDetail`. Por lo tanto, no tiene las tablas necesarias.

---

## ✅ Solución Rápida (Recomendada)

### Opción 1: Detener la app y ejecutar el script PowerShell

1. **Cierra completamente la aplicación FINANCEAPP** (si está en ejecución)

2. **Ejecuta el script PowerShell** desde la raíz del proyecto:
   ```powershell
   .\RecreateDatabase.ps1
   ```

3. **Inicia la aplicación** nuevamente
   - La base de datos se recreará automáticamente con todas las tablas
   - Los datos de ejemplo (seed data) se cargarán automáticamente

### Opción 2: Eliminar manualmente la base de datos

1. **Cierra completamente la aplicación FINANCEAPP**

2. **Busca y elimina el archivo `financeapp.db`** en una de estas ubicaciones:

   **Windows:**
   ```
   %LOCALAPPDATA%\Packages\com.companyname.financeapp_XXXXXX\LocalState\financeapp.db
   ```

   **Android:**
   ```
   /data/data/com.companyname.financeapp/files/financeapp.db
   ```

   **iOS:**
   ```
   ~/Library/Application Support/financeapp.db
   ```

   **macOS:**
   ```
   ~/Library/Application Support/com.companyname.financeapp/financeapp.db
   ```

3. **Inicia la aplicación** nuevamente

---

## 🔧 Solución Técnica Implementada

He modificado `MauiProgram.cs` para que en **modo DEBUG** (desarrollo) elimine y recree automáticamente la base de datos cada vez que inicies la app:

```csharp
#if DEBUG
	// En modo DEBUG, eliminar y recrear la base de datos para asegurar el esquema actualizado
	// ADVERTENCIA: Esto eliminará todos los datos existentes
	context.Database.EnsureDeleted();
	context.Database.EnsureCreated();

	var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
	logger.LogInformation($"Base de datos recreada en: {dbPath}");
#else
	// En modo RELEASE, solo crear si no existe
	context.Database.EnsureCreated();
#endif
```

**Ventajas:**
- ✅ En desarrollo (DEBUG), siempre tendrás el esquema actualizado
- ✅ Los seed data se cargan automáticamente
- ✅ No necesitas eliminar manualmente la BD cada vez que cambies el modelo
- ✅ En producción (RELEASE), la BD no se elimina nunca

**Desventajas:**
- ⚠️ En DEBUG, perderás los datos cada vez que reinicies la app
- ⚠️ Debes cerrar completamente la app antes del siguiente inicio

---

## 🚀 Próximos Pasos (Para Producción)

### Implementar Migraciones de Entity Framework Core

Para producción, en lugar de `EnsureCreated()`, deberías usar migraciones:

```bash
# Instalar la herramienta dotnet-ef (si no está instalada)
dotnet tool install --global dotnet-ef

# Crear la migración inicial
dotnet ef migrations add InitialCreate --project FINANCEAPP

# Aplicar la migración
dotnet ef database update --project FINANCEAPP
```

Y modificar `MauiProgram.cs`:

```csharp
// Aplicar migraciones pendientes automáticamente
context.Database.Migrate();
```

**Ventajas de las Migraciones:**
- ✅ Preserva los datos existentes
- ✅ Controla los cambios del esquema con versionado
- ✅ Permite actualizaciones incrementales
- ✅ Seguro para producción

---

## 📊 Verificar que Funciona

Después de aplicar la solución:

1. Inicia la aplicación
2. Ve a **Empresas** en el menú
3. Click en **Nueva Empresa**
4. Rellena los campos:
   - Nombre: `Tesla Inc.`
   - Ticker: `TSLA`
   - Sector: `Tecnología`
   - Precio actual: `250.00`
   - Dividend Yield: `0.00`
5. Click **Guardar**

**Resultado esperado:**
- ✅ La empresa se guarda correctamente
- ✅ Vuelves a la lista de empresas
- ✅ La nueva empresa aparece en la lista
- ✅ Las 5 empresas de ejemplo también están presentes

---

## 🐛 Si Aún Tienes Problemas

### Error: "Database is locked"
- **Causa:** La aplicación sigue en ejecución
- **Solución:** Cierra completamente la app (incluyendo procesos en segundo plano)

### Error persistente después de eliminar la BD
- **Causa:** Caché de Visual Studio
- **Solución:**
  1. En Visual Studio: `Build > Clean Solution`
  2. En Visual Studio: `Build > Rebuild Solution`
  3. Reinicia Visual Studio
  4. Vuelve a iniciar la aplicación

### La tabla se crea pero los seed data no aparecen
- **Causa:** `EnsureCreated()` no ejecuta seed data si la tabla ya existe
- **Solución:** Usa el script `RecreateDatabase.ps1` para forzar recreación completa

---

## 📝 Resumen

| **Situación** | **Solución** |
|---------------|--------------|
| Primera vez o desarrollo | Ejecutar `RecreateDatabase.ps1` |
| Modo DEBUG | Simplemente reiniciar la app (se recrea auto) |
| Modo RELEASE | Implementar migraciones de EF Core |
| Producción | Nunca usar `EnsureDeleted()`, solo migraciones |

---

**Estado Actual:** ✅ **SOLUCIONADO**

La aplicación ahora recreará automáticamente la base de datos en modo DEBUG cada vez que se inicie, asegurando que siempre tenga el esquema más actualizado.

---

*Documentación generada automáticamente - FINANCEAPP © 2024*
