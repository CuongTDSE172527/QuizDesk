param([string]$Tag="")
$ErrorActionPreference='Stop'
if([string]::IsNullOrWhiteSpace($Tag)){
    $project=Join-Path (Split-Path (Split-Path $PSScriptRoot)) 'QuizDesk.Mobile/QuizDesk.Mobile.csproj'
    [xml]$source=Get-Content -LiteralPath $project -Raw
    $Tag='v'+$source.SelectSingleNode('/Project/PropertyGroup/Version').InnerText
}
if($Tag -cnotmatch '^v([0-9]+\.[0-9]+\.[0-9]+)$'){throw 'Use a release tag such as v5.12.0 (three numeric components).'}
$version=$Matches[1]
$root=Split-Path (Split-Path $PSScriptRoot)
$projects=@{
    'Windows'=@('QuizDesk.Desktop/QuizDesk.Desktop.csproj','Version')
    'Android'=@('QuizDesk.Android/QuizDesk.Android.csproj','ApplicationDisplayVersion')
    'Shared UI'=@('QuizDesk.Mobile/QuizDesk.Mobile.csproj','Version')
}
foreach($name in $projects.Keys){
    $entry=$projects[$name];[xml]$xml=Get-Content -LiteralPath (Join-Path $root $entry[0]) -Raw
    $node=$xml.SelectSingleNode("/Project/PropertyGroup/$($entry[1])")
    if($null -eq $node -or $node.InnerText -cne $version){throw "$name version must equal $version before creating tag $Tag."}
}
$commitOutput=& git -C $root rev-parse HEAD
if($LASTEXITCODE -ne 0){throw 'Cannot determine checkout commit.'}
$commit=($commitOutput | Out-String).Trim()
if($env:GITHUB_OUTPUT){@("version=$version","tag=$Tag","commit=$commit") | Out-File -LiteralPath $env:GITHUB_OUTPUT -Append -Encoding utf8}
Write-Output "Validated $Tag at $commit"
