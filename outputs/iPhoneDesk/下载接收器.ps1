$ErrorActionPreference = 'Stop'
$taskTemp = Join-Path $env:TEMP ('iphonedesk-' + [Guid]::NewGuid().ToString() + '.zip')
try {
    Invoke-WebRequest 'https://github.com/leapbtw/uxplay-windows/releases/download/2.0.0.1736/uxplay-windows.zip' -OutFile $taskTemp
    $taskHash = (Get-FileHash -LiteralPath $taskTemp -Algorithm SHA256).Hash
    if ($taskHash -ne '9D3A51C15FC9DB857351195E7EB7BBB21700D9AE25D936A54BCF8536B62CCA18') { throw '接收器校验失败。' }
    Expand-Archive -LiteralPath $taskTemp -DestinationPath (Join-Path $PSScriptRoot 'receiver') -Force
    Write-Host '接收器已准备好。'
} finally {
    if (Test-Path -LiteralPath $taskTemp) { Remove-Item -LiteralPath $taskTemp }
}
