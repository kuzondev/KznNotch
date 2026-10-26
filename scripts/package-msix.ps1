param([string]$SdkBin, [string]$SigningCertificate, [string]$CertificatePassword, [string]$PublishDirectory, [switch]$DevelopmentCertificate)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $SdkBin) {
    $candidate = Get-ChildItem -LiteralPath (Join-Path $projectRoot '.tools\windows-sdk\bin') -Filter makeappx.exe -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.Directory.Name -eq 'x64' } | Select-Object -First 1
    if (-not $candidate) { $candidate = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin\*\x64\makeappx.exe' -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1 }
    if (-not $candidate) { throw 'Windows SDK BuildTools erforderlich. -SdkBin mit dem Ordner von makeappx.exe angeben.' }
    $SdkBin = $candidate.Directory.FullName
}
$source = if ($PublishDirectory) { $PublishDirectory } else { Join-Path $projectRoot 'artifacts\KznNotch-win-x64' }
if (-not (Test-Path (Join-Path $source 'KznNotch.exe'))) { throw 'Zuerst scripts/build.ps1 -Publish ausführen.' }
$stage = Join-Path $projectRoot ('artifacts\msix-stage-' + [guid]::NewGuid().ToString('N'))
$output = Join-Path $projectRoot 'artifacts\KznNotch-installer'
New-Item -ItemType Directory -Force -Path $stage,$output | Out-Null
Copy-Item -Path (Join-Path $source '*') -Destination $stage -Recurse -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'packaging\AppxManifest.xml') -Destination (Join-Path $stage 'AppxManifest.xml')
New-Item -ItemType Directory -Force -Path (Join-Path $stage 'Assets') | Out-Null
Add-Type -AssemblyName System.Drawing
foreach ($asset in @(@('StoreLogo.png',50),@('Square44x44Logo.png',44),@('Square150x150Logo.png',150))) {
    $size = [int]$asset[1]
    $bitmap = [Drawing.Bitmap]::new($size,$size)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = 'AntiAlias'
    $graphics.Clear([Drawing.Color]::FromArgb(8,8,8))
    $accent = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(185,163,255))
    $dark = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(8,8,8))
    $graphics.FillEllipse($accent,$size*.08,$size*.25,$size*.84,$size*.5)
    $graphics.FillEllipse($dark,$size*.18,$size*.34,$size*.64,$size*.32)
    $graphics.FillEllipse($accent,$size*.62,$size*.42,$size*.14,$size*.14)
    $bitmap.Save((Join-Path $stage ('Assets\' + $asset[0])),[Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose(); $bitmap.Dispose(); $accent.Dispose(); $dark.Dispose()
}
$package = Join-Path $output 'KznNotch-0.10.0-x64.msix'
$packOutput = & (Join-Path $SdkBin 'makeappx.exe') pack /d $stage /p $package /o 2>&1
$packOutput | Set-Content (Join-Path $output 'package-build.log')
if ($LASTEXITCODE -ne 0) { $packOutput | Select-Object -Last 15 | Write-Output; throw 'MSIX-Validierung fehlgeschlagen.' }
if ($DevelopmentCertificate) {
    # Create a file-only development certificate. Never silently modify a certificate trust store.
    $keyDirectory = Join-Path $projectRoot '.tools\signing'
    New-Item -ItemType Directory -Force $keyDirectory | Out-Null
    $SigningCertificate = Join-Path $keyDirectory 'Notchify-Development.pfx'
    $passwordPath = Join-Path $keyDirectory 'password.txt'
    if (-not (Test-Path $SigningCertificate)) {
        $CertificatePassword = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
        $rsa = [Security.Cryptography.RSA]::Create(3072)
        $request = [Security.Cryptography.X509Certificates.CertificateRequest]::new('CN=Notchify Development',$rsa,[Security.Cryptography.HashAlgorithmName]::SHA256,[Security.Cryptography.RSASignaturePadding]::Pkcs1)
        $request.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new($false,$false,0,$true))
        $request.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new([Security.Cryptography.X509Certificates.X509KeyUsageFlags]::DigitalSignature,$true))
        $oids = [Security.Cryptography.OidCollection]::new()
        $oids.Add([Security.Cryptography.Oid]::new('1.3.6.1.5.5.7.3.3')) | Out-Null
        $request.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]::new($oids,$true))
        $certificate = $request.CreateSelfSigned([DateTimeOffset]::Now.AddMinutes(-5),[DateTimeOffset]::Now.AddYears(1))
        [IO.File]::WriteAllBytes($SigningCertificate,$certificate.Export([Security.Cryptography.X509Certificates.X509ContentType]::Pfx,$CertificatePassword))
        [IO.File]::WriteAllText($passwordPath,$CertificatePassword)
        $certificate.Dispose(); $rsa.Dispose()
    } else { $CertificatePassword = [IO.File]::ReadAllText($passwordPath) }
    $certificate = [Security.Cryptography.X509Certificates.X509Certificate2]::new($SigningCertificate,$CertificatePassword)
    [IO.File]::WriteAllBytes((Join-Path $output 'KznNotch-Development.cer'),$certificate.Export([Security.Cryptography.X509Certificates.X509ContentType]::Cert))
    $certificate.Dispose()
}
if ($SigningCertificate) {
    & (Join-Path $SdkBin 'signtool.exe') sign /fd SHA256 /f $SigningCertificate /p $CertificatePassword $package
    if ($LASTEXITCODE -ne 0) { throw 'MSIX-Signierung fehlgeschlagen.' }
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'scripts\Install-KznNotch.ps1') -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'scripts\Migrate-LegacyData.ps1') -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\DISCORD.md') -Destination $output -Force
Write-Output "MSIX erstellt: $package"
