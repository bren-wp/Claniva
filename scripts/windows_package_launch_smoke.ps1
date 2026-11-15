param(
  [string]$Distribution = "dist"
)
$ErrorActionPreference = 'Stop'
$folder = (Resolve-Path $Distribution).Path
$logdir = Join-Path $env:GITHUB_WORKSPACE 'qa\runtime\windows-package'
New-Item -ItemType Directory -Path $logdir -Force | Out-Null

function Wait-ApplicationWindow {
  param(
    [Parameter(Mandatory=$true)][string]$ExpectedProcess,
    [Parameter(Mandatory=$true)][System.Diagnostics.Process]$Launcher,
    [Parameter(Mandatory=$true)][string]$Label
  )

  $deadline = (Get-Date).AddSeconds(50)
  $running = $null
  while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 1
    foreach ($p in @(Get-Process -Name $ExpectedProcess -ErrorAction SilentlyContinue)) {
      $p.Refresh()
      if ($p.HasExited -or $p.MainWindowHandle -eq [IntPtr]::Zero) { continue }
      $title = $p.MainWindowTitle
      if ($title -match 'pogreška pri pokretanju|startup error') {
        throw "$Label opened an error dialog instead of the main window"
      }
      if ($title -match '^Članarina( DEMO)? — by BRENDIGO$') {
        $running = $p
        break
      }
    }
    if ($null -ne $running) { break }
    $Launcher.Refresh()
    if ($Launcher.HasExited) { break }
  }

  if ($null -eq $running) {
    $Launcher.Refresh()
    $code = if ($Launcher.HasExited) { $Launcher.ExitCode } else { "running" }
    throw "$Label did not open a valid $ExpectedProcess main window; launcher exit=$code"
  }
  try {
    Start-Sleep -Seconds 2
    $running.Refresh()
    if ($running.HasExited -or $running.MainWindowHandle -eq [IntPtr]::Zero) {
      throw "$Label stopped immediately after it opened"
    }
    $proof = "PASS: $Label process=$($running.ProcessName) PID=$($running.Id) title=$($running.MainWindowTitle)"
    Write-Host $proof
    Add-Content -Path (Join-Path $logdir "$Label.txt") -Value $proof
  }
  finally {
    $running.Refresh()
    if (-not $running.HasExited) {
      Stop-Process -Id $running.Id -Force -ErrorAction SilentlyContinue
    }
  }
}

foreach ($edition in @(
  @{ Prefix='Clanarina'; App='Clanarina'; Data='Clanarina' },
  @{ Prefix='Clanarina-Demo'; App='ClanarinaDemo'; Data='ClanarinaDemo' }
)) {
  $prefix = $edition.Prefix
  $app = $edition.App
  $setup = Join-Path $folder "$prefix-Setup.exe"
  $portable = Join-Path $folder "$prefix-Portable.exe"
  if (!(Test-Path $setup) -or !(Test-Path $portable)) {
    throw "Missing Setup/Portable executable for $prefix"
  }

  Write-Host "Testing $prefix Portable"
  $launcher = Start-Process -FilePath $portable -PassThru
  try {
    Wait-ApplicationWindow -ExpectedProcess $app -Launcher $launcher -Label "$prefix-Portable"
  }
  finally {
    $launcher.Refresh()
    if (-not $launcher.HasExited) {
      Stop-Process -Id $launcher.Id -Force -ErrorAction SilentlyContinue
    }
  }

  Write-Host "Testing $prefix Setup"
  $installer = Start-Process -FilePath $setup -ArgumentList '/S' -PassThru
  if (-not $installer.WaitForExit(120000)) {
    Stop-Process -Id $installer.Id -Force -ErrorAction SilentlyContinue
    throw "$prefix Setup timed out"
  }
  if ($installer.ExitCode -ne 0) { throw "$prefix Setup failed, exit=$($installer.ExitCode)" }
  $installed = Join-Path $env:LOCALAPPDATA "Programs\$($edition.Data)\$app.exe"
  if (!(Test-Path $installed)) { throw "Missing installed executable: $installed" }
  & "$PSScriptRoot/windows_launch_smoke.ps1" -ApplicationPath $installed -ArtifactName "$prefix-Setup"
}
Write-Host "PASS: Full and Demo Setup/Portable opened real application windows"
