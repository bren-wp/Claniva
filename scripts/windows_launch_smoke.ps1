param(
  [Parameter(Mandatory=$true)][string]$ApplicationPath,
  [string]$ArtifactName = 'Windows',
  [int]$TimeoutSeconds = 35
)
$ErrorActionPreference = 'Stop'
if (!(Test-Path $ApplicationPath)) { throw "Missing application: $ApplicationPath" }
$exe = (Resolve-Path $ApplicationPath).Path
$workdir = Split-Path -Parent $exe
$out = Join-Path $env:GITHUB_WORKSPACE 'qa\runtime\windows'
New-Item -ItemType Directory -Path $out -Force | Out-Null
$report = Join-Path $out (($ArtifactName -replace '[^A-Za-z0-9-]', '_') + '.txt')
$process = $null
try {
  $process = Start-Process -FilePath $exe -WorkingDirectory $workdir -PassThru
  Add-Content $report "Launch: $ArtifactName | PID=$($process.Id) | Session=$($process.SessionId)"
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  $visible = $false
  while ((Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 900
    $process.Refresh()
    if ($process.HasExited) {
      throw "$ArtifactName exited before it showed a window; code=$($process.ExitCode)"
    }
    if ($process.MainWindowHandle -ne [IntPtr]::Zero) {
      $visible = $true
      break
    }
  }
  if (-not $visible) { throw "$ArtifactName did not expose a visible window within $TimeoutSeconds seconds" }
  Add-Content $report "PASS: window handle=$($process.MainWindowHandle)"
  Write-Host "PASS: $ArtifactName opened a real window"
}
catch {
  Add-Content $report "FAIL: $($_.Exception.GetType().Name): $($_.Exception.Message)"
  foreach ($folder in @('Clanarina','ClanarinaDemo')) {
    $log = Join-Path $env:LOCALAPPDATA "$folder\logs\startup.log"
    if (Test-Path $log) {
      Copy-Item $log -Destination (Join-Path $out "$folder-startup.log") -Force
    }
  }
  throw
}
finally {
  if ($null -ne $process) {
    $process.Refresh()
    if (-not $process.HasExited) {
      Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
  }
}
