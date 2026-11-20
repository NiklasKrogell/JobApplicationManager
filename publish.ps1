<#
.SYNOPSIS
    Asentaa tai päivittää Työnhaku-CRM:n tälle koneelle.

.DESCRIPTION
    1. Tarkistaa, ettei sovellus ole käynnissä.
    2. Ottaa varmuuskopion tietokannasta (%LOCALAPPDATA%\JobApplicationManager\backups, 10 uusinta säilytetään).
    3. Julkaisee Release-version väliaikaiskansioon ja peilaa sen asennuskansioon
       (vanhan version ylimääräiset tiedostot poistuvat; data on eri kansiossa eikä muutu).
    4. Luo Käynnistä-valikon pikakuvakkeen, jos sitä ei ole.

    Tietokanta päivittyy (migraatiot) automaattisesti, kun uusi versio käynnistetään.

.EXAMPLE
    .\publish.ps1
#>
[CmdletBinding()]
param(
    [string]$Destination = (Join-Path $env:LOCALAPPDATA 'Programs\Tyonhaku-CRM'),
    [switch]$SkipBackup,
    [switch]$NoShortcut
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'JobApplicationManager.csproj'
$exeName = 'JobApplicationManager.exe'
$dataDir = Join-Path $env:LOCALAPPDATA 'JobApplicationManager'
$database = Join-Path $dataDir 'job_applications.db'

# 1. Sovellus ei saa olla käynnissä (tiedostot lukossa, ja varmuuskopio voisi jäädä kesken).
$running = Get-Process -Name 'JobApplicationManager' -ErrorAction SilentlyContinue
if ($running) {
    Write-Error 'Työnhaku-CRM on käynnissä. Sulje se (ja tallenna muutokset) ja aja skripti uudelleen.'
}

# 2. Varmuuskopio
if (-not $SkipBackup -and (Test-Path $database)) {
    $backupDir = Join-Path $dataDir 'backups'
    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
    $stamp = Get-Date -Format 'yyyy-MM-dd_HHmmss'
    $backup = Join-Path $backupDir "job_applications_$stamp.db"
    Copy-Item -LiteralPath $database -Destination $backup
    Write-Host "Varmuuskopio: $backup"

    Get-ChildItem -LiteralPath $backupDir -Filter 'job_applications_*.db' |
        Sort-Object Name -Descending | Select-Object -Skip 10 |
        Remove-Item -Confirm:$false
}

# 3. Julkaisu väliaikaiskansioon, sitten peilaus asennuskansioon
$staging = Join-Path ([System.IO.Path]::GetTempPath()) ("tyonhaku-crm-publish-" + [guid]::NewGuid().ToString('N'))
try {
    Write-Host 'Julkaistaan Release-versio...'
    & dotnet publish $project -c Release -r win-x64 --self-contained false -o $staging -nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish epäonnistui (exit $LASTEXITCODE). Asennettua versiota ei muutettu." }

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    & robocopy $staging $Destination /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    # robocopy: 0-7 = onnistui, 8+ = virhe
    if ($LASTEXITCODE -ge 8) { throw "Tiedostojen kopiointi epäonnistui (robocopy $LASTEXITCODE)." }
    $global:LASTEXITCODE = 0
}
finally {
    if (Test-Path $staging) { Remove-Item -LiteralPath $staging -Recurse -Force -Confirm:$false }
}

$exe = Join-Path $Destination $exeName
$version = (Get-Item $exe).VersionInfo.FileVersion
Write-Host "Asennettu: $exe (versio $version)"

# 4. Pikakuvake Käynnistä-valikkoon
if (-not $NoShortcut) {
    $shortcutPath = Join-Path ([Environment]::GetFolderPath('Programs')) 'Työnhaku-CRM.lnk'
    if (-not (Test-Path $shortcutPath)) {
        $shell = New-Object -ComObject WScript.Shell
        $shortcut = $shell.CreateShortcut($shortcutPath)
        $shortcut.TargetPath = $exe
        $shortcut.WorkingDirectory = $Destination
        $shortcut.Description = 'Työnhaku-CRM'
        $shortcut.Save()
        Write-Host "Pikakuvake luotu: $shortcutPath"
    }
}

Write-Host 'Valmis. Tietokanta päivittyy automaattisesti, kun käynnistät sovelluksen.'
