# iPhoneDesk — 唯一有效开发规范与知识库

修订日期：2026-10-12（Asia/Shanghai）。适用项目根目录：`C:/guojoe/iphonedesk`。

## 当前有效范围（2026-10-12，优先于下方历史记录）
当前新增安卓ADB/scrcpy支持及一级平台选择菜单（§34）、与iPhone一致的左菜单/右手机嵌入布局和iOS样式（§35）；启动先选苹果/安卓，再进入对应工作台。iPhone USB取消范围不包含本次明确授权的安卓ADB连接。
当前版本包含独立重新连接按钮（§30）和图片显示修复（§31）：默认接收器720x1280@60竖屏，照片保持比例占满显示区宽度，上下比例黑边不自动裁图；分辨率菜单区分竖/横方向。当前源码、EXE、外部接收器参数配套冻结于§32新备份，§29上拖可用旧备份仍保留。
当前运行使用16:32 bottom-anchor行为基线合入诊断日志版（§23），正式源码已撤销§17–19研究行为并与真实回滚DLL分流对照通过，源码与本次发布对应。配置按§26保持普通垂直拖动2，底部独立按钮1/anchorY=32482/32767；禁止全局回退垂直1。2026-10-12用户确认当前版本“向上拖动都可以了”，作为此设备当前实测成功基线（§29）；不外推多任务卡片或所有iPhone手势均稳定。§22/25–27失败记录是之前状态，不能覆盖本次用户确认。每次启动自动写logs/gesture-时间-session.jsonl，新日志已生产生效；§20–22关于“候选未上线/源码不同”的说法为当时状态，被§23取代。
用户已取消iPhone USB功能：iPhone生产路径仅Wi-Fi AirPlay投屏和BLE鼠标；2026-10-12授权新增的安卓ADB USB/Wi-Fi模块见§34，不恢复旧iPhone USB驱动。USB菜单、初始化、采集启动、截图及VideoHost分支均移除；UsbMirror.cs不参与编译。USB相关旧章节、索引和断言是历史研究记录，不得恢复为当前功能。用户还要求删除USB代码和驱动，但批量删除遭自动审批拒绝（blocked by policy，无进一步理由），目前源文件/包尚未物理删除；UsbDk和libusb0卸载结果仍待核验。保留Apple官方驱动及receiver，禁止扩大成删除所有USB设备驱动。鼠标服务复用修复保留，报告未订阅故障仍待验证。
## 0. 权威与动手前检查

本文件是本项目唯一有效的开发规范和知识库。与它冲突的旧代码、注释、旧文档和旧说法以本文件为准；“现状”记录不构成对旧实现的豁免。用户新指令优先，接受后立即更新本文件。AGENTS.md 仅作为入口，不增加任何独立规范。其他 Markdown 只作为用户操作说明或历史证据，不作为开发依据。

修改源码、配置、构建或发布脚本前，必须先读本文件对应章节：UI→§3/§4/§5；蓝牙/拖动→§2/§4/§5；配置→§1/§4/§6；接口→§2/§4/§6；发布→§7/§8。不能出错后才查。先记录任务、读过的章节、涉及模块和验证边界，再编辑。

## 1. 代码生成硬性要求

1. 一律根据元数据、设备能力和配置自动适配，不许写死业务编号、设备型号、设备名或业务名单。蓝牙标准 UUID、Win32 消息码属于协议标识，必须注明来源和编码约束，不能把标准常量当业务编号，也不能凭直觉修改。
2. 所有配置写法都要适配：缺失、空、重复、顺序变化、中文、空格、引号、未知字段、旧版本及未来版本。先查真实解析器和样本，保留原始内容与未知参数；不许只支持常见的几种。
3. 尚未适配的配置必须“只读展示 + 原值回传”，明确说明未支持，不能显示为空值后覆盖原数据。不得把解析失败或权限失败当作配置不存在。配置写入必须先校验、保留原值、备份并原子替换；不能先覆盖再判断重启是否成功。
4. 不许用“够用就好”的阈值控制功能，如超过 N 条才显示搜索。协议长度、安全配额、资源上限与已测的时序约束可以存在，但需说明依据、配置来源及验证，不能冒充体验优化结论。
5. 用到哪个字段就查哪个字段。未知/缺失/类型错误必须显式处理，不依赖“读不到会返回空”的默认行为。API 缺失必填坐标应拒绝请求，不得代入 0 点击左上角。
6. 输入变更优先保持按下/移动/释放顺序。不得用左右键整体互换来修复拖动。异步动作须可取消，失败或退出均释放手机按键。

## 2. 安全基线与禁区

- 本机控制接口只允许 loopback，随机端口，每次启动随机令牌，Bearer 认证，拒绝 Origin；绝不能改为 `0.0.0.0`、公开端口、无认证或固定共享令牌。
- 不得输出、提交或公开 `control-interface.json` 的 token。截图、用户设备信息和日志可能含隐私，不能上传或作为公开测试样本。
- 添加接口不等于授权任意操作手机。只有用户明确要求的具体动作才能执行；付款、发送消息、删除、发布等操作仍需对应授权。只读验证不得偷偷发送点击。
- F9/暂停控制、取消检查、退出释放、输入队列顺序及互斥锁属于安全路径，绝不能为了速度删除或绕过。
- 只停止已核对绝对路径属于本软件包的接收器/发现进程；绝不能按进程名批量结束其他程序，也不能改用户现有鼠标/蓝牙设备映射。
- 不得擅自移除用户配对、卸载 Bonjour、修改全局蓝牙/防火墙/网络设置。
- 不得删除或改写 `receiver/` 中第三方二进制、DLL、LICENSE；升级必须验证来源、版本、校验和、兼容性并保留回退。不得去掉 MIT/第三方许可证。
- 不得把 GATT 订阅、Notify Success、API sent 或单元测试通过写成“手机已经执行成功”。
- 不得恢复 `protocolConfirmed` 等额外确认门槛；不得把 F8 描述成投屏快捷键；当前没有可用横屏选择器，不得沿用旧说明。
- 不能递归删除项目外目录，不能把临时构建当唯一源码，不能用应用程序覆盖来绕过文件锁。

## 3. 接手前必读

### 3.1 环境事实

- 源码：`outputs/iPhoneDesk/src/`；项目：`iPhoneDesk.csproj`。桌面程序为 .NET 8 WPF，Windows WinRT GATT + Win32 原生窗口嵌入。
- 运行实例：当前这台 Windows 用户电脑，本目录的 `outputs/iPhoneDesk/iPhoneDesk.exe`；接收器为同包 `receiver/uxplay-windows.exe`。PID 会变化，不能写死。2026-10-08 核对过路径，不能据此推定以后仍在运行。
- 构建环境已测：.NET SDK 8.0.303，桌面运行时 8.0.7，Windows SDK NuGet 10.0.19041.31；Python 可用。以后接手先运行 `dotnet --info`，不能默认为安装不变。
- 数据库：无数据库，无连接串、迁移或服务端业务表；持久化使用文件和接收器注册表配置。
- 配置：`%APPDATA%/leapbtw/uxplay-windows/arguments.txt`；接收器注册表 `HKCU:/Software/leapbtw/uxplay-windows`，`renderer_mode`。这不是源码目录。
- 日志：`outputs/iPhoneDesk/运行日志.txt`；编码/状态测试 `self-test.txt`；蓝牙创建探测 `bluetooth-probe.txt`；嵌入测试 `embed-smoke.txt`；WPF 界面预览 `界面预览.png`。
- 本机接口地址/令牌文件：`outputs/iPhoneDesk/control-interface.json`；随机端口；重启必须重新读取。
- 临时文件与历史编译：`work/iphonedesk/v2`…`v10`，不代表当前生产版本；研究源码在 `work/iphonedesk/`。未来使用 `work/iphonedesk/<任务名>/`，不要延续容易混淆的最新版说法。
- 用户交付：仅 `outputs/`；不得把用户交付链接指向 work。根目录 skills.md 是项目治理入口，供开发者阅读，不是软件运行必需文件。
- 此根目录已在2026-10-12初始化Git，远程origin为https://github.com/guojoe/iphonedesk.git，分支main；推送以远程提交SHA核对为准。令牌/日志/截图/第三方大包/obj/bin/work均不得提交。

### 3.2 文件与关键函数索引

| 文件 | 入口及用途 | 改动前读 |
|---|---|---|
| src/Program.cs | Main；Desk 生命周期；OpenReceiver；QueueInput/PumpInput/SendAbsolute；ReleaseCapture | §2、§4、§7 |
| src/DeskLayout.cs | BuildLayout；ReadMirrorName/ApplyMirrorName；左侧菜单及待机页 | §1、§4、§6 |
| src/VideoHost.cs | BuildWindowCore/TryAttach/ResizeVideo/DestroyWindowCore；GPU 窗口、圆角、还原 | §2、§4 |
| src/MouseCapture.cs | OnMouse/OverVideo/FitContent/RouteHover/ButtonState；F8/F9与物理事件 | §2、§4、§5 |
| src/HidMouse.cs | Start/ReportMap/Report/AbsoluteReport/Send/SendAt；GATT与按键编码 | §2、§4、§5 |
| src/MirrorResolution.cs | Valid/Current/Change/AddResolutionMenu；保真分辨率编辑、原子保存、接收器重启 | §1、§2、§6 |
| src/LeftGesture.cs | Route/Reset/Load；左点与左拖识别，读取动作配置 | §1、§2、§5 |
| src/native/QtUsbTransport.cpp | USB身份匹配、有界打开恢复、原生ERROR日志 | §2、§6.7 |
| src/native/DeviceManager.cpp | USB物理parent补充识别，保留未知配对字段 | §1、§6.7 |
| src/native/AppleUsbDiscovery.h/.cpp | SetupAPI真实实例身份；上游移动设备筛选 | §2、§6.7 |
| src/native/build-core-recovery.ps1 | 复制维护源到完整树、编译与两项原生测试 | §7、§8 |
| src/UsbMirror.cs | UsbCore ABI；EnableUsb/MonitorUsb/StopUsb/CaptureUsbFrame；USB设备列表、采集及取消 | §2、§6.5 |
| src/LocalControl.cs | StartLocalControl/HandleLocal/RunRemote/CapturePhone；本机接口 | §2、§4、§6 |
| src/iPhoneDesk.csproj | 目标框架、WPF、SDK、图标 | §3、§7 |
| 优化投屏.ps1 | 接收器参数和渲染器档位；现有配置覆盖风险 | §1、§6 |
| 下载接收器.ps1 | 第三方获取、校验与解压 | §2、§7 |
| tools/检查规范.ps1 | 从本文件读取断言、代码指纹、行号索引 | §8 |

### 3.3 页面/模块调试入口

- 主窗口：启动 exe；左侧连接、投屏名称、鼠标与手势、连接详情；右侧手机画面。日志在“连接详情→查看连接日志”。名称“保存并应用”会中断投屏并重启接收器。
- UI：`--ui-smoke` 自动关闭；`--render-ui` 保存 WPF 预览并关闭。RenderTargetBitmap 不包含原生 GPU 子窗口，不能用黑色预览断言实际投屏失败。
- 编码与边界：`--self-test` 不开 UI，不发手机操作；退出码0才通过。
- 鼠标钩子：`--capture-smoke` 注册快捷键/安装释放钩子；测试前关闭主程序避免 F8/F9 冲突，会写 `capture-smoke.txt`。
- 蓝牙：`--probe` 创建并广播 HID，约2.5秒后停止；不能在正常应用广播期间运行，不能当作纯只读检查；不能证明手机执行。
- USB：左侧“启用 USB 自动连接”及“USB 接入说明”；--usb-probe只读探测写usb-probe.json，缺少过滤驱动时不会尝试采集；详见§6.5。
- 接收器：主窗口“连接屏幕镜像”；iPhone 控制中心连接；窗口通过 TryAttach 嵌入。现有同路径实例会复用，不重复启动。
- 本机 API：GET /status 只读；GET /screenshot 抓原生窗口；POST /command 才发操作。具体调试脚本见§8，接口 schema 见§6。

### 3.4 目录说明与清理边界

以下路径均相对项目根目录 `C:/guojoe/iphonedesk`。正式开发只修改 `outputs/iPhoneDesk/src/` 中维护的源码；`work/` 虽有 `.cs`、`.cpp`、`.swift` 文件，也不是正式应用源码目录。

```text
iphonedesk/
├── skills.md                       唯一开发规范与知识库
├── AGENTS.md                       规范发现入口
├── outputs/
│   └── iPhoneDesk/                  用户交付包与当前运行目录
│       ├── src/                    正式应用源码和项目文件
│       │   ├── *.cs                WPF、鼠标、蓝牙、接口实现
│       │   ├── iPhoneDesk.csproj    项目及编译设置
│       │   ├── app.manifest        应用清单
│       │   ├── bin/                可重新构建的编译输出
│       │   └── obj/                可重新生成的中间文件
│       ├── usb/                    USB核心、独立驱动管理器、许可与对应源码
│       ├── iPhoneMirror.UsbConfigurationSwitch.exe USB配置切换助手（上游要求exe同目录）
│       ├── receiver/               第三方接收器、依赖和许可证
│       ├── tools/                  维护检查脚本
│       ├── iPhoneDesk.exe          当前正式运行程序
│       ├── iPhoneDesk.pdb          对应版本的调试符号
│       ├── icon.* / icon-256.png    图标资产
│       ├── *.ps1                   下载、优化等用户脚本
│       ├── *.md                    用户说明、历史验证和规范入口
│       ├── 运行日志.txt             运行诊断记录及已引用证据
│       ├── control-interface.json  本机接口地址与令牌，禁止公开
│       └── 其他 JSON/TXT/PNG        配置备份、探测结果及界面预览
└── work/
    ├── iphonedesk/                 临时构建、实验、研究源码
    │   ├── v2/…v10/               历史发布产物，不是正式源码
    │   ├── handoff-check/          接手验证构建产物
    │   ├── migration-check/        迁移验证构建产物
    │   ├── mainwindow.cpp          第三方参数解析研究证据
    │   ├── uxplay.cpp / video_renderer.c  接收器研究源码
    │   ├── ref-*.swift             HID 兼容性参考实现
    │   └── 其余脚本/素材/压缩包    一次性实验和辅助生成资料
    └── governance/                规范、迁移及负向检查证据
        ├── create-doc.py          一次性建档草稿，禁止重新覆盖规范
        ├── migration-manifest.json 迁移前文件清单与校验和
        ├── negative/              故意含错误断言的测试副本
        └── negative-result.txt    检查正确失败的原始输出
```

| 路径/文件 | 是否参与当前运行或正式开发 | 保留与清理规则 |
|---|---|---|
| `skills.md`、`AGENTS.md` | 开发依据及入口 | 必须保留；唯一规范不得被草稿覆盖 |
| `outputs/iPhoneDesk/src/` 的维护文件 | 正式源码；由 csproj 编译 | 必须保留，不从历史 vN 拷回覆盖 |
| `src/bin/`、`src/obj/` | 构建输出和中间缓存 | 不作为源码；停止相关构建后可清理并重新构建 |
| `iPhoneDesk.exe`、对应 `pdb` | 当前运行程序和诊断符号 | 保留；发布替换应配套备份、停程序、检查版本 |
| `receiver/`、LICENSE、第三方许可证 | 投屏运行依赖及许可 | 禁止随意删改；遵守§2升级流程 |
| `tools/`、包内 `.ps1`、图标资产 | 维护、构建和用户操作所需 | 保留；维护脚本改动同步规范断言与快照 |
| `运行日志.txt`、验证文本/截图 | 部分内容已被规范引用 | 未归档和修正文档证据前不能清空；可能含隐私 |
| `control-interface.json` | 本次运行的访问地址/令牌 | 不公开、不提交；程序重启重建，不要在运行中清理或当作永久配置 |
| `mouse-actions.json` | 鼠标动作配置；dragButtonMask来自已验证手机动作 | 不覆盖已有配置；未知或损坏写法只读原值提示，保留原文件 |
| `投屏原始配置.json` | 优化脚本的回退依据 | 保留，清理前先确认是否仍需恢复原配置 |
| `坐标协议已确认.txt` | 旧版本遗留，不再是就绪条件 | 当前源码不使用；后续清理可移除，本次仅补文档未删除 |
| `work/iphonedesk/v*/`、`handoff-check/`、`migration-check/` | 历史或验证构建，可重新生成 | 确认没有运行实例、不是唯一回退/证据后可选择清理；规范若引用其结果先归档 |
| `work/iphonedesk/mainwindow.cpp`、参考源码和实验脚本 | 非正式源码，但支持实测与复现 | 规范引用的文件必须保留或迁到研究证据目录，并同步路径、行号与检查 |
| `work/governance/` | 本次治理和迁移的审计证据 | 当前保留；negative副本不是真规范，不能据其错误断言开发 |

清理流程：先查本文中的路径引用及实际运行实例，区分可再生构建与不可替代证据；明确要清理的绝对路径，确认处于本项目内，再按§2执行。不得把 `work` 整体当作垃圾目录直接删除。接收器实际运行配置仍位于用户 `%APPDATA%` 和注册表，不在上图中；迁移项目不会自动迁移用户级配置。

本节按2026-10-08实际目录核对新增。本次仅补充目录说明，未移动、删除文件，未修改源码或运行程序；下一次新增、迁移或清理目录必须同步本节。

## 4. 平台/框架实测结论（⚠）

以下每条按“现象 → 实测数据/结构 → 反直觉的坑 → 正确做法 → 来源”记录。协议设计、外部源码和本机测试是不同证据等级，不能互相替代。

### ⚠ F01 投屏已连接但鼠标不能控制

现象：投屏已连接但鼠标不能控制。

实测的真实结构/数据：用户先看到画面，后才确认点击可用；日志16:47:51广播Started、16:48:25镜像窗口1个但报告仍未订阅。

反直觉的坑：Wi-Fi画面与蓝牙输入是两条独立通道；AirPlay画面不代表具备输入控制。

正确做法：分别检查 mirrorConnected 与 mouseConnected，不用投屏状态放行输入。

实测来源：`outputs/iPhoneDesk/src/Program.cs:277`（锚点 `mirrorState.Text=`）；`outputs/iPhoneDesk/src/Program.cs:181`（锚点 `inputState.Text=`）；`outputs/iPhoneDesk/运行日志.txt:107`（锚点 `2026-10-08T16:48:25`）。

### ⚠ F02 手机已配对却定位/按键均为0

现象：手机已配对却定位/按键均为0。

实测的真实结构/数据：旧v5需要额外确认文件；v6发现不存在；当前ClientReady只取AbsoluteConnected。

反直觉的坑：额外确认门槛会拦截真实已连接设备，重新配对不能补程序门槛。

正确做法：不要恢复protocolConfirmed；按真实订阅状态判就绪；先查0计数路径再推断蓝牙慢。

实测来源：`outputs/iPhoneDesk/src/Program.cs:274`（锚点 `capture.ClientReady=hid.AbsoluteConnected;`）；`outputs/iPhoneDesk/验证记录.md` v6；用户16:40截图定位0/按键0（历史截图文件见§5）。

### ⚠ F03 左右键整体互换后右键变成确认

现象：左右键整体互换后右键变成确认。

实测的真实结构/数据：用户报告右键能拖动；v8 MapDesktopButtons交换1/2，用户随后报告右键确认；v9恢复恒等映射。

反直觉的坑：改善一个动作可能同时破坏点击语义；不能把设备动作配置推断成编码错误。

正确做法：保持物理左1/右2/中4，独立验证按下、点击、移动、释放及iOS按钮配置；2026-10-09用户已确认动作分流后的左键拖动成功，见F10。

实测来源：`outputs/iPhoneDesk/src/HidMouse.cs:156`（锚点 `MapDesktopButtons(byte value)=>value`）；`outputs/iPhoneDesk/src/Program.cs:36`（锚点 `HidMouse.MapDesktopButtons(1)!=1`）；用户原话见§5 P02。

### ⚠ F04 日志说报告连接不等于执行

现象：日志说报告连接不等于执行。

实测的真实结构/数据：实际GATT有两个2A4D输入特征，2908描述符分别[3,1]和[2,1]；Notify统计是传输结果。

反直觉的坑：旧注释曾写Report1但实际点击Report3（本次已清理注释）；不能按注释改字节。

正确做法：查描述符和ReportMap真实字节；绝对报告2、相对点击3；当前编码第一字节按钮位不是报告ID。

实测来源：`outputs/iPhoneDesk/src/HidMouse.cs:90`（锚点 `new byte[]{3,1}`）；`outputs/iPhoneDesk/src/HidMouse.cs:103`（锚点 `new byte[]{2,1}`）；`outputs/iPhoneDesk/src/HidMouse.cs:48`（锚点 `return [(byte)(heldButtons&7),(byte)(x&255)`）。

### ⚠ F05 拖动时人为等待导致变慢

现象：拖动时人为等待导致变慢。

实测的真实结构/数据：旧实现每次坐标变化且按住时都等45ms；当前仅按钮状态变化的新按下边沿等待，释放最短80ms。

反直觉的坑：拖动的每一帧都睡眠会形成积压；80/45只是兼容时序，非已测端到端延迟。

正确做法：只对必要按键边沿等待，移动合并但不删按钮转换；保留释放路径。

实测来源：`outputs/iPhoneDesk/src/HidMouse.cs:166`（锚点 `held<80`）；`outputs/iPhoneDesk/src/HidMouse.cs:211`（锚点 `MapDesktopButtons(pressed.Value)!=buttons)`）；`outputs/iPhoneDesk/src/Program.cs:512`（锚点 `inputs.Last.Value=input.Absolute?input:`）。

### ⚠ F06 WPF预览有手机但实时区变灰方块

现象：WPF预览有手机但实时区变灰方块。

实测的真实结构/数据：用户v6截图灰矩形盖住圆角；原生HwndHost独立于WPF视觉树；v7宿主初始空区域、连接后host/video分别SetWindowRgn。

反直觉的坑：Border圆角、WPF裁剪和RenderTargetBitmap不能代替原生窗口圆角或实时图像。

正确做法：原生宿主及renderer均设置区域，断开重置空区域，销毁时归还父窗口/style；实时截图用独立路径验证。

实测来源：`outputs/iPhoneDesk/src/VideoHost.cs:22`（锚点 `CreateRoundRectRgn(0,0,0,0,0,0)`）；`outputs/iPhoneDesk/src/VideoHost.cs:80`（锚点 `SetWindowRgn(host,hostRegion,true)`）；`outputs/iPhoneDesk/src/Program.cs:25`（锚点 `RenderTargetBitmap`）。

### ⚠ F07 接收器名称带空格不能按shell引号理解

现象：接收器名称带空格不能按shell引号理解。

实测的真实结构/数据：下载对应研究源码readArguments在mainwindow.cpp:99用content.split(" ", Qt::SkipEmptyParts)，不是shell tokenizer。

反直觉的坑：引号不一定保护名称空格；UI目前限制无空白，不能声称全配置适配。

正确做法：以实际接收器解析规则为准；未支持原值只读回传；以后支持空格需扩展适配或接收器解析，不能猜引号就行。

实测来源：`work/iphonedesk/mainwindow.cpp:99`（锚点 `return content.split`）；`outputs/iPhoneDesk/src/DeskLayout.cs:106`（锚点 `value.Any(char.IsWhiteSpace)`）。

### ⚠ F08 本机接口可读但截图没验证

现象：本机接口可读但截图没验证。

实测的真实结构/数据：v10实际GET /status：mirrorConnected=false、mouseConnected=true、positionCount=0、buttonCount=0；无token403，截图409。

反直觉的坑：API可用不代表截图GPU内容可读或手机点击可执行。

正确做法：连接镜像后验证PNG非黑且对应实时画面；指定动作后比对前后画面，未经任务授权不试点。

实测来源：`outputs/iPhoneDesk/src/LocalControl.cs:23`（锚点 `new TcpListener(IPAddress.Loopback,0)`）；`outputs/iPhoneDesk/src/LocalControl.cs:69`（锚点 `CapturePhone`）；本会话v10接口验证真实响应，§8可重放只读检查。

### ⚠ F09 重新配对从电脑端连接手机失败

现象：重新配对从电脑端连接手机失败。

实测的真实结构/数据：用户截图Windows请求guojoe PIN后“连接失败”；本机鼠标服务先Started但手机未订阅。

反直觉的坑：通用电脑配对手机流程不等于手机连接BLE指针服务；该截图不足以定位具体底层错误。

正确做法：从iPhone辅助触控→设备→蓝牙设备连接电脑；两端核对提示；失败保留手机页面、订阅日志和错误，不反复盲删配对。

实测来源：`outputs/iPhoneDesk/运行日志.txt:104`（锚点 `2026-10-08T16:47:51.2615879`）；历史截图`C:/Users/lgz35/AppData/Local/Temp/codex-clipboard-00ad0b1d-e577-4588-8ff8-bd271b767dbf.png`（临时证据可能消失）。

### ⚠ F10 左键确认与左键拖动须分开路由（实机已确认）

现象：用户先反复反馈仅右键能拖动；诊断版测试后于2026-10-09明确回复“可以了。更新skill.md”。

实测的真实结构/数据：当前mouse-actions.json为dragButtonMask=2。09:02:30真实日志显示物理按钮位1、自动模式True、动作分流True；随后识别左拖并发送按钮0→2，松开后2→0；09:02:32再次记录同样序列。此前轻点记录为按钮0→1→0。用户确认拖动可用，是实机效果证据；日志是输入与传输路径证据，两者结合才支持结论。

反直觉的坑：桌面物理左键不必在所有手势都直接发同一手机按钮。此手机配置中确认使用按钮1、有效拖动使用按钮2；整体对换会把确认移到右键，单纯修正定位报告也不足以解决。此结论来自当前用户设备，不是通用iOS规律。

正确做法：保留LeftGesture分流；轻点左键在松开时发送1→0，左键移动超过Windows拖动距离时从原按下位置使用配置拖动按钮，松开/取消释放。保持物理左右键原始映射、右键透传、配置保真和日志诊断。自动模式左拖本次通过；F8手动模式、静止长按及跨设备适配仍不能宣称通过。

实测来源：`outputs/iPhoneDesk/运行日志.txt:540`（锚点 `2026-10-09T09:02:30.3169047`）；`outputs/iPhoneDesk/运行日志.txt:542`（锚点 `2026-10-09T09:02:30.5714207`）；`outputs/iPhoneDesk/运行日志.txt:545`（锚点 `2026-10-09T09:02:31.0962188`）；`outputs/iPhoneDesk/运行日志.txt:551`（锚点 `2026-10-09T09:02:32.7989063`）；`outputs/iPhoneDesk/src/LeftGesture.cs` Route/Reset，`outputs/iPhoneDesk/mouse-actions.json` dragButtonMask；用户本会话原话“可以了。更新skill.md”（2026-10-09）。

## 5. 踩坑与事故登记

新错误当次登记：日期、范围、【报错原文】【最小复现】【根因】【正确写法】、证据、是否再次发生。同一个坑第二次即事故，补预防措施和检查入口。没有保存原文必须写“原文未保存”，不得捏造编译器错误或回溯。

### P01 配对确认门槛

【报错原文】用户：“重新配对后仍没反应”；截图：“定位 0 次 · 按键 0 次”。【最小复现】旧v5启用两报告并配对，但确认文件不存在，进入投屏不产生输入。【根因】就绪状态另依赖protocolConfirmed。【正确写法】`ClientReady=hid.AbsoluteConnected`，没有第二确认门槛。来源：F02及Program.cs当前断言。预防：§8负断言检查；新增门槛必须有状态图并以实际就绪事实为依据。历史截图`C:/Users/lgz35/AppData/Local/Temp/codex-clipboard-c79d22aa-8b9b-458d-9493-5b201c3b13bb.png`。

### P02 左右键互换破坏点击

【报错原文】用户：“现在变成右键确认了，改过来”。【最小复现】交换按钮bit1/bit2后左键发送2、右键发送1。【根因】把拖动问题误当全局按键编号问题。【正确写法】恒等映射，分开验证点击和拖动，读取iOS配置才决定动作适配。预防：映射断言 + `--self-test`；实机动作矩阵必须覆盖左点、左拖、右点、组合、移出释放。当时该修复未证明左拖成功；后续成功的是动作分流方案，见F10。

### P03 45ms重复等待

【报错原文】用户：“还是慢，点击没反应。拖动没反应”。【最小复现】SendAt坐标变化时按住按钮，每帧都Task.Delay(45)。【根因】未区分按下边沿和持有状态。【正确写法】按下状态变化才等，移动合并，保留释放80ms兼容等待。预防：断言必须含状态变化判断；性能测试统计动作全程而非LastNotifyMs。

### P04 原生灰块与圆角

【报错原文】用户截图为灰色矩形遮住手机；无系统报错文本。【最小复现】未连接renderer时显示原生STATIC host；仅给WPF Border圆角。【根因】原生窗口不受WPF裁剪约束。【正确写法】待机host空region，连接后两原生窗口roundregion，销毁还原。预防：待机/连接/断开/重连四状态检查；不能只看WPF预览。

### P05 PowerShell文字引号

【报错原文】`A positional parameter cannot be found that accepts argument '投屏名称输入新名称后点保存并应用。支持中文、英文和数字，1–32字符，暂不支持空格。应用会重启接收器并断开当前投屏，请在手机重新连接。'`（2026-10-08工具输出）。【最小复现】Add-Content双引号参数内部直接出现中文弯双引号。【根因】PowerShell也将弯引号视为引号，拆分了参数。【正确写法】使用单引号here-string或Python UTF-8文件写入，不拼接含引号多行命令。预防：PowerShell用AST语法检查，脚本动态文本不作为代码执行。

### P06 Windows rg通配路径

【报错原文】`rg: outputs/iPhoneDesk/src/*.cs: IO error for operation on outputs/iPhoneDesk/src/*.cs: 文件名、目录名或卷标语法不正确。 (os error 123)`。【最小复现】PowerShell传`src/*.cs`给rg。【根因】shell没有把路径展开为文件列表。【正确写法】`rg -n '模式' outputs/iPhoneDesk/src -g '*.cs' -g '!obj/**' -g '!bin/**'`。预防：本项目一致性脚本不使用shell通配路径，直接枚举LiteralPath。

### P07 图标生成运行时不兼容

【报错原文】未保留原始Add-Type异常，不编造；历史记录为PS Core System.Drawing私有程序集解析失败。【最小复现】PS Core Add-Type IconFactory.cs并仅引用System.Drawing。【根因】新运行时引用不完整。【正确写法】已有icon.ico/icon-256.png可直接复用；确需重新生成时用WindowsPowerShell框架System.Drawing已测路径。脚本为`work/iphonedesk/IconFactory.cs`，来源未持久化错误日志，证据等级较低。

### P08 迁移后自动恢复蓝牙启动无响应

【报错原文】`The request was canceled due to the configured HttpClient.Timeout of 5 seconds elapsing.`；原始socket探测`Raw socket request timed out`；进程`Responding=false`。【最小复现】移动项目后从新路径以`--resume-bluetooth`启动，手机订阅日志出现但UI及/status无响应。【根因】尚未定位；只能确认该次自动恢复启动挂起，不能断言是目录、配对或GATT问题。【正确写法】迁移验证先正常启动、不自动启用蓝牙；无响应时只结束核对绝对路径的本包实例，重启后用限时只读/status检查。此次正常启动后`Responding=true`、状态请求200且两连接状态false；用户可从UI连接，蓝牙恢复故障待单独排查。预防：所有接口探测必须设TimeoutSec并显式读取新令牌，不使用无限等待；正常启动与自动恢复分别验收。

### P09 可空控件选项流分析警告

【报错原文】`warning CS8600: 将 null 文本或可能的 null 值转换为不可为 null 类型。`，`warning CS8602: 解引用可能出现空引用。`，MirrorResolution.cs首版第70行。【最小复现】把SelectedItem匹配结果存bool后，在另一个表达式再次强转SelectedItem并取Tag。【根因】bool不保证第二次属性读取非空；编译器不能传递该空值证明。【正确写法】先保存ComboBoxItem可空局部引用，preset显式非空比较，取值通过selected?.Tag is string tag分支，不用null-forgiving掩盖警告。之后重新构建验证。

### A02 左键拖动反复未解决事故（P02的后续）

【报错原文】用户：“变成右键按住拖动的，改成左键”。【最小复现】相对报告发送按钮1按下，随后AbsoluteReport移动首字节仍固定0，最后相对报告释放。【根因】代码层面两报告集合按钮状态不一致；是否为此设备仅右键能拖动的全部原因，尚未实机证明。【正确写法】不交换左右键；AbsoluteReport通过heldButtons参数携带当前已发送buttons，移动与读取报告保持按住状态；Send释放时显式清除两报告集合，即使松开后不移动也回0。掩码7来自当前描述符3个按钮位，不是业务编号。不得把修正后的编码自检当成实机拖动通过。

防复发：Program.Main新增按钮0/1/2/3/4/7的两报告一致性、坐标不变测试，以及左键按下→移动→移动→释放→移动的首字节序列 `[1,1,1,0,0]`。后续任何拖动改动必须跑该回归，并在手机验证左点、左拖、右点、移出释放，未验证须保持待确认。

### A03 左键拖动修正后实机仍失败（A02复发）

【报错原文】用户：“还是按住右键才能拖动，改成左键”。【最小复现】保持按钮映射1/2，绝对报告保留按下位，用户实机仍只有右拖生效。【根因】A02报告状态一致性是代码问题，但不足以解释此手机动作差异；手机按钮动作配置及系统解释未完全查明。不能再次盲目整体对换。

【正确写法】把桌面左点与左拖分别路由。LeftGesture.Route在左键按下保存起点，松开前未超过Windows拖动距离则发送左键1按下/释放；超过距离则从起点以配置dragButtonMask按下、移动、释放。该用户已确认手机右键拖动有效，所以新配置初值2；这是此设备实测动作配置，非所有设备的固定规则。支持配置1/2/4，对应报告描述符三个单按钮位；额外未知JSON字段原样保留，缺失/非法/非JSON配置只读提示原值且不覆盖，读取失败不写默认配置。右键原样透传，不做全局互换。

防复发：新增轻点[1,0]、小幅抖动不触发、拖动[2,2,0]且起点保留、取消待定点击不补发、右键透传、配置1原生拖动回归。实际左点延迟到松开执行，尚未实现静止长按动作，不能声称原长按功能保持；API swipe使用同一配置，tap仍按钮1。上线后必须用户实机确认左点、左拖及移出释放；该阶段只有回归证据；2026-10-09后续取得物理日志及用户成功确认，见F10，不应再列为当前左拖未解决。

### A01 重复不完整/过时文档事故

【报错原文】旧说明仍写Report1、横屏选择器、两窗口、F8及相对增益4，而现状已经改变；多次追加版本记录仍保留互相冲突的“最新版”。【最小复现】只追加段落不重写断言，后来者取旧段落实现。【根因】无唯一知识库和收尾对账。【正确写法】本文件唯一权威，旧文档标历史；每次回写后运行§8检查。防复发：代码指纹、说法/特征串正负断言、行号核对三重检查，代码改变但文档未同步时验收失败。

其他历史缺陷（自包含恢复失败、补丁上下文不匹配）仅有摘要，无报错原文，不能宣称准确根因。再次遇到时保存原始日志并登记，而不是重复猜测。

## 6. 现状、配置与未完成项

### 6.1 接口契约

GET `/status` 返回mirrorConnected、transport（usb/airplay）、mouseConnected、positionCount、buttonCount；GET `/screenshot` 返回PNG，需要镜像窗口；POST `/command` JSON action=tap/swipe/release。必填坐标x/y归一化0–1；swipe另需endX/endY；durationMs可选默认350，当前80–2000ms。tap持有90ms；swipe分步20ms目标。返回sent只表示发送，executionVerified=false。409表示状态、参数或执行错误；403表示认证失败或Origin；404未识别端点。不得把GET变成有副作用指令。

### 6.2 已发现但尚未整改（必须遵守新规范，不能据现状复制）

- 固定比例 `9/19.5`、frame `1179/2556`、标题“iPhone”不是元数据自适配；没有横屏选择器。下一次设备适配必须取得真实视频尺寸/方向/设备配置再映射。
- ReadMirrorName缺失-n回退iPhoneDesk、Regex仅支持单token、名称长度32及禁止空白，ApplyMirrorName直接写文件。未知/带引号/带空格/损坏配置尚未实现“只读原值回传”；下一次碰该模块必须先补保真解析及原子写，不能沿用默认值覆盖未知内容。
- 优化投屏.ps1从原始备份计算参数，可能把后来修改的名称回退；下次改优化必须从当前配置保留全部无关参数，Original模式也需说明恢复范围。
- LocalControl自定义HTTP解析器8192字节限制、5秒超时、异常统一409；待测畸形请求、断开和并发压力。不能仅凭状态检查声称健壮。token未专设ACL，依赖当前用户目录权限；不能声称同用户恶意程序无法读取。
- 截图BitBlt可能受GPU呈现、遮挡、最小化影响；当前只测过未连接409，未证明实时截图可用。不要使用黑图盲目坐标点击。
- 当前设备：左键点击此前已确认；2026-10-09用户确认自动模式左键拖动可用，真实日志证明物理按钮1被识别为拖动按钮2并释放。此结论不外推到F8手动模式、其他设备或所有手势，也不代表助手已有可靠视觉闭环。
- HidMouse.cs旧Report1注释已改为Report3；绝对移动携带当前已发送按键状态，实际手机自动模式左键拖动已由用户确认，见F10。

### 6.3 当前常量/名单清单（每次改代码必须同步本节与断言）

协议：HID1812，特征2A4A/2A4B/2A4C/2A4D，描述符2908，绝对报告2/相对报告3，按钮左1右2中4，绝对0–32767，相对-127–127，小端16位。按下兼容间隔45ms、释放最低80ms。动作配置dragButtonMask为描述符单按钮位1/2/4，新配置2是本用户实测初值；拖动距离来自Windows SystemParameters；左轻点1不变。快捷键F8相对捕获、F9暂停、手动Esc释放。队列上限128、合并位移254：现状资源保护值，未做性能容量实测。

UI：左栏280、窗口1120×840、最小920×680，名称最多32字符，比例9/19.5、1179/2556；属于旧实现现状，不允许视为自适配方案。定时检查100ms、窗口扫描约1秒。接口名单仅status/screenshot/command；动作tap/swipe/release；随机令牌32字节；HTTP头/体当前8192限制、读取5秒。分辨率快捷项自动/720p/1080p/1440p/4K，支持接收器有效自定义；刷新率上限255来自接收器解析，选择尺寸不修改-fps。配置模式Smooth/Stable/Original；目标Smooth1280x720@60，Stable1280x720@30，GPU d3d11h264dec+d3d11convert，vsync no。上述值变更必须重新验证实际支持，并同步快照，不能声称达到目标FPS就证明不卡。

### 6.4 USB 接入研究与边界（2026-10-09）

本次动手前查阅§1/§2/§3/§4 F01/§6/§7/§8；基线31条断言、12个指纹、24个证据行号通过。当前生产代码仅AirPlay接收与BLE输入，没有纯USB采集。电脑Apple Mobile Device Service运行，但Get-NetAdapter未发现Apple USB网络适配器；不能把驱动存在推定为已插线或已信任。USB插线、信任和手机开发者模式须由用户操作，不能自动改变手机安全设置。

候选一：USB个人热点提供网络，现有AirPlay可尝试经该网络连接；Apple文档只证明USB热点支持Windows，未证明此接收器能在此手机发现并经USB传流。必须实测网络接口、AirPlay发现与实际媒体连接路由；只看到画面不构成USB路由证据。参考：https://support.apple.com/en-ie/111785 。候选二：开发者服务USB采集，需要额外依赖和通道，尚未集成，鼠标仍用已有BLE。研究项目BieleckiLtd/IosScreenCaptureTool的版本c933e66ccbfca44cec0e5c132a2ccdcf3ce7738b保存在work/iphonedesk/usb/reference；PymobiledeviceClient.cs的CaptureFrameAsync每帧启动python截图子进程，并非硬件视频流，README所谓up to60fps只是目标选择，不作为本项目性能承诺。不能直接复制慢截图方案称流畅USB投屏。Python3.14.0已安装，pymobiledevice3缺失；未自动安装全局依赖、驱动或修改网络。此为研究阶段记录；用户随后指定插线、信任后投屏，当前实现及验收状态见§6.5。

USB研究目录为临时来源证据，不是正式源码，引用版本必须保留。研究阶段未改exe或手机配置；随后新增USB通道，状态见§6.5。未取得实际帧前不得声称USB投屏实机成功。

USB研究错误记录：原文见work/iphonedesk/usb/错误.txt。【报错原文】Invoke-WebRequest: 404: Not Found。【最小复现】请求未核对的仓库Services/IosStreamService.cs路径。【根因】根据猜测构造源码路径，真实仓库文件为根级PymobiledeviceClient.cs。【正确写法】先克隆并rg --files核对真实文件，再读取；已改用固定版本本地源码。pip show的Package(s) not found是依赖检查结果，不是安装失败，不能据此宣称纯USB不可实现。
### 6.5 USB 视频通道接入与首次驱动阻塞（2026-10-09）

用户指定“插线、信任后就能投屏”。已读§1/§2/§3/§4/§6/§7/§8并通过原31/12/24基线。正式新增src/UsbMirror.cs，AddUsbMenu/EnableUsb/MonitorUsb/StopUsb；VideoHost.BeginUsb/PresentUsb/EndUsb使用同一原生子窗口渲染USB视频，收到真实帧之前保持空区域显示待机画面，并在切换时还原原AirPlay窗口；Program生命周期和LocalControl共享MirrorConnected状态。USB仅在采集状态Streaming、帧数>0、有效宽高时可发鼠标指令；当前USB宽高来自帧元数据，鼠标比例和布局跟随更新。AirPlay旧比例保留，不把此改动描述为所有设备全面自适配。USB单台设备自动选择、多台全部列出由用户选择，不写死设备编号。首次在左侧启用后持续检测插拔；不保存默认接入配置，不自动安装驱动、不启用开发者模式、不改个人热点、不覆盖用户配置。切回Wi-Fi先取消并释放输入、停止会话、还原视频宿主再开接收器。

USB核心为RayrenSX/iPhoneMirror v1.8.3、源码1270c443a29457751cadd20f6d7bbe1ce213b9db，ABI18（CoreApi.h定义），包SHA256核对官方SHA256SUMS.txt一致，未改第三方二进制。原版DLL/驱动管理器/许可证/依赖放outputs/iPhoneDesk/usb/；USB配置切换helper因上游从进程exe父目录找路径，必须放iPhoneDesk.exe旁，不能只放usb子目录。上游会检查系统libusb0运行库和目标设备过滤驱动，包内DLL存在不等于系统驱动就绪。驱动管理器仅准备供用户明确批准后安装，当前未执行安装。核心默认日志为%TEMP%/iPhoneMirror-capture.log，可通过IPHONE_MIRROR_LOG_FILE显式指定；不能仅查本应用运行日志判断核心失败。GPL原始LICENSE、THIRD_PARTY_NOTICES、licenses及对应源码压缩包随组件保留；自行编写的接入源码不隐藏，MIT声明不替代第三方GPL条款。

USB GET /screenshot新增从核心解码BGRA帧生成PNG，不使用GPU BitBlt；无帧拒绝，未知帧格式拒绝，不返回黑图冒充截图。GET /status新增transport=usb/airplay，其他字段保留；sent仍不是执行验证。停止/错误/退出释放控制并清理USB会话，不删原有F9、队列、按键释放和Bearer认证路径。

实测：work/iphonedesk/usb-integration/usb-probe.json来自--usb-probe只读设备及目标过滤后端检查：abi18、deviceCount1、USB1、Paired1、Accessible1、captureFilterAvailable0；真实设备ProductType=iPhone15,2，系统26.6.2。此文件含用户设备元数据，应本地保留不上传。发布构建无警告，--self-test退出0，--render-ui和--render-ui --usb均退出0且已视觉查看新增USB卡片；后一实跑UI准确显示已检测到设备、缺少采集过滤驱动。启动参数--usb可直接启用自动连接；普通启动需在卡片点启用。正式exe/pdb已从usb-integration配套发布（旧产物保存在该目录rollback），正式包--usb-probe与--self-test均退出0，新版已以--usb正常打开并显示缺少过滤驱动。未启动USB采集、未取得真实视频、未修改系统驱动；目前验收阻塞为过滤驱动缺失，实际流畅性、插拔恢复、USB截图、USB状态下左拖必须取得驱动后实测，不能把静态构建写成实机通过。

⚠ USB驱动实测结论：现象→Apple服务运行且手机已信任仍不能开始USB视频；真实数据→usb-probe.json captureFilterAvailable=0；反直觉的坑→Apple配对驱动不等于隐藏USB视频采集过滤驱动，用户态libusb0.dll也不能替代内核驱动；正确做法→只读检查目标设备，提示一次性安装，经用户批准后安装并重新探测，收到帧才报连接成功；来源→src/UsbMirror.cs的im_is_libusb0_device_available调用、work/iphonedesk/usb-integration/usb-probe.json、上游v1.8.3 DRIVER_DEPENDENCIES.md。

接手任务：从USB过滤驱动缺失继续验收。先读本节和USB接入说明.md，确认用户安装授权及Windows管理员操作边界；仅操作当前目标iPhone的过滤驱动，不替换Apple父驱动，不触碰无关设备。重新运行--usb-probe，available1后启用USB、实际观察画面、查真实帧数/尺寸/FPS、验证左点左拖/移出/F9释放/拔插重连、GET截图可见内容；逐项记录实际来源。源代码在src/UsbMirror.cs，不在研究仓库；构建work/iphonedesk/usb-integration；交付USB接入说明.md。不能自动点击信任、解锁、UAC或安全提示。

只读自检（不要与正在采集的实例同时探测）：
```powershell
$taskUsbProbe = Start-Process -FilePath "$PWD/outputs/iPhoneDesk/iPhoneDesk.exe" -ArgumentList '--usb-probe' -WindowStyle Hidden -Wait -PassThru
if ($taskUsbProbe.ExitCode -ne 0) { Get-Content outputs/iPhoneDesk/usb-probe.json; throw 'USB只读探测失败' }
Get-Content outputs/iPhoneDesk/usb-probe.json
```

同坑第二次事故USB-A01：猜文件路径再次读错DriverInstaller/Program.cs，原文“系统找不到指定的文件。 (os error 2)”，最小复现rg该猜测路径；根因为WPF入口实际是App.xaml.cs，继续依靠传统Program.cs直觉。正确写法先rg --files再读取实际路径。防复发：第三方文件在命令前先Test-Path -LiteralPath，失败即列rg --files，不得构造路径直接下载/读；错误原文存work/iphonedesk/usb/错误.txt。该限制同样适用于以后引用第三方接口和证据。

### 6.6 USB 握手失败修正（2026-10-09）

动手前读§1/§2/§6.5/§7/§8；36条断言、13个指纹、24个行号基线通过。后续驱动日志确认libusb0已安装、运行且文件校验一致，核心打开configuration5/interface2成功；“缺少驱动”仅为先前阶段，不再是当前阻塞。真实报错为“QuickTime endpoint opened but iPhone sent no PING; keep the device unlocked”、“QuickTime bulk write: libusb0 error -116”；09:38一次为“QuickTime session established but no video frame arrived within 12 seconds”。不能仅根据文案断定用户锁屏或根据stacked警告确定唯一根因。

【最小复现】UsbMirror.MonitorUsb在State6/7清理会话，下一循环usbSession0自动再次创建，日志09:39至09:40连续重复握手/恢复USB配置，实际帧数0。【根因】把协议失败当成重新插线，不区分生命周期终止状态。【正确写法】错误/停止后释放输入、清理会话、保留实际错误并break结束监控；只有用户点“连接 / 重试 USB”才重启。正常等待插线仍轮询，协议失败不无限循环。防复发：文档断言检查错误分支存在break，新版实机日志确认一次失败后不再创建下一会话。

USB协议模式来自v1.8.3 CoreApi CaptureOptions reserved[2]的协议枚举：0标准采集（上游Demo）、1USB AirPlay、2爱思兼容；不代表复制爱思内部实现。新增完整枚举选择器，当前会话读取选择的Mode；默认从未经验证的2改为0进行兼容性实测。不写入任何用户配置，不改驱动或第三方DLL、不启用开发者模式。不是已证明模式0必然修复所有握手，必须实际观察帧。

本次使用computer-use的Alt+F4优雅关闭旧程序，等待核心USB恢复完成后才发布；新构建位于work/iphonedesk/usb-handshake-fix，回退文件位于该目录rollback。正式源为src/UsbMirror.cs。发布构建与--self-test退出0，正式exe/pdb已配套替换并以--usb启动。标准模式本次09:43:40打开端点，09:44:00仍no PING，真实帧0；因此模式选择不能声明已经解决握手。09:44:00失败后已停止自动重试，不再自动创建会话，后续等待用户解锁并重插进行下一次实测。当前USB视频仍未验收成功。
## 7. 目录、命名、开发与提交流程

1. 动手前读章节，查文件/函数及真实字段与配置；先跑§8只读一致性检查记录基线。未通过时先指出具体漂移。
2. 任务名用清晰小写连字符；C#沿用PascalCase类型/方法、camelCase私有字段；不新增vN作为长期产品名。临时数据放work，交付放outputs。
3. 实现适配及保真，未知配置只读。日志记录阶段和结果，不记录令牌、用户PIN和敏感内容。第三方改动必须独立列清。
4. 每改完一个逻辑单元，立即回写本文件相关现状、F结论、P事故和断言；不要攒到收尾。已知历史错说法在相关章节注明取代。
5. 修复必须验证触发场景与反例。代码断言/自检不是实机动作证据；本次纯文档治理不得偷偷改生产代码或操作手机。
6. 构建到work新目录，检查退出码。需要发布时先准备回退exe/pdb，再优雅关闭主窗口、等待退出、替换exe及pdb、按原工作目录启动。不要同时运行两主实例抢热键/GATT。
7. 有Git时审查diff和敏感文件；提交说明写问题、最终行为、验证和限制。无Git时在本文件记录改动与检查，不能虚构提交。
8. 回写后执行(说法,代码特征串)脚本核对、代码指纹/行号核对；固定常量和清单也同步。最终交付说明实际改了什么、验证到哪里，不能用口头补充替代文档。

## 8. 收尾自检脚本合集（从根目录直接复制运行）

### 8.1 文档↔代码检查（只读，无手机动作）

```powershell
pwsh -NoProfile -File outputs/iPhoneDesk/tools/检查规范.ps1
```

脚本读取本文件末尾内嵌JSON：每条说法对应文件与特征串，正断言必须存在，负断言必须不存在；核对全部维护源码与脚本SHA256，防止改代码忘了回写；核对本文件引用源代码行号。它只验证可静态证明的事实，不能证明动态行为或文字合理。**不能仅重算哈希来使脚本通过**：先审查语义、更新正文及证据，再登记新的快照。源文件新增/删除也必须同步清单。

### 8.2 环境/编译/纯编码

```powershell
dotnet --info
$taskStage = Join-Path (Get-Location) 'work/iphonedesk/handoff-check'
dotnet publish outputs/iPhoneDesk/src/iPhoneDesk.csproj -c Release -r win-x64 --self-contained false -o $taskStage -p:PublishSingleFile=true --source "$env:USERPROFILE/.nuget/packages"
if ($LASTEXITCODE -ne 0) { throw '发布构建失败' }
$taskTest = Start-Process -FilePath (Join-Path $taskStage 'iPhoneDesk.exe') -ArgumentList '--self-test' -WindowStyle Hidden -Wait -PassThru
if ($taskTest.ExitCode -ne 0) { throw "编码自检失败：$($taskTest.ExitCode)" }
Get-Content -LiteralPath (Join-Path $taskStage 'self-test.txt')
```

无需替换运行实例。当前为framework-dependent发布；不要默认self-contained可恢复成功。UI/hook探测需准备图标及目录，并关闭主实例，故不默认收尾执行。

### 8.3 本机接口只读验证（不输出令牌）

```powershell
$taskConnection = Get-Content -LiteralPath outputs/iPhoneDesk/control-interface.json -Raw | ConvertFrom-Json
$taskHeaders = @{Authorization = 'Bearer ' + $taskConnection.token}
Invoke-RestMethod -Uri ($taskConnection.url + '/status') -Headers $taskHeaders
try { Invoke-WebRequest -Uri ($taskConnection.url + '/status') -ErrorAction Stop | Out-Null; throw '未经认证仍成功，验收失败' }
catch { if ($null -eq $_.Exception.Response -or [int]$_.Exception.Response.StatusCode -ne 403) { throw } }
```

截图需镜像连接才运行：`Invoke-WebRequest -Uri ($taskConnection.url + '/screenshot') -Headers $taskHeaders -OutFile work/iphonedesk/phone-check.png`，然后视觉核对。无连接409属预期，但不能写截图通过。不得在此脚本加入POST点击。

### 8.4 原始错误保存及脚本语法

```powershell
$taskSyntaxErrors = $null
[System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path outputs/iPhoneDesk/tools/检查规范.ps1), [ref]$null, [ref]$taskSyntaxErrors) | Out-Null
if ($taskSyntaxErrors.Count) { $taskSyntaxErrors; throw 'PowerShell语法失败' }
rg -n 'SendAt|RunRemote' outputs/iPhoneDesk/src -g '*.cs' -g '!obj/**' -g '!bin/**'
```

失败立即保存原文到work/<任务>/错误.txt并写P记录，第二次补事故编号、预防脚本和验证结果。

## 9. 接手验收场景

具体任务：**完全无上下文的开发者只读本文件，定位当前投屏名称解析、修改为保留未知参数的安全适配，实现只读保真回退，编译且不操作手机完成验证。**

完成标准：能找到源码、实际配置/真实解析器和运行实例；解释名称有空格的限制及未知配置不能覆盖；知道备份/原子写和仅本包进程重启；能独立执行§8构建、自检、规范核对；知道该任务必须新增多种真实配置样本，而当前左拖/截图未验证不能顺带声称解决；每次改动回写§6/F/P及末尾断言。若任一步需要口头说明，补本文件，不用聊天补充代替。

本次文档治理收尾：未改生产C#、未替换运行exe、未发送手机操作。实测记录来源均标代码锚点/真实日志/用户截图或原话；只有动态重新验证后才能升级证据等级。

### 9.1 本次接手验收实跑结果

2026-10-08 已按本文件 §8 命令在 `work/iphonedesk/handoff-check/` 发布成功；运行该产物 `--self-test` 退出码0，报告编码/坐标边界/组合按键/跨显示器/hover拖动边界通过。未替换当前exe，未发手机操作。

`检查规范.ps1` 正向结果：21条说法断言、10个代码指纹、20个证据行号通过。复制到 `work/governance/negative/` 后，仅将就绪特征改成不存在的状态，检查正确失败，同时报告说法冲突和证据行号漂移；没有为通过检查改生产代码。负向输出保存在 `work/governance/negative-result.txt`。

验收边界：静态交接定位与命令可执行已验证；未由另一真人独立实现完整名称适配，不能把静态检查包装成完整独立开发验收。该具体任务所需事实/限制/检查已集中到本文，接手者发现缺项须补本文。生成本文的 `work/governance/create-doc.py` 是一次性历史草稿脚本，禁止再次执行覆盖维护后的唯一文档。

### 9.2 项目迁移记录

2026-10-08 用户明确要求全项目转移到 `C:/guojoe/iphonedesk`。本次动手前已阅读§2/§3/§7/§8，核对新目录为空，只停止旧根目录中本包的主程序、接收器和发现进程。使用PowerShell LiteralPath移动根目录所有内容，保留outputs/work/src及治理入口相对结构；未删除第三方文件或修改用户配对、APPDATA参数和注册表。302个原始文件迁移前后SHA256逐一一致；迁移清单为 `work/governance/migration-manifest.json`。原项目目录已空，不是有效源码或运行位置。

有效源码仍为新根目录 `outputs/iPhoneDesk/src/`；程序为 `C:/guojoe/iphonedesk/outputs/iPhoneDesk/iPhoneDesk.exe`；唯一规范为 `C:/guojoe/iphonedesk/skills.md`。文档和一次性历史草稿中的旧项目路径已更新。日志、进程PID、令牌在重新启动后可能变化；迁移校验快照不能当作以后运行文件不变的约束。APPDATA/注册表仍为用户级原配置，不属于移动的项目目录。

新目录验证已完成：21条断言、10个指纹、20个行号通过；新路径发布构建成功，纯编码自检退出码0。主程序与接收器均从新路径启动。自动恢复蓝牙启动无响应，已记录P08；正常启动后UI响应且本机/status返回200，镜像和蓝牙尚未连接。没有修改C#或第三方二进制，没有发送手机操作。后续工具必须以新根目录为workdir，不能在旧目录重新生成另一份项目。

### 9.3 左键拖动报告状态修正

动手前已读§1/§2/§4 F03–F05/§5 P02–P03/§6/§7，并运行一致性基线通过。本次维护HidMouse.AbsoluteReport/SendAt及Program.Main回归；不改报告ID、GATT描述符、左右键映射、用户按钮配置和释放安全路径。绝对报告默认按钮0用于兼容已有编码调用，实际移动传入已发送buttons，松开时显式发送绝对按钮0；因此左键点击语义保持，拖动期间不会由本程序的定位报告宣告全松开。构建与纯编码回归通过：按钮0/1/2/3/4/7与左键序列[1,1,1,0,0]通过。旧exe/pdb已备份到work/iphonedesk/left-drag-held-state/rollback，主程序优雅退出后替换配套产物并正常启动（不使用曾挂起的自动蓝牙恢复）。当时实机点击及左拖待测，未发送自动手机动作；后续自动模式左拖已确认，见F10。

### 9.4 左键点击/拖动动作分流

已读§1/§2/§4/§5 A02/§6/§7，基线核对通过。新增LeftGesture.cs，以mouse-actions.json配置的dragButtonMask识别手机拖动动作，当前用户首次创建值2；Windows SystemParameters拖动距离用于手势识别，不采用随意体验阈值。修改Program.QueueInput前置路由、HoverChanged/ReleaseCapture重置，保留QueueRawInput原队列和reportLock；LocalControl swipe读取相同拖动配置，tap保持1。UI提示明确左键轻点确认、按住移动拖动。未知配置原值保留并提示，不自动修复覆盖。静止左键长按暂无动作分流支持，需单独设计。构建、编码与手势回归通过（轻点、抖动、拖动、取消、右键透传及配置1）。旧产物已备份到work/iphonedesk/left-gesture-routing/rollback，主程序优雅退出后配套替换exe/pdb并正常启动。规范核对26条断言、11个指纹、20个行号通过。当时手机实际效果待确认，未发送自动操作；2026-10-09后续确认左拖可用，见F10。

### 9.5 实机滑动与物理左键诊断（2026-10-09）

用户授权“你试试”。实际运行exe与left-gesture-routing构建SHA256一致，mouse-actions.json为dragButtonMask=2，镜像和蓝牙均连接。首次GET截图返回黑色PNG，不作为操作依据（同一连接中的手机画面通过窗口激活后可读；不能断言是断流）；使用computer-use技能选择已返回的iPhoneDesk窗口并激活，观察到手机主屏。按观测的主屏空白区调用API swipe（0.8,0.7到0.2,0.7，450ms），观察桌面发生横向切换；这是拖动报告路径的实机证据，不是物理左键触发的证据。

物理鼠标钩子忽略Injected输入（MouseCapture.OnMouse），因此不能用软件模拟左拖声称测过实际左键。当前日志无法区分物理右键和分流后的按钮2；新增Program.QueueInput物理按钮/自动模式/分流状态，以及LeftGesture.Trace按下、识别、轻点、松开日志，只记录必要阶段不记鼠标移动每帧。不改用户按钮配置、不继续猜映射。后续用户已完成物理左拖，日志与确认见F10。本次构建及手势回归通过，27条断言/11个代码指纹/20个行号通过；旧产物备份work/iphonedesk/left-drag-trace/rollback，新版优雅替换启动，已通过computer-use点击“连接鼠标”恢复服务。已请求并取得用户真实左拖结果；软件注入仍不能作为真实按键测试。

### 9.6 左键拖动实机验收（2026-10-09）

用户按诊断要求进行真实左拖，09:02:30与09:02:32日志均包含物理左1→拖动识别→手机按钮2→释放0，随后明确回复“可以了”。当前自动模式左键拖动记为用户实机确认通过；保留此前事故作为历史，取消当前“左拖尚未解决”状态。此前自动API横滑也已观察到页面切换，两类证据分别记录，不互相替代。

本次仅更新唯一规范skills.md，不另建skill.md副本；未修改源码、配置或exe。回归防线仍保留，功能改动后须重新验证；其他设备、手动模式、静止长按和端到端性能保持原有验证边界。

### 9.7 窗口与滚动条细化（2026-10-09）

用户截图标出侧栏粗滚动条和右上角按钮。动手前读§1/§2/§3 UI索引/§4原生窗口/§7发布；本次只改DeskLayout。侧栏作用域ScrollBar模板为8 DIP轨道、4 DIP圆角握柄，无箭头，保留PART_Track、分页RepeatButton和拖动；标题按钮38×30 DIP、10 DIP几何图形、1 DIP线宽及悬停色。上述是视觉规格，不是数据功能阈值。未改鼠标分流、蓝牙、投屏和安全释放路径。发布构建及--render-ui退出码0，已查看预览确认细滚动条及窗口图标。旧exe/pdb备份work/iphonedesk/slim-window-controls/rollback，正式产物配套替换并正常启动；用户交付界面预览同步更新。文档检查通过，未新增镜像或手机操作测试。

### 9.8 投屏分辨率选择（2026-10-09）

已读§1/§2/§3/§6已知配置问题/§7/§8。接收器resources/uxplay_arguments_list.txt第24–25行定义-s wxh[@r]为向客户端请求尺寸；研究uxplay.cpp get_display_settings第1031行定义宽高正数最多4字符、可带正号，刷新率1–255最多3字符。不是保证手机输出相同像素，也不保证目标FPS。

新增MirrorResolution.cs：自动（移除-s）、720p1280x720@60、1080p1920x1080@60、1440p2560x1440@60、4K3840x2160@60及可编辑自定义。预设仅为快捷入口；现有非预设值按原值展示。Current按接收器空格拆词，重复-s以最后一条为当前值；Change只替换所有-s的值或显式自动时移除，其他参数及空白/中文/未知字段保留；-s=或无效参数只读展示原配置并禁止应用，不清空覆盖。空自定义拒绝，明确自动选项才移除-s。选择分辨率不改-fps/-n/GPU选项。

保存前检查外部配置是否变化；同目录临时文件+File.Replace原子替换并备份原文件，无配置时File.Move。应用只停止核对本包路径的接收器和发现进程，重启后提示手机重新连接。没有擅自替用户切换分辨率。回归覆盖有效/非法范围、自定义、重复参数、中文及未知参数保留、原换行、自动及非法原值拒写。发布构建无警告，分辨率参数及既有鼠标回归退出码0；界面预览确认当前720p被读取且自定义可编辑。规范检查通过。原exe/pdb备份work/iphonedesk/resolution-menu/rollback，新产物配套替换并正常打开；未点击应用、未改变现有接收器参数，实际分辨率协商仍待用户应用验证。

## 10. 机器可核对断言与源码快照

以下为本文件的一部分，不是独立知识库。值只供检查，新增变更必须先更新正文后同步。

<!-- DOC_CODE_ASSERTIONS -->
```json
{
  "assertions": [
    {
      "claim": "报告映射先绝对后相对",
      "file": "outputs/iPhoneDesk/src/HidMouse.cs",
      "feature": "[..AbsoluteMap,..RelativeMap]",
      "absent": false
    },
    {
      "claim": "相对点击报告描述符为3",
      "file": "outputs/iPhoneDesk/src/HidMouse.cs",
      "feature": "new byte[]{3,1}",
      "absent": false
    },
    {
      "claim": "绝对坐标描述符为2",
      "file": "outputs/iPhoneDesk/src/HidMouse.cs",
      "feature": "new byte[]{2,1}",
      "absent": false
    },
    {
      "claim": "左右键使用原始映射",
      "file": "outputs/iPhoneDesk/src/HidMouse.cs",
      "feature": "MapDesktopButtons(byte value)=>value",
      "absent": false
    },
    {
      "claim": "Wi-Fi鼠标就绪仅按真实报告订阅",
      "file": "outputs/iPhoneDesk/src/Program.cs",
      "feature": "capture.ClientReady=hid.AbsoluteConnected;",
      "absent": false
    },
    {
      "claim": "待机原生宿主为空区域",
      "file": "outputs/iPhoneDesk/src/VideoHost.cs",
      "feature": "CreateRoundRectRgn(0,0,0,0,0,0)",
      "absent": false
    },
    {
      "claim": "实时窗口与宿主均圆角",
      "file": "outputs/iPhoneDesk/src/VideoHost.cs",
      "feature": "SetWindowRgn(host,hostRegion,true)",
      "absent": false
    },
    {
      "claim": "名称写入-n参数并保留剩余参数",
      "file": "outputs/iPhoneDesk/src/DeskLayout.cs",
      "feature": "\"-n \"+value+\" \"+args",
      "absent": false
    },
    {
      "claim": "名称读取无匹配时使用默认值",
      "file": "outputs/iPhoneDesk/src/DeskLayout.cs",
      "feature": "match.Success?match.Groups[1].Value:\"iPhoneDesk\"",
      "absent": false
    },
    {
      "claim": "本机接口只监听loopback随机端口",
      "file": "outputs/iPhoneDesk/src/LocalControl.cs",
      "feature": "new TcpListener(IPAddress.Loopback,0)",
      "absent": false
    },
    {
      "claim": "拒绝浏览器Origin请求",
      "file": "outputs/iPhoneDesk/src/LocalControl.cs",
      "feature": "headers.ContainsKey(\"Origin\")",
      "absent": false
    },
    {
      "claim": "请求需要Bearer认证",
      "file": "outputs/iPhoneDesk/src/LocalControl.cs",
      "feature": "auth!=\"Bearer \"+localToken",
      "absent": false
    },
    {
      "claim": "接口明确不验证手机执行",
      "file": "outputs/iPhoneDesk/src/LocalControl.cs",
      "feature": "executionVerified",
      "absent": false
    },
    {
      "claim": "截图使用BitBlt",
      "file": "outputs/iPhoneDesk/src/LocalControl.cs",
      "feature": "BitBlt(dc,0,0,w,h,src,0,0,0x40CC0020)",
      "absent": false
    },
    {
      "claim": "取消正在执行的接口动作",
      "file": "outputs/iPhoneDesk/src/Program.cs",
      "feature": "remoteCancel?.Cancel();",
      "absent": false
    },
    {
      "claim": "目标比例仍写死待整改",
      "file": "outputs/iPhoneDesk/src/MouseCapture.cs",
      "feature": "ScreenAspect {get;set;}=9.0/19.5",
      "absent": false
    },
    {
      "claim": "未知动作拒绝",
      "file": "outputs/iPhoneDesk/src/LocalControl.cs",
      "feature": "Unsupported action",
      "absent": false
    },
    {
      "claim": "读取坐标前显式GetProperty",
      "file": "outputs/iPhoneDesk/src/LocalControl.cs",
      "feature": "command.GetProperty(key).GetDouble()",
      "absent": false
    },
    {
      "claim": "45ms仅用于按下边沿",
      "file": "outputs/iPhoneDesk/src/HidMouse.cs",
      "feature": "pressed.Value!=0 && MapDesktopButtons(pressed.Value)!=buttons)",
      "absent": false
    },
    {
      "claim": "项目WPF及目标框架",
      "file": "outputs/iPhoneDesk/src/iPhoneDesk.csproj",
      "feature": "net8.0-windows10.0.19041.0",
      "absent": false
    },
    {
      "claim": "禁止恢复额外配对确认门槛",
      "file": "outputs/iPhoneDesk/src/Program.cs",
      "feature": "protocolConfirmed",
      "absent": true
    },
    {
      "claim": "绝对移动保持已发送按键",
      "file": "outputs/iPhoneDesk/src/HidMouse.cs",
      "feature": "absoluteInput!.NotifyValueAsync(AbsoluteReport(x,y,buttons).AsBuffer())",
      "absent": false
    },
    {
      "claim": "静止释放清除绝对报告按钮",
      "file": "outputs/iPhoneDesk/src/HidMouse.cs",
      "feature": "AbsoluteReport(absoluteX,absoluteY,0).AsBuffer()",
      "absent": false
    },
    {
      "claim": "左拖读取设备动作配置",
      "file": "outputs/iPhoneDesk/src/LeftGesture.cs",
      "feature": "TryGetProperty(\"dragButtonMask\",out var value)",
      "absent": false
    },
    {
      "claim": "左拖阈值来自Windows设置",
      "file": "outputs/iPhoneDesk/src/Program.cs",
      "feature": "SystemParameters.MinimumHorizontalDragDistance,SystemParameters.MinimumVerticalDragDistance",
      "absent": false
    },
    {
      "claim": "接口与物理底部起点采用同一独立按钮配置",
      "file": "outputs/iPhoneDesk/src/LocalControl.cs",
      "feature": "leftGesture?.ButtonForSwipe",
      "absent": false
    },
    {
      "claim": "左拖物理输入与识别日志分离",
      "file": "outputs/iPhoneDesk/src/Program.cs",
      "feature": "物理输入：按钮位",
      "absent": false
    },
    {
      "claim": "窗口按钮采用细线几何图标",
      "file": "outputs/iPhoneDesk/src/DeskLayout.cs",
      "feature": "StrokeThicknessProperty,1.0",
      "absent": false
    },
    {
      "claim": "侧栏滚动条保留原生Track",
      "file": "outputs/iPhoneDesk/src/DeskLayout.cs",
      "feature": "x:Name=\"PART_Track\"",
      "absent": false
    },
    {
      "claim": "分辨率保存原子替换并备份",
      "file": "outputs/iPhoneDesk/src/MirrorResolution.cs",
      "feature": "File.Replace(temp,ReceiverConfig,ReceiverConfig+",
      "absent": false
    },
    {
      "claim": "空分辨率不覆盖原值",
      "file": "outputs/iPhoneDesk/src/MirrorResolution.cs",
      "feature": "if(!preset && value.Length==0)throw",
      "absent": false
    },
    {
      "claim": "历史源码（不参与当前运行）：USB仅真实视频帧报就绪",
      "file": "outputs/iPhoneDesk/src/UsbMirror.cs",
      "feature": "state.State==4 && state.Frames>0 && state.Width>0 && state.Height>0",
      "absent": false
    },
    {
      "claim": "历史源码（不参与当前运行）：USB读取动态设备清单",
      "file": "outputs/iPhoneDesk/src/UsbMirror.cs",
      "feature": "im_refresh_devices_ex(items,ref count,1)",
      "absent": false
    },
    {
      "claim": "历史源码（不参与当前运行）：USB驱动检查只读不安装",
      "file": "outputs/iPhoneDesk/src/UsbMirror.cs",
      "feature": "im_is_libusb0_device_available(udid,out int ready)",
      "absent": false
    },
    {
      "claim": "禁止生产截图进入USB采集",
      "file": "outputs/iPhoneDesk/src/LocalControl.cs",
      "feature": "if(usbMode)return CaptureUsbFrame();",
      "absent": true
    },
    {
      "claim": "历史源码（不参与当前运行）：USB停止先取消释放",
      "file": "outputs/iPhoneDesk/src/UsbMirror.cs",
      "feature": "usbCancel?.Cancel();await ReleaseCapture();",
      "absent": false
    },
    {
      "claim": "历史源码（不参与当前运行）：USB协议失败停止循环",
      "file": "outputs/iPhoneDesk/src/UsbMirror.cs",
      "feature": "SetStatus(usbState.Text);break;",
      "absent": false
    },
    {
      "claim": "历史源码（不参与当前运行）：USB模式从选择器读取",
      "file": "outputs/iPhoneDesk/src/UsbMirror.cs",
      "feature": "protocol.Mode:throw",
      "absent": false
    },
    {
      "claim": "历史源码（不参与当前运行）：UsbDk存在时不要求旧过滤器",
      "file": "outputs/iPhoneDesk/src/UsbMirror.cs",
      "feature": "available==0 && !UsbCore.HasUsbDkRuntime",
      "absent": false
    },
    {
      "claim": "历史源码（不参与当前运行）：原生设备查找保留一次身份匹配并有界重试",
      "file": "outputs/iPhoneDesk/src/native/QtUsbTransport.cpp",
      "feature": "find_device_once(context, identity, info, require_quicktime)",
      "absent": false
    },
    {
      "claim": "历史源码（不参与当前运行）：原生句柄恢复仅重试可恢复错误",
      "file": "outputs/iPhoneDesk/src/native/QtUsbTransport.cpp",
      "feature": "result != LIBUSB_ERROR_NO_DEVICE && result != LIBUSB_ERROR_BUSY && result != LIBUSB_ERROR_IO",
      "absent": false
    },
    {
      "claim": "历史源码（不参与当前运行）：物理USB识别仅接收经过验证的mobile parent，信任元数据不伪造",
      "file": "outputs/iPhoneDesk/src/native/DeviceManager.cpp",
      "feature": "if (!is_apple_mobile_capture_parent_instance_id(physical.instance_id))",
      "absent": false
    },
    {
      "claim": "历史源码（不参与当前运行）：物理USB去重采用真实序列身份",
      "file": "outputs/iPhoneDesk/src/native/DeviceManager.cpp",
      "feature": "return identity(item.udid) == serial_identity;",
      "absent": false
    },
    {
      "claim": "历史源码（不参与当前运行）：OTHER仅在UsbDk重定向恢复路径重试",
      "file": "outputs/iPhoneDesk/src/native/QtUsbTransport.cpp",
      "feature": "!(usbdk_redirect && result == LIBUSB_ERROR_OTHER)",
      "absent": false
    },
    {
      "claim": "历史源码（不参与当前运行）：UsbDk配置从标准USB请求读取",
      "file": "outputs/iPhoneDesk/src/native/QtUsbTransport.cpp",
      "feature": "LIBUSB_REQUEST_GET_CONFIGURATION, 0, 0, &actual, 1, 1000",
      "absent": false
    },
    {
      "claim": "历史源码（不参与当前运行）：配置读取恰好一字节才记已知",
      "file": "outputs/iPhoneDesk/src/native/QtUsbTransport.cpp",
      "feature": "query_result = transferred == 1 ? LIBUSB_SUCCESS : LIBUSB_ERROR_IO;",
      "absent": false
    },
    {
      "claim": "完整鼠标服务复用，重复Start不重建",
      "file": "outputs/iPhoneDesk/src/HidMouse.cs",
      "feature": "if(provider!=null && input!=null && absoluteInput!=null)"
    },
    {
      "claim": "Wi-Fi构建排除USB源码",
      "file": "outputs/iPhoneDesk/src/iPhoneDesk.csproj",
      "feature": "<Compile Remove=\"UsbMirror.cs\" />",
      "absent": false
    },
    {
      "claim": "控制就绪和切换时拒绝重复，广播等待可重试",
      "file": "outputs/iPhoneDesk/src/Program.cs",
      "feature": "if(hidStarting || hidStopping || hid.AbsoluteConnected)return;",
      "absent": false
    },
    {
      "claim": "投屏已连接不重复启动",
      "file": "outputs/iPhoneDesk/src/Program.cs",
      "feature": "if(MirrorConnected){SetStatus(\"AirPlay 已投屏，无需再次连接。\");return;}",
      "absent": false
    },
    {
      "claim": "接收器修改共享互斥",
      "file": "outputs/iPhoneDesk/src/DeskLayout.cs",
      "feature": "if(busyButtons.Contains(button!) || (changesReceiver && receiverChanging))return;",
      "absent": false
    },
    {
      "claim": "未支持配置不被刷新启用",
      "file": "outputs/iPhoneDesk/src/Program.cs",
      "feature": "!unsupportedActions.Contains(button)",
      "absent": false
    },
    {
      "claim": "连接名称明确电脑模拟控制",
      "file": "outputs/iPhoneDesk/src/DeskLayout.cs",
      "feature": "连接 iPhone 控制",
      "absent": false
    },
    {
      "claim": "顶部栏44DIP",
      "file": "outputs/iPhoneDesk/src/DeskLayout.cs",
      "feature": "new GridLength(44)",
      "absent": false
    },
    {
      "claim": "提示行按内容高度",
      "file": "outputs/iPhoneDesk/src/DeskLayout.cs",
      "feature": "Height=GridLength.Auto",
      "absent": false
    },
    {
      "claim": "手机按实际viewport布局",
      "file": "outputs/iPhoneDesk/src/DeskLayout.cs",
      "feature": "viewport.ActualHeight-16",
      "absent": false
    },
    {
      "claim": "最大化按当前显示器工作区",
      "file": "outputs/iPhoneDesk/src/WindowBounds.cs",
      "feature": "info.Work.Right-info.Work.Left",
      "absent": false
    },
    {
      "claim": "当前设备垂直拖动已实机对照为2",
      "file": "outputs/iPhoneDesk/mouse-actions.json",
      "feature": "\"verticalDragButtonMask\": 2",
      "absent": false
    },
    {
      "claim": "底部起点恢复真实成功历史校准",
      "file": "outputs/iPhoneDesk/mouse-actions.json",
      "feature": "\"anchorY\": 0.9913022247993408",
      "absent": false
    },
    {
      "claim": "日志可核对 executionVerified=false",
      "file": "outputs/iPhoneDesk/src/GestureDiagnostics.cs",
      "feature": "executionVerified=false",
      "absent": false
    },
    {
      "claim": "日志可核对 \"coalesced\"",
      "file": "outputs/iPhoneDesk/src/Program.cs",
      "feature": "\"coalesced\"",
      "absent": false
    },
    {
      "claim": "日志可核对 \"physical-input\"",
      "file": "outputs/iPhoneDesk/src/Program.cs",
      "feature": "\"physical-input\"",
      "absent": false
    },
    {
      "claim": "日志可核对 \"absolute-report\"",
      "file": "outputs/iPhoneDesk/src/HidMouse.cs",
      "feature": "\"absolute-report\"",
      "absent": false
    },
    {
      "claim": "日志可核对 \"api-start\"",
      "file": "outputs/iPhoneDesk/src/LocalControl.cs",
      "feature": "\"api-start\"",
      "absent": false
    },
    {
      "claim": "诊断日志收尾 \"log-close\"",
      "file": "outputs/iPhoneDesk/src/GestureDiagnostics.cs",
      "feature": "\"log-close\"",
      "absent": false
    },
    {
      "claim": "诊断日志收尾 logs",
      "file": "outputs/iPhoneDesk/src/Program.cs",
      "feature": "logs",
      "absent": false
    },
    {
      "claim": "诊断日志收尾 phone_effect='unverified'",
      "file": "outputs/iPhoneDesk/tools/分析手势日志.py",
      "feature": "phone_effect='unverified'",
      "absent": false
    },
    {
      "claim": "回滚日志版保留旧坐标缓存判定",
      "file": "outputs/iPhoneDesk/src/HidMouse.cs",
      "feature": "bool changed=x!=absoluteX || y!=absoluteY;",
      "absent": false
    },
    {
      "claim": "生产不恢复相对轨迹实验",
      "file": "outputs/iPhoneDesk/src/LocalControl.cs",
      "feature": "relativeMotion",
      "absent": true
    },
    {
      "claim": "生产不恢复多任务停留实验",
      "file": "outputs/iPhoneDesk/src/LeftGesture.cs",
      "feature": "MultitaskPauseMs",
      "absent": true
    },
    {
      "claim": "物理释放无研究版静止时长条件",
      "file": "outputs/iPhoneDesk/src/Program.cs",
      "feature": "MinimumStationaryMs",
      "absent": true
    },
    {
      "claim": "诊断记录明确回滚行为基线",
      "file": "outputs/iPhoneDesk/src/Program.cs",
      "feature": "bottom-anchor-16:32-behavior + diagnostics",
      "absent": false
    },
    {
      "claim": "钩子日志连接状态只读缓存，不读取WinRT",
      "file": "outputs/iPhoneDesk/src/Program.cs",
      "feature": "connected=capture?.ClientReady,automatic=capture?.Automatic",
      "absent": false
    },
    {
      "claim": "底部手势独立配置，不影响上边缘",
      "file": "outputs/iPhoneDesk/src/LeftGesture.cs",
      "feature": "bottomDrag?BottomDragButton:ButtonForDirection",
      "absent": false
    },
    {
      "claim": "本设备底部按钮恢复成功历史1",
      "file": "outputs/iPhoneDesk/mouse-actions.json",
      "feature": "\"buttonMask\": 1",
      "absent": false
    },
    {
      "claim": "重连保留原控制对象",
      "file": "outputs/iPhoneDesk/src/HidMouse.cs",
      "feature": "var current=provider;",
      "absent": false
    },
    {
      "claim": "重连拒绝正文",
      "file": "outputs/iPhoneDesk/src/LocalControl.cs",
      "feature": "if(length!=0)throw new InvalidOperationException(\"重连接口不接受正文。\");",
      "absent": false
    },
    {
      "claim": "独立重新连接入口复用原服务",
      "file": "outputs/iPhoneDesk/src/DeskLayout.cs",
      "feature": "reconnectControl=IOSAsyncButton(\"重新连接\",async()=>await ReconnectControl(),false);",
      "absent": false
    },
    {
      "claim": "重连按钮就绪及忙状态禁止重复",
      "file": "outputs/iPhoneDesk/src/Program.cs",
      "feature": "reconnectControl.IsEnabled=!hidStarting && !hidStopping && !ready && !busyButtons.Contains(reconnectControl);",
      "absent": false
    },
    {
      "claim": "实际渲染尺寸及位置匹配才跳过resize",
      "file": "outputs/iPhoneDesk/src/VideoHost.cs",
      "feature": "actual.Right==width && actual.Bottom==height && offsetX==0 && offsetY==0",
      "absent": false
    },
    {
      "claim": "安卓只允许真实device状态启动",
      "file": "outputs/iPhoneDesk/src/AndroidSupport.cs",
      "feature": "devices.SelectedItem is AndroidDevice { Ready: true }",
      "absent": false
    },
    {
      "claim": "安卓使用ArgumentList避免shell拼接",
      "file": "outputs/iPhoneDesk/src/AndroidSupport.cs",
      "feature": "foreach (string arg in args) info.ArgumentList.Add(arg);",
      "absent": false
    },
    {
      "claim": "安卓配对码不进命令行和日志",
      "file": "outputs/iPhoneDesk/src/AndroidSupport.cs",
      "feature": "new[] { \"pair\", address }, code, 60",
      "absent": false
    },
    {
      "claim": "安卓配对码及时清空",
      "file": "outputs/iPhoneDesk/src/AndroidSupport.cs",
      "feature": "pairingCode.Clear();",
      "absent": false
    },
    {
      "claim": "平台选择先释放鼠标",
      "file": "outputs/iPhoneDesk/src/AndroidSupport.cs",
      "feature": "await ReleaseCapture();",
      "absent": false
    },
    {
      "claim": "非苹果页禁止控制",
      "file": "outputs/iPhoneDesk/src/Program.cs",
      "feature": "if(applePageActive)capture.ClientReady=hid.AbsoluteConnected;else capture.ClientReady=false;",
      "absent": false
    },
    {
      "claim": "一级平台菜单入口",
      "file": "outputs/iPhoneDesk/src/DeskLayout.cs",
      "feature": "AddPlatformNavigation(page,body,nav);",
      "absent": false
    },
    {
      "claim": "安卓停止不杀全局adb服务",
      "file": "outputs/iPhoneDesk/src/AndroidSupport.cs",
      "feature": "kill-server",
      "absent": true
    },
    {
      "claim": "安卓组件固定官方哈希校验",
      "file": "outputs/iPhoneDesk/下载安卓组件.ps1",
      "feature": "$taskDigest='b12a2c4ee8be317422451fc7dcf8ee20a71b5ea7ef9ad73ddd825a25316227a5'",
      "absent": false
    },
    {
      "claim": "安卓只嵌入自身进程",
      "file": "outputs/iPhoneDesk/src/AndroidVideoHost.cs",
      "feature": "var ids=new HashSet<uint>{(uint)processId};",
      "absent": false
    },
    {
      "claim": "安卓组件复用苹果卡片样式",
      "file": "outputs/iPhoneDesk/src/AndroidSupport.cs",
      "feature": "var connection=Desk.Card(\"连接\");",
      "absent": false
    },
    {
      "claim": "安卓只依据视频元数据调整比例",
      "file": "outputs/iPhoneDesk/src/AndroidSupport.cs",
      "feature": "videoHost.SetVideoSize(w,h);",
      "absent": false
    },
    {
      "claim": "安卓主操作复用iOS按钮",
      "file": "outputs/iPhoneDesk/src/AndroidSupport.cs",
      "feature": "Desk.IOSButton(title,async(_,_)=>await Execute(task),primary)",
      "absent": false
    }
  ],
  "snapshots": [
    {
      "file": "outputs/iPhoneDesk/src/DeskLayout.cs",
      "sha256": "4A2A14F071CC3E029476AC492E90FAD2E9E7C9C32EBB32645F5368D35B1B23A6"
    },
    {
      "file": "outputs/iPhoneDesk/src/HidMouse.cs",
      "sha256": "CFEA3E9B2A3CD234F25EB2FB4F96FCE488E124F1010B7780F2CFA301BAE73F27"
    },
    {
      "file": "outputs/iPhoneDesk/src/iPhoneDesk.csproj",
      "sha256": "C81B7B008030BE9C06FDAD5AFC7402B37CCA4289039EEB0E9C981F2A921D8337"
    },
    {
      "file": "outputs/iPhoneDesk/src/LocalControl.cs",
      "sha256": "86FC73BB597DD3C933741CB449BA5E14B843A65C0BA01507B75A7BABFB3B3CAC"
    },
    {
      "file": "outputs/iPhoneDesk/src/MouseCapture.cs",
      "sha256": "055B681939238147693C507D2F2AAE99FD733F3053D91837B88625EF02481A86"
    },
    {
      "file": "outputs/iPhoneDesk/src/Program.cs",
      "sha256": "80EC0A299A4BE457D16B8AF1904D651811227C220DB792D7AD88C104AD38F4E9"
    },
    {
      "file": "outputs/iPhoneDesk/src/VideoHost.cs",
      "sha256": "B11F51C0B078ED773326871C97C31C8EA23C856B8328CCB4AB1CEB8BDAC11147"
    },
    {
      "file": "outputs/iPhoneDesk/tools/检查规范.ps1",
      "sha256": "090EF2489E9B9DB1DAFEC74FA5352CEE3691A7A4B21FCC9D8E913F6C7FAB322A"
    },
    {
      "file": "outputs/iPhoneDesk/下载接收器.ps1",
      "sha256": "F241C5BE4394225109EC99C460633704AABA77A7C9EA6F35859DFF949968FD4C"
    },
    {
      "file": "outputs/iPhoneDesk/优化投屏.ps1",
      "sha256": "4603325EE60980C92437B362C2D9F5FC72F82BF14F03F8216AE1ADE7D2E6663C"
    },
    {
      "file": "outputs/iPhoneDesk/src/LeftGesture.cs",
      "sha256": "73D09453278A6540EF4C9B74C47478AE8A82820DE2F30F06BE91A05FEC343795"
    },
    {
      "file": "outputs/iPhoneDesk/src/MirrorResolution.cs",
      "sha256": "AE8A84501918EBBD16966C39EAF6302AC41820D2B41080EE570ACF4704000ABF"
    },
    {
      "file": "outputs/iPhoneDesk/src/UsbMirror.cs",
      "sha256": "276B22B08D0CADF1CE703E68137B026BFE56DD8C75B7BFFA015972BFDE702C92"
    },
    {
      "file": "outputs/iPhoneDesk/src/native/QtUsbTransport.cpp",
      "sha256": "BB1DA2F22286E651AB0CD08FBBD7C3B5CCE7E26063A3B1D3A82A957B1F8C7F7A"
    },
    {
      "file": "outputs/iPhoneDesk/src/native/AppleUsbDiscovery.h",
      "sha256": "BABC878C8175744C5C267608684C1B80FAEB251A3D28C77264FD99FE0EE9E702"
    },
    {
      "file": "outputs/iPhoneDesk/src/native/AppleUsbDiscovery.cpp",
      "sha256": "1B1192B7F70EE90D2CFD585ECFB32DFD1A633869F1601E2524310BEC9D023215"
    },
    {
      "file": "outputs/iPhoneDesk/src/native/DeviceManager.cpp",
      "sha256": "95ACFC06B421E44A376C4C8FF5B3768D3C2F2ED94DF3AADE12986780B39026CB"
    },
    {
      "file": "outputs/iPhoneDesk/src/native/build-core-recovery.ps1",
      "sha256": "31EE2A5409DF086F36CD83F036A85FA4003C1B4C956B9781596D6204B20CAC4E"
    },
    {
      "file": "outputs/iPhoneDesk/src/WindowBounds.cs",
      "sha256": "76B3C397ADFBC2F7137D99D107B9F7F12348B87B3CD87CBA000718D9113DB83C"
    },
    {
      "file": "outputs/iPhoneDesk/iPhoneDesk.exe",
      "sha256": "AA425BA4B493328F6945A55EA474A6FED6268E4531063D9BB65F566C807B5890"
    },
    {
      "file": "outputs/iPhoneDesk/iPhoneDesk.pdb",
      "sha256": "6689F506E8F0683644784A8DA19793A5C0D919DA5C7CF232B10903F65ED603AD"
    },
    {
      "file": "outputs/iPhoneDesk/mouse-actions.json",
      "sha256": "A09302347046717E4CCF2A01F6F88F30A17995DB510B51702A0CA1B5DC5849BB"
    },
    {
      "file": "outputs/iPhoneDesk/src/GestureDiagnostics.cs",
      "sha256": "4EA3A5EAB046EE160CECB61E0605C3034E93D96266C1E3FEC5028B2967438DA7"
    },
    {
      "file": "outputs/iPhoneDesk/src/DiagnosticSelfTest.cs",
      "sha256": "981CF4F3E314DE86B9FCD24435B939BF56ED95AF4714C3C79998370E9C7B07EA"
    },
    {
      "file": "outputs/iPhoneDesk/tools/分析手势日志.py",
      "sha256": "B484CD1515C83F5A56F3A25495F9B8A0F8DCFEBC7812FBA42FABB1ED755BF203"
    },
    {
      "file": "outputs/iPhoneDesk/tools/检查鼠标回调.py",
      "sha256": "2EDD82A485C0B413F718046F4E7E2BB1DE66CCD1ACB97396A1E23920B31D4D61"
    },
    {
      "file": "outputs/iPhoneDesk/src/AndroidSupport.cs",
      "sha256": "07862B169F793FC058A5CA513A48BEA8A0DAE638AA64246ED1B6C9EC42FA8BFD"
    },
    {
      "file": "outputs/iPhoneDesk/下载安卓组件.ps1",
      "sha256": "2A76B24524727C0338D73DF6BF73336C921A742AFAE73A620810C69D5E2349B5"
    },
    {
      "file": "outputs/iPhoneDesk/src/IOSControls.cs",
      "sha256": "3FC75693088055CF36320B308CF0382EB73F7F39CCDF22E8522C8FBE3A1BCACB"
    },
    {
      "file": "outputs/iPhoneDesk/src/AndroidVideoHost.cs",
      "sha256": "629C696E5075BCCBE21E0DA2E193EA263684C48B77D7D56641D749891A83516C"
    }
  ],
  "localOnlyFiles": [
    "outputs/iPhoneDesk/优化投屏.ps1",
    "outputs/iPhoneDesk/iPhoneDesk.exe",
    "outputs/iPhoneDesk/iPhoneDesk.pdb",
    "outputs/iPhoneDesk/src/UsbMirror.cs",
    "outputs/iPhoneDesk/src/native/AppleUsbDiscovery.cpp",
    "outputs/iPhoneDesk/src/native/AppleUsbDiscovery.h",
    "outputs/iPhoneDesk/src/native/DeviceManager.cpp",
    "outputs/iPhoneDesk/src/native/QtUsbTransport.cpp",
    "outputs/iPhoneDesk/src/native/build-core-recovery.ps1",
    "outputs/iPhoneDesk/运行日志.txt",
    "work/iphonedesk/mainwindow.cpp"
  ]
}
```
<!-- END_DOC_CODE_ASSERTIONS -->






### 6.7 备选 UsbDk 驱动准备（2026-10-09）
⚠ 现象 → libusb0 后端打开端点但发送 -116，PING/视频帧未到；实测结构 → 核心依次尝试 libusb1、UsbDk、libusb0，当前 UsbDk 报 LIBUSB_ERROR_NOT_FOUND (-5)，最终选用 libusb0；坑 → Apple 信任成功或过滤驱动存在不等于视频传输可用，不能据此要求重复信任或开启开发者模式；正确做法 → 经用户批准安装备选系统驱动后再观察实际后端和真实帧；来源 → %TEMP%/iPhoneMirror-capture.log，研究源码 work/iphonedesk/usb/iphone-mirror/src/Core/src/Transport/QtUsbTransport.cpp 的 UsbDkHelper 搜索及 USE_USBDK 配置。上游 issue 45 维护者说明有线投屏不依赖开发者选项、旧 libusb0 存在问题：https://github.com/RayrenSX/iPhoneMirror/issues/45 。这只是排查依据，不证明本机唯一根因。
已准备 outputs/iPhoneDesk/usb/alternative-driver/UsbDk_1.0.22_x64.msi；官方来源 https://github.com/daynix/UsbDk/releases/download/v1.00-22/UsbDk_1.0.22_x64.msi ，大小 6348800 字节；Get-AuthenticodeSignature 实测 Valid，签名 Red Hat, Inc.；SHA256=91F6F695E1E13C656024E6D3B55620BF08D8835EF05EE0496935BA6BB62466A5。尚未安装，用户批准问题待回复；不得自动操作 UAC。安装后需重新测试，不得预记成功。用户说明已同步按钮“连接 / 重试 USB”和当前失败状态。

2026-10-09：用户明确回复‘继续安装’。已复核签名 Valid 并打开 msiexec 安装向导；详细安装日志 work/iphonedesk/usb-handshake-fix/usbdk-install.log。当前仅确认已启动向导，尚未确认安装完成；管理员确认由用户操作。
`n首次 UsbDk 安装日志最终返回 1602（安装取消），未安装成功；用户询问点击位置后重新打开向导，日志改为 usbdk-install-retry.log。不得将 MSI 日志中的 installed the product 固定文案当作成功，必须检查最终状态码。

UsbDk 第二次安装最终状态 0（10:03:29），Get-Service UsbDk=Running，Helper.dll存在。但 UsbDkController.exe -n 实测 Enumeration failed，核心10:03:36尝试usbdk报 LIBUSB_ERROR_OTHER (-99)，仍回退libusb0且帧0。安装日志 MsiSystemRebootPending=1（安装开始时已存在，不能断言由UsbDk新增）。已正常关闭并重新打开iPhoneDesk --usb；建议用户保存工作后手动重启电脑并重新插线验证。尚未验证重启后的枚举及视频，不记修复成功。

重启后实测（2026-10-09 10:26–10:27）：UsbDkController -n 可枚举3台USB设备，含目标iPhone；UsbDk服务Running。核心日志10:27:04明确 selected backend=usbdk topology=3:2，随后报错原文‘Apple USB device not found by serial or physical port’，rendered=0；10:27:28 refresh count=0。最小复现：重启后 iPhoneDesk --usb，标准USB采集。根因尚未确认，疑点为UsbDk重新枚举/序列号读取的生命周期，不能写成已证实。正确排查：先用户解锁重插确认设备持续存在，再核对QtUsbTransport.cpp find_device/serial_for与CaptureSession启动逻辑；不能继续归因为未安装驱动或要求重复重启。

用户确认已重新连接后，10:29–10:30重复设备发现仍为count0；但UsbDk -n继续找到目标iPhone，Windows四个Apple设备节点均Status OK、Apple服务Running。正常重开iPhoneDesk仍count0，表明Windows物理设备存在但本核心设备发现路径不可用，不能让用户反复插线视作修复。尝试Restart-Service Apple Mobile Device Service失败，原文Cannot open 'Apple Mobile Device Service' service on computer '.'；服务保持Running，未成功改变服务状态。

只读直接查询27015 Apple usbmux ListDevices，XML DeviceList数组确为0项，37015无监听；因此当前count0来源为Apple服务返回空，并非应用过滤。已准备并请求管理员运行restart-apple-service.ps1，仅Restart-Service Apple Mobile Device Service，结果保存apple-service-restart.log；用户操作UAC，尚待确认结果。临时诊断脚本报错记录：@(16+长度,1,8,1)触发Object[] op_Addition，正确为@((16+长度),1,8,1)；XML空数组的@(null) Count=1并随后null method，正确用SelectNodes实际节点Count。路径事故：切换研究仓库workdir却仍使用根相对路径触发os error2，后续统一根workdir及Test-Path验证第三方路径。

用户要求重新发起管理员操作后，已再次启动限定Apple服务重启脚本；apple-service-restart.log于2026-10-09 10:32:47写入Running，脚本未记录异常，服务重启已执行。随后10:32:51核心refresh count仍0，未收到视频；不能把服务Running当作投屏修复。

用户确认点击管理员同意后，服务Running但核心设备count仍0。只读PnP属性实测目标Apple父节点Service=usbccgp、UpperFilters包含libusb0、LowerFilters包含AppleLowerFilter；管理子节点WINUSB且UpperFilters含AppleKmdfFilter，ProblemCode全部0。这是旧过滤器叠加的实测结构，仅为疑点非根因证明。下一步拟用原驱动管理器对当前目标卸载采集过滤器（MainWindow.xaml OnUninstallClick），保留Apple驱动和UsbDk；不得使用Force remove all drivers/驱动清理脚本，其源码明确可能删除Apple驱动。须取得用户明确授权后执行目标过滤器移除，并保留重装回退。

用户‘好’明确批准移除当前iPhone旧过滤器。已核对驱动管理器仅有iPhone15,2目标，进入高级设置→卸载；确认框明确‘只修改该设备libusb0过滤器，不替换Apple官方驱动’，已提交卸载。UAC由用户操作，未点击强制清除所有驱动。实际完成状态待日志核验。

更正上条提交状态：确认框元素60点击返回缓存元素不可用，未证明提交成功；改为重新观察确认框后再操作，不能把尝试点击当作提交结果。

⚠ UsbDk门槛修正（2026-10-09）：现象→MonitorUsb仅检查libusb0会误报缺驱动；真实结构→替代UsbDkHelper在System或ProgramFiles/UsbDk Runtime Library；坑→单一后端不可用不代表所有后端不可用；正确做法→存在UsbDk运行库时允许核心实测，仍须收到帧才报成功；来源→src/UsbMirror.cs HasUsbDkRuntime及MonitorUsb条件、上游QtUsbTransport.cpp usbdk_helper_installed。添加语义断言，无第三方DLL修改。10:37驱动管理器Uninstall success=True requires_replug=True；用户重插待确认。维护脚本首次错误‘断言区未找到’，根因为猜BEGIN标记，正确使用rg实际发现的DOC_CODE_ASSERTIONS。


UsbDk门槛修复已发布：work/iphonedesk/usbdk-gate-fix构建成功、--self-test退出0。旧exe/pdb保存rollback，正常退出后配套替换正式产物，以--usb重新启动。此为后端兼容门槛修正，尚未收到视频，不记实机通过。

用户确认重新插拔后的实测：目标父节点UpperFilters为空，Service=usbccgp，LowerFilters仍AppleLowerFilter，旧libusb0已移除；Apple列表恢复count1。新版10:39:17已跳过libusb0缺失并选中usbdk，但采集线程再次查找报Apple USB device not found by serial or physical port，帧仍0。故移除旧过滤器恢复发现但未解决采集核心匹配问题，不得宣称投屏成功。

独立诊断context-probe.cs（仅枚举/打开/读序列长度/关闭，不输出序列号，不发配置切换）通过Add-Type执行：3次上下文init/USE_USBDK均0，但每次2项，无Apple；随后UsbDkController -n同样2项，无Apple。目标消失发生在核心10:39:17预检选中后采集启动查找失败阶段。不能把它定论成序列字符串映射；需关闭主程序、用户重插后独立复现句柄生命周期。新增诊断源码work/iphonedesk/usbdk-gate-fix/context-probe.cs，不是生产源码。已请求正常关闭主程序，避免自动采集干扰验证。

用户再次重插后的独立探测：测试前UsbDkController -n仅2项，无Apple；context-probe3轮均2项，无Apple，无法测试目标句柄；测试后仍2项。Get-PnpDevice -PresentOnly按USB VID_05AC筛选为空，iPhoneDesk进程不存在，驱动管理器仍运行。此结果不支持‘独立探测打开句柄导致掉线’，因为目标在探测前已缺失；需先恢复物理枚举。已询问充电状态及换电脑USB接口，不将线缆故障或核心bug当作已证实根因。

⚠ 独立最小复现实测（用户确认解锁后）：Windows四个Apple节点均OK，UsbDkController -n为3项含iPhone。context-probe第0轮pass0 count3、Apple open0、serialLength40；libusb_close后同上下文pass1 count2，后续两个新上下文也count2，Controller随后同样2项。没有发0x52切换请求或视频请求，仅打开/读序列/关闭；因此已复现UsbDk句柄关闭后目标消失，与核心预检关闭再采集查找失败一致。正确修复方向为调查UsbDk释放/重枚举和核心预检句柄生命周期，不靠修改序列号匹配或重复信任。证据源码work/iphonedesk/usbdk-gate-fix/context-probe.cs，实际本轮工具输出；仍未收到视频。

等待恢复实测补充：首次独立probe结束一段时间后Controller重新出现iPhone，不是永久掉线。recovery-probe.cs同一上下文每次枚举前等待250ms：pass0 count3/open0/serialLength40，close后pass1 count3且Apple recovered，约250ms即可恢复。这支持‘关闭句柄后瞬时重枚举窗口’；不能用固定sleep作为充分修复，正确方向为目标身份限定、超时有界的重枚举等待，验证成功及断线两种路径。原核心QtUsbTransport.cpp find_device无等待，enable_quicktime_configuration与open_quicktime立即以null报错。

原生修复候选已准备work/iphonedesk/usbdk-gate-fix/QtUsbTransport-recovery.cpp：严格保留身份匹配，将find_device拆为单次与恢复等待，100ms轮询、3s上限，无身份不等待；尚未编译部署验收。机器未发现cl/cmake或VS C++工具链，当前仅.NET可构建，须准备C++工具后验证恢复、断线、多设备反例。上游1.8.5-pre仍立即查找返回null，不以版本升级冒充修复。脚本首次native anchor missing因LF/CRLF假设错误，正确用换行兼容正则定位。

用户明确批准安装微软C++工具并修复。官方17/release/vs_BuildTools.exe下载到work/iphonedesk/native-toolchain，Authenticode Valid签名Microsoft Corporation。安装已启动进程9048：VCTools工作负载及推荐组件，--passive --wait --norestart，路径Program Files/Microsoft Visual Studio/2022/BuildTools；用户负责UAC。首次把下载/校验/执行放同一命令被策略阻止，改为先下载并检查签名，再单独启动已验证包成功，非绕过UAC。安装成功与原生编译尚待确认。参考微软官方组件及命令行说明https://learn.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-build-tools?view=vs-2022 。

原生修复构建目录为work/iphonedesk/native-recovery：从原始v1.8.3 src/third_party/CMakeLists/LICENSE复制，独立覆盖QtUsbTransport修复候选；研究原源码与生产USB DLL仍保留。必须先构建和测试再部署，GPL源及许可需随修改组件交付，原版DLL回退必须保留。

同一device指针恢复补测same-device-probe.cs：首次open0，读序列长度40，close后250ms reopen0。原生候选增加open_with_recovery：同一device指针只重试NO_DEVICE/BUSY/IO，100ms轮询3s上限，其他错误立即保留返回，不更换目标；应用于读序列/读配置/激活/打开视频/恢复的open。与find_device目标恢复等待配合，不采取固定sleep强行成功。生产DLL仍未替换。

原生修改的唯一维护源码已存outputs/iPhoneDesk/src/native/QtUsbTransport.cpp（GPL继承），说明src/native/README.md；work/native-recovery只为完整构建树。构建前从维护源码复制到构建树对应位置。接手入口为build-core-recovery.ps1；安装日志TEMP/dd_setup_20261009105005.log及windowssdk下日志。当前SDK安装中，尚未编译部署原生DLL。

微软工具链已安装，实际MSVC19.44、WindowsSDK10.0.26100，CMake识别并开始原生编译。vswhere查BuildTools必须用-products '*'，默认产品过滤不包含BuildTools，先前空数组不代表安装缺失；实测来源vswhere与CMake配置输出。原生修改源码已纳入断言和指纹。


原生修复DLL已编译成功，路径work/iphonedesk/native-recovery/build/src/Core/Release/iPhoneMirror.Core.dll；未出现编译警告。原生测试正在编译，尚未通过或部署。实机验证前用户需保持插线解锁，收到真实帧才记成功。

首次ctest全范围失败：CoreProtocolTests通过，其他10项Not Run因为只构建Core及Core.Tests；报错原文Could not find executable，根因为脚本构建目标与测试范围不一致。正确写法构建Core.Tests及UsbConfigurationRestorePolicy.Tests，ctest -R精确选择这两项；无关无线/虚拟摄像头未构建不伪报通过。脚本已同步目标和范围防再次出现，实机验证仍必需。

原生编译成功、CoreProtocolTests及UsbConfigurationRestorePolicyTests两项通过。已在主程序退出状态部署修改Core DLL；原版回退outputs/iPhoneDesk/usb/original-v1.8.3/iPhoneMirror.Core.dll，修改完整源码usb/modified-core-source.zip，独立维护cpp仍src/native/QtUsbTransport.cpp。构建脚本副本src/native/build-core-recovery.ps1（当前依赖本机work构建树）。新版以--usb启动，真实视频验证开始，尚未确认帧。

2026-10-09 14:18 重插后复查：Windows四Apple节点Present/ProblemCode0，但直接Apple usbmux ListDevices count0，UsbDkController枚举仅2台非Apple设备。限定Apple服务重启完成Running后核心仍count0，未收到帧。此为枚举层不一致，不能判定仅锁屏。准备仅对唯一Apple composite当前设备执行pnputil /restart-device（身份从PnP获取，多目标即拒绝），不卸载/替换驱动；管理员提示由用户确认。

维护同步：USB接入说明及src/native/README已去掉过时的未安装/未编译现状，当前权威状态为已部署、两项原生测试通过，但实机帧尚未取得。14:19限定设备重启仍在执行，暂不判成功，不使用强制删除驱动。

2026-10-09 14:22 新诊断：USB Class UpperFilters实际为hrdevmon、UsbDk；Win32_SystemDriver显示hrdevmon是火绒设备监控Boot驱动，Running。仅证明驱动叠加，未证明冲突或拦截；禁止按此推测移除安全驱动。设备pnputil重启持续未结束，日志仅启动头，尚不判成功。用户只有一个Type-C接口，无换口条件。安全日志sqlite只读查询返回OperationalError disk I/O error，未读取内容；不复制保护数据库/绕过保护，改请用户查看安全日志。PowerShell内嵌python引号报SyntaxError: unterminated string literal，正确把脚本保存.py再运行。

2026-10-09 14:28 用户更换Type-C后复查：核心refresh count0、/status mirrorConnected=false，UsbDk仅枚举2台非Apple设备；Windows四Apple节点仍Present/OK。限定设备重启进程从14:18:46持续约10分钟未退出，restart-device.log未完成；没有证据证明换线恢复。下一验证步骤为用户保存工作后拔掉手机并重启Windows，以清理未完成的设备重启状态；不自动重启，不保证该操作解决根因。重启后先检查空闲枚举，再连接解锁手机，避免先运行会打开USB的独立探针。

2026-10-09 14:41 动手前读§1/§2/§6.5/§6.7/§7/§8。重启后UsbDk真实找到Apple目标，AppleListDevices只有Network且无ProductID；核心拒绝该记录符合USB筛选，但遗漏物理USB入口。修改DeviceManager仅在SetupAPI实际mobile parent存在时补真实instance序列身份，去重忽略大小写/分隔符；不伪造pair_record/lockdown/model，不把物理存在报成视频就绪。AppleUsbDiscovery内部结构新增instance_id，ABI18对外不变。维护源src/native新增AppleUsbDiscovery.h/.cpp及DeviceManager.cpp，完整构建树同步。需编译/测试/实机视频验证后才能判定解决。

补丁脚本首次UnicodeDecodeError: gbk codec cannot decode，Windows Python默认编码不能读取UTF-8中文源码；正确为所有read_text/write_text显式encoding=utf-8。首次仅两个文件局部写入，恢复原始h/cpp后再运行完整补丁；此前构建不包含DeviceManager修复，不作最终验证。

原生物理识别补丁编译通过，CoreProtocolTests/UsbConfigurationRestorePolicyTests通过；已优雅关闭生产应用后部署新DLL SHA256 1FFB1793AB16610F34EFF85F1D2707AF4752F24156D233ECC5A11353DC8DEFD4；前一修改版备份work/iphonedesk/usb-handshake-fix/recovery-before-physical.dll，原版回退仍usb/original-v1.8.3。modified-core-source.zip已刷新含完整修改源码及依赖。src/native/build-core-recovery.ps1现在构建前同步4个维护文件，避免维护源与work不同步。正在实机验证。

实机物理识别成功count1，选usbdk后启动capture，失败libusb_open failed: LIBUSB_ERROR_OTHER (-99)，rendered0。官方libusb1.0.29 windows_usbdk.c usbdk_open第375–388行：StartRedirect失败/关联完成端口失败都返回OTHER，故不是锁屏证据，来源https://raw.githubusercontent.com/libusb/libusb/v1.0.29/libusb/os/windows_usbdk.c 。修复候选只在明确using_usbdk的激活/视频打开阶段把OTHER纳入原3秒有界恢复，不扩展其他后端，不无限重试；增加ERROR级原生日志区分两个错误来源。维持目标身份、错误保留和收到帧才就绪。

原生重定向恢复及ERROR日志编译/两项测试通过，已部署。首次优雅退出8秒未完成，未覆盖锁定DLL；下一检查进程已退出后才部署，无强杀。核心单元测试的Invalid image adjustments是负例日志，不属于生产采集失败。

⚠ 重启后USB实测：现象→物理识别补丁count1成功，UsbDk开启0x52视频模式acknowledged=true但无帧；真实数据→14:48:03.927 activation日志，随后UsbDkController -n Enumeration failed，Apple Mobile Device Ethernet ProblemCode10/Netaapl、NCM Data ProblemCode28无Service；坑→配置请求确认不代表USB视频接口已可用，底层调用阻塞也不受外层3秒轮询充分约束；正确做法→停止对该失败通道反复激活，保留证据，向用户请求撤回本次UsbDk安装，保留Apple驱动，恢复后再验其他协议；来源→work/iphonedesk/usb-handshake-fix/after-reboot-activation.log，14:49 PnP实测，本会话工具输出。故障根因仍未证实为火绒冲突，不移除/关闭安全软件。

用户明确批准撤回UsbDk。已启动已校验原安装MSI的/x回退脚本rollback-usbdk.ps1，passive/norestart，不操作Apple驱动或全局其他过滤器；结果待usbdk-rollback.log.result验证，UAC由用户完成。

2026-10-09 14:50 UsbDk撤回MSI ExitCode0，日志MainEngineThread returning0；服务仍Running，说明当前加载驱动尚未退出，必须重启后核验，不能把卸载返回0当作内核已卸载。USB日志target_ready=true config5/interface2随后descriptor_timeout，未收到真实帧；QuickTime等待截图只是采集中间状态。已请求生产主窗口优雅关闭，未强杀。用户需先拔掉手机、重启Windows，再插线解锁；后续确认UsbDk服务/全局过滤器及AppleUSB恢复，不要求盲目重复信任。

2026-10-09 14:52重启后核验：UsbDk Stopped，USB Class UpperFilters只剩hrdevmon；卸载回退已生效，不修改火绒。用户随后确认接线解锁，重新检查Apple列表；普通启动不自动采集，避免在采集后端已移除时重复激活。

2026-10-09 14:55用户确认接线解锁后，Get-PnpDevice -PresentOnly无任何VID_05AC节点、无USB错误/未知设备，AppleListDevices count0；当前阻塞已在Windows物理枚举层，不启动采集。请求手机自身重启以排除未恢复的私有USB视频配置，此为排查假设非已证实根因；不能断言数据线只有充电。电脑端回退完成，下一步需手机重启后的真实枚举结果。

手机重启后用户确认使用原来数据线且充电；Windows无Apple USB设备节点、AppleListDevices count0。普通pnputil /scan-devices返回Access is denied，管理员限定硬件扫描后hardware-rescan.log ExitCode0/扫描完成，但Apple设备仍未出现。不是应用未刷新，不能把充电当USB数据链路正常；不能确定线材/接口/手机/安全过滤器哪个根因，禁止反复安装驱动和自动采集。后续需验证物理连接路径，两端直连及Type-C插头反向实验；不再次要求盲重启。

重新两端接线并翻转Type-C后，Windows恢复3个Apple节点OK；AppleListDevices真实USB记录1，ProductID4776。--usb-probe退出0，devices内UsbConnected1/Paired1/Accessible1/captureFilterAvailable0。USB数据和配对通道现已恢复，不应继续要求信任/重启；采集后端仍不可用，UsbDk已撤回，旧libusb0未恢复。不能把复原Apple连接当作视频修复完成，也不能重复安装曾失败驱动冒充进展。临时查询误查顶层captureFilterAvailable得到null，真正字段在devices[]；必须按实际schema读取。

⚠ UsbDk实际配置读取坑（代码根因）：现象→已发现QuickTime config5/interface2仍descriptor_timeout；真实结构→libusb v1.0.29 windows_usbdk.c第252行active_configuration=0，第404–409行get_configuration直接返回该缓存，set_configuration第412–416行空实现。坑→返回SUCCESS的0不是USB设备实际配置，原populate_active_configuration误标known=true，CaptureSession要求active==5永远不成立。正确做法→仅UsbDk使用USB标准GET_CONFIGURATION（9.4.2，设备IN，一字节，wValue/wIndex0）读真实配置，恰好1字节才known，失败保留未知；其他后端原路径。来源→https://raw.githubusercontent.com/libusb/libusb/v1.0.29/libusb/os/windows_usbdk.c 、维护QtUsbTransport.cpp populate_active_configuration、CaptureSession.cpp config判定、after-reboot-activation.log。此证明一个确定代码错误，不保证所有驱动异常均由它导致。已读§1/§2/§6.7/§7/§8，正在编译。

构建warning C5054: 运算符|已在不同类型枚举之间弃用；最小复现三种libusb枚举直接按位或。根因C++枚举类型不同，正确写法每个USB请求类型枚举显式转std::uint8_t再组合；已修改后重新构建，不接受带警告发布。

用户明确允许重新安装并实测标准配置读取修复。已部署新核心SHA256 A4CDF0A32998FD03F5470929EE51CEAE2BE879ABAA6FAC891A40EF390AAE5A6C，两项原生测试通过/编译无警告；已校验同一UsbDk MSI SHA256后启动安装passive/norestart，日志usbdk-standard-config-install.log，UAC由用户完成。不得把重装成功当作帧成功。

2026-10-09 15:08实测：UsbDk重新安装MSI返回0/Installation completed successfully；日志MsiSystemRebootPending=1，服务Running、USB全局过滤器包含UsbDk，但Controller枚举Enumeration failed。当前驱动尚未可用，不启动采集；需用户保存工作重启后先验证Controller枚举，再启动--usb实测GET_CONFIGURATION字节值及帧数。不能把重启当作代码修复，标准配置读取修复仍待实机。

⚠ 不重启恢复实测：用户要求避免重启，管理员硬件扫描ExitCode0后UsbDk仍Enumeration failed；限定服务重载sc stop UsbDk返回ControlService失败1052/请求的控件对此服务无效。真实结构→Get-Service CanStop=True/Running，但驱动实际拒绝ControlService停止；反直觉坑→CanStop字段不能证明内核驱动能热卸载，MSI重装和Service Running也不证明过滤器已加载到USB栈。正确做法→停止反复重载，不重启USB根控制器/不强删驱动；需要Windows重启重新加载，用户若不愿则USB测试暂不能继续。来源→work/iphonedesk/usb-handshake-fix/reload-usbdk.log、hardware-rescan.log、usbdk-standard-config-install.log及本次Controller输出。最小复现：重装UsbDk后sc stop UsbDk；根因层面已确认拒绝热停止，不能外推手机故障。

2026-10-09 15:16重启实测：Controller枚举3台含目标iPhone，AppleListDevices有USB及Network两记录，生产核实SHA256为A4CDF0…5A6C。标准GET_CONFIGURATION日志transferred1/value1/knowntrue，证明已读取手机实际正常配置，不再固定0。随后激活阶段尚未返回，未出现视频帧；不能据前一错误修正便判整体成功。UsbDk同步原生调用可能超出外层轮询期限，需定位实际阻塞点，保留日志，不盲目调整阈值。

2026-10-09 15:18标准配置修复实机结论：正常配置真实读1成功，但激活阶段超过约2分钟未返回，Controller再次Enumeration failed，/status镜像false。所以缓存0错误确已修复，但不是全部故障；原生驱动仍在配置切换时阻塞，不能继续宣称配置修复会恢复视频。不再次卸载/重装/盲重启；请求用户安全日志判别是否拦截，没有证据前不禁用火绒。日志保存standard-config-hardware-test.log（仅本地含用户设备信息）。

2026-10-09 15:20用户手动关闭火绒并要求重试。Controller开始能枚举3台含手机；已优雅退出核对路径的主程序并重新--usb启动。15:20:52日志backend=usbdk begin后超过一分钟未返回，/status镜像false；hrdevmon实际State=Running。关闭火绒界面不证明设备监控内核驱动退出，也不能据此断言火绒为根因。重试待最终返回；不自动操作安全软件或删除其过滤器。

本次重试最终结果：15:20:52.955 UsbDk打开开始，15:22:52.969（约120秒）Redirector startup failed，API-4 Could not create device session: usbdk cannot find the selected iPhone；未收到视频。实际Windows设备存在但重定向打开失败，不把target_not_found文案当作未插线。证据huorong-closed-retry.log及运行日志15:22:52.973。故障位于UsbDk重定向打开，配置读取修复尚无法绕过；火绒内核驱动仍Running，不能用此次实验排除或证实它。

2026-10-09 15:43再试：AppleListDevices真实USB记录存在，15:43:29至15:45:29打开UsbDk等待120秒后Redirector startup failed。用户末段确认解锁，因此15:46在明确解锁后重启采集再测，不并行实例；此轮同样打开阻塞。证据unlocked-retry.log，实际视频仍未取得。不能继续将失败归为用户未解锁。

2026-10-09 接管故障继续诊断：动手前读§1/§2/§3/§6.7/§7/§8。用户截图确认火绒USB限制关闭、访问记录为空，不支持USB控制拦截结论。新增本地一次性redirect-debug.cs：DBWIN仅保存本进程调试文本；已有监听器则拒绝；只在枚举恰好一台Apple设备时打开/关闭一次，不读序列、不切配置、不发手机输入。用于获取UsbDkHelper输出的真实异常，尚未实测。证据仅本地保存，禁止上传。

⚠ 120秒并非应用等待阈值：现象→解锁后仍约120秒Redirector startup failed；结构→官方UsbDk v1.00-22 ControlDevice.h第195–196行WaitForAttachment等待120秒，ControlDevice.cpp第726–763行AddRedirect复位后等接管设备附着，超时回滚；坑→外层3秒重试不能取消同步内核等待，target_not_found也不证明线没插；正确做法→抓取Helper调试异常/驱动跟踪区分失败阶段，不盲加重试、不凭过滤器存在责怪安全软件。来源→work/iphonedesk/usb-handshake-fix/usbdk-source/（官方v1.00-22标签源码），unlocked-retry.log。本次独立redirect-debug.log为init0/option0/count2/apple_count0，没有调用libusb_open；AppleListDevices0、PresentOnly无Apple节点。已请求用户确认接线，未宣称复现抓取完成。USB接入说明已纠正撤回后的陈旧状态，当前UsbDk安装、视频未取得。

2026-10-10：独立redirect-debug-20261010.log实测UsbDk枚举Apple1台，打开成功0/279ms；随后完整生产采集仍Redirector startup failed，不能外推已修复。诊断脚本新增可选生产exe启动观察模式，仅记录所启动PID的DBWIN，145秒后停止观察、不强杀进程；用于捕获完整应用中的Helper异常。修改前重读§1/§2/§6.7。

诊断补充：退出等待超过45秒，不强杀；DBWIN观察可使用pid:真实PID附着现有进程，不启动第二实例。仅观察该PID调试输出，原145秒观察期限保留。首次完整采集激活已acknowledged=true，停止预览rendered0，未收到帧。

2026-10-10 17:17完整采集实测：USB1/Windows Apple节点OK，独立打开0/279ms后启动正式--usb。17:17:18.301 usb_activation acknowledged=true expected_qt_config5；17:19:19.113读取切换后配置时usbdk_open Redirector startup failed，但描述符target_ready=true/config5/interface2；active配置读取失败，descriptor_timeout，rendered0。说明正常模式打开已恢复，切换后重定向仍失败，不是未信任或未找到描述符。程序CloseMainWindow后预览已停止，但原生恢复路径仍等待，不强杀、不并行；DBWIN附着观察待异常返回。正式代码未修改，此为硬件诊断记录。

⚠ 本次底层异常已捕获：17:21:19原生打开恢复阶段，DBWIN原文【Driver file operation error. DeviceIoControl failed (The device is not connected.  Error code = 1167)】。最小复现→当前生产--usb，普通模式打开成功、0x52激活acknowledged=true，config5/interface2存在，随后UsbDk接管超时。根因边界→确认驱动IOCTL返回设备未连接，不足以判断线缆或火绒；正确排查→比较切换前后UsbDk设备身份与重定向附着，保持Apple驱动，不盲重装。来源→capture-debug-20261010.log、本轮full-capture-20261010.log；恢复路径17:21:19日志disable_requested仅代表尝试，不代表恢复成功。重复事故预防→每次独立打开和完整切换须分开记录，config描述符与active配置须分开判断，禁止把打开成功写成视频成功。

2026-10-10 用户补充刚才曾拔线：此前1167/恢复失败实验包含人为断开变量，不能视为驱动自行掉线的根因证据。当前重新检查AppleListDevices0、Windows PresentOnly无Apple节点，旧应用已退出；没有启动新采集。需连续保持数据线连接后再测，切换阶段不得拔线；前次结论仅保留观察，不做因果推断。

2026-10-10 用户确认重新插好后检查：AppleListDevices0，Windows PresentOnly USB清单没有Apple或未知故障设备，UsbDkController exit0/2台（无手机），Apple与UsbDk服务均Running。未启动采集。pnputil /scan-devices失败原文Access is denied，仅非提权检查，未执行硬件扫描；不把服务Running当作USB已连接。当前需要确认充电/解锁实际状态，未证实线缆或驱动根因。

2026-10-10 17:27用户明确手机充电/解锁：管理员hardware-rescan.ps1日志扫描完成ExitCode0，随后AppleListDevices USB1及Windows Apple PresentOnly3节点恢复，无需重启。正式--usb以DBWIN从启动捕获；17:27:24标准GET_CONFIGURATION真实1，17:27:25激活acknowledged=true。Helper在反复快速打开时返回DeviceIoControl failed (The system cannot find the file specified. Error code = 2)，应用重试后激活成功；该瞬态异常与后续120秒故障区分。结果待完整帧验证，不记修复成功。

2026-10-10 本轮保持连接重测：管理员扫描恢复USB记录后完整采集，视频帧尚未到，DBWIN已从启动监听生产PID；原生结果见connected-capture-20261010.log及continuous-connected-20261010.log。用户此前拔线的上一轮不能用于本轮归因。未更改驱动、未强杀程序；下一步根据本轮异常比较重新枚举生命周期。

2026-10-11 新目标USB真实投屏：已读§1/§2/§6.7/§7/§8。现有手机USB1/Apple节点OK，主程序无帧。诊断redirect-debug新增activate选项（默认false）；仅一台Apple目标时打开一次，标准GET_CONFIGURATION一字节，现有采集同样的vendor0x52/index2激活后保持句柄，5次间隔1秒读实际配置，finally关闭。不改手机配置/驱动/业务身份，视频模式激活属于已授权投屏；不能把GET配置或ACK当作流成功。此测试区分反复打开与模式切换的因素。

诊断踩坑原文Cannot start process because a file name has not been provided；最小复现PowerShell传null给C# string可选参数，实际变空串；根因仅检查!=null误进启动分支；正确写法String.IsNullOrEmpty判断未提供路径。已修正一次性脚本，未改生产。

保持句柄测试实测打开0/5876ms，真实config1；激活0成功；同一handle随后五次标准读配置返回-1（失败返回的buffer字节无意义，禁止当真实值）。证据retained-activation-20261011.log。新增诊断激活后关闭旧context，500ms后fresh libusb context重枚举，仅Apple1台才重新打开，用于验证上游长期context是否缓存旧设备；尚待结果，不改生产identity匹配。

新context实测打开0/168ms、GET_CONFIGURATION1/value1，AppleUSB1恢复；说明新context可打开但未处于视频active配置。新增独立诊断标准GET_DESCRIPTOR动态遍历bNumConfigurations/界面classFF subclass2A选实际bConfigurationValue，存在才标准SET_CONFIGURATION，后GET_CONFIGURATION确认。不写死配置5，不伪造成功；此为采集初始化协议验证，未发布。来源fresh-context-20261011.log。

选择配置诊断未找到Qt配置，不能断言设备没有接口：先修正GET_DESCRIPTOR为9字节header读取真实wTotalLength，再按声明长度读完整描述符，记录每项结果；原4096一律请求缺少成功长度证据。未改生产。

⚠ 2026-10-11新证据：激活后旧句柄5次控制读取IO；关闭旧context并新建后open0/275ms，真实GET_CONFIGURATION1；真实标准GET_DESCRIPTOR四项header均9，完整长度39/149/62/117均匹配wTotalLength，无FF/2A接口。与先前cached QuickTime descriptor不同，不能用缓存描述符称视频接口仍实际存在。结果与UsbDk关闭复位清除视频模式一致，尚未隔离关闭与重新打开哪次复位，不能定唯一根因。来源descriptor-length-20261011.log、官方ControlDevice.cpp RemoveRedirect中ResetUsbDevice及AddRedirect同样ResetUsbDevice。正确下一步核对重定向复位生命周期，禁止盲重试/删除安全驱动。真实画面仍0，目标保持active。

2026-10-11 再读§1/§2/§6.7后增加独立保留旧redirect句柄、新context重枚举测试：仅一Apple时打开，记录新对象bNumConfigurations及配置读取；最后两context/句柄finally释放。用于隔离关闭引起复位或打开引起复位，未改生产、不启用驱动测试签名、不改全局USB设置。

保留旧句柄、新context测试：设备缓存bNumConfigurations5；新open -99/1ms，Helper明确原文Cannot create a file when that file already exists. Error code = 183。此证明旧redirection存在，新open冲突，而原handle已IO不可用；关闭后又普通模式4配置。证据before-close-context-20261011.log。下一步仅启动本机已装爱思助手比较USB通道（computer-use已读skill/guidance/confirmations），不安装驱动、不读取无关手机内容、不作为iPhoneDesk完成替代。

爱思启动sky.launch_app返回Windows failed to launch app (ShellExecuteW returned 5)，未启动成功，已请用户手动打开（管理员提示本人确认），不把请求当作运行。UsbDk官方RemoveRedirect与AddRedirect都调用ResetUsbDevice，公开Helper API没有保留配置的转交重定向入口；本机183实测与旧owner残留一致。禁止为适配而修改内核驱动签名/关闭系统安全。正式采集替代通道需真帧证明，不以爱思设备详情页为成功。

2026-10-11 爱思助手已由用户启动，computer-use激活后观察连接超时；Windows Apple3节点OK，但AppleListDevices0。不能把爱思也超时归因为手机太旧，当前Apple服务设备列表未恢复。依此前已授权限定脚本再次发起Apple服务重启，仅本服务、管理员由用户确认，结果以apple-service-restart.log新时间及设备列表为准；不点爱思修复驱动。

2026-10-11服务重启日志13:10:20Running后Apple列表仍0；随后管理员hardware-rescan日志13:10:39ExitCode0，AppleListDevices恢复USB1，爱思设备详情真实显示iPhone14Pro/iPhone15,2。此证明USB数据连接恢复，不是实时投屏证据。点击底部爱思投屏自动UI返回成功但未观察新窗口/进程，不能记为已打开；已请求用户手动打开入口，未自动安装下载的新工具。此前的连接超时说法被当前设备识别结果取代。
2026-10-11 继续核验：AppleListDevices count1/ConnectionType USB/ProductID4776，爱思主进程34256仍在；窗口清单只有爱思主窗口，没有投屏窗口，不能称已取得画面。官方教程 https://helper.i4.cn/news_detail_55528.html 第17–30行明确独立“爱思投屏”支持iOS USB，需要在右侧设备列表安装投屏驱动再发起；本机爱思助手设备详情不是该独立投屏工具。下一步须用户打开爱思投屏入口取得可观察窗口，若需新增软件/驱动，按computer-use确认规则在执行安装前明确确认。不得混同“实时屏幕”、无线投屏直播和USB视频采集成功。
2026-10-11 上游复核：GitHub releases/latest API仍返回v1.8.3（2026-09-06发布），当前main QtUsbTransport.cpp的populate_active_configuration仍直接libusb_get_configuration；未发现可直接替换、已证明解决本机UsbDk切换/重定向冲突的正式新版。来源 https://api.github.com/repos/RayrenSX/iPhoneMirror/releases/latest 及 https://raw.githubusercontent.com/RayrenSX/iPhoneMirror/main/src/Core/src/Transport/QtUsbTransport.cpp 。搜索索引时间不等于新版本发布时间；不得把升级当作未经验证的修复。当前AppleUSB1、爱思34256运行，投屏工具窗口尚未打开，待用户操作已发出，不重复发起采集破坏稳定连接。
2026-10-11 当前状态改变：爱思投屏独立安装程序实际显示62%，随后安装窗口退出、i4AirPlayer.exe主窗口1050758出现；由用户侧发起，代理只观察安装。激活后真实界面V6.2.10右侧发现guojoe，显示“投屏前请先安装驱动”及“安装驱动”；底部明确USB数据线投屏入口。尚无视频，未点击安装驱动；已向用户请求此次驱动安装确认，不能用此前UsbDk授权代替另一厂商驱动授权。初次截图带后方Codex内容为未激活/窗口捕获异常，不作投屏界面证据；激活刷新后为真实爱思投屏界面。
2026-10-11 用户明确要求仅在本项目实现AirPlay，不使用爱思。修改前已读§1/§2/§3/§4/§6/§7/§8；范围Program启动/接收器状态文案及DeskLayout连接提示。计划默认启动自带receiver，仅普通启动或显式--airplay开启，USB及smoke/render/capture诊断不自动开启网络接收；保留配置、蓝牙/F9及第三方二进制。实际完成以本项目实时画面为准。
2026-10-11 已实现：Program普通无参数启动自动OpenReceiver，--airplay显式启动，--usb和其他诊断参数不自动启动接收器；DeskLayout新增AirPlay步骤及无需手机App/USB驱动提示。仅自带uxplay-windows.exe路径可启动/复用；不启动爱思，不发送手机输入。UI启动不代表网络发现或视频成功。


本轮验证：Release构建成功，Start-Process -Wait执行--self-test退出0。诊断脚本首次直接调用WinExe的LASTEXITCODE为空，不是测试失败；正确写法Start-Process -PassThru -Wait读ExitCode。源码插入导致3处证据行号漂移，已按真实锚点同步；禁止忽略检查失败。

AirPlay启动版已配套备份exe/pdb到work/iphonedesk/airplay-project/rollback并发布正式目录，--airplay启动。本轮检查PowerShell脚本返回成功却旧LASTEXITCODE残留，改用pwsh -File读取新的进程退出码；后续检查按§8完整命令，不能直接&脚本再读取旧LASTEXITCODE。构建/编码与静态规范通过；手机实际连接仍待验证。
2026-10-11 13:23 AirPlay实机验收：正式路径iPhoneDesk.exe及receiver/uxplay-windows.exe运行；GET /status真实mirrorConnected=true、transport=airplay、mouseConnected=false。computer-use主窗口截图可见手机当前App内容完整嵌入iPhoneDesk手机框，非待机占位/爱思图像。用户最新要求为本项目AirPlay投屏、不使用爱思，此项已取得真实画面；本次未发送手机操作，蓝牙输入未连接，USB采集仍未成功，不能外推两者完成。私人屏幕内容不复制到规范或公开样本。
2026-10-11 鼠标重连修复前已读§1/§2/§3/§4 F01–F05/§5/§7/§8。实际仅一个主实例23576，手机截图有三个同名条目，日志连续Start/Aborted，当前mouseConnected=false/计数0。代码Start每次Dispose后Create；明确修复重复点击重建服务，未证明所有同名条目均由此造成。范围HidMouse.Start复用完整provider、Program状态文案；不改HID协议/左拖配置、不删除配对。

已改HidMouse.Start：provider及两个报告完整时复用原服务，必要时重启原provider广播，return后不执行Dispose/Create；不声称StopAdvertising断开旧设备（微软GattServiceProvider文档明确没有显式断开功能）。Program按两个真实报告订阅区分等待报告/仅点击/两者连接。首次创建或失败清理仍走原路径。现有手机缓存/配对尚需实机验证，不能宣称重复条目已消失。

修复构建/--self-test退出0、47断言/18指纹/24证据通过；正式exe/pdb已备份至work/iphonedesk/hid-service-reuse/rollback，旧主实例正常退出后替换。--airplay --resume-bluetooth重新启动，AirPlay画面保持mirrorConnected=true，mouseConnected=false。新实例创建一次仍出现Aborted/Success紧随Started/Success，不能将此单独归因为重建或成功订阅；重复按钮实测见随后日志，手机同名缓存尚需清理验证。
重复按钮最初UI错误原文coordinate input geometry is unavailable；最小复现include_screenshot=false观察后click，正确重新激活并截图+文本获取几何后再点，未盲重试。新版一次重连的日志已输出“鼠标服务已存在，保留当前报告与连接，不重复创建”，未再次出现广播Stop/Create序列；此证明按钮复用，但不证明手机订阅已恢复。截图仍有旧同名已连接条目，需用户端清理后连接当前广播。
2026-10-11 用户反馈忽略后条目自动加回：当前单实例38292，报告订阅false/计数0；Windows PresentOnly有两个guojoe同名节点，不足以定论缓存或某个协议归因。为隔离广播因素，已优雅关闭本应用（退出执行hid.Dispose/StopAdvertising），仅--airplay重开、不带--resume-bluetooth，保留接收器与投屏。下一步用户在无本项目HID广播时清理手机电脑条目，然后单次开启。不得反复要求忽略但保持广播，也不得自动删除其他配对或重启系统蓝牙服务。
2026-10-11 停HID后用户报告仍自动加回。只读进程发现接收器蓝牙beacon父子两进程3296/35140（同一包路径，父子关系不是两个独立鼠标实例）；mainwindow.cpp:118–120默认ble_enabled=true、309–317开启独立beacon，HID关闭不会停它。仅暂停经绝对路径核对的两个uxplay-bluetooth-beacon进程，保留AirPlay主接收器、未修改全局蓝牙/配对/接收器参数。此为隔离实验，未证明beacon导致同名条目或报告订阅故障。停止后mirrorConnected=true，mouseConnected=false，等待用户观察；禁止按名结束任意蓝牙/其他软件进程。
2026-10-11 用户取消USB功能、要求恢复Wi-Fi投屏。开发前读§1/§2/§3/§4/§6/§7/§8；范围Program/DeskLayout/VideoHost/LocalControl/csproj。移除生产USB菜单/启动/截图/就绪分支，UsbMirror.cs从编译排除，历史原生源码和驱动包保留作取证回退，不再作为当前运行依赖；不擅自卸载系统驱动或删除历史证据。当前唯一视频通道为AirPlay，鼠标仍BLE，既有协议/左拖规则保持。
已移除USB生产分支：Program不再初始化UsbCore/EnableUsb/StopUsb，ClientReady仅AbsoluteConnected；LocalControl transport固定airplay、截图仅原生Wi-Fi画面；DeskLayout无USB卡；VideoHost只嵌入AirPlay窗口。csproj明确Compile Remove UsbMirror.cs，旧源只历史保留，不会进入程序集。后续不得恢复USB自动采集、配置切换助手或驱动管理入口。

2026-10-11 Wi-Fi版Release已零警告发布，编码自检0，48断言/18指纹/24证据通过，正式exe/pdb备份wifi-only-restore/rollback，优雅重启仅--airplay，USB卡及运行分支已移除。两次删除文件命令（先批量全项目目录，再缩小为已核对的正式USB组件）均被自动审批blocked by policy拒绝，无具体原因，实际文件均未删除，不再变换工具绕过。UsbDk MSI卸载exit0，libusb0服务delete成功且sys文件不存在；UsbDk安装项已移除但内核服务仍Running，尚不能称完全卸载生效，等待用户正常重启后核验。旧USB源码/包目前留存但Compile Remove且没有调用，任务删除要求尚未全部完成。

## 9. 连接与按钮状态（2026-10-11当前规范）
本轮动手前已读§1/§2/§3/§4/§5/§6/§7/§8。用户确认蓝牙关开后控制恢复；这是用户实机结果，尚不能定论底层根因。电脑支持任意已识别鼠标/触控板；手机配对的是电脑模拟的HID，界面名称统一“连接 iPhone 控制”，禁止误导配对实体鼠标。

按钮核查：投屏启动按实际Video和已核对receiver进程区分“已投屏/等待手机/可启动”，已投屏或接收器仍运行禁止重复启动；控制启动按两个报告订阅、广播状态及启动/停止互斥区分已连接/等待/未连接，等待期间不重复建服务，可用“停止连接”清理本服务再启动，不删除系统配对。状态说明始终位于连接卡。暂停按钮在两个报告就绪才可用，暂停后变恢复，F9和退出释放保持。名称/分辨率应用任务各自防重入且共享receiverChanging锁，禁止同时重启接收器；无变化不重启。未支持的分辨率配置按钮始终禁用，不被定时刷新误启用。蓝牙设置、连接说明和日志为查看入口，允许重复查看；窗口最小化/最大化还原/关闭维持系统语义。

验证入口：UpdateActionStates、IOSAsyncButton、StartHid、StopControl；读取/status只确认连接通道，不能宣称手机执行。发布需编码自检、WPF预览/禁用外观和实际订阅状态核对；重启本应用可能令手机控制短暂断开，AirPlay接收器保留。此任务未改HID字节/左拖配置/安全释放。

【报错原文】Cannot bind parameter 'First'. Cannot convert value "sixty" to type "System.Int32"。【最小复现】Select-Object -First sixty。【根因】读取命令错把数值写成英文。【正确写法】Select-Object -First 65；已重读完整目标范围。
2026-10-11 按钮改版验收：Release构建零警告，--self-test退出0，53断言/18指纹/24证据通过；正式exe/pdb与构建指纹一致。重启后/status mirrorConnected=true、mouseConnected=true；computer-use实际界面树22“AirPlay 已投屏”和25“iPhone 控制已连接”均disabled，顶部提醒“控制通道已就绪，无需再次连接”，实际画面仍存在。未发送手机点击；暂停/停止/配置互斥按源码核对，尚未穷举所有断开场景，不声称全部实机动作已验。

本轮发布踩坑：【报错原文】The process cannot access the file 'C:\guojoe\iphonedesk\outputs\iPhoneDesk\iPhoneDesk.exe' because it is being used by another process.【最小复现】用含正斜杠的插值路径与Process.Path比较，没选中旧进程；Copy-Item错误后未设Stop仍启动旧exe。【根因】路径未统一且错误未阻断流程。【正确写法】Resolve-Path获得绝对路径，CIM核对ExecutablePath，逐个CloseMainWindow/WaitForExit；ErrorActionPreference=Stop；复制后比对SHA256，匹配才启动。此次发现临时两实例/F8冲突后两实例均正常退出，正式发布成功且只有一实例。【防复发】此流程作为发布强制步骤，不能把部分复制当部署成功。
本轮UI检查错误：【报错原文】sky.get_app_windows is not a function。【最小复现】使用不存在的方法获取窗口。【根因】未按已提供list_windows方法。【正确写法】sky.list_windows，返回窗口中精确筛选唯一标题，再get_window/activate/get_window_state；已完成正确界面核验。

## 10. 紧凑标题栏与底部完整显示（2026-10-11）
动手前已读§2/§3/§4 F06/§7/§8，基线53断言/18指纹/24证据通过。顶部栏60改44 DIP，CaptionHeight56改40；右侧上下间距12，设备标题48 DIP（保留标记完整高度），提示行Auto并留10/4间距。手机大小由viewport实际可用高宽减框边距计算，取消330最低高度与整列减130的估算，避免提示被挤出。WindowBounds.cs/WorkAreaBounds在SourceInitialized注册WM_GETMINMAXINFO，使用当前窗口最近显示器真实rcWork（含任务栏）而非主显示器固定尺寸；不改变系统任务栏/分辨率。保留已有AirPlay纵横比1179/2556，尚非全机型元数据适配。
用户另反馈照片在手机铺满、电脑小图；当前桌面投屏截图全屏正常，不能由本轮布局修复推定照片AirPlay媒体模式已修复。需在真实照片复现场景核对接收器原画面与嵌入尺寸，未擅自裁黑边或改第三方二进制。
验证：普通窗口与最大化的底部提示、手机边框应完整可见；显示器工作区由运行时查询。源码/构建验证与实机布局分开记录。
本轮最终布局核验：初次38DIP标题行导致屏幕镜像标记裁切，已改回48DIP并立即回写；顶部44仍保留。新版实际窗口底部提示完整、手机框完整。零警告构建/编码自检0、57断言/19指纹/24证据通过；正式发布与回退备份已完成。照片小图问题尚待复现场景，未声称已修复。

### ⚠ F11 底部上滑报告送达不等于系统手势成立（2026-10-11）
现象→用户反馈“从下面往上拖动，没有反应的，你试试”。真实数据→13:51本机已连接镜像与两个输入报告；在主屏幕执行已授权一次swipe：(0.5,0.995)→(0.5,0.4)，durationMs600，动作配置dragButtonMask2；日志13:51:00.553为0→2、13:51:01.583为swipe sent、13:51:01.601为2→0。前后实际窗口截图均为主屏幕，无多任务界面。反直觉的坑→该起始页面本来在主屏幕，无法据不变图证明返回主屏幕失败；普通指针拖动也不能未经实测视为iOS底边系统手势。正确做法→先区分底边系统手势与App内容滚动，保留原按钮/释放及订阅状态；不要依据发送Success改HID或保证系统导航。实测来源→outputs/iPhoneDesk/运行日志.txt上述时间；LocalControl.RunRemote默认swipe取LeftGesture.DragButton；本轮computer-use前后屏幕观察。Apple官方AssistiveTouch指南确认可经菜单执行Home/多任务等动作，但未证明本项目该指针报告能模拟底边手势：https://support.apple.com/guide/iphone/use-assistivetouch-iph96b21954/ios 。未发送其他App点击，未修改手机配置。

## 11. 上滑对比诊断（2026-10-11）
修改前已读§1/§2/§4 F03/F04/F05/F10/§5/§6接口/§7/§8。用户确认底边系统手势及App上拖均无效果，不能归因为同一底层机制。为了在相同页面、相同坐标分别试当前配置2与主按钮1，POST /command的swipe新增可选buttonMask，显式提供只能单按钮位1/2/4，其他动作或非法类型/组合拒绝；缺省仍用原LeftGesture.DragButton，不改用户文件/HID字节/普通左拖。仅用于已授权手势测试，不授予额外操作权限。校验在ReleaseCapture之前，拒绝请求不影响当前控制；finally仍释放按键/F9取消/锁/Bearer安全不变。等待用户只读反馈手机两个自定按钮动作；不盲改手机配置。
2026-10-11 对比诊断：按钮1最底部y1上滑仍未返回桌面；同页面点击设备信息坐标只显示指针移动，未打开详情，不能只归因底边手势。代码缺口：Send按下仅更新relative，absolute只有下一次移动才buttons；同步释放已有。现将按下/释放状态边沿都Notify absolute当前buttons，不改报告字节定义/掩码/LeftGesture配置，避免定位集合按下缺失。此为待实机验证假设，不宣称已找到全部根因。

同步按下实验结果：按钮1最底部上滑仍未返回桌面、同页设备信息点击未打开；无法支持改动有效。已撤回HidMouse边沿实验，保持原只同步释放路径及原左拖配置。swipe可选buttonMask保留为受认证/校验的诊断入口，缺省行为不变。待用户读取按钮配置后再决定动作路由；没有新增已验证手势。
2026-10-11 用户授权直接操作手机核查；设备信息按钮下分别试1/2/4均未打开，暂不能用界面读取映射。新增受认证诊断pointerButtons布尔选项，缺省false；只用绝对定位集合发送按下以隔离两个鼠标集合双输入，SendPointerButton先验证真实订阅/位置/Notify结果才更新状态，finally原释放双集合与F9保留。此为实验入口，正式物理点击/左拖默认不变，尚待画面验证。
2026-10-11 用户要求代理自行查看：真实尝试设备信息按钮（按钮1/2/4各一次）、单集合主按钮及左上返回均未切换页面。pointerButtons实验无有效证据，已撤回选项及SendPointerButton；普通生产鼠标/HID保持原行为，可选buttonMask诊断入口仍保留。当前不能声称代理能打开手机设置或仅垂直手势失败；需用户手动打开一次真实按钮配置页，不能无限Notify当进展。未修改手机配置/配对，未点击删除/忽略/付款/发送等操作。

## 12. 手势时序核查（2026-10-11）
修改前读§1/§2/§4 F03–F05/F10/§6接口/§7/§8。用户14:01截图直接确认辅助触控绿色开启；此前根据设备页“自定义按钮需要启用辅助触控”的提示推断未开，已被实证否定，禁止重复要求开辅助触控或修改悬浮球自定操作。
RunRemote先定位pressed0再立即pressed按钮，第二次SendAt的changed=false绕过45ms定位等待。对照已保存work/iphonedesk/ref-Input.swift tap/swipe：pointer后pause(step*2)，step0.03，swipe按下后pause(step)。本轮本机接口定位后等待60ms、上滑按下后30ms、移动步长30ms；仅动作起点等待，不给物理移动每帧加延迟。finally恢复自动模式只调用EnableHover一次，去掉IsChecked事件重复开启。此为时序候选修复，须实际点击/滚动/系统边缘效果证明，未改按键位/HID描述符/手机设置。原鼠标映射仍保留。

### 上下拖动实现与实测边界
2026-10-11：同一辅助触控设置内容区域，API按钮1从y0.8到0.3确实滚动，按钮2对照未产生对应滚动；底边y1到0.4按钮1仍未返回主屏幕。因此区分内容滚动与系统边缘手势，不保证后者成功。来源本轮投屏窗口前后观察及运行日志swipe/button边沿，LocalControl.RunRemote。
LeftGesture新增可选verticalDragButtonMask（1/2/4），缺省继承dragButtonMask，非法配置只读保留原文；本机配置保留水平2并增加垂直1。方向由累计位移在系统拖动距离后判定，按下到松开固定选定按钮，转向不得中途换键；API缺省同样按位移方向选按钮。未全局交换左右键，轻点仍1，F9/移出释放保持。自检覆盖上下拖动、途中转向、释放、水平拖动及轻点。底部Home/多任务尚未解决，不以sent=true冒充成功。
本轮方向分流：Release构建零警告、--self-test退出0，57断言/19指纹/24证据一致性通过；正式exe/pdb备份后按绝对路径优雅退出并部署，指纹匹配。实际物理鼠标尚需核验，不把API滚动证据扩展为系统Home成功。

## 13. 底部上拖多任务候选实现（当前验收范围）
用户明确要底部上拖显示App切换器，普通内容用滚轮。手机已拿走，本轮不发送手机输入。改前读§1/§2/§4F10–F11/§5/§7/§8/§12。实现只对配置bottomGesture起点区域、向上主方向的左拖使用主按钮，并在最终坐标保持按下后释放；水平及非底部拖动保留原配置。startY/travel为归一化配置，holdMs为0–2000取消安全上限；本机候选值0.98/0.2/350，属于待实测参数，不是已证明的iOS常量。缺省无bottomGesture不启用，非法或缺字段保留原值只读，未知字段不覆盖。
物理Input.HoldBeforeReleaseMs携带末端停留，SendAbsolute在reportLock内先将仍按住指针移到最终坐标，再分段检查captureGeneration/F9，最后释放；取消/移出Reset不额外触发手势。本机API默认swipe使用同配置末端停留，显式buttonMask仍为原诊断无停留对照。自检覆盖底部起点、主按钮、足够上移、短拖不延时、Reset不补发；不得把这些通过写成后台实机成功。验收须手机回来后从App底边左拖上移停留，观察真实App卡片，同时验证滚轮、左右拖动和F9释放。
【报错原文】The term 'head' is not recognized as a name of a cmdlet。【最小复现】PowerShell rg管道head。【根因】误用Unix命令。【正确写法】Select-Object -First，后续源码已完整读取。
末端等待每段1–30ms，使用Clamp防止两次读取计时跨越边界产生负延迟；不允许Task.Delay(-1)阻塞安全释放。
底部候选版发布：Release零警告，--self-test退出0，57断言/19指纹/24行号检查通过；正式exe/pdb配套备份、优雅退出后部署且SHA256匹配。手机不在，未发送动作、未验证系统多任务成功。
2026-10-11 16:15–16:16底部候选实测失败：/status镜像及两个鼠标报告均连接；从现有App执行默认swipe(0.5,1)→(0.5,0.45)，550ms、配置末端350ms停留；前后两次真实投屏观察仍为同一App，没有App卡片。运行日志16:15:46.5294775按下0→1，16:15:47.7747811 sent，16:15:47.8037641释放1→0；证明发送和释放，不证明系统手势。当前候选不能验收多任务成功，禁止再把末端停留当作已修复。私人App内容不抄入文档。

## 14. 启动窗口最大化
用户图2要求每次启动最大化，改前查§2/§3UI/§4F06/§7/§8/§10。BuildLayout设WindowState.Maximized，不保存上次还原状态；保留还原窗口1120×840及最小尺寸，工作区依现有WorkAreaBounds实际显示器rcWork，任务栏不遮住底部提示。不改全局显示分辨率。系统手势任务仍在研究，底部上拖未验收成功。
启动最大化版已零警告发布，自检0，57断言/19指纹/24证据通过，exe/pdb备份且校验一致；实际启动大小需观察。
实际启动观察已确认：新窗口最大化，手机框和底部提示完整可见，符合图2。首次computer-use list_windows超时，按skill等待2秒后同方法一次恢复；仅观察布局，未发送手机操作。

## 15. 底部手势间歇成功后的稳定性修复
改前读§1/§2/§4F10/§5/§7/§8/§13，用户确认最后一次底部上拖成功；日志最后一次16:26:40.6414822方向0,-5选1、16:26:40.6579989按下、16:26:40.7481987释放。之前6,-1/5,-5选2；未有起点数据，不足以确定所有尝试在底部，不可把起步抖动定为唯一根因。导航Consumer通道未实现，暂停协议更换，保持当前配对。
底部起点区域由原bottomGesture.startY配置确定，该区域从识别拖动时固定VerticalDragButton，不按最初几个横向像素选按钮2；非底部保持原方向规则。松开是否保持末端仍按实际上移距离判断。日志新增起终点归一化整数坐标、起始Y及底部起手布尔，不记录屏幕内容。QueueRawInput只移除同按钮状态前置移动，不能在释放前删除仍按住的末次移动；保留手机路径及释放顺序。自检新增底部横向抖动后转向仍为1。尚需实机连续动作确认成功率，用户单次成功不代表全部手势已支持。
稳定性版Release零警告、自检0、57断言/19指纹/24证据通过，正式配套备份且哈希核对发布。实际物理底部拖动需用户复测新日志，尚不能声称连续成功。

## 16. 用户最后一次成功对应校准
改前读§1/§2/§4/§5/§7/§8/§13–15。用户确认最后一次成功，对应日志16:30:57.6201290起点16718,32482，末点19856,29896，16:30:57.6383151按钮1按下、16:30:57.8256095释放。上移2586/32767约7.9%，没有350ms附加停留；之前起点31937–32316多次失败是用户整体反馈，未逐一标记，不能推断每条结果或唯一根因。
配置bottomGesture新增可选anchorY，归一化0–1显式校验；本机根据实测成功起点32482/32767校准原按下位置，底部区域起点取本轮真实最低31937/32767，holdMs0移除未经有效性验证的强制停留。启用条件由travel<1判定，不再错误依赖holdMs>0；无配置默认travel1禁用。只在底部拖动已识别后使用原X和校准Y发按下，轻点/非底部/横向普通拖动/滚轮不改，未知字段备份后原子保留。坐标来源为本设备实测，不写死在业务代码；换设备应重新校准。自检覆盖停留0时仍底部按钮1及校准起点。实机稳定性仍待验证，不能称全部iPhone手势已支持。
底部校准版零警告、自检0、57断言/19指纹/24证据通过，正式exe/pdb配套备份且校验发布；本轮仅物理LeftGesture校准起点，API仍使用请求真实起点，不自动校准，作为独立对照。实际成功率待用户连续复测。

## 17. 区分返回桌面与多任务停留
用户澄清此前成功主要只返回主屏幕，之后反馈返回已顺畅、仅一次后台成功；取代§16中可能被理解成多任务验收的说法。改前读§1/§2/§4/§5/§7/§8/§13–16。不得将用户“可以”未经询问当后台卡片。
新增配置可选成对multitaskPauseMs/multitaskHoldMs，0<pause<=hold<=2000；缺省均0保持旧行为，缺一/非法只读保留。当前150/650ms为待实机调校的意图区分参数，不是iOS官方保证。LeftGesture用单调时钟记录超过Windows拖动距离的最近移动；底部上移达到原travel，且用户按住静止超过pause，松开前补足hold减已有静止时长；快速松开仍0附加延时，不修改已顺畅的返回逻辑。F9/移出/Reset不补发；暂停不是总按住时长，不得混淆移动慢与停留。
API swipe新增可选endHoldMs0–2000，校验在ReleaseCapture前，末端延时可取消、finally释放。用于逐项实测系统卡片；默认0维持原API。桌面提示当前标注“尝试多任务”，不保证成功。自检以可注入时钟覆盖真停留补足、快速松开及取消；不能代替实机证据。
2026-10-11 16:44–16:45 停留识别候选版已部署：正式exe/pdb备份到work/iphonedesk/multitask-pause/rollback，SHA256匹配后以--airplay --resume-bluetooth启动，真实/status镜像和控制均true。代理实机对照：(0.5,0.9913022)到(0.5,0.5)，450ms移动+650ms末端停留、按钮1，仅内容滚动；(0.5,1)到(0.5,0.65)，600ms移动+900ms停留、按钮1，仍原页面，未出现后台卡片。两次sent=true不代表成功；此版意图判断与自检通过，但多任务实机验收仍失败，不能标为修复完成。API对照不经过物理LeftGesture，不能据此断言物理路径一定失败，也不能反过来宣称它成功。
本轮UI调用踩坑：【报错原文】window.app must be a non-empty string and window.id must be an integer >= 0。【最小复现】sky.activate_window(phoneWindow)直接传Window。【根因】省略所需的window属性包装。【正确写法】sky.activate_window({window:phoneWindow})、sky.get_window_state({window:phoneWindow})；已重读api.md后按正确签名完成前后观察。
本轮继续多任务及全屏照片排查，改前已查§1–5/§7–8/§10/§12–17。范围Input/LeftGesture/SendAbsolute/HidMouse发送计时；不改HID描述符、配对或手机设置。16:53:43日志物理静止672ms补足0；这是电脑时间，未测手机收到末坐标时间。拟按蓝牙成功发送时间补足末端停留，快松开保持旧行为。照片小图不能直接判断是黑边编码还是渲染尺寸，当前/status镜像false，实际画面复测等待重连。
已增加MinimumStationaryMs：仅真实底部上拖且物理停留满足配置才携带目标。HidMouse成功定位Notify后更新Stopwatch时间；SendAbsolute先送最终按住坐标，再按最后发送时间补足剩余停留，与原HoldBeforeRelease取较大值，避免双重等待。快速松开无目标不等待；F9/移出仍分段取消。自检新增物理静止700ms但待补发送停留，以及发送余量100/700/649.5ms边界。来源src/LeftGesture.cs MinimumStationaryMs、HidMouse.cs lastPositionSent、Program.cs releaseWait；尚无后台卡片实机成功。
本轮编译踩坑：【报错原文】error CS0103: 当前上下文中不存在名称“Log”。【最小复现】SendAbsolute调用Log日志方法。【根因】未核对现有方法名，实际为SetStatus。【正确写法】复用SetStatus，其已有UI和文件日志路径；已核对函数定义后修正，编译失败版未发布。
发送时钟候选版Release零警告、--self-test退出0、57断言/19指纹/24证据通过；正式exe/pdb配套备份到gesture-frame/rollback，优雅退出后部署且SHA256一致。当前镜像断开，未发手机动作；后台效果仍待真实画面。
18:02重连后/status镜像和控制均true；新物理日志18:02:02.3253728目标650ms实际补足103ms，证明发送时间路径生效，不证明后台。18:03代理上拖对照(x0.42,y0.9913022→x0.62,y0.65，300ms+650ms，按钮1)返回sent，随后实际画面为锁屏而非App卡片；无法区分用户同时锁屏或手机映射原因，不推断唯一根因。已停止手机输入，等待解锁；未读取邮件/消息内容。全屏照片小图尚未复现到当前连接，不作成功声明。computer-use轻量调用两次超时后重置JS内核并恢复窗口观察，未用旧坐标盲操作。
## 18. 返回主屏与启动最大化回归修复（当前优先）
用户报告返回主屏及自动全屏失效。改前读§1–5/§7–8/§14/§16–17。撤回生产多任务停留：配置新增multitaskEnabled布尔，默认false，原候选参数保留但默认不参与物理路由；显式非法布尔仍只读。保持§16已由用户确认顺畅的校准/按钮/holdMs0，不重配对。启动最大化在Loaded后的Dispatcher ContextIdle执行一次，避免初次显示还原状态覆盖构造时Maximized；此为候选根因，未实测前不能定论。用户以后手动还原不被定时强制最大化。
已关闭生产多任务实验，mouse-actions.json保留候选150/650及未知字段、增加multitaskEnabled=false，备份home-restore/mouse-actions-before.json后原子替换。未改holdMs0及校准值。界面移除尝试多任务提示，标注实验暂停。Loaded的一次ContextIdle最大化并记录真实WindowState；无定时最大化。
恢复版Release零警告、自检0、57断言/19指纹/24证据通过，正式exe/pdb配套备份和哈希匹配部署。18:19:08.2333747日志WindowState=Maximized；/status镜像和控制均true。返回主屏已撤回到无实验停留路径，仍待物理操作实机确认，不能用状态日志代替。
### 返回主屏失败日志核对（18:41–18:48）
最新三次18:41:56/59、18:42:01均底部起手True、按钮1、按下Y32482、补足0ms、正常1→0释放，起始到按下约18–80ms；未出现断连或发送错误。不能再归因于多任务停留未关闭。代理真实对照：从已存在后台卡片执行约8%短上拖150ms后主屏出现；进入设置后重复同轨迹及最底边y1对照均仍设置页。此仅证明后台退出可成功，不证明普通App底边Home有效，也不能把失败定为唯一坐标/时序问题。当前根因未查明，禁止继续只加延时或称恢复完成。
本机GET /screenshot在窗口最小化时返回黑色PNG但/status镜像true；black不代表手机黑屏。computer-use刷新发现窗口minimized，恢复后可见真实画面；恢复操作不作为启动最大化验收。工具调用一次报window.app must be a non-empty string and window.id must be an integer >= 0，单独activate_window({window:logWindow})成功，后续逐步刷新；不得批量混合状态变化及重取窗口假定成功。
## 19. Home连续相对轨迹对照
改前读§1–5/§6接口/§7–8/§16–18。按钮2底边250ms上拖实测仍设置页，未改手机配置。当前绝对坐标发送与相对点击属于两个集合；拟新增仅API显式relativeMotion布尔对照，起点仍绝对定位，按住后相对X/Y按运行时video客户区尺寸计算，以同一relative报告连续移动；缺省false不改变生产。不是已证实根因。取消及finally释放、认证、loopback保留；元数据缺失拒绝而非0坐标。
relativeMotion诊断实现已加入LocalControl.RunRemote；relativeWidth/Height来自当前video客户区，RelativeStep用累计整除之差保持总位移，超过单报告±127分包并检查取消。仅显式true才启用，默认仍绝对轨迹；自检覆盖±1000及±1/0累计总位移。此不改变物理控制，待手机实测后决定是否采用。

19:00相对连续移动按钮1/2对照仍设置页，未返回Home。后续代码核对发现Send释放会发送仍保留起点的absoluteX/Y，造成相对轨迹末端跳回起点。诊断修正：记录最后实际发送absoluteButtons，仅relativeMotion动作在绝对集合没有按下时不补发绝对释放；若该集合曾按下仍强制释放。生产及F9默认双集合释放不变。此为候选干扰，尚非已证明Home根因。

19:08修正释放跳回后，从普通App底边相对按钮1上拖仍停原App，未证明Home修复。下一单变量对照：仅relativeMotion且请求y=1时，按下前发送相对向下127以接触真实底边，再保持原上拖；生产物理输入仍不改变。

⚠ 相对移动后的绝对坐标缓存失效：现象→多次y=1对照指针仍在顶部；真实结构→Send相对X/Y移动不更新absoluteX/Y，下一次SendAt相同起点changed=false而跳过定位。坑→缓存只是最后绝对报告，不等于当前手机坐标。正确做法→成功相对位移后absolutePositionKnown=false，下一次SendAt必须重发定位，成功后才置true。来源src/HidMouse.cs Send/SendAt，19:10–19:11真实窗口指针位置。此为确定的缓存缺口，Home效果仍须实机。

19:12缓存修正版实测仍原App。单次127相对向下不能保证覆盖当前屏幕；贴边诊断改为当前video客户区高度分包完成，取消检查逐包保留。不写死机型尺寸，物理路径仍未更改。

19:14–19:19贴底高度分包按钮1/2/4均未取得普通App返回主屏证据；现有辅助触控菜单可打开，但菜单主屏项代理点击未通过验收，不把菜单坐标写死进入生产。请求用户只读核对电脑的按钮1/2/3实际动作；禁止盲换映射/重新配对。诊断入口保留显式relativeMotion，默认false，生产仍原左拖。新增接口轨迹日志记录请求起终点、按钮、模式、真实尺寸和时序，不记屏幕内容或token；下次排查以实测画面和该日志配对。缓存自检覆盖相对位移后同坐标必须重发、已知同坐标跳过、改变坐标重发。
19:21 home-trace正式部署：Release零警告、--self-test退出0；60断言/19指纹/24证据核对通过，exe/pdb备份home-trace/rollback并校验SHA256后重启。新增缓存修复及轨迹日志，物理按钮配置未改，Home/后台未实机验收成功；当前等待真实手机按钮映射，不能宣称问题已解决。

## 20. 用户要求回滚到已确认Home顺畅版（2026-10-11）
改前已读§0–4/§7–8/§14–19并通过60断言/19指纹/24证据基线。用户“不能回滚吗”授权直接回滚；撤销上一轮按钮映射核对要求，无需为回滚修改手机设置。选择16:32 bottom-anchor产物，与multitask-pause/rollback exe及pdb SHA256完全一致；对应用户随后“返回主屏已经很顺畅”反馈，不代表所有设备或后台手势稳定。恢复正式exe/pdb和multitask-pause/mouse-actions.before.json原始配置；先备份当前版本、配置、源码顶层文件和规范至 C:/guojoe/iphonedesk/work/iphonedesk/rollback-confirmed-home-20261011-192828。不更改接收器、名称、分辨率、配对或系统驱动。当前src仍为19:21研究实现，与回滚运行二进制不同；禁止直接重编译覆盖这个已确认运行基线。源码恢复须独立核对历史源，不宣称源码已经完全回滚。运行基线由正式exe/pdb及配置指纹记录。

回滚完成：旧版--self-test退出0，正式exe/pdb/配置与候选三项哈希完全一致；完整备份rollback-confirmed-home-20261011-192828。保留源码研究实现的版本差异已明确置顶；新增正式运行三项指纹防止无意覆盖。回滚后只读检查连接，不代替用户当前Home操作验证。

## 21. 回滚后上下拖动故障：实机对照及配置修复（2026-10-11）
改前读§0–5/§6.1–6.3/§7–8/§20，基线60断言、22指纹、24证据通过。保持16:32 exe/pdb，不编译仍不同版本的src。本次只改动作配置并优雅重启读取。

### ⚠ F11 垂直拖动不能凭此前偶发成功固定为按钮1
现象：用户报告上下拖动均失效；回滚配置仍是水平2/垂直1。实测真实数据：同一右上角轨迹(0.94,0.003)→(0.94,0.5)、450ms，按钮1未打开控制中心，按钮2实际打开控制中心；从相机以(0.5,1)→(0.5,0.55)、300ms、按钮2后实际观察到主屏。反直觉的坑：绝对坐标与相对按键报告都成功不代表iOS触摸手势生效；一个方向曾偶发成功不构成按钮1的稳定依据。正确做法：按设备配置选择拖动动作，此设备垂直与水平同为2；电脑仍按左键拖动，轻点编码1不改，HID全局编码不互换。底部锚点改为归一化边界1.0，原startY/travel/holdMs及未知配置字段保留。来源：运行日志2026-10-11T19:39:48.1500757（按钮1）与19:40:12.0080432（按钮2），19:46实机截图的相机→主屏；本会话computer-use实际窗口画面。API证据只覆盖报告路径，不能冒充物理左键完整验收；也不证明多任务可靠。

### 拖动映射复发事故（关联P02）
【报错原文】用户“越改越差了，从从上到下拖动和从下到上都不行了”。【最小复现】原配置verticalDragButtonMask=1；右上角下拉，按钮1无控制中心，而按钮2同轨迹成功。【根因】生产保留了尚未稳定验证的垂直按钮差异，回滚仅还原旧配置没有复验当前实际效果。【正确写法】此设备verticalDragButtonMask=2、bottomGesture.anchorY=1.0，不修改左点/全局按钮编码。【防再发生】规范检查新增生产JSON字段断言；以后映射发布/回滚都先检查当前配置，再逐项验证点击、普通拖动、上边缘、下边缘，未验证项明确保留待测，禁止把API sent当验收。

源码与运行仍不同，§20禁重编译规则继续有效。备份work/iphonedesk/vertical-gesture-repair/mouse-actions.before.json；需要撤回本次配置时使用备份，而不是重建研究版exe。此次没有改手机设置、配对、接收器或驱动。
发布核验：19:49:16日志确认动作分流重新加载；正式exe/pdb仍为§20回滚指纹，只有一个正式路径主实例。重启后status的mirrorConnected/mouseConnected均true。收尾62断言、22指纹、24证据通过。重启后computer-use激活/窗口枚举超时，因此未完成新实例上的物理左键上下拖动复验；不得把重启前API实机成功扩大成新实例物理操作稳定验收。

## 22. 手势诊断日志补全（2026-10-11）
用户手机已拿走，授权先完善日志；立即停止手机操作。改前读§0–5/§6接口/§7–8/§20–21。源码仍为研究19:21基线，本次日志改动只构建至work/iphonedesk/gesture-diagnostics/build；不部署覆盖16:32生产exe。以后部署须先完成源码与运行基线对齐，不能借日志更新恢复实验手势。

当前实机反证：20:04–20:05普通App最底边300ms上拖，按钮2与1均未返回主屏；取代§21对底部已修复的任何理解。用户19:58四次物理拖动均底部True、按下Y32767、按钮2正常释放。根因仍未证实，日志发送成功不等于手机执行。

新增src/GestureDiagnostics.cs：每次运行独立logs/gesture-时间-session.jsonl；schema1，session随机UUID，seq递增、at墙钟、elapsedMs单调时钟、gesture相关ID、kind及data；全部executionVerified=false。Channel4096为诊断内存保护，不限手机输入，不控制功能；满时丢诊断并累计Dropped，写盘异常WriteError显式暴露，不能把有缺口日志当完整轨迹。异步写盘并逐记录Flush，可在线读取；不写token、请求认证头、截图或手机页面内容，不记录用户PIN。保留运行日志.txt和老日志，诊断文件不自动删除。

索引：Program.QueueInput记录原始每个手势样本；QueueRawInput记录入队与被合并的sample；SendAbsolute记录锁等待、队列等待、坐标、generation、真实报告计数增量、耗时及跳过/错误；HidMouse.Send/SendAt记录两报告集合实际提交坐标/按键、缓存判断、Notify客户端结果；LocalControl.RunRemote记录API轨迹及异常/取消，UI hover/ReleaseCapture/关闭记录取消原因。profile含实际加载的配置与Windows拖动距离，不推断iOS配置。诊断只观察，不更改报告编码、按键映射、队列顺序、停留或释放。

【报错原文】本次PowerShell ParserError: Missing argument in parameter list；【最小复现】rg传src/{Program.cs,LocalControl.cs,LeftGesture.cs}；【根因】把Bash花括号路径用于PowerShell；【正确写法】逐个明确路径或目录+rg -g过滤，属于P06 Windows路径再次出错事故，后续命令审查禁用花括号路径。新增文件后规范检查提示“维护文件没有登记快照”，属于正确的防漏报检查，不绕过，立即登记新增文件及语义。

日志收尾补充：关闭先取消输入和释放HID，然后等待诊断写入最多2秒，超时在运行日志显式flushed=false，二次关闭不重复处理；窗口不会在首次async Closing返回时立即销毁。诊断面板显示session、丢记录数和写入错误；GET /status新增diagnostics的session/file/dropped/error，只读不暴露token。session-start记录OS/进程位数/报告描述符SHA256，build-identity后台读取真实运行exe SHA256。每次物理起手记录映射视口真实坐标/尺寸/aspect/嵌入状态；绝对释放、45ms定位等待也独立记录。序号分配与入内存队列同锁，避免多线程正常入队被分析器错报乱序；锁内无磁盘IO、无等待队列空间。

新增工具outputs/iPhoneDesk/tools/分析手势日志.py，纯只读，支持旧运行日志与schema1 JSONL，输出独立时间戳JSON/CSV。未知schema/半行/seq缺口显式告警，不伪造成功；旧日志无法恢复的move/sample/合并数量保持null。它不连接控制接口，不触发手机动作。首次真实旧日志分析提取408段物理起手，报告work/iphonedesk/gesture-diagnostics/reports/gesture-report-20261011-201619-443432.json；段数不是成功次数。

收尾复制运行（源码日志候选尚未生产部署）：
```powershell
python outputs/iPhoneDesk/tools/分析手势日志.py outputs/iPhoneDesk/运行日志.txt --out work/iphonedesk/gesture-diagnostics/reports
$taskLogTest = Start-Process -FilePath (Join-Path (Get-Location) 'work/iphonedesk/gesture-diagnostics/build/iPhoneDesk.exe') -ArgumentList '--diagnostic-self-test' -WindowStyle Hidden -Wait -PassThru
if ($taskLogTest.ExitCode -ne 0) { throw "诊断自检失败：$($taskLogTest.ExitCode)" }
Get-Content work/iphonedesk/gesture-diagnostics/build/diagnostic-self-test.txt
pwsh -NoProfile -File outputs/iPhoneDesk/tools/检查规范.ps1
```
新增src/DiagnosticSelfTest.cs的--diagnostic-self-test只做文件写入/解析：验证序号/gesture/session关联、合并证据、正常flush、关闭后Dropped及磁盘不可写异常；不创建GATT或发送手机操作。Release编译通过，自检和原--self-test均0；日志源码待与回滚运行基线对齐后部署。接手者只读§22应能生成旧日志报告、跑离线自检，并明确底部手势尚未修复，不能把候选作为已上线。

日志覆盖补充：F9/Esc请求明确cancel-request；连接状态仅变化时记录，区分镜像、相对/绝对订阅、自动模式与窗口状态；手动相对捕获的HID报告也通过input.GestureId关联，触控板/滚轮直接Send以gesture0标记非物理手势，不猜编号。api-end现在在finally释放尝试后记录，不能用它推断iOS已松开；Notify字段和错误须一并核对。多线程64事件顺序自检通过。正式旧日志报告已输出outputs/iPhoneDesk/诊断报告/gesture-report-20261011-201921-117904.json及同名CSV；输出不包含手机内容、配对信息或认证头。
最终离线验收：日志候选Release构建成功，--diagnostic-self-test和--self-test均退出0；分析器回归通过；文档70断言、25指纹、24证据通过。正式exe SHA256仍8FF70D006C7C57E0F85E13B64A75F47CA74F659827C7967730F7D9519A6DCCC9，未部署日志候选、未操作已拿走的手机。当前生产不会生成新增JSONL；旧运行日志分析工具现在即可使用，新JSONL功能须源码与回滚基线对齐后部署。

2026-10-11 用户要求回滚生效：执行前读取§0–3/§7–8/§20–22，核对正式exe与bottom-anchor回滚产物SHA256一致，旧版--self-test退出0。当前无正式主实例，启动正式回滚exe，参数--airplay --resume-bluetooth；未部署研究源码或日志候选，不改动作配置/接收器/驱动/配对。新增JSONL仍未上线，启动只使回滚程序运行，不代表底部手势已修复。

## 23. 回滚基线合入诊断日志（2026-10-11）
用户明确要求新日志加上并测试；改前读§0–5/§6接口/§7–8/§20–22。先备份当前日志候选源码至work/iphonedesk/gesture-diagnostics/before-baseline-align，解包16:32回滚exe的实际iPhoneDesk.dll，ILSpy9.1反编译只用于本机核对，不替代维护源码。来源work/iphonedesk/gesture-diagnostics/baseline-decompiled/iPhoneDesk.decompiled.cs的LeftGesture.Route/Load、HidMouse.Send/SendAt、Desk.RunRemote/SendAbsolute/BuildLayout。
撤销研究行为：MultitaskPause/Hold、MinimumStationary、relativeMotion/endHold接口、absolutePositionKnown及相对释放例外、Loaded再次最大化；恢复回滚版构造时最大化、实际相同坐标跳过定位、双集合释放、45ms按下等待/80ms释放等待及API30ms分步/60ms定位等待。当前生产动作配置保持垂直2/anchor1，不退回旧垂直1；未知字段不写回。日志观察路径保留；async关闭仅为完成日志flush。尚待编译、离线动作等价核对与发布；不能据日志证明手机Home或后台成功。
【报错原文】MetadataFileNotSupportedException: PE file does not contain any managed metadata.【最小复现】直接ILSpy读取单文件iPhoneDesk.exe。【根因】exe是.NET原生bundle宿主，托管程序集在bundle中。【正确写法】按bundle6头与真实entry偏移/长度解出iPhoneDesk.dll后反编译；提取结果147968字节，仅本地保留。

§23离线验收：维护源码构建成功；--self-test和--diagnostic-self-test均退出0；分析器回归PASS。work/iphonedesk/gesture-diagnostics/equivalence/Program.cs用两个隔离AssemblyLoadContext加载真实回滚DLL与新DLL，27,000个输入样本覆盖单按钮映射、纵横方向、底部anchor缺省/0.99/1、停留0/350、边界坐标、混合按键/滚轮/取消Reset及非绝对输入，全部分流输出一致；三份HID描述符及30个绝对边界报告一致。它不创建UI/GATT，不连接手机。HidMouse发送及API时序另逐段核对回滚反编译代码，保留45/80/60/30ms规则；上述等价不外推iOS成功率。
复验命令：`dotnet run --project work/iphonedesk/gesture-diagnostics/equivalence/equivalence.csproj -c Release`（必须先按§8构建新DLL，旧DLL仅取§20哈希匹配的bundle）。当前研究源码已撤销，诊断源码与回滚行为基线完成核对；将要发布，正式旧产物会保留至独立rollback备份。

§23生产验收：20:47:58部署前正式旧exe/pdb/动作配置备份work/iphonedesk/gesture-diagnostics/deploy-rollback-20261011-204756；旧主实例正常关闭、配套复制SHA256匹配后启动。GET /status真实mirrorConnected=true、mouseConnected=true，diagnostics.dropped=0/error=null。实际日志outputs/iPhoneDesk/logs/gesture-20261011-204758-c5f5ac1154604fa286daf2133519e365.jsonl含session-start/build-identity/profile/connection-state，所有executionVerified=false。正常关闭日志10条、session-end和末尾log-close存在、dropped0；再启动独立session。本轮未发送手机动作（计数0），不能把通道连接当Home验收。日志分析报告outputs/iPhoneDesk/诊断报告/gesture-report-20261011-204855-216403.json，0手势、warnings空；它验证生产格式和落盘，手机动作仍待后续复测。
接手复验：先运行§8构建/--self-test，再运行构建产物--diagnostic-self-test；读取认证GET /status的diagnostics.file（令牌不输出），分析命令python outputs/iPhoneDesk/tools/分析手势日志.py <该路径> --out outputs/iPhoneDesk/诊断报告。正常退出后文件末尾应含log-close、dropped0；未关闭文件可以缺log-close，seq缺口或error不得忽略。实际gesture事件需用户物理操作，离线测试仅验证记录与分流，不能伪造物理来源。日志没有触发屏幕截图/手机点击；退出的安全释放与回滚版保持。当前日志版已生产生效，无需用户另复制候选文件。

## 24. 新日志读取COM导致鼠标移入闪退（2026-10-11）
改前读§0–5/§7–8/§22–23。用户“开了又关，鼠标基本动不了”，两个真实.NET Runtime事件20:50:40/20:52:05均System.Runtime.InteropServices.COMException (0x8001010D)，堆栈IGattLocalCharacteristicMethods.get_SubscribedClients→HidMouse.AbsoluteConnected→Desk.HoverChanged Program.cs:410→MouseCapture.OnMouse:174。正式进程已消失、JSONL停在connection-state且无session-end/log-close。这是本次日志引入的回调上下文错误，不能归因手机/配对。
⚠ 现象→鼠标进入画面应用退出；真实结构→HoverChanged从WH_MOUSE_LL回调同步调用，日志匿名对象求值触发WinRT SubscribedClients读取；坑→日志写盘是异步，构造data时的COM访问仍同步发生在不可执行此调用的回调上下文；正确做法→日志仅读取MouseCapture.ClientReady缓存（由既有UI定时器更新），钩子内不得读Gatt/WinRT连接属性；实测来源→Windows Application/.NET Runtime事件上述时间及src/Program.cs HoverChanged、MouseCapture.OnMouse。
【报错原文】System.Runtime.InteropServices.COMException (0x8001010D), The process was terminated due to an unhandled exception.【最小复现】蓝牙已订阅后鼠标移入投屏，HoverChanged中DiagnosticEvent data读取hid.AbsoluteConnected。【根因】新增诊断访问WinRT越过低层鼠标回调边界。【正确写法】connected=capture?.ClientReady，日志记录缓存快照；不改变实际连接判断/动作映射。
防复发：规范新增HoverChanged缓存特征断言；独立脚本提取整个HoverChanged函数，禁止任何hid成员调用，同时检查诊断输入函数仍无同步文件IO。每次诊断发布必须验证真实鼠标移入/移出，启动/status/编码自检不覆盖该场景，§23“生产格式通过”不能理解为已验证物理钩子。本次底部系统手势仍未修复，不更改手机配置。

§24离线修复检查：Release构建成功，两项自检退出0，27,000样本回滚分流仍一致，HID描述符和边界报告仍一致。防复发脚本已放正式tools/检查鼠标回调.py，直接运行python outputs/iPhoneDesk/tools/检查鼠标回调.py检查HoverChanged整个函数无hid访问与日志入口无磁盘等待；属于回调边界静态检查，不冒充实际物理鼠标测试。

§24修复产物已配套部署并启动；日志新会话/status可读，此次读取mirrorConnected=false、mouseConnected=true，日志dropped0/error空；等待视频窗口重新出现，不能称镜像此刻已连接。等待用户物理移入/移出反馈，不能先记稳定成功。旧20:48关闭文本dropped1说明终止队列后仍可能收到迟到回调；§23在log-close当下dropped0不等于所有后续回调均已记录，后续分析需同时核对运行日志收尾值。

### §24同类COM复发事故：漏查QueueInput
用户随后反馈关闭、鼠标进画面慢，真实20:57:09.NET Runtime异常0x8001010D堆栈Connected→Desk.Send:367→ReleaseCapture:445→ReleaseRequested。真实代码QueueInput的physical-input日志仍同步读取hid.AbsoluteConnected；它在Received catch被捕获后Stop，再同步ReleaseRequested把COM调用带入错误上下文，Send前置Connected在try外使异常逃逸。第一次仅删Hover日志读COM不足，视为事故。
正确写法：physical-input同样用capture.ClientReady，ReleaseRequested经Dispatcher.BeginInvoke延后处理，禁止在钩子异常回调同步访问Gatt。检查脚本扩大为HoverChanged、QueueInput、QueueRawInput三个整函数均无hid访问且ReleaseRequested必须延后，不能只检查导致第一条堆栈的单函数。日志构造仅内存值，蓝牙实际状态仍由原UI定时器/发送路径读取。不修改映射/报告/手势/配对。下一次发布必须待物理移入/按下/移出确认，离线通过不提前宣称稳定。

§24复发修复发布：两项自检0、27,000基线样本一致、完整回调COM边界检查通过；exe/pdb配套备份并哈希核对后启动。用户要求电脑所有已识别鼠标均可控制，本程序采集Windows事件，没有实体蓝牙鼠标限制。BLE仅电脑→iPhone传输通道，Wi-Fi承载画面；日志不得在鼠标钩子查询该通道。物理效果待用户确认，禁止把静态检查说成已稳定。

## 25. 21:02用户底部返回失败日志复核
真实日志outputs/iPhoneDesk/logs/gesture-20261011-210052-475018be057f494daa477beb561fcf7d.jsonl，分析报告outputs/iPhoneDesk/诊断报告/gesture-report-20261011-210354-139701.json共7段：1轻点、1顶部下拖、5底部上拖（gesture3–7，21:02:16/20/22/24/27）。底部起点原Y32648/32459/32245/32482/32530，均校准实际按下Y32767，按钮2→0，持有约161/528/560/672/212ms。无send-error/cancel/send-skipped，队列等待中位数12.8/6.7/6.3/4.4/4.3ms，最大129.3/140.7/69.9/53.3/120.0ms。不能把最大单次等待写成所有帧延迟；不能把发送完整写成系统Home成功。用户反馈五次仍失败，真实iOS效果未被API确认。当前COM修复版运行/status可读，positionCount526/buttonCount14，diagnostics dropped0/error空；物理日志证据已覆盖移入/按下/移出，没有本轮新闪退证据。此不是长期稳定验收。
本轮仅读取/分析日志、未改源码或配置，保留当前坐标/按钮以免无依据更改。需将实际屏幕变化与gesture编号配对才能判断系统边缘解释，不再盲切按钮/延长等待/改蓝牙配对。

## 26. 按真实成功历史单独恢复底部手势（2026-10-11）
用户要求改进历史能用的实现；改前读§0–5/§7–8/§12–16/§20–25。对照work/iphonedesk/multitask-pause/mouse-actions.before.json真实16:32参数：水平2、垂直1、anchorY32482/32767；§16成功原始日志16:30:57同坐标、按钮1、无额外停留。当前失败配置垂直2、anchor1是§21后来为顶部下拉修改，不能作为底部成功证据。§25五次完整按钮2底部仍失败，故撤销§21将底部与上部绑定的结论，不全局回退普通拖动。
新增可选bottomGesture.buttonMask从配置独立选择1/2/4，缺省继承verticalDragButtonMask保持旧配置行为；非法类型/值只读保留原文件，未知字段仍不写回。当前按真实成功历史将bottomGesture.buttonMask设1、anchorY恢复0.9913022247993408；普通横纵拖动均保持2，轻点1。物理底部识别后固定BottomDragButton直到释放，中途转向不换；API缺省用同ButtonForSwipe起点分流，显式buttonMask仍诊断覆盖。保留holdMs0、安全取消/双集合释放、全部COM回调修复及日志；不改手机设置/配对/HID映射。先备份至work/iphonedesk/gesture-diagnostics/bottom-history-repair。
自检增加底部按钮1、校准点、转向不换、释放0、非底部仍2、轻点仍1及API一致性。历史坐标属于此设备实测配置，不写死到路由代码。修改已按历史依据完成，仍需用户实机验证，不能保证多任务或所有iPhone手势。当前范围置顶中anchor1说法由此节取代。

§26发布检查：Release构建成功，两项自检0，新底部分流测试通过；缺省配置27,000旧基线样本仍一致、HID描述符及30边界报告仍一致；全回调COM边界检查通过。exe/pdb备份、优雅关闭、配套复制SHA256一致后启动；当前底部按钮1仅来自真实历史配置，普通垂直2保持。实际Home是否恢复仍待本次用户物理测试，未代发手机操作。

§26收尾纠正：接口断言遗漏由ButtonForDirection更新为ButtonForSwipe导致首次一致性失败，已按真实起点分流语义同步。PowerShell ErrorActionPreference=Stop不能自动阻断外部pwsh/python非0退出，首次发布脚本仍继续部署；这是§7发布检查流程缺口。以后每个外部检查显式if($LASTEXITCODE -ne 0){throw}，检查完成才复制；本次不得把首检失败称全部通过，最终按更新断言重新验收。

## 27. 历史参数失败后的验证边界（2026-10-12）
已读§0–5/§7–8/§23–26，继续核对实际输入和画面映射。21:08–21:09真实session480bd4d3cddb4f72b25e2abc60c24673中7次底部上拖均按钮1→0、按下Y32482，无send-error/send-skipped/cancel；移动队列中位3.78–5.58ms。用户反馈仍失败，证明§26恢复两个历史参数不足，不能作为已修复结论。保留本轮取证work/iphonedesk/gesture-diagnostics/history-parameter-failures.json，不上传用户数据。
昨天computer-use两次get_window返回foreground window did not report a process id，未继续盲坐标。今天重新启动程序，真实/status镜像和输入均未连接，已请求用户连接后进行实际画面对照；在连接前不发送任何手机动作，不以重新配对/改按钮等替代证据。本轮源码/配置未改，COM边界检查与75断言/26指纹/24证据通过。

## 28. 未订阅时允许原服务重试（2026-10-12）
改前读§0–5/§7–9/§24–27。只有一个主实例和一个本包AirPlay接收器，无beacon。手机LAPTOP显示已连接，两个报告订阅0；不能把SY-32895当本项目设备。确定缺口：Advertising导致未就绪也禁用连接按钮。已改为两个报告就绪才禁用，广播中未就绪显示“重试控制连接”，互斥保留。ReconnectAdvertising保留同provider/characteristics，StopAdvertising等待Stopped/Aborted再StartAdvertising，不Dispose/Create，不删配对、不动描述符/手势。2秒等待是退出保护、25ms轮询；微软官方StopAdvertising不保证断开central，因此重新广播不保证手机重新订阅，不能称根因已解决。官方来源https://learn.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.genericattributeprofile.gattserviceprovider.stopadvertising 。
状态新增control.advertisement/relativeClients/absoluteClients/reconnecting，卡片显示实际报告数量。认证POST /control/reconnect空正文经原Bearer/Origin限制、remoteLock与UI Dispatcher，只重发广播；GET无副作用、已连接不重连。用户授权本次修复及重连测试，不授予其他手机动作。仍无订阅必须记录实测失败，禁止无限重新创建服务或重启系统蓝牙。
报错原文Could not find a part of the path ...control-reconnect/edit.py；最小复现目录创建语句在尚未能写入的脚本内部；根因先写文件后建父目录；正确写法先建任务目录再保存脚本，所有外部命令退出码显式检查。本次初次脚本未保存未执行，随后重新保存并编辑，不把第一次旧源码构建当改版。

§28初检发现StartHid旧广播拒绝断言仍未同步，外部退出码保护已正确停止发布（未替换运行程序）；按未就绪允许原服务重试语义更新后再检查。两项离线自检和COM边界检查已通过。

⚠ §28真实反直觉行为：此次空正文重连在当前手机显示已连接/两报告0时StopAdvertising后2秒AdvertisementStatus仍Started，返回409“控制广播未能停止，未重复创建服务。”来源正式运行日志及POST /control/reconnect响应；不能把Started当可控制或把Stop请求当明确断开。停止等待条件已撤销，按微软官方示例同一provider Stop/Start，不重新Create；reconnect-stop-requested只记录请求后观察到的状态。以最终计数验收，不等待不可靠状态、不自动无限循环。安全测试GET404、无Bearer403、带Origin403、非空正文409均通过。

§28当前实机结果：用户回复看看后10:16读取status镜像true、mouseConnectedtrue，relativeClients=1/absoluteClients=1，positionCount186/buttonCount8，诊断dropped0/error空。10:16:23与25物理轻点正常1→0，已有移动记录，无发送错误；实际App点击效果仍需用户确认，不自动把Notify成功扩大成全部手势恢复。此次连接状态恢复发生在重新广播和用户手机侧操作之后；用户未明确说开关已完成，不可断言是某一步的唯一效果。保留当前服务和连接，不再为验证重启主实例；底部返回主屏仍待单独验证。最终构建及两项离线自检0，恢复广播POST200，状态读回1/1；所有原鉴权保护保持。

## 29. 用户确认向上拖动可用并冻结备份（2026-10-12）
用户原话“这个版本要备份。向上拖动都可以了。”改前读§0–3/§7–8/§24–28。冻结当前control-reconnect正式exe/pdb与维护源码、mouse-actions.json、图标/许可证、自带receiver全目录、tools及skills.md/AGENTS.md。当前参数普通拖动2、底部独立1、anchor32482/32767、hold0；保留原服务重连和COM回调防复发检查。用户确认是当前真实操作可用，不改成代理自行验证全部手势、多任务或跨设备稳定。备份不重启程序、不更改现有连接/配对/系统配置。
目录backups/confirmed-upward-<时间>/为冻结包，结构保留outputs/iPhoneDesk相对路径；正式交付ZIP在outputs/iPhoneDesk/备份/。不带control-interface.json令牌、bin/obj、USB研究源码/驱动/旧USB说明。源码顶层*.cs保留但排除UsbMirror.cs（原csproj Compile Remove仍可构建）；不带native USB研究树。receiver全目录原样保留，包括第三方许可证，不修改其二进制。备份额外保存接收器arguments.txt与HKCU Software/leapbtw/uxplay-windows设置快照（本地配置原值，不上传），以及当前日志副本（运行中可能没有log-close）。
恢复方式：先优雅退出本包iPhoneDesk.exe；对照manifest.json逐文件SHA256验证，再将备份的outputs/iPhoneDesk中列出的文件复制到正式同名路径，不删除无关文件、不移除配对或驱动；恢复源码时排除旧bin/obj重新构建，修改前再读此skills.md。root的skills.md/AGENTS.md一并恢复。接收器外部配置只在确需复原名称/分辨率时使用settings/副本，不能自动覆盖后来用户配置。任何恢复后重新读认证地址/token，旧token不在备份。启动仍--airplay --resume-bluetooth，手机订阅需真实1/1才可控制，蓝牙已配对不等于就绪。
后续任何手势改动必须先保存此基线，分别回归轻点、普通拖动、滚轮、底部上拖、上边缘下拉、移入移出/F9安全释放和重连；不能重引入§24 COM坑、§26全局垂直回退、§17–19研究路径。备份验收以清单SHA256与ZIP CRC通过为准，禁止仅说“已复制”而未核对。

§29备份完成：冻结目录`backups/confirmed-upward-20261012-102115`，交付ZIP`outputs/iPhoneDesk/备份/confirmed-upward-20261012-102115.zip`；200个文件逐项SHA256与原文件一致，ZIP CRC校验通过。正式exe SHA256 AB27406DCE5C34DF55243FC229B425AC3BD0F74E18B15DDCF7E07BDFBFD5FF9E。恢复时必须用同一冻结包中的源码/配置/规范，不能再混用研究源码和旧EXE；当前运行实例未停止、未重启。

## 30. 独立重新连接按钮（2026-10-12）
用户要求未知根因时提供重连按钮；改前读§0–5/§7–9/§24/§28–29，已确认可用冻结备份不覆盖。新增连接卡独立“重新连接”按钮，调用已有ReconnectControl和原provider Stop/Start，不重建服务/重启主程序/删除配对；按钮动作不改变已确认底部拖动/报告映射/配置。未就绪且无切换任务可用，重连时禁用防连点，两报告真正就绪后显示“控制已连接”并禁用（沿用户已连接不再连接规则）。
reconnectRequested记录一次用户重试；卡片展示实际定位/点击客户端数，等待时明确重连仅已发起而非成功；如果手机仍已连接但计数0，提示手机开关蓝牙保留配对。收到真实就绪才清除等待提示；不增加够用阈值/盲目超时判失败，不后台无限重启。操作异常由IOSAsyncButton捕获显示并解除忙状态。GET状态仍只读，所有COM读取仅UI状态/发送路径，鼠标钩子禁止调用。
本轮只UI入口/提醒，不能承诺按钮每次能恢复手机CCCD；已有已确认向上拖动备份仍在§29原路径，不修改冻结内容。

§30发布验收：Release构建成功，编码/日志自检和--render-ui均0；预览实际可见左侧连接卡独立重新连接按钮，79断言/26指纹/24证据通过，COM回调检查通过。exe/pdb另行备份并哈希核对后替换启动，不覆盖§29冻结备份。此轮未更改已确认上拖参数或发送手机手势；按钮重连效果按实际报告状态核对，不能保证每次恢复。

## 31. 照片小图时核对原生渲染窗口尺寸（2026-10-12）
用户确认独立重连按钮可用，转向照片显示修复。改前读§0–5 F06/§7–8/§10/§29–30；用户截图手机框大黑背景中央小图，不能假设手机源图也小。已有BitBlt截图返回全黑GPU帧，明确不能据黑图裁边/取内容或点击；本轮不操作手机/不改鼠标手势。
确定代码缺口：VideoHost.ResizeVideo仅比较宿主width/height缓存，相等直接return；不检查渲染窗口因模式/自身尺寸变化而偏离宿主。改为实际GetClientRect(renderer)以及renderer/host ClientToScreen相对位置都匹配才跳过，偏离时MoveWindow原位置0,0及宿主尺寸。原GPU路径/圆角/销毁还原保持，不截图拷贝/重编码，不裁照片或按截图硬编码倍数。此是窗口尺寸一致性修复，是否解释此次JPEG小图必须读实际geometry，不能直接宣称唯一根因。
新增只读GET /status.videoLayout，记录最近resize前真实尺寸/位置、目标尺寸、MoveWindow结果及renderer句柄；无图片内容、无token。来源src/VideoHost.cs ResizeVideo实际测量，程序原250ms连接定时器TryAttach自然触发，鼠标钩子无新工作。源码回退备份work/iphonedesk/photo-fill/*.before，已确认向上拖动冻结包不覆盖。
上游参考仅用于理解照片管线：work/iphonedesk/video_renderer.c:334–354独立jpegdec/videoscale/imagefreeze与视频管线；官方https://github.com/FDH2/UxPlay和https://github.com/leapbtw/uxplay-windows/wiki/2.-Configuration 。不修改第三方DLL/接收器配置，不把上游支持照片等同本机已正常缩放。

§31实际画面反证：computer-use成功取得新版窗口，仍为黑底中央小图，否定仅窗口尺寸修复已经解决。此时实际目标638×1382、渲染窗口尺寸匹配；BitBlt全黑不能证明没有图片。接收器真实参数-s1280x720@60为横向画布，截图中竖图只约占手机宽31%，与横画布内容先等比拟合再放进竖屏的几何关系吻合（推断，非已证唯一根因）。
独立配置对照：只将真实-s1280x720@60改为-s720x1280@60；写前检查无优先ProgramData配置且原值匹配，保留名称/所有其他参数，备份work/iphonedesk/photo-fill/arguments-landscape.before.txt后原子写回。该对照仅改变广告显示方向，保持720p质量；不裁掉照片、不改鼠标/GATT或第三方二进制。接收器需单独重启重新协商，主应用及蓝牙保持。等待重新镜像同一照片验证；未知参数不假设为空、不替换整文件。

§31实机对照成功：只重启自带接收器后，主实例11036未重启，蓝牙mouseConnectedtrue保持，mirrorConnectedtrue恢复。computer-use刷新同一主窗口，原照片由中央约100×179逻辑像素放大至约319×567，宽度占满手机内容区域，照片内容完整；因照片9:16与手机9:19.5不同，上下仍有合理黑边，不称无裁剪全高铺满。私人图中文字不转存到规范。竖屏720×1280对照与观察证实方向设置影响显示大小；窗口几何修复单独未解决，因此不能把§31最初假设当唯一根因。
分辨率菜单新增全部现有档位竖/横两种方向，保留自动、自定义、原始参数解析/原子写入和接收器互斥流程，当前720x1280可匹配竖屏预设；任何用户明确选择横屏可能改变照片填充效果，菜单标签明确区分方向。旧优化投屏.ps1仍含历史横屏档位且按旧备份计算，当前不调用；禁止使用它覆盖已验证竖屏配置，后续修改此脚本前须先完成配置保真，不能只替换几个常量。本轮不改该脚本，以免引入名称/未知配置回退。

§31最终竖/横菜单版配套发布，冻结向上拖动备份仍原样保留。Release构建及自检0，80断言/26指纹/24行号通过；本轮实际照片宽度填满由computer-use观察，非BitBlt黑图推断。接收器参数保持720x1280@60，鼠标配置完全未改；菜单版上线所需重启主程序后真实连接状态仍需读回，不能沿用重启前1/1。

## 32. 照片显示修复版冻结备份（2026-10-12）
用户要求“更新skills.md备份这个版本”。改前读§0–3/§7–8/§29–31；本次仅更新规范/复制备份，不修改代码、配置或重启任何程序。版本包含向上拖动已确认基线、独立重连按钮、真实原生窗口尺寸核对和竖/横分辨率菜单。照片宽度铺满由§31真实画面验证，不声称裁边后无黑边；后台多任务可靠性和偶发手机重新订阅根因仍未完全解决。
新冻结目录backups/photo-fit-<时间>/及交付outputs/iPhoneDesk/备份/photo-fit-<时间>.zip，保留旧confirmed-upward-20261012-102115备份。包含配套EXE/PDB、实际维护源/项目/manifest、图标/许可证、receiver完整目录、tools、skills.md/AGENTS.md、当前鼠标配置、接收器真实arguments.txt/注册表设置快照、当前日志副本。不含认证token、bin/obj或USB研究/驱动；不上传用户私有照片。
恢复照片修复版时，除§29的EXE/源码校验及复制，还必须检查接收器实际分辨率：settings/arguments.txt应为720x1280@60，主文件外的这个设置是修复的必要部分；只恢复EXE仍使用横向1280x720可能再次小图。先检查ProgramData是否有优先覆盖，保留后来修改的名称/未知参数，必要时用MirrorResolution.Change定点修改-s，不能盲覆盖后来用户配置或运行历史优化投屏.ps1。接收器参数改变后单独重启本包receiver并重新连接镜像，正常主程序启动不修改蓝牙配对。文件恢复前按manifest逐项SHA256核对，ZIP CRC也须通过，不能混用两个冻结包的源码和二进制。

§32备份完成：冻结目录`backups/photo-fit-20261012-105817`，交付ZIP`outputs/iPhoneDesk/备份/photo-fit-20261012-105817.zip`；200个文件逐项SHA256与原文件一致，ZIP CRC校验通过。正式exe SHA256 8E636E4D6274791BA7D5C97C5A2E460B289C28DBA62B7D2BB7167B6A5E21BDBC。恢复时必须用同一冻结包中的源码/配置/规范，不能再混用研究源码和旧EXE；当前运行实例未停止、未重启。

§32备份验收踩坑：报错原文FileNotFoundError ...photo-fit-20261012-105817.zip；最小复现Python read_text()未指定UTF-8读取含中文路径的UTF-8 JSON，再打开该路径；根因默认文本编码与保存编码不一致导致路径错误（本机preferred encoding cp936）；正确写法read_text(encoding="utf-8")，含中文脚本保存UTF-8文件运行。重新核对竖屏settings、规范原字节、无token及旧备份均通过。

## 33. GitHub远程与推送规范（2026-10-12）
用户明确授权推送到 **https://github.com/guojoe/iphonedesk**，并要求记录地址。Git remote：`origin https://github.com/guojoe/iphonedesk.git`；分支main。原远程只有README，基线提交686e7abe798fbe443366e3073830967bd747ce9f；本地接续它提交，不强推、不覆盖远程历史。仅设置本仓库提交署名guojoe与GitHub验证的noreply邮箱，不修改全局Git配置，不输出认证令牌。
发布范围：正式C#/项目/manifest（排除已取消UsbMirror/native USB）、图标、mouse-actions配置、tools、接收器/安卓组件官方固定下载脚本、两份许可证、README/AGENTS/skills/.gitignore。接收器包通过固定来源+SHA256下载，不提交第三方二进制。忽略work/backups、bin/obj、所有exe/dll/pdb/zip、认证地址token、本地日志/手机图像/诊断报告/本机设置快照及USB旧组件。用户可用本地备份恢复运行包，Git为可构建源，不能把Git clone当作含已安装接收器。
完整规范含历史本机证据。新增检查参数-RepositoryOnly，文档JSON的localOnlyFiles显式列出不提交的产物/历史文件；此模式只跳过名单项并打印跳过计数，其他缺文件仍报错；完整本机检查不跳过。新克隆应运行仓库模式、先构建再做两项EXE自检；运行EXE哈希/历史日志自检仍只能本机执行，不伪造提交。
推送前检查git diff --cached/stat和敏感文件名单，源码复核不得包含真实token；只用git push origin main（不force）。推送后git ls-remote origin refs/heads/main与git rev-parse HEAD一致才称完成。PR不在此次任务范围。github CLI未安装，使用现有Git及已连接GitHub身份；不新增工具安装或要求用户重复审批。

§33提交前字节一致性：.gitattributes设置* -text保留UTF-8/BOM/换行原字节，避免autocrlf使新克隆的规范SHA256失配；不改全局Git配置。将index导出独立目录后做仓库检查与构建。报错原文目录名称无效(os error267)，最小复现把尚未创建的review目录作为工具workdir；正确做法先从项目根建立任务目录，再进入验证。

§33推送前验收：29个暂存文件审查通过，未包含token/日志/私人照片/EXE或USB源码；从Git index导出的独立目录通过RepositoryOnly检查，Release构建成功，--self-test和--diagnostic-self-test均0。远程地址已记录，正常快进推送main，结果必须再核对远程HEAD；不会重启当前手机控制或覆盖本地冻结备份。

§33格式检查踩坑：git diff --cached --check将原样保存的CRLF末尾CR报告trailing whitespace；根因为保留字节的-text属性，正确检查git -c core.whitespace=cr-at-eol diff --cached --check。该检查退出0，不改源码换行或全局设置。空输出经Set-Content管道没有生成文件，不能再Get-Content不存在的检查输出；直接以退出码验收。

§33最新源码范围：按用户“保留最新的代码，之前的可以不要”，当前Git树仅保留维护版本，旧优化投屏.ps1移出跟踪并忽略；旧实验目录、构建和冻结备份不发布。历史实测/踩坑知识仍保留，旧脚本列入localOnlyFiles使新克隆检查可独立通过。本地冻结备份不改写，Git提交历史不强制重写。首批源码提交a29796e99d0001c1450d166c84bf8fb516b615af已正常推送并与远程SHA核对一致；本次范围清理不改运行行为、不重启程序。

## 34. 安卓投屏与控制（2026-10-12）
任务：用户要求提供安卓支持。动手前读取§0–5/§7–8/§29–33，80断言/26指纹/24证据行号基线通过。正式新增src/AndroidSupport.cs，AndroidProtocol.ParseDevices/Endpoint/SelfTest与AndroidWindow；DeskLayout.BuildLayout在左侧增加安卓工作台入口，Program --android可直接运行独立安卓面板，不创建iPhone GATT/热键/本机API。主程序打开入口复用一个安卓面板，关闭主窗口会关闭面板；不替换已验证iPhone手势或渲染路径。
安卓以官方scrcpy独立窗口显示/处理输入，不嵌入VideoHost，不使用iPhone MouseCapture钩子。USB调试或Android11+无线调试，由手机用户授权；设备列表完整保留ADB serial/state/model元数据，只有state=device放行，unauthorized/offline/未知状态只读展示。多设备由用户选择，唯一设备可自动选择；不写死设备序列号。当前投屏进程存活时禁用重复启动和切换设备；进程退出解除禁用并显示真实退出码。进程启动仅报告已启动，不冒充首帧/手势实机成功。
目录与索引：src/AndroidSupport.cs为维护源；outputs/iPhoneDesk/下载安卓组件.ps1为固定下载/校验入口；outputs/iPhoneDesk/android/scrcpy-win64-v5.0.1为本机官方完整包（含ADB/server/许可证），download-随机目录为校验来源ZIP，不进入Git。官方https://github.com/Genymobile/scrcpy/releases/download/v5.0.1/scrcpy-win64-v5.0.1.zip，SHA256 b12a2c4ee8be317422451fc7dcf8ee20a71b5ea7ef9ad73ddd825a25316227a5（来源官方doc/windows.md）。不下载任意站点/不改系统驱动/不静默升级；已存在组件必须逐文件匹配已校验官方包，否则保留原文件并报错。版本与完整目录名/哈希在代码和脚本同步。
无线入口：配对地址+PasswordBox六位码 → adb pair地址，码走stdin且输出屏蔽，立即清空、不持久化；随后填写无线调试首页的连接地址 → adb connect → 刷新；配对端口和连接端口可能不同。Endpoint要求显式端口，支持IPv4/DNS/括号IPv6，拒绝空白、用户信息、路径、越界端口。ProcessStartInfo.ArgumentList传参，不拼shell。ADB一次操作有超时/关闭取消，超时仅停止该次命令；不执行kill-server、不撤销其他配对、不批量杀进程。断开只对所选Wi-Fi地址；停止/关闭仅终止持有的scrcpy进程，最多1秒优雅退出，保留全局ADB会话。首次ADB调用会按上游行为启动本机ADB服务，这是安卓调试组件，不冒充无后台进程。
输入说明：独立窗口左键点击/拖动、滚轮、键盘；右键返回、中键主页、Alt+S多任务、F11全屏、Ctrl+左键双指缩放/旋转（来源官方v5.0.1/doc/shortcuts.md）。默认禁用音频与自动剪贴板同步，不读取用户照片到项目；只展示本地最近30条状态/错误（有界诊断内存），不把此上限作为功能展示阈值。部分品牌还需USB调试安全设置，受保护内容可能黑屏；无真实安卓设备时不能宣称输入/画面实测通过。
调试与收尾：在构建目录运行iPhoneDesk.exe --android只开安卓面板；--android-self-test不创建UI/GATT/ADB/手机输入，验证真实格式样本的状态解析及地址合法性。构建后执行--self-test、--diagnostic-self-test、--android-self-test与python outputs/iPhoneDesk/tools/检查鼠标回调.py，最后运行检查规范.ps1及-RepositoryOnly。实机验收需用户提供已授权安卓：刷新→选device→启动→确认画面/左拖/滚轮/键盘/返回/多任务/断线/重启；无线配对和连接分别验。禁区：旧iPhone USB研究/过滤驱动不恢复、不改iPhone BLE报告/已确认动作配置；未授权状态不发送操作，不记录配对码，不伪造全手势支持。当前待编译、官方组件校验和离线测试，实机尚未验收。

§34即时回写：初次Release构建无警告、三项自检均退出0、钩子COM边界检查通过；官方下载ZIP哈希通过并本地安装完整组件。scrcpy --version真实5.0.1，adb version真实1.0.41/37.0.1-15733141，scrcpy --help确认--no-audio/--no-clipboard-autosync/--serial/--window-title可用（本机命令来源）。新增--android-ui-smoke仅构造安卓面板并保存WPF图到产物目录，refreshOnLoad=false不调用ADB/手机；可与现有iPhone实例并行验UI，不抢热键/GATT。下载脚本保存UTF8 BOM适配内置WindowsPowerShell5.1中文，程序安装按钮仅该子进程ExecutionPolicy Bypass，不修改用户/系统执行策略。此不是实机图像或手势验收。

§34用户追加菜单分级：启动先显示平台选择页，顶部一级菜单“苹果手机 / 安卓手机”，选择后才显示对应工作台。AddPlatformNavigation隐藏另一平台菜单；安卓AndroidPanel为主窗口内的UserControl，原AndroidWindow仅--android独立入口包装。选择苹果才按原启动参数开AirPlay/BLE并启用Hover；安卓不自动启动苹果服务，capture.ClientReady在非苹果页固定false；切换先await ReleaseCapture释放按键，安卓面板Dispose停止自有投屏/命令并从page移除，关闭主程序同样Dispose。iPhone后台服务在切换后保留但不接管鼠标，不自动撤销配对。此前“左侧新增安卓卡片/独立工作台面板”是本轮中间状态，被此分级菜单取代；scrcpy画面仍为独立窗口以使用原生输入。平台菜单和安卓字段操作期间禁用，避免并发切换/覆盖地址。

§34字段/按钮核对：组件不存在时刷新/配对/连接禁用；未授权、无设备或已有投屏时启动禁用；无投屏时停止禁用。Wi-Fi断开支持IP:port与ADB mDNS的._adb-tls-connect._tcp服务序列（协议标识，非业务白名单），USB/未识别序列不允许断开并保留原值。设备刷新保留未知状态/型号缺省不推断授权。非苹果页定时器不查找/嵌入AirPlay窗口，避免安卓页重新抢苹果渲染器；原iPhone字段/钩子/动作配置保持。新增--platform-ui-smoke验证选择页→安卓→苹果→选择页可见性和销毁；单独测试不传--airplay/--resume，不能与正式主实例抢热键，同时必须按发布流程先退出正式主实例。安卓UI黑底截图发现是透明Content截图漏了Window背景，不是实机渲染失败，已将ScrollViewer.Background显式绑定面板背景，第二次查看图正确；此仅静态WPF界面证据。

§34只读环境检查：adb devices -l仅输出List of devices attached、无设备行，当前无可验收安卓。组件已下载但手机授权/厂商控制/无线实际连接仍待设备，不据组件version称实机可用。首份Android UI smoke图已查看；背景修正后正常显示标题/状态/安卓菜单/无线输入。透明截图踩坑：【报错原文】无异常，截图黑底且黑标题看不见；【最小复现】RenderTargetBitmap仅渲染透明ScrollViewer.Content；【根因】Window.Background不属于被渲染Content；【正确写法】显式设置ScrollViewer.Background，重新渲染并视觉核对。不改变VideoHost/GPU渲染。

§34发布验收：Release构建无警告；--self-test/--diagnostic-self-test/--android-self-test退出0，检查鼠标回调.py通过。已优雅关闭正式旧实例6960（核对绝对路径），旧EXE/PDB备份work/iphonedesk/android-support/rollback-20261012-114923，旧Git HEAD源码归档before-android-source.zip；两个已冻结用户备份保持原样。候选--platform-ui-smoke实际完成选择页→安卓→苹果→选择页，报告PASS，未启GATT或发送手机输入；--render-ui及--android-ui-smoke均退出0，两张WPF界面图已视觉核对。正式EXE/PDB与候选逐项SHA256匹配部署；未改mouse-actions.json、HidMouse/MouseCapture/LeftGesture/VideoHost或receiver配置/二进制。新版默认先选平台，这是用户追加要求，不能再期待刚启动就自动进入iPhone页。选择苹果后才根据--airplay/--resume-bluetooth启动原服务。
新接手者只读本节可独立准备安卓组件、识别授权状态、用分别的无线端口配对/连接、启动/停止投屏、定位模块和执行三项离线测试；真实安卓手势和图像仍未验收（ADB当前无设备）。不把UI切换测试包装成安卓投屏实机测试。
§34正式EXE SHA256：00C4C4BB647DB3C54D19DE960F82814B4A8D1E730E08887FB1D21F65BE7DA1FC。
§34启动只读核验：正式路径新版27152响应正常，认证GET /status可达；平台选择页mirror=false/mouse=false为未选择平台的真实状态，不冒充连接。git diff确认HidMouse.cs/MouseCapture.cs/LeftGesture.cs/VideoHost.cs/mouse-actions.json与已确认Git基线无差异。现有本机API仍描述iPhone路径，安卓通过自己的工作台状态和scrcpy窗口控制，不把该API当作安卓连接验收。

§34返回平台选择（用户追加）：改前读§0–3/§7–8/§34。顶部新增“← 返回选择”，苹果/安卓页显示，选择页隐藏；点击走同一个selection.SelectedIndex=0安全切换路径，保留释放输入、安卓Dispose、配对保留。切换处理中同步禁用下拉和返回按钮；WindowChrome.SetIsHitTestVisibleInChrome使标题栏内两个控件可直接点击，避免被当成拖动窗口。扩展原平台smoke以RaiseEvent触发实际返回按钮，并验证回到选择页且按钮隐藏；不改任何手机手势或驱动。只读adb曾识别小米13授权state=device，仅是连接检查，不是投屏操作验收；不在文档写设备序列号。

§34返回按钮编译踩坑：【报错原文】CS0119: “Desk.Button(string, RoutedEventHandler)”是一个方法，这在给定的上下文中无效；【最小复现】Desk partial中Button.ClickEvent；【根因】既有Button方法遮蔽WPF类型名；【正确写法】System.Windows.Controls.Button.ClickEvent全限定引用。首次构建失败时未关闭或替换原程序。
§34返回按钮验收：Release构建通过，平台smoke实际触发返回按钮回到选择页PASS。旧EXE/PDB备份work/iphonedesk/platform-back/rollback，新版配套部署；手机手势/鼠标配置未改。

## 35. 安卓布局与iOS样式统一（2026-10-12）
用户要求统一iOS样式，并明确参照iPhone左菜单/右手机。改前读§0–5/§7–8/§31/§34；只调整安卓布局、原生scrcpy宿主与样式工厂，不修改iPhone HID/手势/配置/VideoHost。AndroidPanel改280逻辑像素左侧滚动菜单（连接、组件、无线、手势、详情）、右侧手机画面；复用Desk.Card/IOSButton/Label/ThinScrollStyle，公开为internal static供同程序集调用，统一13/12px字体、14px卡片圆角、9px按钮、蓝主按钮/浅灰次按钮。新增IOSControls.Input圆角输入模板及ComboStyle，下拉仍由ComboBox/Popup/ItemsPresenter处理，PasswordBox保持遮掩，原文本/选择值不更改。
新增src/AndroidVideoHost.cs：只枚举当前AndroidPanel持有scrcpy PID及固定window-title，不按进程名抢别的软件；SetParent/窗口样式去标题边框/按真实宿主尺寸MoveWindow，保持原GPU和SDL输入，不截图重编码、不走iPhone钩子。解绑恢复原父窗口/样式/位置；停止前先解绑，嵌入失败保留独立窗口并提示，左侧“切换独立窗口/嵌入右侧手机画面”可切换。纵横比例初值来自真实原生clientrect，后续scrcpy日志Texture元数据更新比例；无帧不得把PID存活写成手机操作已成功。原生日志元数据缺失保留已有比例，不硬写小米机型尺寸。
本节取代§34默认独立窗口的中间方案；菜单/启动禁用/配对处理仍按§34。正式维护文件索引：AndroidSupport.cs AndroidPanel/FitPhone/Output/Update；AndroidVideoHost.cs TryAttach/SetVideoSize/Detach；IOSControls.cs Input/ComboStyle；DeskLayout.cs共享样式工厂。调试--android-ui-smoke仅WPF待机布局、不调用ADB；真实GPU画面不能用WPF黑图判断，需实际桌面视觉验证。当前待编译和实机嵌入核对。

§35即时回写：首轮Release无警告，--android-ui-smoke退出0，已查看左卡片/右手机待机布局，输入框与下拉均为圆角。AndroidVideoHost独立维护，不改原VideoHost；只有持有PID+准确窗口标题匹配才嵌入。输入由SDL原生子窗口接收，是否可靠仍须实际嵌入后测试；独立窗口回退保留。AndroidPanel.Update启动/停止/已授权限制沿用；用户截图已经出现真实独立安卓画面，只证明此前投屏成功，不证明此次嵌入成功。computer-use技能用于实际界面检查，不操作手机安全设置/授权对话框。
§35构建与菜单测试：三项离线自检及平台切换smoke通过，已备份旧配套EXE/PDB到work/iphonedesk/android-ios-layout/rollback并优雅退出旧实例，新产物配套部署启动。等待真实嵌入窗口验收。

§35真实界面验收：computer-use在正式新版选择安卓→启动投屏，实际右侧出现小米13设置画面，主窗口原生子窗可访问树包含iPhoneDesk·Android，不再是独立浮动窗口。鼠标滚轮在手机区域向上滚动后设置列表由桌面/显示等位置滚动到系统个性化/锁屏/通知等位置，证明本轮嵌入输入有效；不改变任何手机设置。顶部平台下拉真实点击能展开三项；复验已发现下拉项仍使用系统蓝框，已补ComboBoxItem圆角/浅蓝选中样式统一。左侧启动文案在宿主接入后一次更新为投屏已连接，避免一直显示正在启动；报告仅窗口连接，无执行验证字段。未验证所有键盘/多点/旋转/无线功能，不扩大证据范围。

§35推送前收尾（用户明确要求更新规范并推送）：正式源码/EXE指纹已同步，保留§34/35阶段记录并以末尾验收取代“待编译”初始状态。最新小米13实际画面和滚轮验证通过，未验证的旋转/无线/全部键盘仍保留限制。AndroidVideoHost/IOSControls/AndroidSupport及下载脚本进入Git；android官方二进制包、设备日志、WPF/手机截图、配对码、令牌与work/backups不提交。
Computer-use调试踩坑：【报错原文】coordinate input geometry is unavailable；随后新用户消息后call get_window_state before using this window。【最小复现】仅include_screenshot=false取得树后对需要坐标的索引点击，或跨消息复用旧观察。【根因】输入几何/观察缓存未就绪或失效。【正确写法】每次动作前新观察含截图，核对截图实际目标，失败先重新观察，不盲重试。后续观察返回树为iPhoneDesk但图为前台其他应用，停止UI动作，未点击该应用；不把混合观察作为新版视觉证据。上述不涉及源码运行异常，不改变手机或系统设置。
发布/复验：复制最新EXE/PDB须同一build并比对SHA256；Git发布仅源，第三方包用下载脚本固定校验。复验--self-test/--diagnostic-self-test/--android-self-test及--android-ui-smoke不发送手机输入；平台smoke须独立主实例。最新发布检查通过后正常快进推送origin main，使用git ls-remote与HEAD核对，不强推、不提交旧实验/运行文件。
§35 Git源验收：32个暂存文件范围/令牌审查通过，index导出android-review通过RepositoryOnly；从该独立导出目录Release构建成功，--self-test/--diagnostic-self-test/--android-self-test/--android-ui-smoke均退出0。本轮发布含安卓支持、分级平台及返回按钮、统一iOS样式和安卓原生嵌入；正式手机内容/日志/序列号未作为文件提交。
