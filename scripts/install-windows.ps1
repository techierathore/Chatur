<#
.SYNOPSIS
  Builds Chatur for Windows (Release, unpackaged, self-contained) and installs it for this user, with a Start menu
  entry "Chatur" (REQ-NFR-012, owner's evening test of 21 Nov 2026).

.DESCRIPTION
  - Publishes src/Chatur for win-x64 into src/Chatur/bin/Release/install (this repository's own bin folder).
  - Installs it to %LOCALAPPDATA%\Programs\Chatur, replacing an earlier install in place.
  - Makes the Start menu entry %APPDATA%\Microsoft\Windows\Start Menu\Programs\Chatur.lnk.
  - Readies this repository's bin folder for the owner's test: the folders that test makes go there, never beside
    your projects.
  - Your data stays where it is: the installed app is the same application (com.techierathore.chatur, publisher
    Techie Rathore), so it keeps its database, logs and sign-in under
    %LOCALAPPDATA%\Techie Rathore\com.techierathore.chatur\Data, exactly as the app started from Visual Studio did.
  - A Release build does not read user secrets, so the App Manager address and installed-app key (pk_…) are taken
    from this project's user secrets (Visual Studio "Manage User Secrets") into a settings file under
    src/Chatur/obj/install, embedded in the build, and that file is deleted again. The key is never printed.

  Run from Windows PowerShell, or from WSL:
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts\install-windows.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$Repo = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $Repo 'src\Chatur\Chatur.csproj'
$Tfm = 'net10.0-windows10.0.19041.0'
$PublishDir = Join-Path $Repo 'src\Chatur\bin\Release\install'
$SettingsDir = Join-Path $Repo 'src\Chatur\obj\install'
$SettingsFile = Join-Path $SettingsDir 'appsettings.json'
$InstallDir = Join-Path $env:LOCALAPPDATA 'Programs\Chatur'
$Shortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'Chatur.lnk'

# 1. Nothing is replaced under a running copy.
$running = Get-Process -Name Chatur -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and $_.Path.StartsWith($InstallDir, [StringComparison]::OrdinalIgnoreCase) }
if ($running) {
    throw "Chatur is running from $InstallDir. Quit it, then run this again."
}

# 2. The settings the installed build embeds: appsettings.json with the App Manager values from user secrets.
[xml]$csproj = Get-Content -LiteralPath $Project -Raw
$secretsId = @($csproj.Project.PropertyGroup | ForEach-Object { $_.UserSecretsId } | Where-Object { $_ })[0]
$secretsFile = Join-Path $env:APPDATA "Microsoft\UserSecrets\$secretsId\secrets.json"
$settings = Get-Content -LiteralPath (Join-Path $Repo 'src\Chatur\appsettings.json') -Raw | ConvertFrom-Json
if (Test-Path -LiteralPath $secretsFile) {
    $secrets = Get-Content -LiteralPath $secretsFile -Raw | ConvertFrom-Json
    foreach ($name in 'BaseUrl', 'ApplicationId', 'ApiKey') {
        $value = $secrets."AppManager:$name"
        if ($value) {
            $settings.AppManager | Add-Member -NotePropertyName $name -NotePropertyValue $value -Force
        }
    }
}
$key = "$($settings.AppManager.ApiKey)"
if (-not $key) {
    throw "No AppManager:ApiKey in this project's user secrets ($secretsId). Add the installed-app key (pk_...) with Manage User Secrets."
}
if (-not $key.StartsWith('pk_')) {
    throw "AppManager:ApiKey must be an installed-app key (it starts pk_). A website key needs a secret and must not ship inside an app."
}
$key = $null
New-Item -ItemType Directory -Force -Path $SettingsDir | Out-Null
$settings | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $SettingsFile -Encoding UTF8
Write-Host "Settings for the install written (the key is not shown)."

# 3. Build: Release, unpackaged, self-contained, into this repository's own bin folder.
try {
    Write-Host "Publishing Chatur (Release, win-x64, self-contained) to $PublishDir"
    & dotnet publish $Project -f $Tfm -c Release -r win-x64 --self-contained `
        -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true "-p:ChaturSettingsFile=$SettingsFile" `
        -o $PublishDir -nologo -v:minimal
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }
}
finally {
    Remove-Item -LiteralPath $SettingsFile -Force -ErrorAction SilentlyContinue
}

if (-not (Test-Path -LiteralPath (Join-Path $PublishDir 'Chatur.exe'))) {
    throw "The publish folder has no Chatur.exe: $PublishDir"
}

# 4. Install, replacing an earlier install in place.
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
& robocopy $PublishDir $InstallDir /MIR /R:2 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) {
    throw "Copying to $InstallDir failed (robocopy exit code $LASTEXITCODE)."
}
$global:LASTEXITCODE = 0

# 5. The Start menu entry.
$exe = Join-Path $InstallDir 'Chatur.exe'
$shell = New-Object -ComObject WScript.Shell
$link = $shell.CreateShortcut($Shortcut)
$link.TargetPath = $exe
$link.WorkingDirectory = $InstallDir
$link.IconLocation = "$exe,0"
$link.Description = 'Chatur'
$link.Save()

# 6. The folder the owner's test works in: this repository's own bin folder (owner, 23 Nov 2026: nothing that belongs
#    to testing Chatur is made beside the owner's projects or on the root of a drive). Three small files keep Chatur's
#    own build settings and package feed away from the test projects made under it, and a .git file that leads nowhere
#    makes those projects read as "not a repository", so nothing done in them can reach Chatur's own repository.
$TestRoot = Join-Path $Repo 'bin'
New-Item -ItemType Directory -Force -Path $TestRoot | Out-Null
Set-Content -LiteralPath (Join-Path $TestRoot 'Directory.Build.props') -Value '<Project />' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $TestRoot 'Directory.Build.targets') -Value '<Project />' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $TestRoot 'nuget.config') -Encoding UTF8 -Value @'
<?xml version="1.0" encoding="utf-8"?>
<configuration><packageSources><clear /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>
'@
Set-Content -LiteralPath (Join-Path $TestRoot '.git') -Value 'gitdir: ./no-repository-here' -Encoding ASCII

$version = (Get-Item -LiteralPath $exe).VersionInfo.ProductVersion
Write-Host "Installed Chatur $version to $InstallDir"
Write-Host "Start menu entry: $Shortcut"
Write-Host "Folder for the owner's test: $TestRoot"
