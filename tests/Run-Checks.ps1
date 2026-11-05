param([switch]$PowerPoint)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$build = Join-Path $root 'bin\Release'
$scratch = Join-Path $root 'obj\PrompterChecks'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
foreach ($file in @('zClock.exe', 'zClock.exe.config', 'QRCoder.dll')) {
    Copy-Item (Join-Path $build $file) $scratch -Force
}
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
$arguments = @('/nologo', ('/out:' + (Join-Path $scratch 'IntegrationChecks.exe')), ('/r:' + (Join-Path $scratch 'zClock.exe')), '/r:System.Drawing.dll', '/r:System.Windows.Forms.dll', '/r:System.Web.Extensions.dll', '/r:Microsoft.CSharp.dll', (Join-Path $PSScriptRoot 'IntegrationChecks.cs'))
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
if ($PowerPoint) { & (Join-Path $scratch 'IntegrationChecks.exe') --powerpoint }
else { & (Join-Path $scratch 'IntegrationChecks.exe') }
if ($LASTEXITCODE -ne 0) { throw 'Integration checks failed' }
& $compiler /nologo ("/out:" + (Join-Path $scratch 'ReviewChecks.exe')) ("/r:" + (Join-Path $scratch 'zClock.exe')) /r:System.Windows.Forms.dll (Join-Path $PSScriptRoot 'ReviewChecks.cs')
if ($LASTEXITCODE -ne 0) { throw 'Parser check compilation failed' }
& (Join-Path $scratch 'ReviewChecks.exe')
if ($LASTEXITCODE -ne 0) { throw 'Parser checks failed' }
