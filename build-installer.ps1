$ErrorActionPreference='Stop'
Set-Location $PSScriptRoot
cmd /c build.cmd
if($LASTEXITCODE -ne 0){throw 'Error al compilar'}
$stage=Join-Path $PSScriptRoot 'release\payload'
if(Test-Path $stage){Remove-Item $stage -Recurse -Force}
New-Item -ItemType Directory -Path $stage,(Join-Path $stage 'backend') -Force | Out-Null
Copy-Item Gili.exe,LICENSE,README.md $stage
Copy-Item backend\app.py,backend\web_desktop.py,backend\voice.ps1 (Join-Path $stage 'backend')
Compress-Archive -Path "$stage\*" -DestinationPath payload.zip -Force
$fw=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
& "$fw\csc.exe" /nologo /target:winexe /platform:x64 /optimize+ /out:Manaure-Setup.exe /win32icon:assets\gili.ico /win32manifest:src\app.manifest /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.IO.Compression.dll /r:System.Core.dll /r:Microsoft.CSharp.dll /resource:payload.zip,payload.zip Setup.cs
if($LASTEXITCODE -ne 0){throw 'Error al compilar instalador'}
Get-FileHash Manaure-Setup.exe -Algorithm SHA256
