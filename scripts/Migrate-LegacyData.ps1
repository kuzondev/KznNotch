$ErrorActionPreference = 'Stop'
$roaming = [Environment]::GetFolderPath('ApplicationData')
$package = Get-AppxPackage -Name Notchify.Desktop -ErrorAction SilentlyContinue
$sources = @((Join-Path $roaming 'Notchify'))
$targets = @((Join-Path $roaming 'Kzn Notch'))
if ($package) {
    # MSIX redirects app-data access. Migrate from outside the packaged process so the legacy directory cannot shadow files.
    $cache = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) ('Packages\' + $package.PackageFamilyName + '\LocalCache\Roaming')
    $sources += Join-Path $cache 'Notchify'
    $targets += Join-Path $cache 'Kzn Notch'
}
$copied = 0
foreach ($name in @('settings.json','todos.json','reminders.json','timer.json')) {
    $candidate = $sources | ForEach-Object { Join-Path $_ $name } | Where-Object { Test-Path -LiteralPath $_ } | ForEach-Object { Get-Item -LiteralPath $_ } | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    if (-not $candidate) { continue }
    foreach ($directory in $targets) {
        $target = Join-Path $directory $name
        if (-not (Test-Path -LiteralPath $target)) {
            New-Item -ItemType Directory -Path $directory -Force | Out-Null
            [IO.File]::Copy($candidate.FullName,$target,$false)
            $copied++
        }
    }
}
Write-Output "Legacy data migration: $copied file copies; existing destination files preserved."
