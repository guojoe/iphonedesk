param([ValidateSet('Smooth','Stable','Original')][string]$Mode='Smooth')
$ErrorActionPreference='Stop'
$taskReceiver=Join-Path $PSScriptRoot 'receiver\uxplay-windows.exe'
$taskConfig=Join-Path $env:APPDATA 'leapbtw\uxplay-windows\arguments.txt'
$taskRegistry='HKCU:\Software\leapbtw\uxplay-windows'
$taskBackup=Join-Path $PSScriptRoot '投屏原始配置.json'
if (!(Test-Path -LiteralPath $taskReceiver)) {throw '找不到投屏接收器。'}
if (!(Test-Path -LiteralPath $taskConfig)) {throw '请先运行一次投屏接收器。'}
if (!(Test-Path -LiteralPath $taskBackup)) {
    $taskOldKey=Get-Item -LiteralPath $taskRegistry -ErrorAction SilentlyContinue
    $taskOldRenderer=if($taskOldKey) {$taskOldKey.GetValue('renderer_mode',$null)} else {$null}
    @{arguments=(Get-Content -LiteralPath $taskConfig -Raw);renderer=$taskOldRenderer} |
        ConvertTo-Json | Set-Content -LiteralPath $taskBackup -Encoding UTF8
}
$taskOriginal=Get-Content -LiteralPath $taskBackup -Raw | ConvertFrom-Json
# Keep unrelated user options (name, PIN/password, ports, etc.). Only replace
# resolution/FPS/decoder/converter/sync controls in our performance profiles.
$taskTokens=@($taskOriginal.arguments.Trim() -split '\s+')
$taskKeep=[System.Collections.Generic.List[string]]::new()
for($taskIndex=0;$taskIndex -lt $taskTokens.Length;$taskIndex++) {
    if($taskTokens[$taskIndex] -in @('-s','-fps','-vd','-vc')) {$taskIndex++;continue}
    if($taskTokens[$taskIndex] -eq '-vsync') {
        if($taskIndex+1 -lt $taskTokens.Length -and $taskTokens[$taskIndex+1] -notmatch '^-') {$taskIndex++}
        continue
    }
    if($taskTokens[$taskIndex] -eq '-avdec') {continue}
    $taskKeep.Add($taskTokens[$taskIndex])
}
switch($Mode) {
    'Smooth' {$taskNew=($taskKeep -join ' ')+' -s 1280x720@60 -fps 60 -vd d3d11h264dec -vc d3d11convert -vsync no'}
    'Stable' {$taskNew=($taskKeep -join ' ')+' -s 1280x720@30 -fps 30 -vd d3d11h264dec -vc d3d11convert -vsync no'}
    'Original' {$taskNew=$taskOriginal.arguments}
}
# Stop only this bundle's receiver and its bundled discovery helper.
foreach($taskProcess in @(Get-Process uxplay-windows,uxplay-bluetooth-beacon -ErrorAction SilentlyContinue)) {
    $taskKnownPaths=@($taskReceiver,(Join-Path $PSScriptRoot 'receiver\uxplay-bluetooth-beacon.exe'))
    if($taskProcess.Path -in $taskKnownPaths) {Stop-Process -Id $taskProcess.Id}
}
[System.IO.File]::WriteAllText($taskConfig,$taskNew,[System.Text.UTF8Encoding]::new($false))
New-Item -Path $taskRegistry -Force | Out-Null
if($Mode -eq 'Original') {
    if($null -eq $taskOriginal.renderer) {
        if((Get-Item -LiteralPath $taskRegistry).GetValueNames() -contains 'renderer_mode') {
            Remove-ItemProperty -LiteralPath $taskRegistry -Name 'renderer_mode'
        }
    }
    else {Set-ItemProperty -LiteralPath $taskRegistry -Name 'renderer_mode' -Value $taskOriginal.renderer}
} else {Set-ItemProperty -LiteralPath $taskRegistry -Name 'renderer_mode' -Value 'd3d11'}
Start-Process -FilePath $taskReceiver -WorkingDirectory (Split-Path $taskReceiver)
Write-Output "Applied $Mode : $taskNew"
Write-Output '接收器已重启。请在 iPhone 控制中心重新连接屏幕镜像。'
