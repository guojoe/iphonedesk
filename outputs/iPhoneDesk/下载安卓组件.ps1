$ErrorActionPreference='Stop'
[Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
# Pinned official Windows x64 release and digest from Genymobile/scrcpy doc/windows.md.
$taskVersion='5.0.1'
$taskDigest='b12a2c4ee8be317422451fc7dcf8ee20a71b5ea7ef9ad73ddd825a25316227a5'
$taskUri="https://github.com/Genymobile/scrcpy/releases/download/v$taskVersion/scrcpy-win64-v$taskVersion.zip"
$taskRoot=Join-Path $PSScriptRoot 'android'
$taskTarget=Join-Path $taskRoot "scrcpy-win64-v$taskVersion"
$taskStage=Join-Path $taskRoot ('download-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskStage -Force | Out-Null
$taskZip=Join-Path $taskStage 'official.zip'
Invoke-WebRequest -Uri $taskUri -OutFile $taskZip -UseBasicParsing
if((Get-FileHash -LiteralPath $taskZip -Algorithm SHA256).Hash -ne $taskDigest){throw 'scrcpy 校验失败，禁止安装。下载文件保留供排查。'}
Expand-Archive -LiteralPath $taskZip -DestinationPath $taskStage
$taskExtracted=Join-Path $taskStage "scrcpy-win64-v$taskVersion"
foreach($taskRequired in @('scrcpy.exe','adb.exe','scrcpy-server')) {
    if(!(Test-Path -LiteralPath (Join-Path $taskExtracted $taskRequired) -PathType Leaf)){throw "官方包缺少 $taskRequired"}
}
if(Test-Path -LiteralPath $taskTarget) {
    # No silent upgrades or replacement of existing user files: compare with authenticated archive.
    foreach($taskFile in Get-ChildItem -LiteralPath $taskExtracted -Recurse -File) {
        $taskRelative=$taskFile.FullName.Substring($taskExtracted.Length+1)
        $taskExisting=Join-Path $taskTarget $taskRelative
        if(!(Test-Path -LiteralPath $taskExisting -PathType Leaf) -or
           (Get-FileHash -LiteralPath $taskExisting).Hash -ne (Get-FileHash -LiteralPath $taskFile.FullName).Hash) {
            throw "现有安卓组件不完整或不同：$taskRelative。未覆盖；已校验下载包保留于 $taskStage"
        }
    }
    Write-Output 'PASS: installed files match authenticated official archive.'
} else {
    Move-Item -LiteralPath $taskExtracted -Destination $taskTarget
    Write-Output 'PASS: official scrcpy installed locally; licenses retained. No system driver installed.'
}
# Preserve verified archive as provenance; do not commit binaries or local diagnostics.
