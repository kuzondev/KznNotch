param([switch]$DevelopmentCertificate, [string]$SigningCertificate, [string]$CertificatePassword)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $projectRoot
if (-not $DevelopmentCertificate -and -not $SigningCertificate) { throw 'Specify your existing -SigningCertificate or explicitly use -DevelopmentCertificate.' }
if (git status --porcelain) { throw 'Commit changes before publishing a release.' }
[xml]$project = Get-Content src/KznNotch/KznNotch.csproj
$version = $project.Project.PropertyGroup.Version | Select-Object -First 1
& (Join-Path $PSScriptRoot 'build.ps1') -Publish
if ($DevelopmentCertificate) { & (Join-Path $PSScriptRoot 'package-msix.ps1') -DevelopmentCertificate }
else { & (Join-Path $PSScriptRoot 'package-msix.ps1') -SigningCertificate $SigningCertificate -CertificatePassword $CertificatePassword }
$head = git rev-parse HEAD
git push origin HEAD
if ($LASTEXITCODE -ne 0) { throw 'Push failed.' }
$releasePackage = Join-Path $projectRoot "artifacts/KznNotch-installer/KznNotch-$version-x64.msix"
gh release create "v$version" $releasePackage --repo kuzondev/KznNotch --target $head --title "Kzn Notch v$version" --generate-notes
if ($LASTEXITCODE -ne 0) { throw 'GitHub Release failed.' }
