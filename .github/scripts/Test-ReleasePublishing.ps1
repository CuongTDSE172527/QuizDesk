param([string]$Repository=(Split-Path (Split-Path $PSScriptRoot)))
$ErrorActionPreference='Stop'
$testTemp=if($env:RUNNER_TEMP){$env:RUNNER_TEMP}else{[IO.Path]::GetTempPath()}
$folder=Join-Path $testTemp ('release-tests-'+[Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $folder | Out-Null
$publish=Join-Path $Repository '.github/scripts/Publish-Release.ps1'
$global:QDReleaseTestCalls=[Collections.Generic.List[string]]::new();$global:QDReleaseTestExists=$false;$global:QDReleaseTestAssets=@();$global:QDReleaseTestDraft=$false
$global:QDReleaseTestCommit='aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'
$global:QDReleaseTestTagCommit=$global:QDReleaseTestCommit
$global:QDReleaseTestTagExists=$true;$global:QDReleaseTestAnnotated=$false;$global:QDReleaseTestForbidden=$false
$global:QDReleaseTestTagRace=$false;$global:QDReleaseTestReleaseRace=$false
$global:QDReleaseTestCreateResponse=$false;$global:QDReleaseTestDelayedReads=0;$global:QDReleaseTestCreateDenied=$false
function gh {
    $a=@($args);$global:QDReleaseTestCalls.Add(($a -join ' '));$global:LASTEXITCODE=0
    if($a[0] -eq 'api'){
        if($a[1] -eq '--method'){
            if($global:QDReleaseTestCreateDenied){Write-Error 'Creation denied (HTTP 403)' -ErrorAction Continue;$global:LASTEXITCODE=1;return}
            $global:QDReleaseTestTagExists=$true
            $global:QDReleaseTestTagCommit=(@($a | Where-Object {$_ -like 'sha=*'})[0]).Substring(4)
            if($global:QDReleaseTestTagRace){$global:LASTEXITCODE=1}
            if($global:QDReleaseTestCreateResponse){@{ref='refs/tags/v5.11.0';object=@{type='commit';sha=$global:QDReleaseTestTagCommit}} | ConvertTo-Json -Compress}
            return
        }
        if($global:QDReleaseTestForbidden){Write-Error 'Forbidden (HTTP 403)' -ErrorAction Continue;$global:LASTEXITCODE=1;return}
        if($a[1] -like '*/git/ref/tags/*'){
            if($global:QDReleaseTestTagExists -and $global:QDReleaseTestDelayedReads -gt 0){$global:QDReleaseTestDelayedReads--;Write-Error 'Not Found (HTTP 404)' -ErrorAction Continue;$global:LASTEXITCODE=1;return}
            if(-not $global:QDReleaseTestTagExists){Write-Error 'Not Found (HTTP 404)' -ErrorAction Continue;$global:LASTEXITCODE=1;return}
            @{object=@{type=$(if($global:QDReleaseTestAnnotated){'tag'}else{'commit'});sha=$global:QDReleaseTestTagCommit}} | ConvertTo-Json -Compress;return
        }
        if($a[1] -like '*/git/tags/*'){@{object=@{type='commit';sha=$global:QDReleaseTestTagCommit}} | ConvertTo-Json -Compress;return}
        if(-not $global:QDReleaseTestExists){Write-Error 'Not Found (HTTP 404)' -ErrorAction Continue;$global:LASTEXITCODE=1;return}
        @{tag_name='v5.11.0';html_url='https://example.invalid/release';assets=@($global:QDReleaseTestAssets);draft=$global:QDReleaseTestDraft} | ConvertTo-Json -Depth 5 -Compress;return
    }
    if($a[0] -eq 'release' -and $a[1] -eq 'create'){$global:QDReleaseTestExists=$true;if($global:QDReleaseTestReleaseRace){$global:LASTEXITCODE=1};return}
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
& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform windows -Directory $folder
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'release create*'}).Count -eq 1 -and @($global:QDReleaseTestCalls | Where-Object {$_ -like 'release upload*'}).Count -eq 2) 'Creates a release and uploads exactly installer plus checksum'
$global:QDReleaseTestCalls.Clear();$global:QDReleaseTestAssets=@($file,$manifest) | ForEach-Object { @{name=[IO.Path]::GetFileName($_);digest='sha256:'+(Get-FileHash $_).Hash.ToLowerInvariant()} }
& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform windows -Directory $folder
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'release upload*'}).Count -eq 0) 'Rerun skips identical published assets'
$global:QDReleaseTestCalls.Clear();$global:QDReleaseTestAssets[0].digest='sha256:different'
Reject {& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform windows -Directory $folder} 'Refuses to overwrite published bytes'
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'release upload*'}).Count -eq 0) 'Conflict detected before upload'
$global:QDReleaseTestCalls.Clear();[IO.File]::WriteAllText($manifest,'tampered')
Reject {& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform windows -Directory $folder} 'Refuses an artifact whose checksum was tampered'
Check ($global:QDReleaseTestCalls.Count -eq 0) 'Checksum failure causes no GitHub operation'
"$hash  $([IO.Path]::GetFileName($file))" | Set-Content $manifest -Encoding ascii
$global:QDReleaseTestAssets=@();$global:QDReleaseTestDraft=$true
& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform windows -Directory $folder
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'release edit*--draft=false'}).Count -eq 1) 'Publishes an existing draft after successful upload'
Reject {& $publish -Tag '../unsafe' -Commit $global:QDReleaseTestCommit -Platform windows -Directory $folder} 'Rejects invalid release tags'
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
& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform android -Directory $folder
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'release create*'}).Count -eq 0 -and @($global:QDReleaseTestCalls | Where-Object {$_ -like 'release upload*Android*'}).Count -eq 2) 'Android attaches APK and checksum to the same existing release'
& (Join-Path $Repository '.github/scripts/Get-ReleaseVersion.ps1')
Check ([IO.File]::ReadAllText($env:GITHUB_OUTPUT).Contains('tag=v'+$sourceVersion)) 'Blank manual tag derives the version tag from source'
$global:QDReleaseTestCalls.Clear();$global:QDReleaseTestTagExists=$false
& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform android -Directory $folder
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'api --method POST *git/refs*sha=aaaaaaaa*'}).Count -eq 1) 'Missing tag is created at the exact successfully built commit'
$global:QDReleaseTestCalls.Clear();$global:QDReleaseTestTagCommit='bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb'
Reject {& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform android -Directory $folder} 'An existing tag at another commit cannot publish new build bytes'
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'api --method POST*' -or $_ -like 'release *'}).Count -eq 0) 'Conflicting tag is never moved and release is untouched'
$global:QDReleaseTestCalls.Clear();$global:QDReleaseTestTagCommit=$global:QDReleaseTestCommit;$global:QDReleaseTestForbidden=$true
Reject {& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform android -Directory $folder} 'API authorization failure is not treated as a missing tag'
$global:QDReleaseTestForbidden=$false;$global:QDReleaseTestAnnotated=$true;$global:QDReleaseTestCalls.Clear()
& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform android -Directory $folder
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'api *git/tags/*'}).Count -eq 1) 'Annotated tag is resolved to the built commit'
$global:QDReleaseTestAnnotated=$false;$global:QDReleaseTestTagCommit='bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb';$global:QDReleaseTestCalls.Clear()
& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform android -Directory $folder -Automatic
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'release *' -or $_ -like 'api --method POST*'}).Count -eq 0) 'A push with an already released version keeps artifacts without touching old tag/release'
$global:QDReleaseTestTagCommit=$global:QDReleaseTestCommit;$global:QDReleaseTestCalls.Clear()
$global:QDReleaseTestAssets=@($apk,$apkManifest) | ForEach-Object { @{name=[IO.Path]::GetFileName($_);digest='sha256:older-build'} }
& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform android -Directory $folder -Automatic
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'release *'}).Count -eq 0) 'Automatic rebuild never replaces existing published platform assets'
$global:QDReleaseTestAssets=@();$global:QDReleaseTestTagExists=$false;$global:QDReleaseTestTagRace=$true;$global:QDReleaseTestCalls.Clear()
& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform android -Directory $folder
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'release upload*'}).Count -eq 2) 'Concurrent tag creation is read back and accepted only at the built commit'
$global:QDReleaseTestTagRace=$false;$global:QDReleaseTestExists=$false;$global:QDReleaseTestReleaseRace=$true;$global:QDReleaseTestCalls.Clear()
& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform android -Directory $folder
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'release upload*'}).Count -eq 2) 'Concurrent release creation is read back before platform upload'
$global:QDReleaseTestReleaseRace=$false
foreach($arch in @('arm64','x64')){
    $pkg=Join-Path $folder "QuizDesk-Setup-5.11.0-macos-$arch.pkg";$pkgManifest=Join-Path $folder "QuizDesk-5.11.0-macos-$arch-SHA256.txt"
    [IO.File]::WriteAllText($pkg,'Inert package fixture')
    $pkgHash=(Get-FileHash $pkg).Hash.ToLowerInvariant();"$pkgHash  $([IO.Path]::GetFileName($pkg))" | Set-Content $pkgManifest -Encoding ascii
    $global:QDReleaseTestCalls.Clear()
    & $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform "macos-$arch" -Directory $folder
    Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like "release upload*macos-$arch*"}).Count -eq 2) "macOS $arch attaches its PKG and separate checksum to the shared release"
}
foreach($arch in @('arm64','x64')){
    $dmg=Join-Path $folder "QuizDesk-5.11.0-macos-$arch.dmg";$dmgManifest=Join-Path $folder "QuizDesk-5.11.0-macos-$arch-DMG-SHA256.txt"
    [IO.File]::WriteAllText($dmg,'Inert disk image fixture')
    $dmgHash=(Get-FileHash $dmg).Hash.ToLowerInvariant();"$dmgHash  $([IO.Path]::GetFileName($dmg))" | Set-Content $dmgManifest -Encoding ascii
    $global:QDReleaseTestCalls.Clear()
    & $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform "macos-$arch" -MacOSFormat dmg -Directory $folder
    Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like "release upload*macos-$arch*"}).Count -eq 2 -and @($global:QDReleaseTestCalls | Where-Object {$_ -like 'release upload*.pkg*'}).Count -eq 0) "macOS $arch publishes only DMG and its separate checksum"
}
$global:QDReleaseTestCalls.Clear()
[IO.File]::WriteAllText($dmg,'tampered image')
Reject {& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform macos-x64 -MacOSFormat dmg -Directory $folder} 'Tampered DMG is rejected before publication'
Check ($global:QDReleaseTestCalls.Count -eq 0) 'DMG checksum failure causes no GitHub operation'
$ipa=Join-Path $folder 'QuizDesk-5.11.0-ios-arm64-unsigned.ipa';$ipaManifest=Join-Path $folder 'QuizDesk-5.11.0-iOS-unsigned-SHA256.txt'
[IO.File]::WriteAllText($ipa,'Inert IPA fixture')
$ipaHash=(Get-FileHash $ipa).Hash.ToLowerInvariant();"$ipaHash  $([IO.Path]::GetFileName($ipa))" | Set-Content $ipaManifest -Encoding ascii
$global:QDReleaseTestCalls.Clear();$global:QDReleaseTestAssets=@()
& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform ios-unsigned -Directory $folder
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'release upload*unsigned*'}).Count -eq 2 -and @($global:QDReleaseTestCalls | Where-Object {$_ -like 'release create*'}).Count -eq 0) 'Unsigned IPA and checksum attach to the shared existing release'
$global:QDReleaseTestCalls.Clear()
[IO.File]::WriteAllText($ipa,'tampered IPA')
Reject {& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform ios-unsigned -Directory $folder} 'Tampered IPA is rejected before publication'
Check ($global:QDReleaseTestCalls.Count -eq 0) 'IPA checksum failure causes no GitHub operation'
[IO.File]::WriteAllText($ipa,'Inert IPA fixture')
$global:QDReleaseTestAssets=@($ipa,$ipaManifest) | ForEach-Object { @{name=[IO.Path]::GetFileName($_);digest=('sha256:'+(Get-FileHash $_).Hash.ToLowerInvariant())} }
& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform ios-unsigned -Directory $folder
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'release upload*'}).Count -eq 0) 'Identical existing IPA assets are not uploaded again'
$global:QDReleaseTestCalls.Clear();$global:QDReleaseTestTagCommit='bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb'
& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform ios-unsigned -Directory $folder -Automatic
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'release *' -or $_ -like 'api --method POST*'}).Count -eq 0) 'IPA automatic push preserves a version tag assigned to another commit'
$global:QDReleaseTestTagCommit=$global:QDReleaseTestCommit
$global:QDReleaseTestAssets=@();$global:QDReleaseTestTagExists=$false;$global:QDReleaseTestCreateResponse=$true;$global:QDReleaseTestDelayedReads=2;$global:QDReleaseTestCalls.Clear()
& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform windows -Directory $folder
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'api *git/ref/tags/*'}).Count -eq 1) 'Successful tag creation uses POST response without an eventually consistent GET'
$global:QDReleaseTestTagExists=$false;$global:QDReleaseTestCreateResponse=$false;$global:QDReleaseTestTagRace=$true;$global:QDReleaseTestCalls.Clear()
& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform windows -Directory $folder
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'api *git/ref/tags/*'}).Count -eq 4) 'Concurrent creation tolerates two delayed 404 reads'
$global:QDReleaseTestTagRace=$false;$global:QDReleaseTestTagExists=$false;$global:QDReleaseTestCreateDenied=$true;$global:QDReleaseTestCalls.Clear()
$failure='';try {& $publish -Tag v5.11.0 -Commit $global:QDReleaseTestCommit -Platform windows -Directory $folder}catch{$failure=$_.Exception.Message}
Check ($failure -match 'HTTP 403' -and $failure -match 'gh exit 1') 'Creation failure retains the original GitHub error'
Check (@($global:QDReleaseTestCalls | Where-Object {$_ -like 'api *git/ref/tags/*'}).Count -eq 6 -and @($global:QDReleaseTestCalls | Where-Object {$_ -like 'release *'}).Count -eq 0) 'Missing tag retries are bounded and never upload assets'
Write-Output 'All 37 release automation checks passed; gh was mocked (no network writes).'


} finally {
    Remove-Variable -Name QDReleaseTestCreateResponse,QDReleaseTestDelayedReads,QDReleaseTestCreateDenied -Scope Global -ErrorAction SilentlyContinue
    foreach($key in $originalEnvironment.Keys){[Environment]::SetEnvironmentVariable($key,$originalEnvironment[$key])}
    Remove-Variable -Name QDReleaseTestCalls,QDReleaseTestExists,QDReleaseTestAssets,QDReleaseTestDraft,QDReleaseTestCommit,QDReleaseTestTagCommit,QDReleaseTestTagExists,QDReleaseTestAnnotated,QDReleaseTestForbidden,QDReleaseTestTagRace,QDReleaseTestReleaseRace -Scope Global -ErrorAction SilentlyContinue
}

# GitHub Actions forwards LASTEXITCODE after running a PowerShell step.
# The final negative test deliberately leaves the mocked gh exit code at 1.
# Reset it only after all assertions and cleanup succeed, never in finally:
# an actual test failure must still terminate the step with an error.
$global:LASTEXITCODE=0
