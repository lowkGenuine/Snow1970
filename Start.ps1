param([switch]$BuildOnly)
$ErrorActionPreference = 'Stop'
$base = $PSScriptRoot
$sources = @('Snow1970.cs', 'Snow1970.Collision.cs', 'Snow1970.Excavator.cs', 'Snow1970.Decorations.cs', 'Snow1970.Composition.cs', 'Snow1970.UI.cs', 'Snow1970.Theme.cs', 'Snow1970.Text.cs', 'Snow1970.Checks.cs') | ForEach-Object { Join-Path $base $_ }
$buildLock = $null
$locked = $false
try {
    # Every source revision has its own executable. A running .exe is never overwritten.
    $assets = @('Snowman-01.png','Snowman-02.png','Snowman-03.png','Fir-01.png','Fir-02.png','Excavator-Santa.png') | ForEach-Object { Join-Path (Join-Path $base 'Assets') $_ }
    $inputs = @($sources) + @($assets) + @((Join-Path $base 'app.manifest'), (Join-Path $base 'Snow1970.exe.config'))
    $hashText = 'Snow1970-build-v2:' + (($inputs | ForEach-Object { (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash }) -join ':')
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $hash = ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($hashText)))).Replace('-', '').Substring(0, 20) }
    finally { $sha.Dispose() }
    $buildDir = Join-Path (Join-Path $base 'bin') $hash
    $exe = Join-Path $buildDir 'Snow1970.exe'
    $buildLock = New-Object System.Threading.Mutex($false, ('Local\Snow1970.Build.' + $hash))
    $locked = $buildLock.WaitOne(15000)
    if (-not $locked) { throw 'Another build is still running. Try starting again in a moment.' }
    if (-not (Test-Path -LiteralPath $exe)) {
        New-Item -ItemType Directory -Path $buildDir -Force | Out-Null
        $framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
        if (-not (Test-Path -LiteralPath $framework)) { $framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319' }
        $compiler = Join-Path $framework 'csc.exe'
        if (-not (Test-Path -LiteralPath $compiler)) { throw 'The Windows .NET Framework compiler was not found.' }
        $references = @('System.dll', 'System.Core.dll', 'System.Drawing.dll', 'System.Windows.Forms.dll', 'System.Xaml.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
        $references += @('WindowsBase.dll', 'PresentationCore.dll', 'PresentationFramework.dll', 'UIAutomationClient.dll', 'UIAutomationTypes.dll') | ForEach-Object { '/reference:' + (Join-Path (Join-Path $framework 'WPF') $_) }
        $temporaryExe = Join-Path $buildDir ('Snow1970.build.' + [Guid]::NewGuid().ToString('N') + '.exe')
        $arguments = @('/nologo', '/target:winexe', '/platform:anycpu', '/optimize+', '/codepage:65001', ('/out:' + $temporaryExe), ('/win32manifest:' + (Join-Path $base 'app.manifest')))
        $arguments += $references
        $arguments += $sources
        $resourceNames = @('Snowman1','Snowman2','Snowman3','Fir1','Fir2','ExcavatorSanta')
        for ($assetIndex = 0; $assetIndex -lt $assets.Length; $assetIndex++) { $arguments += ('/resource:' + $assets[$assetIndex] + ',Snow1970.' + $resourceNames[$assetIndex]) }
        $compilerOutput = @(& $compiler @arguments 2>&1)
        $result = $LASTEXITCODE
        $log = Join-Path $base 'Build.log'
        $compilerOutput | Out-File -LiteralPath $log -Encoding utf8
        if ($result -ne 0) { throw ('Compilation failed (' + $result + ').' + [Environment]::NewLine + ($compilerOutput -join [Environment]::NewLine) + [Environment]::NewLine + 'Full report: ' + $log) }
        Copy-Item -LiteralPath (Join-Path $base 'Snow1970.exe.config') -Destination (Join-Path $buildDir 'Snow1970.exe.config')
        Move-Item -LiteralPath $temporaryExe -Destination $exe
    }
    if ($BuildOnly) { Write-Output $exe }
    else {
        # Delegate to Explorer so the snow survives the launcher closing.
        try {
            $shellApp = New-Object -ComObject Shell.Application
            $shellApp.ShellExecute($exe, ('--replace --data-dir "' + $base + '"'), $base, 'open', 1)
            [void][Runtime.InteropServices.Marshal]::ReleaseComObject($shellApp)
        }
        catch { Start-Process -FilePath $exe -ArgumentList @('--replace', '--data-dir', ('"' + $base + '"')) -WorkingDirectory $base -WindowStyle Hidden }
    }
}
catch {
    if ($BuildOnly) { throw }
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show($_.Exception.Message, 'Snow1970') | Out-Null
    exit 1
}
finally {
    if ($locked -and $buildLock) { $buildLock.ReleaseMutex() }
    if ($buildLock) { $buildLock.Dispose() }
}
