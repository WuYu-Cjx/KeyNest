[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Push-Location $root
try {
    Write-Host "还原依赖…"
    dotnet restore "KeyNest.sln"

    Write-Host "编译解决方案（$Configuration）…"
    dotnet build "KeyNest.sln" -c $Configuration --no-restore

    Write-Host "完成。"
}
finally {
    Pop-Location
}
