param(
    [Parameter(Mandatory)][ValidatePattern('^v[0-9]+\.[0-9]+\.[0-9]+$')][string]$Tag,
    [Parameter(Mandatory)][ValidatePattern('^[a-fA-F0-9]{40}$')][string]$Commit,
    [Parameter(Mandatory)][ValidateSet('android','windows','macos-arm64','macos-x64','ios-unsigned')][string]$Platform,
    [Parameter(Mandatory)][string]$Directory,
    [ValidateSet('pkg','dmg')][string]$MacOSFormat='pkg',
    [switch]$Automatic
)
$ErrorActionPreference='Stop'
# Handle expected gh API 404s explicitly; never log credentials.
$PSNativeCommandUseErrorActionPreference=$false
if(-not $env:GH_TOKEN -or -not $env:GITHUB_REPOSITORY){throw 'GitHub token and repository context required.'}
$version=$Tag.Substring(1)
$asset=switch($Platform){android{"QuizDesk-$version-Android.apk"} windows{"QuizDesk-Setup-$version-x64.exe"} ios-unsigned{"QuizDesk-$version-ios-arm64-unsigned.ipa"} default{"QuizDesk-Setup-$version-$Platform.pkg"}}
$manifest=switch($Platform){android{"QuizDesk-$version-Android-SHA256.txt"} windows{"QuizDesk-$version-Windows-SHA256.txt"} ios-unsigned{"QuizDesk-$version-iOS-unsigned-SHA256.txt"} default{"QuizDesk-$version-$Platform-SHA256.txt"}}
if($MacOSFormat -eq 'dmg'){
    if($Platform -notlike 'macos-*'){throw 'DMG publication requires a macOS platform.'}
    $asset="QuizDesk-$version-$Platform.dmg"
    $manifest="QuizDesk-$version-$Platform-DMG-SHA256.txt"
}
$files=@((Join-Path $Directory $asset),(Join-Path $Directory $manifest))
foreach($file in $files){if(-not (Test-Path -LiteralPath $file -PathType Leaf)){throw "Release file missing: $file"}}
$actual=(Get-FileHash -LiteralPath $files[0] -Algorithm SHA256).Hash.ToLowerInvariant()
$record=[IO.File]::ReadAllText($files[1]).Trim()
if($record -cne "$actual  $asset"){throw 'Downloaded release asset does not match its SHA-256 manifest.'}
function Skip-Publish([string]$Reason){
    $message="Build retained in Actions Artifacts. Release publication skipped: $Reason"
    Write-Output $message
    if($env:GITHUB_STEP_SUMMARY){$message | Out-File -LiteralPath $env:GITHUB_STEP_SUMMARY -Append -Encoding utf8}
}
# Publication is isolated by platform/commit; concurrent tag/release creation is resolved by reading back the result.
. (Join-Path $PSScriptRoot 'GitHub-Api.ps1')
$refEndpoint="repos/$env:GITHUB_REPOSITORY/git/ref/tags/$Tag"
$reference=Get-GitHubJson $refEndpoint
if($null -eq $reference){
    & gh api --method POST "repos/$env:GITHUB_REPOSITORY/git/refs" -f "ref=refs/tags/$Tag" -f "sha=$Commit" | Out-Null
    $reference=Get-GitHubJson $refEndpoint
    if($null -eq $reference){throw 'Cannot create release tag at the built commit.'}
}
$target=$reference.object
for($depth=0;$null -ne $target -and $target.type -eq 'tag' -and $depth -lt 8;$depth++){
    $annotated=Get-GitHubJson "repos/$env:GITHUB_REPOSITORY/git/tags/$($target.sha)"
    $target=$annotated.object
}
if($null -eq $target -or $target.type -ne 'commit'){throw "Cannot resolve tag $Tag to a commit."}
if($target.sha -ine $Commit){
    if($Automatic){Skip-Publish "$Tag is already assigned to another commit. Increase the app version to publish a new release.";return}
    throw "Tag $Tag does not point to the built commit $Commit. Existing tags will not be moved. Build from the tag or use a new version."
}
$release=Get-GitHubJson "repos/$env:GITHUB_REPOSITORY/releases/tags/$Tag"
if($null -eq $release){
    & gh release create $Tag --repo $env:GITHUB_REPOSITORY --verify-tag --title "QuizDesk $version" --generate-notes
    $release=Get-GitHubJson "repos/$env:GITHUB_REPOSITORY/releases/tags/$Tag"
    if($null -eq $release){throw 'Cannot read created release.'}
}
if($release.tag_name -cne $Tag){throw 'Unexpected release tag.'}
if($Automatic -and -not $release.draft -and @($release.assets | Where-Object {$_.name -cin @($asset,$manifest)}).Count -eq 2){
    Skip-Publish "Assets for $Platform already exist on $Tag. Published files remain unchanged.";return
}
foreach($file in $files){
    $name=[IO.Path]::GetFileName($file);$hash=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
    $existing=@($release.assets | Where-Object { $_.name -ceq $name })
    if($existing.Count -gt 0){
        if($existing.Count -eq 1 -and $existing[0].digest -ceq "sha256:$hash"){Write-Output "Already published with identical SHA-256: $name";continue}
        throw "Asset $name already exists with different or unverifiable bytes. Use a new version tag; published files will not be overwritten."
    }
    & gh release upload $Tag $file --repo $env:GITHUB_REPOSITORY
    if($LASTEXITCODE -ne 0){throw "Release asset upload failed: $name"}
}
if($release.draft){
    & gh release edit $Tag --repo $env:GITHUB_REPOSITORY --draft=false
    if($LASTEXITCODE -ne 0){throw 'Cannot publish existing draft release.'}
}
Write-Output "Published $Platform assets: $($release.html_url)"
if($env:GITHUB_STEP_SUMMARY){"Published **$Platform** assets to [QuizDesk $version]($($release.html_url))." | Out-File -LiteralPath $env:GITHUB_STEP_SUMMARY -Append -Encoding utf8}
