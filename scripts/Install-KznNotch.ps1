#Requires -Version 5.1
param([switch]$TrustDevelopmentCertificate)
$ErrorActionPreference = 'Stop'
Start-Transcript -Path (Join-Path $PSScriptRoot 'install.log') -Append | Out-Null
$package = Join-Path $PSScriptRoot 'KznNotch-0.10.7-x64.msix'
if (-not (Test-Path -LiteralPath $package)) { throw 'Das KznNotch-MSIX muss neben diesem Installationsskript liegen.' }
if ($TrustDevelopmentCertificate) {
    # Explicit installer opt-in only. Broad public releases must use a production/Store signature.
    $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Zum Vertrauen des lokalen Entwicklungszertifikats dieses Skript als Administrator ausführen.' }
    $certificate = Join-Path $PSScriptRoot 'KznNotch-Development.cer'
    Import-Certificate -FilePath $certificate -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople' | Out-Null
}
Add-AppxPackage -Path $package
& (Join-Path $PSScriptRoot 'Migrate-LegacyData.ps1')
Write-Output 'Kzn Notch installiert. Im Startmenü öffnen und Einstellungen > Integrations > Discord Integration aktivieren.'
Stop-Transcript | Out-Null
