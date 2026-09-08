$ErrorActionPreference = 'Stop'
$clientDirectory = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../OtakuQuest/otakuquest.client'))
$vitePath = Join-Path $clientDirectory 'node_modules/vite/bin/vite.js'
$nodePath = (Get-Command node).Source
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = $listener.LocalEndpoint.Port
$listener.Stop()
$process = Start-Process -FilePath $nodePath -ArgumentList @(('"' + $vitePath + '"'), '--host', '127.0.0.1', '--port', $port, '--strictPort') -WorkingDirectory $clientDirectory -WindowStyle Hidden -PassThru
try {
    $ready = $false
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        if ($process.HasExited) { throw 'Vite exited before startup.' }
        $html = & curl.exe --silent --insecure --fail --max-time 2 "https://127.0.0.1:$port/"
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Milliseconds 500
    }
    if (-not $ready -or ($html -join "`n") -notmatch 'id="root"') { throw 'React index did not load over HTTPS.' }
    Write-Output 'PASS: Vite HTTPS development server serves React index'
    $module = & curl.exe --silent --insecure --fail --max-time 10 "https://127.0.0.1:$port/src/main.tsx"
    if ($LASTEXITCODE -ne 0 -or ($module -join "`n") -notmatch 'createRoot') { throw 'Vite could not transform main.tsx.' }
    Write-Output 'PASS: Vite transforms and serves main.tsx'
} finally {
    if (-not $process.HasExited) { Stop-Process -Id $process.Id }
    $process.Dispose()
}
