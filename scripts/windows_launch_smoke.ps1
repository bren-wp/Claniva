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
  # Exercise resizing at different desktop widths and verify the app remains windowed.
  # Checking only an existing process/window handle previously missed real startup defects.
  $interop = @'
using System;
using System.Runtime.InteropServices;
public static class ClanarinaWindowTest {
  [StructLayout(LayoutKind.Sequential)]
  public struct Rect { public int Left, Top, Right, Bottom; }
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);
  [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hwnd, int x, int y, int w, int h, bool repaint);
  [DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr hwnd);
  [DllImport("user32.dll")] public static extern int GetSystemMetrics(int index);
}
'@
  if (-not ('ClanarinaWindowTest' -as [type])) { Add-Type -TypeDefinition $interop }
  $hwnd = $process.MainWindowHandle
  $bounds = New-Object ClanarinaWindowTest+Rect
  if (-not [ClanarinaWindowTest]::GetWindowRect($hwnd, [ref]$bounds)) {
    throw "$ArtifactName has an inaccessible window rectangle"
  }
  $width = $bounds.Right - $bounds.Left
  $height = $bounds.Bottom - $bounds.Top
  $screenW = [ClanarinaWindowTest]::GetSystemMetrics(0)
  $screenH = [ClanarinaWindowTest]::GetSystemMetrics(1)
  if ([ClanarinaWindowTest]::IsZoomed($hwnd) -or ($width -ge $screenW -and $height -ge $screenH)) {
    throw "$ArtifactName opened maximized/full-screen ($width x $height), not windowed"
  }
  Add-Content $report "Initial window: $width x $height, desktop: $screenW x $screenH"
  foreach ($size in @(@(840, 620), @(560, 440))) {
    $w = [Math]::Min($size[0], [Math]::Max(350, $screenW - 100))
    $h = [Math]::Min($size[1], [Math]::Max(300, $screenH - 100))
    if (-not [ClanarinaWindowTest]::MoveWindow($hwnd, 40, 40, $w, $h, $true)) {
      throw "$ArtifactName could not resize to $w x $h"
    }
    Start-Sleep -Seconds 2
    $process.Refresh()
    if ($process.HasExited -or $process.MainWindowHandle -eq [IntPtr]::Zero) {
      throw "$ArtifactName crashed while resizing to $w x $h"
    }
    Add-Content $report "PASS resized: $w x $h"
  }

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
