$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outputDir = Join-Path $PSScriptRoot 'release\tests'
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$testExe = Join-Path $outputDir 'PrayerAlarmTests.exe'
& $compiler /nologo /target:exe /warn:4 "/out:$testExe" (Join-Path $PSScriptRoot 'src\PrayerAlarm.cs') (Join-Path $PSScriptRoot 'tests\PrayerAlarmTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'No se pudieron compilar las pruebas' }
& $testExe
if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas de la alarma' }
