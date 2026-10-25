---
name: publish-app
description: Publica (compila en Release) la app MAUI de FINANCEAPP para Windows en C:\Users\Mr X\Apps\FINANCEAPP, la carpeta del acceso directo del escritorio, y comprueba el acceso directo. Úsala cuando el usuario pida publicar, compilar o actualizar la app del acceso directo, o con /publish-app.
---

# Publicar la app de escritorio (Windows)

Deja en `C:\Users\Mr X\Apps\FINANCEAPP` la versión Release de la app, la que abre el acceso directo «FINANCEAPP» del escritorio. Usa la misma base de datos que Visual Studio (`AppData\Local\User Name\com.companyname.financeapp\Data\financeapp.db`): esta skill **no toca la base de datos** ni el código.

Responde siempre en español.

## Pasos

1. **Comprobar que la app no está abierta** (si lo está, el `.exe` está bloqueado y la publicación falla):

   ```bash
   tasklist | grep -i financeapp
   ```

   Si aparece `FINANCEAPP.exe`, **no lo cierres tú**: pide al usuario que cierre la app (o que pare la depuración en Visual Studio) y espera su confirmación antes de seguir.

2. **Publicar** desde la raíz del repositorio (`C:\Users\Mr X\source\repos\FINANCEAPP`). Tarda uno o dos minutos; lanza la orden con un tiempo de espera amplio (10 minutos):

   ```bash
   dotnet publish FINANCEAPP/FINANCEAPP.csproj -f net10.0-windows10.0.19041.0 -c Release -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true -o "C:\Users\Mr X\Apps\FINANCEAPP" -nodeReuse:false
   ```

   - **No añadas `-r win-x64`**: se aplica a todos los destinos del proyecto (también Android) y la restauración falla con `NU1102 ... Microsoft.NETCore.App.Runtime.Mono.win-x64`. Windows ya usa `win-x64` por defecto.
   - Si sale un error de fichero bloqueado (`MSB3021`/`MSB3027`, «está siendo utilizado en otro proceso»), vuelve al paso 1.
   - Si sale un error de compilación, enséñaselo al usuario tal cual y no digas que la app está publicada.

3. **Comprobar el resultado**:

   ```bash
   ls -la "/c/Users/Mr X/Apps/FINANCEAPP/FINANCEAPP.exe"
   ```

   La fecha del fichero debe ser la de ahora.

4. **Icono y acceso directo** del escritorio (PowerShell). El acceso directo usa `financeapp.ico` (el logo en 16–256 px, copiado del proyecto) y no el del `.exe`: el que genera MAUI solo trae 64 px y Windows guarda en caché el icono antiguo de los accesos directos.

   ```powershell
   $app = 'C:\Users\Mr X\Apps\FINANCEAPP'
   Copy-Item 'C:\Users\Mr X\source\repos\FINANCEAPP\FINANCEAPP\Resources\AppIcon\financeapp.ico' "$app\financeapp.ico" -Force
   $lnk = Join-Path ([Environment]::GetFolderPath('Desktop')) 'FINANCEAPP.lnk'
   $s = (New-Object -ComObject WScript.Shell).CreateShortcut($lnk)
   if (-not (Test-Path $lnk) -or $s.IconLocation -ne "$app\financeapp.ico,0") {
       $s.TargetPath = "$app\FINANCEAPP.exe"
       $s.WorkingDirectory = $app
       $s.IconLocation = "$app\financeapp.ico,0"
       $s.Description = 'FINANCEAPP - patrimonio e inversiones'
       $s.Save()
   }
   Test-Path $lnk
   ```

   Si cambia el logo, hay que regenerar `Resources/AppIcon/appiconfg.png`, `Resources/Splash/splash.png` y `Resources/AppIcon/financeapp.ico` (ver `AGENTS.md`).

5. **Informar** en pocas líneas: si se publicó (o el error), la hora del `.exe` y que ya puede abrirla con el acceso directo del escritorio. Recuerda que no conviene tenerla abierta a la vez que la de Visual Studio (escriben en la misma base de datos).

## Reglas

- No borres a mano la carpeta `C:\Users\Mr X\Apps\FINANCEAPP` ni nada de `Backups/` o de la base de datos.
- No hagas commit ni push.
- No cambies el `.csproj`, `Platforms/` ni la configuración de firma.
