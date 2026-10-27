function Get-GitHubJson([string]$Endpoint,[int]$MissingAttempts=1) {
    $errorPath=[IO.Path]::GetTempFileName()
    try {
        for($attempt=1;$attempt -le $MissingAttempts;$attempt++){
            $text=(& gh api $Endpoint 2>$errorPath | Out-String)
            if($LASTEXITCODE -eq 0){return ($text | ConvertFrom-Json)}
            $errorText=[IO.File]::ReadAllText($errorPath)
            if($errorText -notmatch '\bHTTP 404\b'){throw "GitHub API request failed for $Endpoint. $errorText"}
            if($attempt -lt $MissingAttempts){Start-Sleep -Seconds 2}
        }
        return $null
    } finally {Remove-Item -LiteralPath $errorPath -Force}
}
