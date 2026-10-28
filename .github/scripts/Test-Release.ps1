param([string]$DotNet='dotnet')
$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot)
$project=Join-Path $root 'QuizDesk.Mobile.Tests/QuizDesk.Mobile.Tests.csproj'
& $DotNet build $project -c Release -m:1 --nologo
if($LASTEXITCODE -ne 0){throw 'Core test build failed.'}
$testRoot=Join-Path $env:RUNNER_TEMP ('quizdesk-checks-'+[Guid]::NewGuid().ToString('N'))
foreach($suite in @('snapshot-isolation','word-symbols','word-sections','recognition','review','batch-import','merge','media','export-sampling','office-export')){
    & $DotNet run --project $project -c Release --no-build -- "--$suite" (Join-Path $testRoot $suite)
    if($LASTEXITCODE -ne 0){throw "Regression suite $suite failed."}
}
