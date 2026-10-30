[CmdletBinding()]
param([switch]$Bootstrap)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$toolsRoot = Join-Path $projectRoot '.tools'
$buildRoot = Join-Path $projectRoot 'build'
$distRoot = Join-Path $projectRoot 'dist'
New-Item -ItemType Directory -Path $toolsRoot,$buildRoot,$distRoot -Force | Out-Null

function Invoke-Checked {
    param([string]$Program, [string[]]$Arguments)
    & $Program @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Program exited with $LASTEXITCODE" }
}
function Get-PinnedDownload {
    param([string]$Url,[string]$Destination,[string]$Sha256)
    if (-not (Test-Path -LiteralPath $Destination)) { Invoke-WebRequest -Uri $Url -OutFile $Destination }
    if ((Get-FileHash -LiteralPath $Destination -Algorithm SHA256).Hash -ne $Sha256) {
        throw "SHA256 mismatch: $Destination. No downloaded program was executed."
    }
}

$msklcRoot = Join-Path $toolsRoot 'msklc'
if ($env:MSKLC_DIR) { $msklcRoot = $env:MSKLC_DIR }
if (-not (Test-Path -LiteralPath (Join-Path $msklcRoot 'bin\i386\kbdutool.exe'))) {
    if (-not $Bootstrap) { throw 'MSKLC_DIR must point to MSKLC 1.4, or run with -Bootstrap.' }
    $download = Join-Path $toolsRoot 'MSKLC.exe'
    $sevenZip = Join-Path $toolsRoot '7zr.exe'
    Get-PinnedDownload 'https://download.microsoft.com/download/6/f/5/6f5ce43a-e892-4fd1-b9a6-1a0cbb64e6e2/MSKLC.exe' $download 'CD12A6B08066133DC4AD71BDCACCF1492E3D9A1DE93CF84B7B2824309F0D7255'
    Get-PinnedDownload 'https://www.7-zip.org/a/7zr.exe' $sevenZip '256FECA8E274E5DA655E2A284FABAFD9F554365EB164862089DACD4E8276D282'
    if ((Get-AuthenticodeSignature -LiteralPath $download).Status -ne 'Valid') { throw 'MSKLC Microsoft signature validation failed.' }
    $extract = Join-Path $toolsRoot 'msklc-download'
    Invoke-Checked $sevenZip @('x','-y',('-o' + $extract),$download)
    $msklcMsi = Get-ChildItem -LiteralPath $extract -Filter MSKLC.msi -Recurse | Select-Object -First 1
    if (-not $msklcMsi) { throw 'MSKLC.msi not found in verified download.' }
    $job = Start-Process -FilePath msiexec.exe -ArgumentList '/a',('"'+$msklcMsi.FullName+'"'),'/qn',('TARGETDIR="'+$msklcRoot+'"') -WindowStyle Hidden -Wait -PassThru
    if ($job.ExitCode -ne 0) { throw "MSKLC extraction failed: $($job.ExitCode)" }
}

$wix = Join-Path $toolsRoot 'wix\wix.exe'
if (-not (Test-Path -LiteralPath $wix)) {
    if (-not $Bootstrap) { throw 'WiX 5.0.2 missing. Run with -Bootstrap.' }
    Invoke-Checked 'dotnet' @('tool','install','wix','--version','5.0.2','--tool-path',(Join-Path $toolsRoot 'wix'))
}
$utilRoot = Join-Path $toolsRoot 'wix-util'
$utilExtension = Join-Path $utilRoot 'wixext5\WixToolset.Util.wixext.dll'
if (-not (Test-Path -LiteralPath $utilExtension)) {
    if (-not $Bootstrap) { throw 'WiX Util 5.0.2 missing. Run with -Bootstrap.' }
    $utilDownload = Join-Path $toolsRoot 'wix-util.zip'
    Invoke-WebRequest -Uri 'https://api.nuget.org/v3-flatcontainer/wixtoolset.util.wixext/5.0.2/wixtoolset.util.wixext.5.0.2.nupkg' -OutFile $utilDownload
    Expand-Archive -LiteralPath $utilDownload -DestinationPath $utilRoot -Force
}
$zigRoot = Join-Path $toolsRoot 'zig\ziglang'
if (-not (Test-Path -LiteralPath (Join-Path $zigRoot 'zig.exe'))) {
    if (-not $Bootstrap) { throw 'Zig 0.14.1 missing. Run with -Bootstrap.' }
    Invoke-Checked 'python' @('-m','pip','install','--target',(Join-Path $toolsRoot 'zig'),'ziglang==0.14.1')
}
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $csc)) { throw '.NET Framework 4 compiler missing.' }
$priorTemp = $env:TEMP; $priorTmp = $env:TMP
$priorDirectory = Get-Location
try {
    # Legacy tools and Remotion both need a writable process-local temp directory.
    $env:TEMP = Join-Path $buildRoot 'tmp'; $env:TMP = $env:TEMP
    New-Item -ItemType Directory -Path $env:TEMP -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $buildRoot 'uzbkeys.klc'),[IO.File]::ReadAllText((Join-Path $projectRoot 'src\uzbkeys.klc')),[Text.Encoding]::Unicode)
    $kbdTool = Join-Path $msklcRoot 'bin\i386\kbdutool.exe'
    foreach ($target in @(@{Name='x86';Flag='-x'},@{Name='x64';Flag='-m'},@{Name='wow64';Flag='-o'},@{Name='generated';Flag='-s'})) {
        $targetRoot = Join-Path $buildRoot $target.Name
        New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null
        Set-Location -LiteralPath $targetRoot
        if ($target.Name -ne 'generated' -and (Test-Path -LiteralPath 'uzbkeys.dll')) { Remove-Item -LiteralPath 'uzbkeys.dll' }
        Invoke-Checked $kbdTool @('-u',$target.Flag,'..\uzbkeys.klc')
        # KbdUTool can report success even when CL or LINK failed.
        $expectedFile = if ($target.Name -eq 'generated') { 'uzbkeys.c' } else { 'uzbkeys.dll' }
        if (-not (Test-Path -LiteralPath $expectedFile)) { throw "MSKLC produced no $expectedFile for $($target.Name)." }
    }
    $armRoot = Join-Path $buildRoot 'arm64'
    New-Item -ItemType Directory -Path $armRoot -Force | Out-Null
    foreach ($name in 'uzbkeys.c','uzbkeys.h','uzbkeys.def','uzbkeys.rc') {
        Copy-Item -LiteralPath (Join-Path $buildRoot ('generated\'+$name)) -Destination $armRoot -Force
    }
    Copy-Item -LiteralPath (Join-Path $msklcRoot 'inc\kbd.h') -Destination $armRoot -Force
    Set-Location -LiteralPath $armRoot
    Invoke-Checked (Join-Path $msklcRoot 'bin\i386\rc.exe') @(('/i'+(Join-Path $msklcRoot 'inc')),'/fouzbkeys.res','uzbkeys.rc')
    Invoke-Checked (Join-Path $zigRoot 'zig.exe') @('cc','-target','aarch64-windows-gnu','-shared','-nostdlib','-ffreestanding','-fno-stack-protector','-fms-extensions','-Wno-invalid-token-paste','-O2','-isystem',(Join-Path $zigRoot 'lib\libc\include\any-windows-any'),'-Wl,--entry,DllMain','-o','uzbkeys.dll','uzbkeys.c',(Join-Path $projectRoot 'src\dll-entry.c'),'uzbkeys.def','uzbkeys.res')
    Set-Location -LiteralPath $projectRoot

    $uninstaller = Join-Path $buildRoot 'Uninstall.exe'
    $commonCsc = @('/nologo','/target:winexe','/platform:x86','/optimize+','/r:System.Windows.Forms.dll',('/win32manifest:'+(Join-Path $projectRoot 'installer\app.manifest')))
    Invoke-Checked $csc ($commonCsc + @(('/out:'+$uninstaller),(Join-Path $projectRoot 'installer\Bootstrapper.cs')))
    $products = @{
        x86=@('{0325C442-84EF-42EC-A531-BE53A1B3CD31}','{4F0FD64D-CE6D-4681-B5AA-4C28C61D8971}')
        x64=@('{D96A670B-71C2-4C92-827B-640E2C825C53}','{D4E781D3-6C5A-4191-8386-B01FCCB2E0A1}')
        arm64=@('{C65B72F6-06AA-43F1-9F8E-0D114E6EE842}','{5D4ACD27-6BB3-472D-85D2-F865908CE932}')
    }
    foreach ($architecture in 'x86','x64','arm64') {
        $wixArguments = @('build',(Join-Path $projectRoot 'installer\Package.wxs'),'-ext',$utilExtension,'-arch',$architecture,'-out',(Join-Path $distRoot ('UzbekKeys-'+$architecture+'.msi')),
            '-d',('ProductCode='+$products[$architecture][0]),'-d',('UpgradeCode='+$products[$architecture][1]),
            '-d',('NativeDll='+(Join-Path $buildRoot ($architecture+'\uzbkeys.dll'))),
            '-d',('Wow64Dll='+(Join-Path $buildRoot 'wow64\uzbkeys.dll')),
            '-d',('UninstallExe='+$uninstaller),'-d',('LicenseFile='+(Join-Path $projectRoot 'LICENSE')))
        Invoke-Checked $wix $wixArguments
    }
    $setupArguments = $commonCsc + @(('/out:'+(Join-Path $distRoot 'UzbekKeys-Setup.exe')),(Join-Path $projectRoot 'installer\Bootstrapper.cs'))
    foreach ($architecture in 'x86','x64','arm64') {
        $setupArguments += '/resource:'+(Join-Path $distRoot ('UzbekKeys-'+$architecture+'.msi'))+',UzbekKeys.'+$architecture+'.msi'
    }
    Invoke-Checked $csc $setupArguments
    Copy-Item -LiteralPath $uninstaller -Destination (Join-Path $distRoot 'Uninstall.exe') -Force
    Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination (Join-Path $distRoot 'LICENSE.txt') -Force
    Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination (Join-Path $distRoot 'README.md') -Force
    Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\assets\keyboard-layout.svg') -Destination (Join-Path $distRoot 'keyboard-layout.svg') -Force
    $installerTests = Join-Path $buildRoot 'InstallerTests.exe'
    Invoke-Checked $csc @('/nologo','/target:exe','/main:InstallerTests','/platform:x86','/r:System.Windows.Forms.dll',('/out:'+$installerTests),(Join-Path $projectRoot 'scripts\InstallerTests.cs'),(Join-Path $projectRoot 'installer\Bootstrapper.cs'))
    Invoke-Checked $installerTests @()
    Invoke-Checked 'python' @((Join-Path $PSScriptRoot 'test_layout.py'))
    $zipInputs = @('UzbekKeys-Setup.exe','Uninstall.exe','UzbekKeys-x86.msi','UzbekKeys-x64.msi','UzbekKeys-arm64.msi','LICENSE.txt','README.md','keyboard-layout.svg') | ForEach-Object { Join-Path $distRoot $_ }
    Compress-Archive -LiteralPath $zipInputs -DestinationPath (Join-Path $distRoot 'UzbekKeys-Windows.zip') -Force
    $hashInputs = @('UzbekKeys-Setup.exe','Uninstall.exe','UzbekKeys-x86.msi','UzbekKeys-x64.msi','UzbekKeys-arm64.msi','UzbekKeys-Windows.zip')
    $checksums = foreach ($name in $hashInputs) { ((Get-FileHash -LiteralPath (Join-Path $distRoot $name) -Algorithm SHA256).Hash.ToLowerInvariant())+'  '+$name }
    [IO.File]::WriteAllLines((Join-Path $distRoot 'SHA256SUMS.txt'),$checksums,[Text.UTF8Encoding]::new($false))
    Write-Host "Built unsigned UzbekKeys installers and ZIP in $distRoot"
}
finally { $env:TEMP=$priorTemp; $env:TMP=$priorTmp; Set-Location -LiteralPath $priorDirectory.Path }
