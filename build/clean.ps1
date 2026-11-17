[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "清理生成目录…"
foreach ($path in @(
    (Join-Path $root "build\out"),
    (Join-Path $root "dist"),
    (Join-Path $root "src\KeyNest.Installer\Payload\payload.zip")
)) {
    if (Test-Path $path) {
        Remove-Item $path -Recurse -Force
    }
}

Get-ChildItem -Path $root -Directory -Recurse -Include bin, obj |
    ForEach-Object { Remove-Item $_.FullName -Recurse -Force }

Write-Host "完成。"
