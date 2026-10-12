param([string]$ProjectRoot=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path,[switch]$RepositoryOnly)
$ErrorActionPreference='Stop'
$taskDocPath=Join-Path $ProjectRoot 'skills.md'
$taskDoc=Get-Content -LiteralPath $taskDocPath -Raw -Encoding utf8
$taskMatch=[regex]::Match($taskDoc,'(?s)<!-- DOC_CODE_ASSERTIONS -->\s*```json\s*(.*?)\s*```\s*<!-- END_DOC_CODE_ASSERTIONS -->')
if (!$taskMatch.Success) { throw 'skills.md 缺少可核对断言' }
$taskManifest=$taskMatch.Groups[1].Value | ConvertFrom-Json
$taskFailures=[System.Collections.Generic.List[string]]::new()
$taskSkipped=0
if($null -eq $taskManifest.PSObject.Properties['localOnlyFiles']){throw '规范缺少明确的本机文件范围'}
foreach($taskAssertion in $taskManifest.assertions) {
    if($RepositoryOnly -and $taskAssertion.file -in $taskManifest.localOnlyFiles){$taskSkipped++;continue}
    $taskFile=Join-Path $ProjectRoot $taskAssertion.file
    if (!(Test-Path -LiteralPath $taskFile -PathType Leaf)) { $taskFailures.Add('缺少文件：'+$taskAssertion.file);continue }
    $taskSource=Get-Content -LiteralPath $taskFile -Raw -Encoding utf8
    $taskFound=$taskSource.Contains([string]$taskAssertion.feature,[System.StringComparison]::Ordinal)
    if ($taskFound -eq [bool]$taskAssertion.absent) { $taskFailures.Add('说法/代码冲突：'+$taskAssertion.claim+'；特征：'+$taskAssertion.feature) }
}
foreach($taskSnapshot in $taskManifest.snapshots) {
    if($RepositoryOnly -and $taskSnapshot.file -in $taskManifest.localOnlyFiles){$taskSkipped++;continue}
    $taskFile=Join-Path $ProjectRoot $taskSnapshot.file
    if (!(Test-Path -LiteralPath $taskFile)) { $taskFailures.Add('快照文件丢失：'+$taskSnapshot.file);continue }
    if ((Get-FileHash -LiteralPath $taskFile -Algorithm SHA256).Hash -ne $taskSnapshot.sha256) { $taskFailures.Add('代码已改变，请先回写正文和证据再更新快照：'+$taskSnapshot.file) }
}
$taskActual=@(Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'outputs/iPhoneDesk/src') -File | Where-Object Extension -in '.cs','.csproj')
$taskActual+=@(Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'outputs/iPhoneDesk') -File -Filter '*.ps1')
$taskActual+=@(Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'outputs/iPhoneDesk/tools') -File -Filter '*.ps1')
foreach($taskFile in $taskActual) {
    $taskRelative=[IO.Path]::GetRelativePath($ProjectRoot,$taskFile.FullName).Replace('\','/')
    if($RepositoryOnly -and $taskRelative -in $taskManifest.localOnlyFiles){continue}
    if ($taskRelative -notin $taskManifest.snapshots.file) { $taskFailures.Add('维护文件没有登记快照：'+$taskRelative) }
}
$taskReferences=[regex]::Matches($taskDoc,'`(?<file>[^`\r\n]+):(?<line>\d+)`（锚点 `(?<feature>[^`\r\n]+)`）')
foreach($taskReference in $taskReferences) {
    if($RepositoryOnly -and $taskReference.Groups['file'].Value -in $taskManifest.localOnlyFiles){$taskSkipped++;continue}
    $taskFile=Join-Path $ProjectRoot $taskReference.Groups['file'].Value
    $taskLines=@(Get-Content -LiteralPath $taskFile -Encoding utf8)
    $taskLine=[int]$taskReference.Groups['line'].Value
    if ($taskLine -gt $taskLines.Count -or !$taskLines[$taskLine-1].Contains($taskReference.Groups['feature'].Value)) { $taskFailures.Add('证据行号漂移：'+$taskReference.Groups['file'].Value+':'+$taskLine) }
}
if($taskFailures.Count) { $taskFailures | ForEach-Object { Write-Output ('FAIL '+$_) };throw ('文档一致性检查失败，共 '+$taskFailures.Count+' 项') }
if($RepositoryOnly){Write-Output ('PASS：仓库源码检查；显式跳过 '+$taskSkipped+' 个本机产物/历史证据项');exit 0}
Write-Output ('PASS：'+$taskManifest.assertions.Count+' 条说法/代码断言，'+$taskManifest.snapshots.Count+' 个代码指纹，'+$taskReferences.Count+' 个证据行号。')
