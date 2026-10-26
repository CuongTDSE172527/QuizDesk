param(
    [string]$DotNet='dotnet',
    [string]$AndroidSdkDirectory=$env:ANDROID_HOME,
    [string]$JavaSdkDirectory=$env:JAVA_HOME,
    [string]$KeyStore=(Join-Path $PSScriptRoot '.tooling/quizdesk-dev.keystore'),
    [string]$StorePassword='android',
    [string]$KeyPassword='android',
    [string]$KeyAlias='quizdesk-dev',
    [switch]$RequireExistingKey
)
$ErrorActionPreference='Stop'
if(-not $AndroidSdkDirectory -or -not $JavaSdkDirectory){throw 'Set ANDROID_HOME and JAVA_HOME, or pass AndroidSdkDirectory and JavaSdkDirectory.'}
$env:AVALONIA_TELEMETRY_OPTOUT='1'
if(-not (Test-Path -LiteralPath $KeyStore)) {
    if($RequireExistingKey){throw 'Release signing key missing. No replacement key will be generated.'}
    New-Item -ItemType Directory -Force (Split-Path $KeyStore) | Out-Null
    $keytool=Join-Path $JavaSdkDirectory $(if($IsWindows -or $env:OS -eq 'Windows_NT'){'bin/keytool.exe'}else{'bin/keytool'})
    & $keytool -genkeypair -keystore $KeyStore -alias $KeyAlias -storepass $StorePassword -keypass $KeyPassword -keyalg RSA -keysize 2048 -validity 10000 -dname 'CN=QuizDesk Development, O=QuizDesk, C=VN' -noprompt
    if($LASTEXITCODE -ne 0){throw 'Development signing key generation failed'}
}
& $DotNet publish (Join-Path $PSScriptRoot 'QuizDesk.Android/QuizDesk.Android.csproj') -c Release -m:1 "-p:AndroidSdkDirectory=$AndroidSdkDirectory" "-p:JavaSdkDirectory=$JavaSdkDirectory" -p:AndroidKeyStore=true "-p:AndroidSigningKeyStore=$KeyStore" "-p:AndroidSigningStorePass=$StorePassword" "-p:AndroidSigningKeyPass=$KeyPassword" "-p:AndroidSigningKeyAlias=$KeyAlias" -o (Join-Path $PSScriptRoot 'dist/android')
if($LASTEXITCODE -ne 0){throw 'Android publish failed'}
Write-Output 'Development/test APK built. Keep the signing key private and retain it for future updates. Choose a production key before publishing to a store.'
