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
function Save-WindowCapture {
  param([IntPtr]$WindowHandle, [string]$Label)
  # Visual evidence only. Do not turn a headless screenshot limitation into a
  # false application crash, and do not claim pixels match an unapproved reference.
  $bitmap = $null
  $graphics = $null
  try {
    $rect = New-Object ClanarinaWindowProbe+Rect
    if (-not [ClanarinaWindowProbe]::GetWindowRect($WindowHandle, [ref]$rect)) {
      throw "GetWindowRect failed"
    }
    $w = $rect.Right - $rect.Left
    $h = $rect.Bottom - $rect.Top
    if ($w -le 0 -or $h -le 0 -or $w -gt 4000 -or $h -gt 3000) {
      throw "Capture bounds are invalid"
    }
    Add-Type -AssemblyName System.Drawing
    $bitmap = [System.Drawing.Bitmap]::new($w, $h)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
    $destination = Join-Path $out (($Label -replace '[^A-Za-z0-9-]', '_') + '.png')
    $bitmap.Save($destination, [System.Drawing.Imaging.ImageFormat]::Png)
    Add-Content $report "CAPTURE: $destination ($w x $h)"
  }
  catch {
    Add-Content $report "CAPTURE_UNAVAILABLE: $($_.Exception.GetType().Name)"
  }
  finally {
    if ($null -ne $graphics) { $graphics.Dispose() }
    if ($null -ne $bitmap) { $bitmap.Dispose() }
  }
}


# A native HWND can be visible before WinUI has painted the client area.
# Reject blank captures rather than treating them as successful UI evidence.
function Assert-RenderedCapture {
  param([string]$Label)
  $path = Join-Path $out (($Label -replace '[^A-Za-z0-9-]', '_') + '.png')
  if (!(Test-Path $path)) { throw "$Label screenshot was not saved" }
  Add-Type -AssemblyName System.Drawing
  $bmp = [System.Drawing.Bitmap]::FromFile($path)
  try {
    $painted = 0
    $stepX = [Math]::Max(16, [int]($bmp.Width / 16))
    $stepY = [Math]::Max(16, [int]($bmp.Height / 14))
    for ($y = 48; $y -lt ($bmp.Height - 16); $y += $stepY) {
      for ($x = 16; $x -lt ($bmp.Width - 16); $x += $stepX) {
        $pixel = $bmp.GetPixel($x, $y)
        if ($pixel.R -lt 248 -or $pixel.G -lt 248 -or $pixel.B -lt 248) {
          $painted++
        }
      }
    }
    if ($painted -lt 10) {
      throw "$Label has an unpainted/blank client area (nonwhite samples=$painted)"
    }
    Add-Content $report "RENDERED: $Label nonwhite samples=$painted"
  }
  finally { $bmp.Dispose() }
}

function Wait-RenderedCapture {
  param([IntPtr]$WindowHandle, [string]$Label, [string]$Kind = 'none')
  $deadline = (Get-Date).AddSeconds(20)
  $lastFailure = 'No screenshot captured'
  $capturePath = Join-Path $out (($Label -replace '[^A-Za-z0-9-]', '_') + '.png')
  do {
    # Never reuse a successful old image when the current capture fails.
    Remove-Item -Path $capturePath -Force -ErrorAction SilentlyContinue
    Save-WindowCapture -WindowHandle $WindowHandle -Label $Label
    try {
      Assert-RenderedCapture -Label $Label
      if ($Kind -ne 'none') {
        Assert-ResponsiveCapture -Label $Label -Kind $Kind
      }
      Add-Content $report "PAINT PASS: $Label"
      return
    }
    catch {
      $lastFailure = $_.Exception.Message
      Add-Content $report "RENDER PENDING: $lastFailure"
      Start-Sleep -Milliseconds 800
    }
  } while ((Get-Date) -lt $deadline)
  throw "$Label did not render a valid UI within 20 seconds: $lastFailure"
}

function Assert-ResponsiveCapture {
  param([string]$Label, [string]$Kind)
  $name = ($Label -replace '[^A-Za-z0-9-]', '_') + '.png'
  $path = Join-Path $out $name
  if (!(Test-Path $path)) { throw "$Label screenshot was not saved; visual regression cannot be verified" }
  Add-Type -AssemblyName System.Drawing
  $bmp = [System.Drawing.Bitmap]::FromFile($path)
  try {
    if ($Kind -eq 'wide' -and $bmp.Width -ge 900 -and $bmp.Height -ge 440) {
      # A 900+ px window must show its expanded navy navigation at x=210,
      # rather than a 48px strip with clipped white labels.
      $sample = $bmp.GetPixel(210, 400)
      if (($sample.R + $sample.G + $sample.B) -gt 450) {
        throw "$Label pane is not expanded/navy at width $($bmp.Width): RGB=$($sample.R),$($sample.G),$($sample.B)"
      }
      Add-Content $report "VISUAL PASS: expanded pane at $($bmp.Width)px"
    }
    if ($Kind -eq 'minimal' -and $bmp.Width -le 580 -and $bmp.Height -ge 300) {
      # On narrow windows the menu button must not overlap the gold DEMO
      # banner; the content's top gap remains background, not the banner.
      $sample = $bmp.GetPixel(65, 55)
      if ($sample.R -gt 220 -and $sample.G -gt 200 -and $sample.G -lt 240 -and $sample.B -lt 200) {
        throw "$Label DEMO banner overlaps minimal menu region: RGB=$($sample.R),$($sample.G),$($sample.B)"
      }
      Add-Content $report "VISUAL PASS: minimal menu top padding at $($bmp.Width)px"
    }
  }
  finally { $bmp.Dispose() }
}

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
  # Verify the actual WinUI main window, never just an alive process or MessageBox.
  # A stale HWND can legitimately be observed momentarily during WinUI activation.
  $interop = @'
using System;
using System.Runtime.InteropServices;
using System.Text;
public static class ClanarinaWindowProbe {
  [StructLayout(LayoutKind.Sequential)]
  public struct Rect { public int Left, Top, Right, Bottom; }
  [DllImport("user32.dll", SetLastError=true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  public static extern bool IsWindow(IntPtr hwnd);
  [DllImport("user32.dll", SetLastError=true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  public static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);
  [DllImport("user32.dll", SetLastError=true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  public static extern bool MoveWindow(IntPtr hwnd, int x, int y, int w, int h, bool repaint);
  [DllImport("user32.dll", EntryPoint="GetWindowTextW", CharSet=CharSet.Unicode, SetLastError=true)]
  public static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int capacity);
  [DllImport("user32.dll", SetLastError=true)]
  public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
  [DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr hwnd);
  [DllImport("user32.dll")] public static extern int GetSystemMetrics(int index);
}
'@
  if (-not ('ClanarinaWindowProbe' -as [type])) { Add-Type -TypeDefinition $interop }
  $hwnd = [IntPtr]::Zero
  $bounds = New-Object ClanarinaWindowProbe+Rect
  $lastProblem = "No readable WinUI window yet"
  $ready = $false
  $probeDeadline = (Get-Date).AddSeconds(15)
  while ((Get-Date) -lt $probeDeadline) {
    $process.Refresh()
    if ($process.HasExited) { throw "$ArtifactName stopped during WinUI window inspection; exit=$($process.ExitCode)" }
    $candidate = $process.MainWindowHandle
    if ($candidate -eq [IntPtr]::Zero -or -not [ClanarinaWindowProbe]::IsWindow($candidate)) {
      $lastProblem = "Window not yet valid: HWND=$candidate"
      Start-Sleep -Milliseconds 500
      continue
    }
    [uint32]$windowPid = 0
    [void][ClanarinaWindowProbe]::GetWindowThreadProcessId($candidate, [ref]$windowPid)
    if ($windowPid -ne $process.Id) {
      throw "$ArtifactName HWND belongs to PID=$windowPid, expected PID=$($process.Id)"
    }
    $caption = New-Object System.Text.StringBuilder 512
    [void][ClanarinaWindowProbe]::GetWindowText($candidate, $caption, $caption.Capacity)
    $title = $caption.ToString()
    if ($title -match 'pogreška pri pokretanju|startup error') {
      throw "$ArtifactName displayed an error dialog, not the application: $title"
    }
    if ($title -notmatch '^Članarina( DEMO)? — by BRENDIGO$') {
      $lastProblem = "Unexpected title: '$title' (HWND=$candidate)"
      Start-Sleep -Milliseconds 500
      continue
    }
    if (-not [ClanarinaWindowProbe]::GetWindowRect($candidate, [ref]$bounds)) {
      $win32 = [System.Runtime.InteropServices.Marshal]::GetLastWin32Error()
      $lastProblem = "GetWindowRect failed: win32=$win32 hwnd=$candidate title='$title'"
      Start-Sleep -Milliseconds 500
      continue
    }
    $hwnd = $candidate
    $ready = $true
    break
  }
  if (-not $ready) { throw "$ArtifactName did not expose its native main window: $lastProblem" }
  $width = $bounds.Right - $bounds.Left
  $height = $bounds.Bottom - $bounds.Top
  $screenW = [ClanarinaWindowProbe]::GetSystemMetrics(0)
  $screenH = [ClanarinaWindowProbe]::GetSystemMetrics(1)
  if ($width -lt 320 -or $height -lt 280 -or $screenW -le 0 -or $screenH -le 0) {
    throw "$ArtifactName invalid window/desktop bounds ($($width)x$($height), desktop $($screenW)x$($screenH))"
  }
  if ([ClanarinaWindowProbe]::IsZoomed($hwnd) -or ($width -ge $screenW -and $height -ge $screenH)) {
    throw "$ArtifactName started maximized or fullscreen ($($width)x$($height))"
  }
  Add-Content $report "Window title=$title hwnd=$hwnd pid=$windowPid size=$($width)x$($height)"
  Wait-RenderedCapture -WindowHandle $hwnd -Label "$ArtifactName-default" -Kind "wide"
  # Include a width between the 740px compact and 660px minimal transitions.
  foreach ($size in @(@(840, 620), @(700, 540), @(560, 440))) {
    $w = [Math]::Min($size[0], [Math]::Max(350, $screenW - 100))
    $h = [Math]::Min($size[1], [Math]::Max(300, $screenH - 100))
    if (-not [ClanarinaWindowProbe]::MoveWindow($hwnd, 40, 40, $w, $h, $true)) {
      throw "$ArtifactName could not resize to $($w)x$($h), Win32=$([System.Runtime.InteropServices.Marshal]::GetLastWin32Error())"
    }
    Start-Sleep -Seconds 2
    $process.Refresh()
    if ($process.HasExited -or -not [ClanarinaWindowProbe]::IsWindow($hwnd)) {
      throw "$ArtifactName crashed or lost its main window after resize to $($w)x$($h)"
    }
    Add-Content $report "PASS resized to $($w)x$($h)"
    $kind = if ($w -le 580) { 'minimal' } else { 'none' }
    Wait-RenderedCapture -WindowHandle $hwnd -Label "$ArtifactName-$($w)x$($h)" -Kind $kind
  }
  $product = if ($ArtifactName -match 'Demo') { 'ClanarinaDemo' } else { 'Clanarina' }
  $startupLog = Join-Path $env:LOCALAPPDATA "$product\logs\startup.log"
  if (-not (Test-Path $startupLog)) { throw "$ArtifactName did not record application activation" }
  $tail = Get-Content $startupLog -Tail 40
  if (-not ($tail | Where-Object { $_ -match "stage=main-window-activated pid=$($process.Id)(\s|$)" })) {
    throw "$ArtifactName did not record its activation in $startupLog"
  }
  Start-Sleep -Seconds 2
  $process.Refresh()
  if ($process.HasExited) { throw "$ArtifactName stopped immediately after startup and resize" }
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
