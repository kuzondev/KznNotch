@echo off
setlocal
set "KZN_NOTCH_ROOT=%~dp0"
powershell.exe -NoProfile -Command "$package = Get-AppxPackage -Name Notchify.Desktop; if ($package) { Start-Process explorer.exe -ArgumentList ('shell:AppsFolder\' + $package.PackageFamilyName + '!App'); exit 0 }; exit 1" >nul 2>&1
if not errorlevel 1 exit /b 0
if exist "%KZN_NOTCH_ROOT%artifacts\KznNotch-win-x64\KznNotch.exe" (
  start "" "%KZN_NOTCH_ROOT%artifacts\KznNotch-win-x64\KznNotch.exe" %*
  exit /b 0
)
if exist "%KZN_NOTCH_ROOT%artifacts\KznNotch-win-x64\KznNotch.exe" (
  start "" "%KZN_NOTCH_ROOT%artifacts\KznNotch-win-x64\KznNotch.exe" %*
  exit /b 0
)
if exist "%KZN_NOTCH_ROOT%.tools\dotnet\dotnet.exe" set "DOTNET_ROOT=%KZN_NOTCH_ROOT%.tools\dotnet"
if exist "%KZN_NOTCH_ROOT%src\KznNotch\bin\Release\net10.0-windows10.0.26100.0\KznNotch.exe" (
  start "" "%KZN_NOTCH_ROOT%src\KznNotch\bin\Release\net10.0-windows10.0.26100.0\KznNotch.exe" %*
  exit /b 0
)
echo Bitte zuerst powershell -ExecutionPolicy Bypass -File scripts\build.ps1 -Publish ausfuehren.
pause
