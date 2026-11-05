param([switch]$Restore)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
if ($Restore) {
    & nuget restore (Join-Path $root 'zClock.sln') -NonInteractive
    if ($LASTEXITCODE -ne 0) { throw 'NuGet restore failed. Install NuGet CLI or restore packages in Visual Studio.' }
}
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (!(Test-Path $framework)) { $framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319' }
$resgen = Get-ChildItem (Join-Path ${env:ProgramFiles(x86)} 'Microsoft SDKs\Windows') -Filter ResGen.exe -Recurse -ErrorAction SilentlyContinue | Where-Object FullName -Match 'NETFX 4' | Select-Object -First 1 -ExpandProperty FullName
if (!$resgen) { throw 'ResGen.exe not found. Install the .NET Framework SDK (Visual Studio desktop .NET development workload).' }
$build = Join-Path $root 'bin\Release'
$intermediate = Join-Path $root 'obj\Build'
New-Item -ItemType Directory -Path $build,$intermediate -Force | Out-Null
foreach ($form in @('Form1','InputForm')) {
    & $resgen (Join-Path $root ($form+'.resx')) (Join-Path $intermediate ($form+'.resources'))
    if ($LASTEXITCODE -ne 0) { throw 'Resource compilation failed' }
}
$qrcoder = Join-Path $root 'packages\QRCoder.1.6.0\lib\net40\QRCoder.dll'
if (!(Test-Path $qrcoder)) { throw 'Restore NuGet packages first (nuget restore zClock.sln).' }
$arguments = @('/nologo','/target:winexe','/optimize+','/platform:anycpu',('/out:'+(Join-Path $build 'zClock.exe')),('/win32manifest:'+(Join-Path $root 'app.manifest')),('/win32icon:'+(Join-Path $root 'sandglass.ico')),
    '/r:System.dll','/r:System.Core.dll','/r:System.Drawing.dll','/r:System.Windows.Forms.dll','/r:System.Configuration.dll','/r:System.Data.dll','/r:System.Web.Extensions.dll','/r:Microsoft.CSharp.dll',('/r:'+$qrcoder),
    ('/resource:'+(Join-Path $intermediate 'Form1.resources')+',zClock.zClock.resources'),('/resource:'+(Join-Path $intermediate 'InputForm.resources')+',zClock.InputForm.resources'),('/resource:'+(Join-Path $root 'Prompter.html')+',zClock.Prompter.html'))
$arguments += Get-ChildItem -LiteralPath $root -Filter '*.cs' -File | Select-Object -ExpandProperty FullName
$arguments += Get-ChildItem (Join-Path $root 'Properties') -Filter '*.cs' -File | Select-Object -ExpandProperty FullName
& (Join-Path $framework 'csc.exe') @arguments
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
Copy-Item (Join-Path $root 'App.config') (Join-Path $build 'zClock.exe.config') -Force
Copy-Item $qrcoder $build -Force
foreach ($file in @('LICENSE','QRCoder-LICENSE.txt','THIRD_PARTY_NOTICES.md','提词器使用说明.md')) {
    if (Test-Path (Join-Path $root $file)) { Copy-Item (Join-Path $root $file) $build -Force }
}
Write-Output ('Built '+(Join-Path $build 'zClock.exe'))
