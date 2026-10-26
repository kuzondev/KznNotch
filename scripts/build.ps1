param([switch]$Publish, [switch]$Verify)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$localSdk = Join-Path $projectRoot '.tools\dotnet\dotnet.exe'
$dotnet = if (Test-Path -LiteralPath $localSdk) { $localSdk } else { 'dotnet' }
$env:DOTNET_CLI_HOME = Join-Path $projectRoot '.tools\cli'
$env:NUGET_PACKAGES = Join-Path $projectRoot '.tools\nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
& $dotnet build (Join-Path $projectRoot 'KznNotch.sln') -c Release --nologo -m:1 -nr:false -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'Build fehlgeschlagen.' }
if ($Verify) {
    $verificationDirectory = Join-Path $projectRoot ('artifacts\verification\' + [guid]::NewGuid().ToString('N'))
    & $dotnet run --project (Join-Path $projectRoot 'tests\KznNotch.Smoke\KznNotch.Smoke.csproj') -c Release -- $verificationDirectory
    if ($LASTEXITCODE -ne 0) { throw 'Funktionsprüfung fehlgeschlagen.' }
}
if ($Publish) {
    & $dotnet publish (Join-Path $projectRoot 'src\KznNotch\KznNotch.csproj') -c Release -r win-x64 --self-contained true -o (Join-Path $projectRoot 'artifacts\KznNotch-win-x64') --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Veröffentlichungsbuild fehlgeschlagen.' }
    Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE'), (Join-Path $projectRoot 'README.md'), (Join-Path $projectRoot 'THIRD-PARTY-NOTICES.md'), (Join-Path $projectRoot 'DESIGN.md') -Destination (Join-Path $projectRoot 'artifacts\KznNotch-win-x64') -Force
    Copy-Item -LiteralPath (Join-Path $projectRoot 'docs') -Destination (Join-Path $projectRoot 'artifacts\KznNotch-win-x64') -Recurse -Force
    Compress-Archive -Path (Join-Path $projectRoot 'artifacts\KznNotch-win-x64\*') -DestinationPath (Join-Path $projectRoot 'artifacts\KznNotch-0.10.7-win-x64.zip') -Force
}
