# iPhoneDesk

Windows 上的 iPhone Wi-Fi AirPlay 投屏与电脑鼠标控制工具。

- WPF 单窗口显示投屏，支持竖屏/横屏分辨率配置。
- 采集 Windows 鼠标事件，支持有线鼠标、无线接收器、蓝牙鼠标和触控板。
- 电脑通过 BLE HID 将输入传给 iPhone；手机配对的是电脑。
- 提供独立重新连接按钮、真实报告订阅状态和本地诊断日志。

开发前必须阅读 [skills.md](skills.md)，它是唯一开发规范与知识库。源码位于 `outputs/iPhoneDesk/src`。

## 构建

需要 Windows、.NET 8 SDK 和桌面运行时。在仓库根目录执行：

```powershell
dotnet publish outputs/iPhoneDesk/src/iPhoneDesk.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o work/release
if ($LASTEXITCODE -ne 0) { throw '构建失败' }
Copy-Item outputs/iPhoneDesk/icon.ico work/release/
$test = Start-Process work/release/iPhoneDesk.exe -ArgumentList '--self-test' -Wait -PassThru
if ($test.ExitCode -ne 0) { throw '编码自检失败' }
```

正常退出已有 iPhoneDesk 实例后，将生成的 EXE/PDB 复制到 `outputs/iPhoneDesk/`，保留同目录图标和配置。

## 接收器与运行

第三方接收器不存入 Git。下载脚本固定官方来源与 SHA256：

```powershell
pwsh -NoProfile -File outputs/iPhoneDesk/下载接收器.ps1
Start-Process outputs/iPhoneDesk/iPhoneDesk.exe -ArgumentList '--airplay','--resume-bluetooth'
```

下载脚本中的旧版本受固定校验保护；不要绕过哈希或静默升级。接收器许可证随下载包保留。

电脑和 iPhone 连接同一 Wi-Fi，手机控制中心选择屏幕镜像。鼠标控制还需电脑支持 BLE 外设模式，在 iPhone 辅助触控的设备列表连接电脑。定位和点击报告均就绪才可控制。

照片显示使用竖屏分辨率，例如 `720x1280@60`。接收器参数保存在 `%APPDATA%/leapbtw/uxplay-windows/arguments.txt`，菜单可修改。仓库只保留当前维护源码，旧实验代码和旧优化脚本不发布；详见 skills.md。

## 检查

```powershell
pwsh -NoProfile -File outputs/iPhoneDesk/tools/检查规范.ps1 -RepositoryOnly
python outputs/iPhoneDesk/tools/检查鼠标回调.py
```

本机完整检查不加 `-RepositoryOnly`，还会检查未提交的运行产物和历史证据。运行令牌、日志、手机截图、备份、构建输出和 USB 研究组件均排除在 Git 之外。

## 已知限制

- 手机显示蓝牙已连接不代表报告已订阅；重试广播不保证每次恢复，可能仍需手机端重新建立连接。
- 已在当前设备确认底部上拖可用，不能保证所有设备、所有系统手势或多任务手势均可靠。
- 照片 App 可向 AirPlay 外屏输出不同内容，菜单和属性可能只显示在手机。
- USB 功能已取消。

本项目使用 MIT 许可证；相关互操作参考和第三方接收器各自的许可证必须保留。
