[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Push-Location $root
try {
    Write-Host "运行单元测试…"
    dotnet test "tests\KeyNest.Tests\KeyNest.Tests.csproj" -c $Configuration
}
finally {
    Pop-Location
}
