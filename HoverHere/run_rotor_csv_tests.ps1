param(
    [string]$UnityExe = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe"
)

$ErrorActionPreference = "Stop"
$projectPath = $PSScriptRoot
$logPath = Join-Path $projectPath "Logs\RotorCsvTestRunner.log"
$tempProjectPath = Join-Path $env:TEMP "HoverHereRotorCsvTest"

if (-not (Test-Path -LiteralPath $UnityExe -PathType Leaf)) {
    throw "Unity Editor executable not found: $UnityExe"
}

New-Item -ItemType Directory -Path $tempProjectPath -Force | Out-Null
if (-not (Test-Path -LiteralPath (Join-Path $tempProjectPath "Assets"))) {
    New-Item -ItemType Junction `
        -Path (Join-Path $tempProjectPath "Assets") `
        -Target (Join-Path $projectPath "Assets") | Out-Null
}
Copy-Item -LiteralPath (Join-Path $projectPath "Packages") -Destination $tempProjectPath -Recurse -Force
Copy-Item -LiteralPath (Join-Path $projectPath "ProjectSettings") -Destination $tempProjectPath -Recurse -Force

$arguments = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath `"$tempProjectPath`"",
    "-executeMethod RotorCsvTestRunner.ExportCsvFiles",
    "-logFile `"$logPath`""
)
$unity = Start-Process -FilePath $UnityExe -ArgumentList $arguments -Wait -PassThru -NoNewWindow
if ($unity.ExitCode -ne 0) {
    throw "Unity batch export failed with exit code $($unity.ExitCode). See $logPath"
}

$expectedFiles = @("bemt_rotor_test.csv", "vrs_plot_data.csv")
foreach ($fileName in $expectedFiles) {
    $tempCsvPath = Join-Path $tempProjectPath $fileName
    if (-not (Test-Path -LiteralPath $tempCsvPath -PathType Leaf)) {
        throw "Expected CSV was not created: $tempCsvPath. See $logPath"
    }

    if ((Get-Item -LiteralPath $tempCsvPath).Length -eq 0) {
        throw "CSV is empty: $tempCsvPath. See $logPath"
    }

    Copy-Item -LiteralPath $tempCsvPath -Destination (Join-Path $projectPath $fileName) -Force
    Write-Output "Verified and updated $(Join-Path $projectPath $fileName)"
}

Write-Output "Rotor CSV test exports completed. Unity log: $logPath"
Write-Output "Reusable isolated Unity project/cache: $tempProjectPath"
