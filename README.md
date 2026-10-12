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

## 安卓支持

启动后先选择“苹果手机”或“安卓手机”；顶部平台菜单可切换。选择安卓后只显示安卓连接与控制菜单。首次点“准备 / 校验安卓组件”，使用官方 scrcpy 5.0.1 固定版本和 SHA256；组件、ADB 与许可证保存在本机 `outputs/iPhoneDesk/android/`，不上传 Git。

- USB：手机开启开发者选项与 USB 调试，接线后确认调试授权；刷新设备，选择已授权设备，再启动。
- Wi-Fi（Android 11+）：开启无线调试，使用配对码页面的地址和六位码配对；再用无线调试首页的连接地址连接。两个端口通常不同。
- 安卓采用左侧菜单、右侧嵌入手机画面；可以切换独立窗口。操作：左键点击/拖动、滚轮、键盘；右键返回、中键主页、Alt+S 多任务、F11 全屏、Ctrl+左键拖动缩放/旋转。无需配对蓝牙鼠标。
- 停止投屏或切换平台或关闭程序只停止它启动的 scrcpy，保留配对。unauthorized/offline/未知状态不会允许启动；再次连接可刷新设备后重试。

部分手机控制还需“USB 调试（安全设置）”；受保护内容可能无法显示。已实测小米 13 嵌入画面与滚轮控制；无线、旋转和全部键盘操作仍待进一步验收，不承诺所有厂商手势。iPhone 的 AirPlay 与 BLE 路径保持。

可直接启动 `iPhoneDesk.exe --android`。离线检查：`--android-self-test`；界面检查：`--android-ui-smoke`（不调用 ADB 或手机操作）。普通安卓工作台刷新会启动本机 ADB 服务，不安装系统驱动。
