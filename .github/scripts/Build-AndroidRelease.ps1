param(
    [Parameter(Mandatory)][ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+$')][string]$Version,
    [Parameter(Mandatory)][string]$ExpectedCertificateSha256
)
$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot)
foreach($name in @('QD_ANDROID_KEYSTORE_BASE64','QD_ANDROID_STORE_PASSWORD','QD_ANDROID_KEY_PASSWORD','QD_ANDROID_KEY_ALIAS')){
    if([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))){throw "Missing GitHub Actions secret for $name. Keep the existing signing key; do not generate a replacement."}
}
$expected=($ExpectedCertificateSha256 -replace '[:\s]','').ToLowerInvariant()
if($expected -notmatch '^[a-f0-9]{64}$'){throw 'Expected Android signing certificate SHA-256 must contain 64 hex digits.'}
$keystore=Join-Path $env:RUNNER_TEMP 'quizdesk-release.keystore'
try {
    [IO.File]::WriteAllBytes($keystore,[Convert]::FromBase64String($env:QD_ANDROID_KEYSTORE_BASE64))
    & (Join-Path $root 'build-android.ps1') -KeyStore $keystore -RequireExistingKey -StorePassword 'env:QD_ANDROID_STORE_PASSWORD' -KeyPassword 'env:QD_ANDROID_KEY_PASSWORD' -KeyAlias $env:QD_ANDROID_KEY_ALIAS
    $apks=@(Get-ChildItem -LiteralPath (Join-Path $root 'dist/android') -Filter '*-Signed.apk' -Recurse -File)
    if($apks.Count -ne 1){throw "Expected one signed universal APK; found $($apks.Count). Refusing to publish an ambiguous APK."}
    $tools=Join-Path $env:ANDROID_HOME 'build-tools/36.0.0'
    $signer=Join-Path $tools 'apksigner.bat';$aapt=Join-Path $tools 'aapt.exe'
    $certificate=(& $signer verify --verbose --print-certs $apks[0].FullName | Out-String)
    if($LASTEXITCODE -ne 0){throw 'APK signature verification failed.'}
    if($certificate -notmatch 'Signer #1 certificate SHA-256 digest:\s*([a-fA-F0-9]+)'){throw 'Cannot read APK signing certificate.'}
    if($Matches[1].ToLowerInvariant() -cne $expected){throw 'Signing certificate does not match the expected QuizDesk key. APK will not be released.'}
    $metadata=(& $aapt dump badging $apks[0].FullName | Out-String)
    if($LASTEXITCODE -ne 0){throw 'Cannot read APK metadata.'}
    if($metadata -notmatch "package: name='com.quizdesk.mobile' versionCode='([0-9]+)' versionName='([^']+)'"){throw 'APK package identity is incorrect.'}
    if($Matches[2] -cne $Version){throw 'APK display version does not match the release tag.'}
    $output=Join-Path $root 'dist/release-android';New-Item -ItemType Directory -Path $output -Force | Out-Null
    $file=Join-Path $output "QuizDesk-$Version-Android.apk";Copy-Item -LiteralPath $apks[0].FullName -Destination $file
    $hash=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $([IO.Path]::GetFileName($file))" | Set-Content -LiteralPath (Join-Path $output "QuizDesk-$Version-Android-SHA256.txt") -Encoding ascii
    Write-Output 'Signed APK verified against the expected QuizDesk certificate.'
} finally {
    if(Test-Path -LiteralPath $keystore){Remove-Item -LiteralPath $keystore -Force}
}
