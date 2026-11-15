param(
  [string]$Distribution = "dist"
)
$ErrorActionPreference = 'Stop'
$folder = (Resolve-Path $Distribution).Path
$logdir = Join-Path $env:GITHUB_WORKSPACE 'qa\runtime\windows-package'
New-Item -ItemType Directory -Path $logdir -Force | Out-Null

function Wait-ApplicationWindow {
  param([string]$ExpectedProcess, [System.Diagnostics.Process]$Launcher, [string]$Label)
  $deadline = (Get-Date).AddSeconds(50)
  $running = $null
  while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 1
    foreach ($p in @(Get-Process -Name $ExpectedProcess -ErrorAction SilentlyContinue)) {
      $p.Refresh()
      if (!$p.HasExited -and $p.MainWindowHandle -ne [IntPtr]::Zero) {
        $caption = $p.MainWindowTitle
        if ($caption -match 'pogreška pri pokretanju|startup error') {
          throw "$Label displayed a startup error dialog instead of the main window"
        }
        if ($caption -match '^Članarina( DEMO)? — by BRENDIGO
    }
    if ($null -ne $running) { break }
    $Launcher.Refresh()
    if ($Launcher.HasExited) { break }
  }
  if ($null -eq $running) {
    $Launcher.Refresh()
    $code = if ($Launcher.HasExited) { $Launcher.ExitCode } else { "running" }
    throw "$Label did not open a visible $ExpectedProcess window; launcher status=$code"
  }
  try {
    Write-Host "PASS: $Label opened $($running.ProcessName) PID=$($running.Id)"
    Add-Content (Join-Path $logdir "$Label.txt") "PASS: process=$($running.ProcessName) window=$($running.MainWindowHandle)"
  }
  finally {
    Stop-Process -Id $running.Id -Force -ErrorAction SilentlyContinue
  }
}

foreach ($edition in @(
  @{Name='Full'; Prefix='Clanarina'; App='Clanarina'; Data='Clanarina'},
  @{Name='Demo'; Prefix='Clanarina-Demo'; App='ClanarinaDemo'; Data='ClanarinaDemo'}
)) {
  $prefix = $edition.Prefix
  $app = $edition.App
  $setup = Join-Path $folder "$prefix-Setup.exe"
  $portable = Join-Path $folder "$prefix-Portable.exe"
  if (!(Test-Path $setup) -or !(Test-Path $portable)) {
    throw "Missing Setup or Portable executable for $prefix"
  }
  Write-Host "Running $prefix Portable (no installation)"
  $launcher = Start-Process -FilePath $portable -PassThru
  try {
    Wait-ApplicationWindow -ExpectedProcess $app -Launcher $launcher -Label "$prefix-Portable"
  }
  finally {
    $launcher.Refresh()
    if (!$launcher.HasExited) { Stop-Process -Id $launcher.Id -Force -ErrorAction SilentlyContinue }
  }

  Write-Host "Installing $prefix Setup in silent test mode"
  $installer = Start-Process -FilePath $setup -ArgumentList '/S' -PassThru
  if (!$installer.WaitForExit(120000)) {
    Stop-Process -Id $installer.Id -Force -ErrorAction SilentlyContinue
    throw "$prefix installer timed out"
  }
  if ($installer.ExitCode -ne 0) { throw "$prefix installer failed with code $($installer.ExitCode)" }
  $installed = Join-Path $env:LOCALAPPDATA "Programs\$($edition.Data)\$app.exe"
  if (!(Test-Path $installed)) { throw "Installed executable is missing: $installed" }
  & "$PSScriptRoot/windows_launch_smoke.ps1" -ApplicationPath $installed -ArtifactName "$prefix-Setup"
}
Write-Host "PASS: Full and Demo Setup and Portable all produced real windows"
) {
          $running = $p
          break
        }
      }
    }
    if ($null -ne $running) { break }
    $Launcher.Refresh()
    if ($Launcher.HasExited) { break }
  }
  if ($null -eq $running) {
    $Launcher.Refresh()
    $code = if ($Launcher.HasExited) { $Launcher.ExitCode } else { "running" }
    throw "$Label did not open a visible $ExpectedProcess window; launcher status=$code"
  }
  try {
    Write-Host "PASS: $Label opened $($running.ProcessName) PID=$($running.Id)"
    Add-Content (Join-Path $logdir "$Label.txt") "PASS: process=$($running.ProcessName) window=$($running.MainWindowHandle)"
  }
  finally {
    Stop-Process -Id $running.Id -Force -ErrorAction SilentlyContinue
  }
}

foreach ($edition in @(
  @{Name='Full'; Prefix='Clanarina'; App='Clanarina'; Data='Clanarina'},
  @{Name='Demo'; Prefix='Clanarina-Demo'; App='ClanarinaDemo'; Data='ClanarinaDemo'}
)) {
  $prefix = $edition.Prefix
  $app = $edition.App
  $setup = Join-Path $folder "$prefix-Setup.exe"
  $portable = Join-Path $folder "$prefix-Portable.exe"
  if (!(Test-Path $setup) -or !(Test-Path $portable)) {
    throw "Missing Setup or Portable executable for $prefix"
  }
  Write-Host "Running $prefix Portable (no installation)"
  $launcher = Start-Process -FilePath $portable -PassThru
  try {
    Wait-ApplicationWindow -ExpectedProcess $app -Launcher $launcher -Label "$prefix-Portable"
  }
  finally {
    $launcher.Refresh()
    if (!$launcher.HasExited) { Stop-Process -Id $launcher.Id -Force -ErrorAction SilentlyContinue }
  }

  Write-Host "Installing $prefix Setup in silent test mode"
  $installer = Start-Process -FilePath $setup -ArgumentList '/S' -PassThru
  if (!$installer.WaitForExit(120000)) {
    Stop-Process -Id $installer.Id -Force -ErrorAction SilentlyContinue
    throw "$prefix installer timed out"
  }
  if ($installer.ExitCode -ne 0) { throw "$prefix installer failed with code $($installer.ExitCode)" }
  $installed = Join-Path $env:LOCALAPPDATA "Programs\$($edition.Data)\$app.exe"
  if (!(Test-Path $installed)) { throw "Installed executable is missing: $installed" }
  & "$PSScriptRoot/windows_launch_smoke.ps1" -ApplicationPath $installed -ArtifactName "$prefix-Setup"
}
Write-Host "PASS: Full and Demo Setup and Portable all produced real windows"
