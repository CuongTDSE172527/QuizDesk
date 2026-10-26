function Get-GitHubJson([string]$Endpoint) {
    $errorPath=[IO.Path]::GetTempFileName()
    try {
        $text=(& gh api $Endpoint 2>$errorPath | Out-String)
        if($LASTEXITCODE -eq 0){return ($text | ConvertFrom-Json)}
        $errorText=[IO.File]::ReadAllText($errorPath)
        if($errorText -match '\bHTTP 404\b'){return $null}
        throw "GitHub API request failed for $Endpoint. $errorText"
    } finally {Remove-Item -LiteralPath $errorPath -Force}
}
