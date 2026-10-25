param([string]$DotNet='dotnet',[switch]$IncludeIOS)
$ErrorActionPreference='Stop'
$qdProjects=@('QuizDesk.Desktop','QuizDesk.Android')
if($IncludeIOS){$qdProjects+='QuizDesk.iOS'}
$qdReports=Join-Path $PSScriptRoot 'dist/security'
New-Item -ItemType Directory -Force -Path $qdReports | Out-Null
foreach($qdProject in $qdProjects){
    $qdPath=Join-Path $PSScriptRoot "$qdProject/$qdProject.csproj"
    & $DotNet restore $qdPath --force --no-http-cache -p:NuGetAudit=true -p:NuGetAuditMode=all -p:NuGetAuditLevel=low
    if($LASTEXITCODE -ne 0){throw "Audit restore failed: $qdProject"}
    $qdRaw=& $DotNet list $qdPath package --vulnerable --include-transitive --format json
    if($LASTEXITCODE -ne 0){throw "Audit query failed: $qdProject"}
    $qdRaw | Set-Content -LiteralPath (Join-Path $qdReports "$qdProject.json") -Encoding utf8
    $qdAudit=($qdRaw -join [Environment]::NewLine) | ConvertFrom-Json
    if($qdAudit.logs | Where-Object { $_.level -in @('Error','Warning') }){throw 'Audit source could not be verified'}
    foreach($qdItem in $qdAudit.projects){foreach($qdFramework in $qdItem.frameworks){foreach($qdPackage in @($qdFramework.topLevelPackages)+@($qdFramework.transitivePackages)){if($qdPackage.vulnerabilities){throw "Known vulnerability: $($qdPackage.id)"}}}}
}
Write-Output 'No known vulnerabilities reported by the configured NuGet audit source. This does not certify that the app has no vulnerabilities.'
