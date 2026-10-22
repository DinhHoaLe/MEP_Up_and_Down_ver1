param([string]$AddinsDirectory = (Join-Path $env:APPDATA 'Autodesk\Revit\Addins\2024'))
$ErrorActionPreference = 'Stop'
$assembly = Join-Path $PSScriptRoot 'bin\Release\RevitCodexChat.dll'
if (!(Test-Path -LiteralPath $assembly)) { throw 'Build Release before installing.' }
if (Get-Process Revit -ErrorAction SilentlyContinue) { throw 'Close Revit before installing this startup add-in.' }
[xml]$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'RevitCodexChat.addin') -Encoding UTF8
$manifest.SelectSingleNode('/RevitAddIns/AddIn/Assembly').InnerText = [string]$assembly
New-Item -ItemType Directory -Path $AddinsDirectory -Force | Out-Null
$target = Join-Path $AddinsDirectory 'RevitCodexChat.addin'
$manifest.Save($target)
Write-Output "Installed: $target"
Write-Output 'Start Revit 2024 > Add-Ins > Codex AI > Mở chat AI.'
