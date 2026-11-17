[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

$outRoot = Join-Path $root "build\out"
$appOut = Join-Path $outRoot "app"
$uninstallerOut = Join-Path $outRoot "uninstaller"
$installerOut = Join-Path $outRoot "installer"
$payloadDir = Join-Path $outRoot "payload"
$zipPath = Join-Path $root "src\KeyNest.Installer\Payload\payload.zip"
$dist = Join-Path $root "dist"

Write-Host "清理输出目录…"
if (Test-Path $outRoot) { Remove-Item $outRoot -Recurse -Force }
New-Item -ItemType Directory -Path $appOut, $uninstallerOut, $installerOut, $payloadDir -Force | Out-Null

$publishArgs = @(
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true",
    "-p:DebugType=None",
    "-p:DebugSymbols=false"
)

Write-Host "发布主程序…"
dotnet publish (Join-Path $root "src\KeyNest.App\KeyNest.App.csproj") @publishArgs -o $appOut

Write-Host "发布卸载程序…"
dotnet publish (Join-Path $root "src\KeyNest.Uninstaller\KeyNest.Uninstaller.csproj") @publishArgs -o $uninstallerOut

Write-Host "复制合规文本…"
Copy-Item (Join-Path $root "docs\THIRD-PARTY-NOTICES.txt") $appOut -Force
Copy-Item (Join-Path $root "docs\PRIVACY.txt") $appOut -Force

Write-Host "组装负载…"
Copy-Item (Join-Path $appOut "*") $payloadDir -Recurse -Force
Copy-Item (Join-Path $uninstallerOut "KeyNest.Uninstaller.exe") $payloadDir -Force

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
New-Item -ItemType Directory -Path (Split-Path $zipPath) -Force | Out-Null
Compress-Archive -Path (Join-Path $payloadDir "*") -DestinationPath $zipPath -CompressionLevel Optimal

Write-Host "发布安装器（内嵌负载）…"
dotnet publish (Join-Path $root "src\KeyNest.Installer\KeyNest.Installer.csproj") @publishArgs -o $installerOut

New-Item -ItemType Directory -Path $dist -Force | Out-Null
Copy-Item (Join-Path $installerOut "KeyNest.Installer.exe") (Join-Path $dist "KeyNest-Setup.exe") -Force

Write-Host ""
Write-Host "打包完成：dist\KeyNest-Setup.exe"
Write-Host "提醒：发布前请按 docs\SIGNING.txt 完成数字签名与杀软误报核查。"
