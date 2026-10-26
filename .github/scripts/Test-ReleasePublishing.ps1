param([string]$Repository=(Split-Path (Split-Path $PSScriptRoot)))
$ErrorActionPreference='Stop'
$testTemp=if($env:RUNNER_TEMP){$env:RUNNER_TEMP}else{[IO.Path]::GetTempPath()}
$folder=Join-Path $testTemp ('release-tests-'+[Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $folder | Out-Null
$publish=Join-Path $Repository '.github/scripts/Publish-Release.ps1'
$global:QDReleaseTestCalls=[Collections.Generic.List[string]]::new();$global:QDReleaseTestExists=$false;$global:QDReleaseTestAssets=@();$global:QDReleaseTestDraft=$false
function gh {
    $a=@($args);$global:QDReleaseTestCalls.Add(($a -join ' '));$global:LASTEXITCODE=0
    if($a[0] -eq 'api'){
        if(-not $global:QDReleaseTestExists){$global:LASTEXITCODE=1;return}
        @{tag_name='v5.11.0';html_url='https://example.invalid/release';assets=@($global:QDReleaseTestAssets);draft=$global:QDReleaseTestDraft} | ConvertTo-Json -Depth 5 -Compress;return
    }
    if($a[0] -eq 'release' -and $a[1] -eq 'create'){$global:QDReleaseTestExists=$true;return}
    if($a[0] -eq 'release' -and $a[1] -in @('upload','edit')){return}
    throw 'Unexpected gh command'
}
function Check($condition,$message){if(-not $condition){throw $message};Write-Output "PASS $message"}
function Reject([scriptblock]$action,$message){$rejected=$false;try{& $action}catch{$rejected=$true};Check $rejected $message}
$originalEnvironment=@{}
foreach($key in @('GH_TOKEN','GITHUB_REPOSITORY','GITHUB_STEP_SUMMARY','GITHUB_OUTPUT')){$originalEnvironment[$key]=[Environment]::GetEnvironmentVariable($key)}
try {
$env:GH_TOKEN='test-placeholder';$env:GITHUB_REPOSITORY='fixture/quizdesk';$env:GITHUB_STEP_SUMMARY=''
$file=Join-Path $folder 'QuizDesk-Setup-5.11.0-x64.exe';$manifest=Join-Path $folder 'QuizDesk-5.11.0-Windows-SHA256.txt'
[IO.File]::WriteAllText($file,'Fixture only; never execute')
$hash=(Get-FileHash $file).Hash.ToLowerInvariant();"$hash  $([IO.Path]::GetFileName($file))" | Set-Content $manifest -Encoding ascii
& $publish -Tag v5.11.0 -Platform windows -Directory $folder
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'release create*'}).Count -eq 1 -and @($global:QDReleaseTestCalls | Where-Object {$_ -like 'release upload*'}).Count -eq 2) 'Creates a release and uploads exactly installer plus checksum'
$global:QDReleaseTestCalls.Clear();$global:QDReleaseTestAssets=@($file,$manifest) | ForEach-Object { @{name=[IO.Path]::GetFileName($_);digest='sha256:'+(Get-FileHash $_).Hash.ToLowerInvariant()} }
& $publish -Tag v5.11.0 -Platform windows -Directory $folder
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'release upload*'}).Count -eq 0) 'Rerun skips identical published assets'
$global:QDReleaseTestCalls.Clear();$global:QDReleaseTestAssets[0].digest='sha256:different'
Reject {& $publish -Tag v5.11.0 -Platform windows -Directory $folder} 'Refuses to overwrite published bytes'
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'release upload*'}).Count -eq 0) 'Conflict detected before upload'
$global:QDReleaseTestCalls.Clear();[IO.File]::WriteAllText($manifest,'tampered')
Reject {& $publish -Tag v5.11.0 -Platform windows -Directory $folder} 'Refuses an artifact whose checksum was tampered'
Check ($global:QDReleaseTestCalls.Count -eq 0) 'Checksum failure causes no GitHub operation'
"$hash  $([IO.Path]::GetFileName($file))" | Set-Content $manifest -Encoding ascii
$global:QDReleaseTestAssets=@();$global:QDReleaseTestDraft=$true
& $publish -Tag v5.11.0 -Platform windows -Directory $folder
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'release edit*--draft=false'}).Count -eq 1) 'Publishes an existing draft after successful upload'
Reject {& $publish -Tag '../unsafe' -Platform windows -Directory $folder} 'Rejects invalid release tags'
[xml]$android=Get-Content -LiteralPath (Join-Path $Repository 'QuizDesk.Android/QuizDesk.Android.csproj') -Raw
$sourceVersion=$android.Project.PropertyGroup.ApplicationDisplayVersion
$env:GITHUB_OUTPUT=Join-Path $folder 'outputs.txt'
& (Join-Path $Repository '.github/scripts/Get-ReleaseVersion.ps1') -Tag ('v'+$sourceVersion)
Check ([IO.File]::ReadAllText($env:GITHUB_OUTPUT).Contains('version='+$sourceVersion)) 'Validates source versions and exports release metadata'
Reject {& (Join-Path $Repository '.github/scripts/Get-ReleaseVersion.ps1') -Tag v999.0.0} 'Rejects tag/source version mismatch'
Reject {& (Join-Path $Repository 'build-android.ps1') -RequireExistingKey -KeyStore (Join-Path $folder 'missing.keystore') -AndroidSdkDirectory $folder -JavaSdkDirectory $folder} 'Release mode refuses to create a replacement Android key'
$global:QDReleaseTestCalls.Clear();$global:QDReleaseTestAssets=@();$global:QDReleaseTestDraft=$false
$apk=Join-Path $folder 'QuizDesk-5.11.0-Android.apk';$apkManifest=Join-Path $folder 'QuizDesk-5.11.0-Android-SHA256.txt'
[IO.File]::WriteAllText($apk,'APK fixture only')
$apkHash=(Get-FileHash $apk).Hash.ToLowerInvariant();"$apkHash  $([IO.Path]::GetFileName($apk))" | Set-Content $apkManifest -Encoding ascii
& $publish -Tag v5.11.0 -Platform android -Directory $folder
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'release create*'}).Count -eq 0 -and @($global:QDReleaseTestCalls | Where-Object {$_ -like 'release upload*Android*'}).Count -eq 2) 'Android attaches APK and checksum to the same existing release'
Write-Output 'All 12 release automation checks passed; gh was mocked (no network writes).'
} finally {
    foreach($key in $originalEnvironment.Keys){[Environment]::SetEnvironmentVariable($key,$originalEnvironment[$key])}
    Remove-Variable -Name QDReleaseTestCalls,QDReleaseTestExists,QDReleaseTestAssets,QDReleaseTestDraft -Scope Global -ErrorAction SilentlyContinue
}
