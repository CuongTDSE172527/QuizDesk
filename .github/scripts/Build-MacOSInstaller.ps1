param(
    [Parameter(Mandatory)][ValidateSet('arm64','x64')][string]$Architecture,
    [Parameter(Mandatory)][ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+$')][string]$Version
)
$ErrorActionPreference='Stop'
if(-not $IsMacOS){throw 'macOS installer build requires a macOS runner.'}
$root=Split-Path (Split-Path $PSScriptRoot)
& (Join-Path $root 'build-desktop.ps1') -Runtime "osx-$Architecture"
$bundle=Join-Path $root "dist/macos-$Architecture"
& python3 (Join-Path $root 'package-macos.py') (Join-Path $root "dist/osx-$Architecture") $bundle --arch $Architecture
if($LASTEXITCODE -ne 0){throw 'macOS app bundle packaging failed.'}
$destination=Join-Path $root "dist/release-macos-$Architecture/QuizDesk-Setup-$Version-macos-$Architecture.pkg"
& python3 (Join-Path $root 'Installer/macOS/build-native-installer.py') --app (Join-Path $bundle 'QuizDesk.app') --output $destination --arch $Architecture --version $Version
if($LASTEXITCODE -ne 0){throw 'Native macOS installer packaging failed.'}

$dmg=Join-Path $root "dist/release-macos-$Architecture/QuizDesk-$Version-macos-$Architecture.dmg"
& python3 (Join-Path $root 'Installer/macOS/build-dmg.py') --app (Join-Path $bundle 'QuizDesk.app') --output $dmg --arch $Architecture --version $Version
if($LASTEXITCODE -ne 0){throw 'macOS disk image packaging/verification failed.'}
