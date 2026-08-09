# Script para corregir errores de compilación en archivos Razor
# 1. Añadir @using Microsoft.Extensions.Logging
# 2. Corregir sintaxis de NavigateTo (quitar comillas escapadas)
# 3. Corregir llamadas a servicios con firmas incorrectas

$ErrorActionPreference = "Stop"
$componentsPath = "FINANCEAPP\Components\Pages"

Write-Host "=== Iniciando corrección de errores Razor ===" -ForegroundColor Green

# Función para añadir using de ILogger si falta
function Add-LoggerUsing {
	param([string]$FilePath)

	$content = Get-Content $FilePath -Raw -Encoding UTF8

	# Si ya tiene ILogger inyectado pero no tiene el using
	if ($content -match '@inject ILogger' -and $content -notmatch '@using Microsoft\.Extensions\.Logging') {
		Write-Host "  -> Añadiendo @using Microsoft.Extensions.Logging" -ForegroundColor Yellow

		# Buscar la posición después del último @using
		if ($content -match '(@using [^\r\n]+\r?\n)+') {
			$content = $content -replace '((@using [^\r\n]+\r?\n)+)', "`$1@using Microsoft.Extensions.Logging`r`n"
		}
		else {
			# Si no hay @using, añadir después de @page
			$content = $content -replace '(@page "[^"]+"\r?\n)', "`$1@using Microsoft.Extensions.Logging`r`n"
		}

		Set-Content $FilePath -Value $content -Encoding UTF8 -NoNewline
	}
}

# Función para corregir NavigateTo con comillas escapadas
function Fix-NavigateTo {
	param([string]$FilePath)

	$content = Get-Content $FilePath -Raw -Encoding UTF8
	$modified = $false

	# Patrón: @onclick="() => Navigation.NavigateTo(\"algo\")"
	# Corregir a: @onclick='() => Navigation.NavigateTo("/algo")'
	if ($content -match 'NavigateTo\(\\') {
		Write-Host "  -> Corrigiendo NavigateTo con comillas escapadas" -ForegroundColor Yellow

		# Reemplazar todos los casos de NavigateTo(\"/...\") por NavigateTo('/...')
		$content = $content -replace '@onclick="([^"]*?)Navigation\.NavigateTo\(\\\"([^\\]+)\\\"\)([^"]*?)"', "@onclick='`$1Navigation.NavigateTo(`"`$2`")`$3'"

		# También corregir casos con selectedPlusvaliaOption = \"A\"
		$content = $content -replace '@onclick="([^"]*?)=\s*\\\"([A-Z])\\\"([^"]*?)"', "@onclick='`$1= `"`$2`"`$3'"

		$modified = $true
	}

	if ($modified) {
		Set-Content $FilePath -Value $content -Encoding UTF8 -NoNewline
	}
}

# Función para corregir llamadas a servicios
function Fix-ServiceCalls {
	param([string]$FilePath)

	$content = Get-Content $FilePath -Raw -Encoding UTF8
	$modified = $false

	# 1. FundService.CreateOperationAsync(FundName, newOperation) -> CreateOperationAsync(newOperation)
	if ($content -match 'FundService\.CreateOperationAsync\(FundName,\s*newOperation\)') {
		Write-Host "  -> Corrigiendo FundService.CreateOperationAsync" -ForegroundColor Yellow
		$content = $content -replace 'await FundService\.CreateOperationAsync\(FundName,\s*newOperation\);', 'newOperation.FundName = FundName;
			await FundService.CreateOperationAsync(newOperation);'
		$modified = $true
	}

	# 2. FundService.UpdateFundValueWithHistoryAsync(FundName, newHistory.Value, ...) 
	#    -> crear FundValueHistory completo
	if ($content -match 'FundService\.UpdateFundValueWithHistoryAsync\(FundName,\s*newHistory\.Value') {
		Write-Host "  -> Corrigiendo FundService.UpdateFundValueWithHistoryAsync" -ForegroundColor Yellow
		$content = $content -replace 'await FundService\.UpdateFundValueWithHistoryAsync\(FundName,\s*newHistory\.Value,\s*newHistory\.ReturnAtDate,\s*newHistory\.Notes\);', 'await FundService.UpdateFundValueWithHistoryAsync(FundName, newHistory.Value);'
		$modified = $true
	}

	# 3. BankService.CreateOperationAsync(AccountName, newOperation) -> CreateOperationAsync(newOperation)
	if ($content -match 'BankService\.CreateOperationAsync\(AccountName,\s*newOperation\)') {
		Write-Host "  -> Corrigiendo BankService.CreateOperationAsync" -ForegroundColor Yellow
		$content = $content -replace 'await BankService\.CreateOperationAsync\(AccountName,\s*newOperation\);', 'newOperation.AccountName = AccountName;
			await BankService.CreateOperationAsync(newOperation);'
		$modified = $true
	}

	# 4. BankService.UpdateBankAccountAsync(Id, account) -> UpdateBankAccountAsync(account)
	if ($content -match 'BankService\.UpdateBankAccountAsync\(Id,\s*account\)') {
		Write-Host "  -> Corrigiendo BankService.UpdateBankAccountAsync" -ForegroundColor Yellow
		$content = $content -replace 'await BankService\.UpdateBankAccountAsync\(Id,\s*account\);', 'account.Id = Id;
				await BankService.UpdateBankAccountAsync(account);'
		$modified = $true
	}

	# 5. FundService.UpdateFundAsync(Id, fund) -> UpdateFundAsync(fund)
	if ($content -match 'FundService\.UpdateFundAsync\(Id,\s*fund\)') {
		Write-Host "  -> Corrigiendo FundService.UpdateFundAsync" -ForegroundColor Yellow
		$content = $content -replace 'await FundService\.UpdateFundAsync\(Id,\s*fund\);', 'fund.Id = Id;
				await FundService.UpdateFundAsync(fund);'
		$modified = $true
	}

	# 6. SafeHavenService.CreateOperationAsync(AssetName, newOperation) -> CreateOperationAsync(newOperation)
	if ($content -match 'SafeHavenService\.CreateOperationAsync\(AssetName,\s*newOperation\)') {
		Write-Host "  -> Corrigiendo SafeHavenService.CreateOperationAsync" -ForegroundColor Yellow
		$content = $content -replace 'await SafeHavenService\.CreateOperationAsync\(AssetName,\s*newOperation\);', 'newOperation.AssetName = AssetName;
			await SafeHavenService.CreateOperationAsync(newOperation);'
		$modified = $true
	}

	# 7. SafeHavenService.UpdateAssetAsync(Id, asset) -> UpdateAssetAsync(asset)
	if ($content -match 'SafeHavenService\.UpdateAssetAsync\(Id,\s*asset\)') {
		Write-Host "  -> Corrigiendo SafeHavenService.UpdateAssetAsync" -ForegroundColor Yellow
		$content = $content -replace 'await SafeHavenService\.UpdateAssetAsync\(Id,\s*asset\);', 'asset.Id = Id;
				await SafeHavenService.UpdateAssetAsync(asset);'
		$modified = $true
	}

	# 8. PropertyService.CreateOperationAsync(PropertyName, newOperation) -> CreateOperationAsync(newOperation)
	if ($content -match 'PropertyService\.CreateOperationAsync\(PropertyName,\s*newOperation\)') {
		Write-Host "  -> Corrigiendo PropertyService.CreateOperationAsync" -ForegroundColor Yellow
		$content = $content -replace 'await PropertyService\.CreateOperationAsync\(PropertyName,\s*newOperation\);', 'newOperation.PropertyName = PropertyName;
			await PropertyService.CreateOperationAsync(newOperation);'
		$modified = $true
	}

	# 9. PropertyService.UpdatePropertyAsync(Id, property) -> UpdatePropertyAsync(property)
	if ($content -match 'PropertyService\.UpdatePropertyAsync\(Id,\s*property\)') {
		Write-Host "  -> Corrigiendo PropertyService.UpdatePropertyAsync" -ForegroundColor Yellow
		$content = $content -replace 'await PropertyService\.UpdatePropertyAsync\(Id,\s*property\);', 'property.Id = Id;
				await PropertyService.UpdatePropertyAsync(property);'
		$modified = $true
	}

	if ($modified) {
		Set-Content $FilePath -Value $content -Encoding UTF8 -NoNewline
	}
}

# Procesar todos los archivos .razor recursivamente
Get-ChildItem -Path $componentsPath -Filter "*.razor" -Recurse | ForEach-Object {
	Write-Host "Procesando: $($_.FullName)" -ForegroundColor Cyan

	Add-LoggerUsing -FilePath $_.FullName
	Fix-NavigateTo -FilePath $_.FullName
	Fix-ServiceCalls -FilePath $_.FullName
}

Write-Host "`n=== Corrección completada ===" -ForegroundColor Green
Write-Host "Ejecuta 'dotnet build' para verificar los cambios" -ForegroundColor Yellow
