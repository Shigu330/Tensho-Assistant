<#
.SYNOPSIS
    Rebuilds community-templates/index.json from community-templates/tools/*.json (TEMPLATES-001).
.DESCRIPTION
    Each row holds the tool's identifier, name, author and the fingerprint the plugin checks: SHA-256 of the file's text with line ends as "\n". The tests then check every file with the plugin's rules.
#>
$ErrorActionPreference='Stop'
$root=Join-Path (Split-Path $PSScriptRoot -Parent) 'community-templates'
$sha=[Security.Cryptography.SHA256]::Create()
$rows=foreach($file in Get-ChildItem (Join-Path $root 'tools') -Filter '*.json' | Sort-Object Name){
    $text=[IO.File]::ReadAllText($file.FullName).TrimStart([char]0xFEFF).Replace("`r`n","`n")
    $tool=$text | ConvertFrom-Json
    if($tool.id -cne $file.BaseName){throw "$($file.Name): its id is '$($tool.id)'; the file must be named after it."}
    [ordered]@{
        id=$tool.id
        name=$tool.name
        author=$tool.author
        sha256=[Convert]::ToHexString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($text))).ToLowerInvariant()
    }
}
$index=[ordered]@{schema=1;tools=@($rows)} | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText((Join-Path $root 'index.json'),$index.Replace("`r`n","`n")+"`n",(New-Object Text.UTF8Encoding($false)))
Write-Host "community-templates/index.json: $(@($rows).Count) tools."
