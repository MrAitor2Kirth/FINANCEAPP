# Script para eliminar la base de datos SQLite y permitir que se recree
# ADVERTENCIA: Esto eliminará todos los datos existentes

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "   FINANCEAPP - Recrear Base de Datos SQLite" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Buscar el archivo de base de datos
$possiblePaths = @(
	"$env:LOCALAPPDATA\Packages\com.companyname.financeapp*\LocalState\financeapp.db",
	"$env:APPDATA\FINANCEAPP\financeapp.db",
	".\financeapp.db"
)

$foundDatabase = $false

foreach ($pattern in $possiblePaths) {
	$files = Get-ChildItem -Path $pattern -ErrorAction SilentlyContinue -Recurse

	if ($files) {
		foreach ($file in $files) {
			Write-Host "Base de datos encontrada: " -NoNewline -ForegroundColor Yellow
			Write-Host $file.FullName -ForegroundColor White

			try {
				Remove-Item -Path $file.FullName -Force
				Write-Host "[✓] Base de datos eliminada correctamente" -ForegroundColor Green
				$foundDatabase = $true
			}
			catch {
				Write-Host "[✗] Error al eliminar: $_" -ForegroundColor Red
				Write-Host "    Asegúrate de que la aplicación no esté en ejecución" -ForegroundColor Yellow
			}
		}
	}
}

if (-not $foundDatabase) {
	Write-Host "[i] No se encontró ninguna base de datos existente" -ForegroundColor Cyan
	Write-Host "    La base de datos se creará automáticamente al iniciar la app" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Instrucciones:" -ForegroundColor White
Write-Host "1. Asegúrate de que FINANCEAPP no esté en ejecución" -ForegroundColor Gray
Write-Host "2. Ejecuta este script para eliminar la base de datos" -ForegroundColor Gray
Write-Host "3. Inicia la aplicación" -ForegroundColor Gray
Write-Host "4. La base de datos se recreará automáticamente con el nuevo esquema" -ForegroundColor Gray
Write-Host "5. Los datos de ejemplo (seed data) se cargarán automáticamente" -ForegroundColor Gray
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""
