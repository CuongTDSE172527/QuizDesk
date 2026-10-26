param([Parameter(Mandatory)][ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+$')][string]$Version)
$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot)
& (Join-Path $root 'build-desktop.ps1') -Runtime win-x64
$publish=Join-Path $root 'dist/win-x64'
if(-not (Test-Path -LiteralPath (Join-Path $publish 'QuizDesk.exe'))){throw 'Desktop executable missing.'}
$iscc=Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6/ISCC.exe'
if(-not (Test-Path -LiteralPath $iscc)){throw 'Install Inno Setup 6. GitHub windows-2025 runners include it.'}
$output=Join-Path $root 'dist/release-windows';New-Item -ItemType Directory -Path $output -Force | Out-Null
& $iscc "/DAppVersion=$Version" "/DAppSource=$publish" "/DSetupOutput=$output" (Join-Path $root 'Installer/Windows/QuizDesk.iss')
if($LASTEXITCODE -ne 0){throw 'Inno Setup compilation failed.'}
$file=Join-Path $output "QuizDesk-Setup-$Version-x64.exe"
if(-not (Test-Path -LiteralPath $file)){throw 'Installer was not produced.'}
$hash=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($file))" | Set-Content -LiteralPath (Join-Path $output "QuizDesk-$Version-Windows-SHA256.txt") -Encoding ascii
