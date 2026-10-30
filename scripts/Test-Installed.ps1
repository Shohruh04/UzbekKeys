$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$csc=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
foreach($platform in 'x86','x64') {
    $output=Join-Path $projectRoot ('build\TestNativeInput-'+$platform+'.exe')
    & $csc /nologo /target:exe ('/platform:'+$platform) ('/out:'+$output) (Join-Path $PSScriptRoot 'TestNativeInput.cs')
    if($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
    & $output
    if($LASTEXITCODE -ne 0) { throw "Native input test failed: $platform" }
}
python (Join-Path $PSScriptRoot 'test_layout.py') --installed
if($LASTEXITCODE -ne 0) { throw 'Layout test failed' }
