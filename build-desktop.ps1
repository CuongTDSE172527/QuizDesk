param([ValidateSet('win-x64','osx-arm64','osx-x64')][string]$Runtime='win-x64',[string]$DotNet='dotnet')
$ErrorActionPreference='Stop'
$env:AVALONIA_TELEMETRY_OPTOUT='1'
$output=Join-Path $PSScriptRoot "dist/$Runtime"
& $DotNet publish (Join-Path $PSScriptRoot 'QuizDesk.Desktop/QuizDesk.Desktop.csproj') -c Release -r $Runtime --self-contained true -m:1 -p:UseSharedCompilation=false -o $output
if($LASTEXITCODE -ne 0){throw 'Desktop publish failed'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'TestAssets/Samples') -Destination $output -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'QuizDesk.Mobile/Assets/Licenses') -Destination $output -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination $output
Write-Output "Published $Runtime to $output"
