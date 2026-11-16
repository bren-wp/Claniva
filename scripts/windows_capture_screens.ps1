# Capture all existing WinUI 3 modules using accessibility SelectionItemPattern.
# We keep a failed/missing entry rather than manufacturing a baseline screenshot.
# Requires functions Save-WindowCapture and Assert-RenderedCapture from
# scripts/windows_launch_smoke.ps1 in the same PowerShell scope.
function Capture-WindowsModuleScreens {
    param(
        [Parameter(Mandatory=$true)][IntPtr]$WindowHandle,
        [Parameter(Mandatory=$true)][string]$ArtifactName,
        [Parameter(Mandatory=$true)][string]$OutputFolder,
        [switch]$Strict
    )
    $pages = @(
        @{ Id='dashboard'; Name='Nadzorna ploča' },
        @{ Id='members'; Name='Članovi' },
        @{ Id='dues'; Name='Članarine' },
        @{ Id='payments'; Name='Uplate' },
        @{ Id='debts'; Name='Dugovanja' },
        @{ Id='slips'; Name='Uplatnice' },
        @{ Id='bank'; Name='Bankovni izvodi' },
        @{ Id='reports'; Name='Izvještaji' },
        @{ Id='license'; Name='Licenca' },
        @{ Id='settings'; Name='Postavke' }
    )
    $results = @()
    $root = $null
    try {
        Add-Type -AssemblyName UIAutomationClient
        Add-Type -AssemblyName UIAutomationTypes
        $root = [System.Windows.Automation.AutomationElement]::FromHandle($WindowHandle)
    }
    catch {
        $problem = "UIAutomation unavailable: $($_.Exception.Message)"
        Add-Content -Path $report -Value "MODULE CAPTURE ERROR: $problem"
        foreach ($page in $pages) {
            $results += @{ module=$page.Id; name=$page.Name; status='unverified'; error=$problem }
        }
    }

    if ($null -ne $root) {
        $scope = [System.Windows.Automation.TreeScope]::Descendants
        $nameProperty = [System.Windows.Automation.AutomationElement]::NameProperty
        $selectionPattern = [System.Windows.Automation.SelectionItemPattern]::Pattern
        foreach ($page in $pages) {
            try {
                $condition = [System.Windows.Automation.PropertyCondition]::new(
                    $nameProperty, $page.Name)
                $matches = $root.FindAll($scope, $condition)
                $pattern = $null
                foreach ($match in $matches) {
                    try {
                        $pattern = $match.GetCurrentPattern($selectionPattern)
                        if ($null -ne $pattern) { break }
                    }
                    catch { continue }
                }
                if ($null -eq $pattern) {
                    throw "NavigationViewItem not accessible by SelectionItemPattern: $($page.Name)"
                }
                $pattern.Select()
                Start-Sleep -Milliseconds 900
                $label = "$ArtifactName-module-$($page.Id)"
                # The screenshot must be real, nonempty and newly rendered.
                Wait-RenderedCapture -WindowHandle $WindowHandle -Label $label
                $results += @{ module=$page.Id; name=$page.Name; status='captured-unverified';
                               image="$($label -replace '[^A-Za-z0-9-]', '_').png" }
                Add-Content -Path $report -Value "MODULE CAPTURED: $($page.Id)"
            }
            catch {
                $problem = $_.Exception.Message
                $results += @{ module=$page.Id; name=$page.Name; status='unverified'; error=$problem }
                Add-Content -Path $report -Value "MODULE UNVERIFIED: $($page.Id) | $problem"
            }
        }
        # Return to the same dashboard before measuring responsive breakpoints.
        try {
            $condition = [System.Windows.Automation.PropertyCondition]::new(
                $nameProperty, 'Nadzorna ploča')
            foreach ($match in $root.FindAll($scope, $condition)) {
                try {
                    $pattern = $match.GetCurrentPattern($selectionPattern)
                    if ($null -ne $pattern) { $pattern.Select(); break }
                } catch { continue }
            }
            Start-Sleep -Milliseconds 400
        }
        catch { Add-Content -Path $report -Value "MODULE RESET UNAVAILABLE: $($_.Exception.Message)" }
    }

    $unverified = @($results | Where-Object { $_.status -ne 'captured-unverified' })
    $metadata = @{
        artifact=$ArtifactName
        platform='windows'
        os=[Environment]::OSVersion.Version.ToString()
        culture=[System.Globalization.CultureInfo]::CurrentCulture.Name
        approvedPixelBaseline=$false
        expected=$pages.Count
        captured=$pages.Count - $unverified.Count
        pages=$results
    }
    $fileName = ($ArtifactName -replace '[^A-Za-z0-9-]', '_') + '-modules.json'
    $metadata | ConvertTo-Json -Depth 5 |
        Set-Content -Path (Join-Path $OutputFolder $fileName) -Encoding utf8
    Add-Content -Path $report -Value "MODULE COVERAGE: $($metadata.captured)/$($pages.Count), baselines unverified"
    if ($Strict -and $unverified.Count -gt 0) {
        throw "$($unverified.Count) WinUI modules were not captured; see $fileName"
    }
}
