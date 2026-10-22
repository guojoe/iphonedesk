using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;

namespace iPhoneDesk;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if(args.Contains("--android-self-test"))return AndroidProtocol.SelfTest();
        if(args.Contains("--android") || args.Contains("--android-ui-smoke"))
        {
            var androidApp=new Application(); var android=new AndroidWindow(!args.Contains("--android-ui-smoke"));
            if(args.Contains("--android-ui-smoke"))
            {
                android.Loaded+=(_,_)=>android.Dispatcher.BeginInvoke(()=>
                {
                    var visual=(FrameworkElement)android.Content;
                    var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)visual.ActualWidth,(int)visual.ActualHeight,96,96,PixelFormats.Pbgra32);
                    bitmap.Render(visual);var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                    using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,"android-ui-smoke.png")))encoder.Save(file);
                    android.Close();
                },System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }
            androidApp.Run(android); return 0;
        }
        if(args.Contains("--diagnostic-self-test"))return DiagnosticSelfTest.Run();
        if(args.Contains("--self-test"))
        {
            if(HidMouse.MapDesktopButtons(1)!=1 || HidMouse.MapDesktopButtons(2)!=2 || HidMouse.MapDesktopButtons(0)!=0 || HidMouse.MapDesktopButtons(3)!=3 || HidMouse.MapDesktopButtons(4)!=4)return 11;
            var r=HidMouse.Report(1,-200,200,-1);
            if(!r.SequenceEqual(new byte[]{1,129,127,255}) || HidMouse.RelativeMap.Length!=54) return 1;
            if(!HidMouse.AbsoluteReport(32767,-1).SequenceEqual(new byte[]{0,255,127,0,0}))return 9;
            if(!HidMouse.AbsoluteReport(16384,32767).SequenceEqual(new byte[]{0,0,64,255,127}))return 10;
            // Both report collections must agree on held buttons during motion.
            foreach(byte heldButtons in new byte[]{0,1,2,3,4,7})
            {
                var movement=HidMouse.AbsoluteReport(1234,5678,heldButtons);
                if(movement[0]!=heldButtons || movement[0]!=HidMouse.Report(heldButtons,0,0,0)[0] || !movement.Skip(1).SequenceEqual(HidMouse.AbsoluteReport(1234,5678).Skip(1)))return 12;
            }
            byte[] dragStates={HidMouse.Report(1,0,0,0)[0],HidMouse.AbsoluteReport(2000,3000,1)[0],HidMouse.AbsoluteReport(2500,3000,1)[0],HidMouse.Report(0,0,0,0)[0],HidMouse.AbsoluteReport(2500,3000,0)[0]};
            if(!dragStates.SequenceEqual(new byte[]{1,1,1,0,0}))return 13;
            var gesture=new LeftGesture(2);
            var down=new MouseCapture.Input(0,0,0,1,false,true,1000,2000);
            if(gesture.Route(down,4,4).Length!=0)return 14;
            var click=gesture.Route(down with {Buttons=0},4,4);
            if(!click.Select(e=>e.Buttons).SequenceEqual(new byte[]{1,0}))return 15;
            gesture.Route(down,4,4);
            if(gesture.Route(down with {X=1,Y=1,IsMove=true},4,4).Length!=0)return 16;
            var drag=gesture.Route(down with {X=5,IsMove=true,AbsoluteX=1500},4,4);
            if(!drag.Select(e=>e.Buttons).SequenceEqual(new byte[]{2,2}) || drag[0].AbsoluteX!=1000)return 17;
            if(gesture.Route(down with {Buttons=0},4,4).Single().Buttons!=0)return 18;
            gesture.Route(down,4,4);gesture.Reset();
            if(gesture.Route(down with {Buttons=0},4,4).Length!=1)return 19;
            var right=gesture.Route(down with {Buttons=2},4,4);
            if(right.Single().Buttons!=2)return 20;
            var nativeGesture=new LeftGesture(1);nativeGesture.Route(down,4,4);
            if(nativeGesture.Route(down with {X=8,IsMove=true},4,4).Any(e=>e.Buttons!=1))return 21;
            var directional=new LeftGesture(2,1);directional.Route(down,4,4);
            if(directional.Route(down with {Y=-8,IsMove=true},4,4).Any(e=>e.Buttons!=1))return 26;
            if(directional.Route(down with {X=20,IsMove=true},4,4).Single().Buttons!=1)return 27;
            if(directional.Route(down with {Buttons=0},4,4).Single().Buttons!=0)return 28;
            directional.Route(down,4,4);
            if(directional.Route(down with {X=8,IsMove=true},4,4).Any(e=>e.Buttons!=2))return 29;
            directional.Reset();directional.Route(down,4,4);
            if(!directional.Route(down with {Buttons=0},4,4).Select(e=>e.Buttons).SequenceEqual(new byte[]{1,0}))return 30;
            var bottomTest=new LeftGesture(2,1,0.98,0.2,350);
            var bottomDown=down with {AbsoluteY=32767};bottomTest.Route(bottomDown,4,4);
            if(bottomTest.Route(bottomDown with {Y=-8,AbsoluteY=32000,IsMove=true},4,4).Any(e=>e.Buttons!=1))return 31;
            if(bottomTest.Route(bottomDown with {Buttons=0,AbsoluteY=16000},4,4).Single().HoldBeforeReleaseMs!=350)return 32;
            bottomTest.Route(bottomDown,4,4);bottomTest.Route(bottomDown with {Y=-8,IsMove=true},4,4);
            if(bottomTest.Route(bottomDown with {Buttons=0,AbsoluteY=32000},4,4).Single().HoldBeforeReleaseMs!=0)return 33;
            bottomTest.Reset();if(bottomTest.Route(bottomDown with {Buttons=0},4,4).Single().HoldBeforeReleaseMs!=0)return 34;
            var jitterGesture=new LeftGesture(2,1,0.98,0.2,350);jitterGesture.Route(bottomDown,4,4);
            if(jitterGesture.Route(bottomDown with {X=5,Y=0,IsMove=true},4,4).Any(e=>e.Buttons!=1))return 35;
            if(jitterGesture.Route(bottomDown with {X=0,Y=-15,IsMove=true},4,4).Single().Buttons!=1)return 36;
            var anchored=new LeftGesture(2,1,0.97,0.2,0,0.99);
            anchored.Route(bottomDown with {AbsoluteY=32000},4,4);
            var anchoredDrag=anchored.Route(bottomDown with {X=5,IsMove=true},4,4);
            if(anchoredDrag[0].AbsoluteY!=(int)Math.Round(0.99*32767) || anchoredDrag.Any(e=>e.Buttons!=1))return 37;
            if(anchored.Route(bottomDown with {Buttons=0,AbsoluteY=24000},4,4).Single().HoldBeforeReleaseMs!=0)return 38;
            var separated=new LeftGesture(2,2,0.97,0.2,0,0.99,1);
            separated.Route(bottomDown,4,4);
            var separatedDrag=separated.Route(bottomDown with {Y=-8,AbsoluteY=32000,IsMove=true},4,4);
            if(separatedDrag.Any(e=>e.Buttons!=1) || separatedDrag[0].AbsoluteY!=(int)Math.Round(0.99*32767))return 46;
            if(separated.Route(bottomDown with {X=30,Y=0,IsMove=true},4,4).Single().Buttons!=1)return 47;
            if(separated.Route(bottomDown with {Buttons=0,AbsoluteY=18000},4,4).Single().Buttons!=0)return 48;
            separated.Route(down,4,4);
            if(separated.Route(down with {Y=-8,IsMove=true},4,4).Any(e=>e.Buttons!=2))return 49;
            separated.Reset();separated.Route(bottomDown,4,4);
            if(!separated.Route(bottomDown with {Buttons=0},4,4).Select(e=>e.Buttons).SequenceEqual(new byte[]{1,0}))return 50;
            if(separated.ButtonForSwipe(32767,0,-100)!=1 || separated.ButtonForSwipe(2000,0,100)!=2)return 51;
            if(!MirrorResolution.Valid("1179x2556@60") || MirrorResolution.Valid("0x720") || MirrorResolution.Valid("720x1280@256"))return 22;
            string rawArgs="-n 中文名字 -s 800x600 -fps 60 -custom keep -s 1280x720@60\r\n";
            string updatedArgs=MirrorResolution.Change(rawArgs,"1920x1080@60");
            if(updatedArgs!="-n 中文名字 -s 1920x1080@60 -fps 60 -custom keep -s 1920x1080@60\r\n" || MirrorResolution.Current(rawArgs)!="1280x720@60")return 23;
            if(MirrorResolution.Change("-n name", "640x480")!="-n name -s 640x480" || MirrorResolution.Change("-s 640x480 -custom keep",null)!=" -custom keep")return 24;
            try{MirrorResolution.Change("-s invalid -n name","640x480");return 25;}catch(InvalidOperationException){}
            var fit=MouseCapture.FitContent(1000,1000,0.5);
            if(fit!=(250,0,500,1000) || MouseCapture.MapAbsolute(0,500)!=0 || MouseCapture.MapAbsolute(499,500)!=32767 || MouseCapture.MapAbsolute(500,1001)!=16384)return 11;
            byte state=0;
            state=MouseCapture.ButtonState(state,0x201);
            state=MouseCapture.ButtonState(state,0x204);
            if(state!=3 || MouseCapture.ButtonState(state,0x202)!=2 || MouseCapture.ButtonState(state,0x205)!=1) return 2;
            if(!MouseCapture.Contains(-100,10,-100,0,0,20) || MouseCapture.Contains(0,10,-100,0,0,20) || MouseCapture.Contains(-50,20,-100,0,0,20)) return 3;
            bool blocked=false;
            if(MouseCapture.RouteHover(false,false,1,ref blocked) || !blocked) return 4;
            if(MouseCapture.RouteHover(true,false,1,ref blocked)) return 5;
            if(MouseCapture.RouteHover(true,false,0,ref blocked) || blocked) return 6;
            if(!MouseCapture.RouteHover(true,false,0,ref blocked)) return 7;
            if(MouseCapture.RouteHover(false,true,1,ref blocked) || !blocked) return 8;
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"self-test.txt"),"PASS: absolute coordinate payload/clamping/endianness; corners/center/letterbox mapping; relative button release; hover/drag boundaries\n");
            return 0;
        }
        if(args.Contains("--probe"))
        {
            Probe().GetAwaiter().GetResult(); return 0;
        }
        // One main instance per executable path: a second launch (e.g. desktop shortcut) would fight for
        // F8/F9, GATT and the embedded renderer, so it brings the running window forward instead.
        string instanceKey=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Environment.ProcessPath!.ToUpperInvariant())))[..16];
        using var instance=new Mutex(true,@"Local\iPhoneDesk-"+instanceKey,out bool firstInstance);
        if(!firstInstance){ActivateRunningInstance();return 0;}
        var app=new Application(); app.Run(new Desk()); return 0;
    }
    private static void ActivateRunningInstance()
    {
        using var self=Process.GetCurrentProcess();
        foreach(var other in Process.GetProcessesByName(self.ProcessName))using(other)
        {
            try
            {
                if(other.Id==self.Id || !string.Equals(other.MainModule?.FileName,Environment.ProcessPath,StringComparison.OrdinalIgnoreCase))continue;
                nint window=other.MainWindowHandle;if(window==0)continue;
                ShowWindow(window,IsIconic(window)?9:5);SetForegroundWindow(window);return;
            }
            catch{}
        }
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")]private static extern bool ShowWindow(nint hwnd,int how);
    [System.Runtime.InteropServices.DllImport("user32.dll")]private static extern bool IsIconic(nint hwnd);
    [System.Runtime.InteropServices.DllImport("user32.dll")]private static extern bool SetForegroundWindow(nint hwnd);
    private static async Task Probe()
    {
        string result;
        try
        {
            using var hid=new HidMouse();
            var events=new System.Collections.Concurrent.ConcurrentQueue<string>();
            hid.Status+=events.Enqueue;
            await hid.Start(); await Task.Delay(2500);
            result="HID service creation and advertising requested successfully. iPhone pairing NOT tested.\n"+string.Join("\n",events);
        }
        catch(Exception ex) { result=ex.ToString(); }
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"bluetooth-probe.txt"),result);
    }
}

public sealed partial class Desk : Window
{
    private readonly HidMouse hid=new();
    private bool MirrorConnected=>videoHost.Video!=0;
    private LeftGesture? leftGesture;
    private readonly TextBlock status=new(){Text="尚未启用蓝牙控制。",TextWrapping=TextWrapping.Wrap};
    private readonly TextBox log=new(){IsReadOnly=true,AcceptsReturn=true,Height=100,
        VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
    private readonly Border pad=new(){Background=new SolidColorBrush(Color.FromRgb(35,47,65)),
        CornerRadius=new CornerRadius(12),Height=220,Focusable=true};
    private Point? last;
    private bool sending;
    private double pendingX,pendingY;
    private byte held;
    private Process? engine;
    private readonly SemaphoreSlim reportLock=new(1);
    private Button control=null!,mirrorButton=null!,stopControl=null!,pauseControl=null!,reconnectControl=null!;
    private readonly TextBlock connectionNotice=new();
    private readonly HashSet<Button> busyButtons=new();
    private readonly List<Button> receiverActions=new();
    private readonly HashSet<Button> unsupportedActions=new();
    private bool hidStarting,hidStopping,receiverChanging;
    private bool reconnectRequested;
    private bool ReceiverRunning {get{try{return engine!=null && !engine.HasExited;}catch{return false;}}}
    private void UpdateActionStates()
    {
        bool ready=hid.AbsoluteConnected;
        control.IsEnabled=!hidStarting && !hidStopping && !ready;
        control.Content=hidStopping?"正在停止控制…":hidStarting?"正在启动控制…":ready?"iPhone 控制已连接":hid.Advertising?"重试控制连接":"连接 iPhone 控制";
        reconnectControl.IsEnabled=!hidStarting && !hidStopping && !ready && !busyButtons.Contains(reconnectControl);
        reconnectControl.Content=hidStarting?"正在重新连接…":ready?"控制已连接":"重新连接";
        if(ready)reconnectRequested=false;
        mirrorButton.IsEnabled=!MirrorConnected && !ReceiverRunning && !receiverChanging;
        mirrorButton.Content=receiverChanging?"正在应用设置…":MirrorConnected?"AirPlay 已投屏":ReceiverRunning?"等待 iPhone 投屏…":"启动 AirPlay 投屏";
        stopControl.IsEnabled=!hidStarting && !hidStopping && (hid.Advertising || hid.Connected) && !busyButtons.Contains(stopControl);
        pauseControl.IsEnabled=ready && !hidStarting && !hidStopping && !busyButtons.Contains(pauseControl);
        pauseControl.Content=capture?.Automatic==true || capture?.Active==true?"暂停控制":"恢复控制";
        foreach(var button in receiverActions)button.IsEnabled=!receiverChanging && !busyButtons.Contains(button) && !unsupportedActions.Contains(button);
        inputState.Text=ready?"● iPhone 控制已连接":hid.Connected?"◐ 等待定位通道连接":hid.Advertising?"◐ 等待 iPhone 连接控制":"○ iPhone 控制未连接";
        connectionNotice.Text=ready?"控制通道已就绪，无需再次连接。":hid.Advertising?$"点击通道：{hid.RelativeClients}；定位通道：{hid.AbsoluteClients}。手机显示已连接仍须两个报告就绪，可点重试恢复广播。":ReceiverRunning && !MirrorConnected?"接收器已启动，请在 iPhone 控制中心选择屏幕镜像。":"投屏和控制分别连接；断开后按钮会恢复可用。";
        if(reconnectRequested && !ready)connectionNotice.Text=$"已发起重连，等待手机恢复。点击通道：{hid.RelativeClients}；定位通道：{hid.AbsoluteClients}。若手机仍显示已连接但这里都是0，请在手机设置中开关一次蓝牙，保留配对。";
    }
    private MouseCapture? capture;
    private readonly LinkedList<MouseCapture.Input> inputs=new();
    private bool pumping;
    private int captureGeneration;
    private readonly GestureDiagnostics diagnostics=new(Path.Combine(AppContext.BaseDirectory,"logs"));
    private long nextGesture,activeGesture,nextSample,sendingGesture;
    private bool diagnosticsClosing,diagnosticsClosed;
    private string? diagnosticConnection;
        private void DiagnosticEvent(string kind,long id,object data)=>diagnostics.Event(kind,id,data);
    private readonly System.Windows.Threading.DispatcherTimer connectionTimer=new(){Interval=TimeSpan.FromMilliseconds(100)};
    private readonly CheckBox automaticMode=new(){Content="鼠标进入投屏画面自动控制，离开自动恢复",IsChecked=true,Margin=new Thickness(0,8,0,8)};
    private bool changingMode;
    private double pointerGain=1;
    private DateTime lastWindowScan=DateTime.MinValue;
    private int foundWindows=-1;
    public Desk()
    {
        BuildLayout();
        DiagnosticEvent("session-start",0,new {version=typeof(Desk).Assembly.GetName().Version?.ToString(),
            executable=Environment.ProcessPath,coordinateMax=32767,phoneEffect="unverified",
            sourceBaseline="bottom-anchor-16:32-behavior + diagnostics",
            os=Environment.OSVersion.ToString(),process64=Environment.Is64BitProcess,
            hidMapSha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(HidMouse.ReportMap))});
        _=Task.Run(()=>{try{using var file=File.OpenRead(Environment.ProcessPath!);
            DiagnosticEvent("build-identity",0,new {sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(file))});
            }catch(Exception ex){DiagnosticEvent("build-identity-error",0,new {type=ex.GetType().Name,error=ex.Message});}});
        hid.Diagnostic+=(kind,data)=>DiagnosticEvent(kind,sendingGesture,data);
        Loaded+=(_,_)=>{try{leftGesture=LeftGesture.Load(out var error);if(leftGesture!=null){leftGesture.Trace+=message=>{DiagnosticEvent("route",activeGesture,new {message});SetStatus(message);};SetStatus($"左键动作分流已加载：轻点按钮1，拖动按钮{leftGesture.DragButton}。");}if(error!=null)SetStatus(error);}catch(Exception ex){SetStatus("鼠标动作配置读取失败，原文件未改："+ex.Message);}};
        Loaded+=(_,_)=>{DiagnosticEvent("profile",0,new {
            drag=leftGesture?.DragButton,vertical=leftGesture?.VerticalDragButton,bottomButton=leftGesture?.BottomDragButton,
            bottomStart=leftGesture?.BottomEdgeStart,anchor=leftGesture?.BottomAnchorY,
            travel=leftGesture?.BottomTravel,hold=leftGesture?.BottomHoldMs,
            thresholdX=SystemParameters.MinimumHorizontalDragDistance,thresholdY=SystemParameters.MinimumVerticalDragDistance});};
        hid.Status+=message=>Dispatcher.BeginInvoke(()=>SetStatus(message));
        pad.MouseEnter+=(_,e)=>last=e.GetPosition(pad);
        pad.MouseLeave+=(_,_)=>{if(!pad.IsMouseCaptured) last=null;};
        pad.MouseMove+=Move;
        pad.MouseLeftButtonDown+=async (_,e)=>
        {
            if(!hid.Connected){SetStatus("尚未连接 iPhone，不能发送点击。");return;}
            pad.Focus(); pad.CaptureMouse(); last=e.GetPosition(pad); held=1;
            await Send(pressed:1); e.Handled=true;
        };
        pad.MouseLeftButtonUp+=async (_,e)=>{held=0;pad.ReleaseMouseCapture();await Send(pressed:0);e.Handled=true;};
        pad.MouseWheel+=async (_,e)=>{await Send(wheel:Math.Sign(e.Delta));e.Handled=true;};
        pad.LostMouseCapture+=async (_,_)=>{if(held!=0){held=0;await Send(pressed:0);}};
        Deactivated+=async (_,_)=>{if(capture?.Active==true)return;held=0;pad.ReleaseMouseCapture();if(hid.Connected) await Send(pressed:0);};
        PreviewKeyDown+=async (_,e)=>{if(e.Key==Key.Escape){held=0;pad.ReleaseMouseCapture();await Send(pressed:0);}};
        Closing+=async (_,e)=>
        {
            if(diagnosticsClosed)return;
            e.Cancel=true;if(diagnosticsClosing)return;diagnosticsClosing=true;
            try
            {
            remoteCancel?.Cancel();
            localServer?.Stop();localServer=null;
            DiagnosticEvent("cancel",activeGesture,new {reason="window-close",generation=captureGeneration,queued=inputs.Count});
            connectionTimer.Stop();captureGeneration++;inputs.Clear();capture?.Dispose();
            try{videoHost.Detach();}catch{}
            if(hid.Connected) { try{hid.Send(pressed:0).Wait(500);}catch{} }
            hid.Dispose();
            DiagnosticEvent("session-end",0,new {dropped=diagnostics.Dropped,error=diagnostics.WriteError});
            bool flushed=await diagnostics.Complete(TimeSpan.FromSeconds(2));
            try{File.AppendAllText(Path.Combine(AppContext.BaseDirectory,"运行日志.txt"),$"{DateTimeOffset.Now:O} 诊断日志收尾：flushed={flushed} dropped={diagnostics.Dropped} error={diagnostics.WriteError}\n");}catch{}
            }
            finally{diagnosticsClosed=true;Close();}
        };
        SourceInitialized+=(_,_)=>
        {
            HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(WorkAreaBounds);
            try
            {
                capture=new MouseCapture(new WindowInteropHelper(this).Handle);
                capture.ToggleRequested+=ToggleCapture;
                capture.ReleaseRequested+=()=>Dispatcher.BeginInvoke(async ()=>{DiagnosticEvent("cancel-request",activeGesture,new {reason="F9-Esc-or-hook-failure"});await ReleaseCapture();});
                capture.Received+=QueueInput;
                capture.HoverChanged+=HoverChanged;
                EnableHover();
            }
            catch(Exception ex){SetStatus(ex.Message);}
        };
        connectionTimer.Tick+=async (_,_)=>
        {
            string state=$"{MirrorConnected}/{hid.Connected}/{hid.AbsoluteConnected}/{capture?.Automatic}";
            if(state!=diagnosticConnection){diagnosticConnection=state;
                DiagnosticEvent("connection-state",activeGesture,new {mirror=MirrorConnected,
                    relativeSubscribed=hid.Connected,absoluteSubscribed=hid.AbsoluteConnected,
                    automatic=capture?.Automatic,windowState=WindowState.ToString()});}
            if(capture!=null)
            {
                if(applePageActive)capture.ClientReady=hid.AbsoluteConnected;else capture.ClientReady=false;
                if(applePageActive && (videoHost.Video!=0 || DateTime.UtcNow-lastWindowScan>TimeSpan.FromSeconds(1)))
                { try { videoHost.TryAttach(Path.Combine(AppContext.BaseDirectory,"receiver","uxplay-windows.exe")); capture.EmbeddedVideo=videoHost.Video; } catch(Exception ex) { SetStatus(ex.Message); } }
                mirrorState.Text=MirrorConnected ? "● 屏幕镜像已连接" : "○ 等待 iPhone 屏幕镜像";
                UpdateActionStates();
                metrics.Text=$"定位 {hid.PositionCount} 次 · 按键 {hid.ButtonCount} 次 · 最近发送 {hid.LastNotifyMs:0.0} ms\n诊断日志 {diagnostics.Session[..8]} · 丢失 {diagnostics.Dropped} 条"+(diagnostics.WriteError==null?"":$" · 写入失败：{diagnostics.WriteError}");
                if(applePageActive && DateTime.UtcNow-lastWindowScan>TimeSpan.FromSeconds(1))
                {
                    lastWindowScan=DateTime.UtcNow;
                    capture.RefreshVideoWindows(Path.Combine(AppContext.BaseDirectory,"receiver","uxplay-windows.exe"));
                    if(foundWindows!=capture.VideoWindowCount)
                    {
                        foundWindows=capture.VideoWindowCount;
                        SetStatus(foundWindows>0?$"已识别 {foundWindows} 个投屏窗口。{(hid.AbsoluteConnected?"坐标同步已连接。":"等待绝对坐标协议连接；如未自动连接请在手机重新配对。")}":"等待投屏画面窗口；接收器设置窗口不触发自动控制。");
                    }
                }
            }
            if(capture?.Active==true && (!hid.Connected || (capture.Automatic && !hid.AbsoluteConnected)))
            {
                if(capture.Automatic){capture.LeaveHover();SetStatus("蓝牙断开，电脑操作已恢复；自动切换等待重新连接。");}
                else await ReleaseCapture("蓝牙连接断开，电脑鼠标已自动恢复。");
            }
        };
        connectionTimer.Start();
        Loaded+=(_,_)=>{try{StartLocalControl();}catch(Exception ex){SetStatus("本机控制接口启动失败："+ex.Message);}};
        var launchArgs=Environment.GetCommandLineArgs();
        // Platform selection starts Apple services; Android selection never starts them.
        if(Environment.GetCommandLineArgs().Contains("--ui-smoke"))
        {
            var timer=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromSeconds(2)};
            timer.Tick+=(_,_)=>{timer.Stop();File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"ui-smoke.txt"),"PASS: WPF window loaded");Close();};timer.Start();
        }
        if(Environment.GetCommandLineArgs().Contains("--render-ui"))
        {
            var timer=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromSeconds(2)};
            timer.Tick+=(_,_)=>
            {
                timer.Stop();
                var visual=(FrameworkElement)Content;
                var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)visual.ActualWidth,(int)visual.ActualHeight,96,96,PixelFormats.Pbgra32);
                bitmap.Render(visual);
                var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,"界面预览.png")))encoder.Save(file);
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"embed-smoke.txt"),videoHost.Video!=0?"PASS: live GPU-rendered video window attached to unified UI":"WAIT: UI rendered; no live video window available for embedding test");
                Close();
            };timer.Start();
        }
        if(Environment.GetCommandLineArgs().Contains("--capture-smoke"))
        {
            // No mouse events are synthesized and no phone input is sent.
            Loaded+=(_,_)=>
            {
                try
                {
                    if(capture==null)throw new InvalidOperationException("hotkey initialization failed");
                    capture.Start();if(!capture.Active)throw new InvalidOperationException("hook inactive");
                    capture.Stop();if(capture.Active)throw new InvalidOperationException("hook failed to release");
                    capture.EnableAutomatic();capture.ClientReady=false;
                    if(!capture.Installed || capture.Active || !capture.Automatic)throw new InvalidOperationException("automatic watcher incorrectly captured mouse without a client");
                    capture.RefreshVideoWindows(Path.Combine(AppContext.BaseDirectory,"receiver","uxplay-windows.exe"));
                    int count=capture.VideoWindowCount;capture.Stop();
                    File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"capture-smoke.txt"),$"PASS: hotkeys, hook install/release, automatic watcher passes through without a client; identified video windows={count}");
                }
                catch(Exception ex){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"capture-smoke.txt"),"FAIL: "+ex);}
                Close();
            };
        }
    }
    private TextBlock Text(string value,int size=14)=>new(){Text=value,FontSize=size,
        TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,8),Foreground=Brushes.Black};
    private Button Button(string value,RoutedEventHandler handler)
    {
        var b=new Button{Content=value,Padding=new Thickness(14,9,14,9),Margin=new Thickness(0,5,10,5)};
        b.Click+=handler;return b;
    }
    private void SetStatus(string message)
    {
        status.Text=message;log.AppendText($"{DateTime.Now:HH:mm:ss}  {message}\n");log.ScrollToEnd();
        try{File.AppendAllText(Path.Combine(AppContext.BaseDirectory,"运行日志.txt"),$"{DateTimeOffset.Now:O} {message}\n");}catch{}
    }
    private void OpenReceiver(object sender,RoutedEventArgs e)
    {
        if(MirrorConnected){SetStatus("AirPlay 已投屏，无需再次连接。");return;}
        try
        {
            if(engine!=null && !engine.HasExited){SetStatus("接收器已经打开，请查看任务栏或系统托盘。");return;}
            var dir=Path.Combine(AppContext.BaseDirectory,"receiver");
            var file=Directory.Exists(dir)?Directory.GetFiles(dir,"uxplay-windows.exe",SearchOption.AllDirectories).FirstOrDefault():null;
            if(file==null) throw new FileNotFoundException("接收器文件缺失，请运行“下载接收器.ps1”。");
            foreach(var existing in Process.GetProcessesByName("uxplay-windows"))
            {
                try
                {
                    if(string.Equals(existing.MainModule?.FileName,file,StringComparison.OrdinalIgnoreCase))
                    {
                        engine=existing;
                        // A receiver left by an earlier session without any renderer window cannot show video
                        // (observed 2026-10-22: phone session open, no window). Heal once per session, never loop.
                        // Another iPhoneDesk (any path) may hold the renderer embedded, where it is not top-level.
                        if(!receiverHealed && !receiverChanging && !OtherDeskRunning() && VideoHost.FindRenderer(file)==0)
                        {
                            receiverHealed=true;_=AutoRestartReceiver("检测到上次遗留的投屏接收器没有画面窗口，正在自动重启接收器…");return;
                        }
                        SetStatus("投屏接收器已经运行，请从 iPhone 屏幕镜像连接，或查看系统托盘。不会重复启动。");return;
                    }
                }
                catch{} existing.Dispose();
            }
            engine=Process.Start(new ProcessStartInfo(file){WorkingDirectory=Path.GetDirectoryName(file)!,UseShellExecute=true});
            SetStatus("已启动本项目 AirPlay 接收器。电脑与 iPhone 连接同一 Wi-Fi，在 iPhone 控制中心 → 屏幕镜像中选择投屏名称。等待手机连接，无需手机 App 或 USB 驱动。");
        }
        catch(Exception ex){SetStatus(ex.Message);}
    }
    private bool receiverHealed;
    private static bool OtherDeskRunning()
    {
        using var self=Process.GetCurrentProcess();
        var others=Process.GetProcessesByName(self.ProcessName);
        try{return others.Any(p=>p.Id!=self.Id);}finally{foreach(var p in others)p.Dispose();}
    }
    // Stops only receiver processes at this package's absolute paths (§2), then starts a fresh receiver.
    private async Task RestartReceiver()
    {
        string dir=Path.Combine(AppContext.BaseDirectory,"receiver");
        foreach(string name in new[]{"uxplay-windows","uxplay-bluetooth-beacon"})foreach(var process in Process.GetProcessesByName(name))using(process)
        {
            try{if(!string.Equals(process.MainModule?.FileName,Path.Combine(dir,name+".exe"),StringComparison.OrdinalIgnoreCase))continue;}catch{continue;}
            try{if(!process.HasExited)process.Kill();}catch(InvalidOperationException){}
            await process.WaitForExitAsync();
        }
        engine=null;OpenReceiver(this,new RoutedEventArgs());
    }
    private async Task AutoRestartReceiver(string reason)
    {
        if(receiverChanging)return;
        receiverChanging=true;UpdateActionStates();SetStatus(reason);
        try{await RestartReceiver();SetStatus("接收器已重启。请在 iPhone 控制中心 → 屏幕镜像重新选择投屏名称。");}
        catch(Exception ex){SetStatus("接收器自动重启失败，可点“重启投屏接收器”重试："+ex.Message);}
        finally{receiverChanging=false;UpdateActionStates();}
    }
    private async void StartHid(object sender,RoutedEventArgs e)
    {
        if(hidStarting || hidStopping || hid.AbsoluteConnected)return;
        hidStarting=true;UpdateActionStates();
        bool resumeAutomatic=automaticMode.IsChecked==true;
        try{await ReleaseCapture();SetStatus("检查蓝牙外设支持并创建 HID 服务…");if(hid.Advertising)await hid.ReconnectAdvertising();else await hid.Start();if(resumeAutomatic)EnableHover();}
        catch(Exception ex){SetStatus(ex.Message);}
        finally{hidStarting=false;UpdateActionStates();}
    }
    private async Task ReconnectControl()
    {
        if(hidStarting || hidStopping)throw new InvalidOperationException("控制服务正在切换。");
        if(hid.AbsoluteConnected)return;
        hidStarting=true;UpdateActionStates();bool resume=automaticMode.IsChecked==true;
        try{reconnectRequested=true;SetStatus("正在重新发布控制服务…");await ReleaseCapture();await hid.ReconnectAdvertising();if(resume)EnableHover();SetStatus(hid.AbsoluteConnected?"控制已重新连接。":"重连已发起，等待手机订阅定位和点击报告；请查看连接卡状态。");}
        finally{hidStarting=false;UpdateActionStates();}
    }
    private async Task StopControl()
    {
        if(hidStarting || hidStopping)return;
        hidStopping=true;UpdateActionStates();
        try{await ReleaseCapture();held=0;pad.ReleaseMouseCapture();if(hid.Connected)await Send(pressed:0);hid.Dispose();SetStatus("iPhone 控制已停止，可重新连接。");}
        finally{hidStopping=false;UpdateActionStates();}
    }
    private async Task Send(int x=0,int y=0,int wheel=0,byte? pressed=null,int? expectedGeneration=null,long diagnosticGesture=0)
    {
        if(!hid.Connected) return;
        await reportLock.WaitAsync();
        try{
            sendingGesture=diagnosticGesture;
            DiagnosticEvent("direct-send",diagnosticGesture,new {x,y,wheel,pressed,expectedGeneration,currentGeneration=captureGeneration,connected=hid.Connected});
            if(hid.Connected && (!expectedGeneration.HasValue || expectedGeneration==captureGeneration))await hid.Send(x,y,wheel,pressed);
            else DiagnosticEvent("send-skipped",diagnosticGesture,new {reason="disconnected-or-generation",expectedGeneration,currentGeneration=captureGeneration});
        }
        catch(Exception ex){DiagnosticEvent("send-error",diagnosticGesture,new {type=ex.GetType().Name,error=ex.Message});capture?.Stop();captureGeneration++;inputs.Clear();SetStatus(ex.Message+" 电脑鼠标已恢复。");held=0;pad.ReleaseMouseCapture();hid.Dispose();}
        finally{sendingGesture=0;reportLock.Release();}
    }
    private async void Move(object sender,MouseEventArgs e)
    {
        var pos=e.GetPosition(pad);var prev=last;last=pos;
        if(prev==null || !hid.Connected) return;
        pendingX+=(pos.X-prev.Value.X)*pointerGain;pendingY+=(pos.Y-prev.Value.Y)*pointerGain;
        if(sending)return;sending=true;
        try
        {
            while(hid.Connected && (Math.Abs(pendingX)>=1 || Math.Abs(pendingY)>=1))
            {
                var x=Math.Clamp((int)pendingX,-127,127);var y=Math.Clamp((int)pendingY,-127,127);
                pendingX-=x;pendingY-=y;await Send(x,y);await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
            }
        }
        finally{sending=false;if(!hid.Connected){pendingX=0;pendingY=0;}}
    }
    private void OpenFile(string file)=>Shell(Path.Combine(AppContext.BaseDirectory,file));
    private void EnableHover()
    {
        if(!applePageActive)return;
        if(capture==null)return;
        try
        {
            captureGeneration++;inputs.Clear();held=0;
            capture.EnableAutomatic();capture.ClientReady=hid.AbsoluteConnected;
            capture.RefreshVideoWindows(Path.Combine(AppContext.BaseDirectory,"receiver","uxplay-windows.exe"));
            changingMode=true;automaticMode.IsChecked=true;changingMode=false;
            SetStatus(hid.AbsoluteConnected?"绝对坐标同步已连接。鼠标移入投屏定位并控制，移出恢复电脑；F9 暂停。":"坐标同步模式已启用，等待手机订阅新报告；如未自动连接请在手机忽略此电脑并重新配对。");
        }
        catch(Exception ex){changingMode=true;automaticMode.IsChecked=false;changingMode=false;SetStatus(ex.Message);}
    }
    private void HoverChanged(bool inside)
    {
        DiagnosticEvent("hover",activeGesture,new {inside,generation=captureGeneration,queued=inputs.Count,
            connected=capture?.ClientReady,automatic=capture?.Automatic});
        activeGesture=0;
        leftGesture?.Reset();captureGeneration++;inputs.Clear();int generation=captureGeneration;
        if(inside)QueueInput(new MouseCapture.Input(0,0,0,0,false));
        _ = Dispatcher.BeginInvoke(async ()=>
        {
            if(generation!=captureGeneration)return;
            if(!inside)await Send(pressed:0,expectedGeneration:generation);
            SetStatus(inside?"鼠标在投屏画面内，按画面位置发送绝对坐标和点击。":"鼠标已离开投屏画面，已恢复电脑操作并释放手机按键。");
        });
    }
    private async void ToggleCapture()
    {
        if(capture==null){SetStatus("全局鼠标功能初始化失败，可使用触控板模式。");return;}
        if(capture.Active && !capture.Automatic){await ReleaseCapture();return;}
        if(!hid.Connected){SetStatus("先连接 iPhone 控制；没有客户端连接时不会接管鼠标。");return;}
        try
        {
            held=0;pad.ReleaseMouseCapture();await Send(pressed:0);
            if(!hid.Connected){SetStatus("蓝牙连接已经断开，请先重新配对。");return;}
            captureGeneration++;inputs.Clear();capture.Start();
            changingMode=true;automaticMode.IsChecked=false;changingMode=false;
            SetStatus("电脑鼠标正在控制手机。观察手机指针操作；按 F9 或 Esc 恢复电脑鼠标。");
        }
        catch(Exception ex){capture.Stop();SetStatus(ex.Message);}
    }
    private async Task ReleaseCapture(string? message=null)
    {
        DiagnosticEvent("cancel",activeGesture,new {reason=message??"release-capture",generation=captureGeneration,queued=inputs.Count});
        activeGesture=0;
        remoteCancel?.Cancel();leftGesture?.Reset();
        bool wasActive=capture?.Active==true;
        capture?.Stop();captureGeneration++;inputs.Clear();held=0;
        changingMode=true;automaticMode.IsChecked=false;changingMode=false;
        await Send(pressed:0);
        if(wasActive || message!=null)SetStatus(message??"已恢复电脑鼠标。按 F8 可再次控制手机。");
    }
    private void QueueInput(MouseCapture.Input input)
    {
        if(activeGesture==0 && input.Buttons!=0){activeGesture=++nextGesture;
            DiagnosticEvent("physical-start",activeGesture,new {viewport=capture?.DiagnosticViewport,
                automatic=capture?.Automatic,generation=captureGeneration});}
        input=input with {GestureId=activeGesture,ReceivedTicks=Stopwatch.GetTimestamp(),Sample=++nextSample};
        if(activeGesture!=0)DiagnosticEvent("physical-input",activeGesture,new {input.Sample,input.X,input.Y,
            input.AbsoluteX,input.AbsoluteY,input.Buttons,input.Wheel,input.IsMove,input.Absolute,
            input.ReceivedTicks,generation=captureGeneration,queued=inputs.Count,connected=capture?.ClientReady});
        if(!input.IsMove && input.Absolute && input.Buttons!=0)SetStatus($"物理输入：按钮位{input.Buttons}，自动模式{capture?.Automatic}，动作分流{leftGesture!=null}。");
        if(leftGesture==null){QueueRawInput(input);if(!input.IsMove && input.Buttons==0)activeGesture=0;return;}
        foreach(var routed in leftGesture.Route(input,SystemParameters.MinimumHorizontalDragDistance,SystemParameters.MinimumVerticalDragDistance))QueueRawInput(routed);
        if(!input.IsMove && input.Buttons==0)activeGesture=0;
    }
    private void QueueRawInput(MouseCapture.Input input)
    {
        if(input.GestureId!=0)DiagnosticEvent("queued",input.GestureId,new {input.Sample,input.Buttons,
            input.AbsoluteX,input.AbsoluteY,input.IsMove,input.HoldBeforeReleaseMs,
            generation=captureGeneration,queueBefore=inputs.Count});
        if(input.IsMove && !input.Absolute)input=input with {X=(int)Math.Round(input.X*pointerGain),Y=(int)Math.Round(input.Y*pointerGain)};
        // Same-thread queue: coalesce adjacent motion packets without reordering
        // button transitions. Keep Bluetooth latency from growing without bound.
        if(input.IsMove && inputs.Last?.Value is {IsMove:true} previous && previous.Buttons==input.Buttons && previous.Absolute==input.Absolute)
            {
            if(input.GestureId!=0)DiagnosticEvent("coalesced",input.GestureId,new {replacedSample=previous.Sample,keptSample=input.Sample,previous.Buttons,previous.AbsoluteX,previous.AbsoluteY});
            inputs.Last.Value=input.Absolute?input:previous with {X=Math.Clamp(previous.X+input.X,-254,254),Y=Math.Clamp(previous.Y+input.Y,-254,254),GestureId=input.GestureId,ReceivedTicks=input.ReceivedTicks,Sample=input.Sample};
        }
        else
        {
            // A button report includes its target coordinates. Any pending motion
            // directly before it is redundant only in the same button state.
            // A held final movement must precede release; never erase that path.
            if(input.Absolute && !input.IsMove && inputs.Last?.Value is {IsMove:true,Absolute:true} lastMove && lastMove.Buttons==input.Buttons)inputs.RemoveLast();
            inputs.AddLast(input);
        }
        if(inputs.Count>128)
        {
            DiagnosticEvent("cancel",input.GestureId,new {reason="input-queue-overflow",queued=inputs.Count,generation=captureGeneration});
            capture?.Stop();captureGeneration++;inputs.Clear();
            Dispatcher.BeginInvoke(async ()=>await ReleaseCapture("输入发送积压，电脑鼠标已恢复。"));return;
        }
        if(!pumping)Dispatcher.BeginInvoke(PumpInput);
    }
    private async void PumpInput()
    {
        if(pumping)return;pumping=true;
        int generation=captureGeneration;
        try
        {
            while(capture?.Active==true && hid.Connected && generation==captureGeneration && inputs.First!=null)
            {
                var next=inputs.First.Value;inputs.RemoveFirst();
                if(next.Absolute)
                {
                    await SendAbsolute(next,generation);
                    await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
                    continue;
                }
                int x=next.X,y=next.Y;
                do
                {
                    int dx=Math.Clamp(x,-127,127),dy=Math.Clamp(y,-127,127);
                    if(generation!=captureGeneration || capture?.Active!=true)break;
                    await Send(dx,dy,next.Wheel,next.Buttons,generation,next.GestureId);x-=dx;y-=dy;
                    // Yield so F9/Esc and disconnect handling always remain live.
                    await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
                    next=next with {Wheel=0};
                }while((x!=0 || y!=0) && hid.Connected && generation==captureGeneration);
            }
        }
        finally
        {
            pumping=false;
            if(inputs.Count>0 && capture?.Active==true && hid.Connected)_ = Dispatcher.BeginInvoke(PumpInput);
        }
    }
    private async Task SendAbsolute(MouseCapture.Input input,int generation)
    {
        if(!hid.AbsoluteConnected || generation!=captureGeneration){DiagnosticEvent("send-skipped",input.GestureId,new {reason="disconnected-or-generation",generation,currentGeneration=captureGeneration});return;}
        var sendWatch=Stopwatch.StartNew();
        await reportLock.WaitAsync();
        double lockMs=sendWatch.Elapsed.TotalMilliseconds;
        long positionsBefore=hid.PositionCount,buttonsBefore=hid.ButtonCount;
        sendingGesture=input.GestureId;
        DiagnosticEvent("send-start",input.GestureId,new {input.Sample,input.Buttons,input.AbsoluteX,input.AbsoluteY,
            generation,currentGeneration=captureGeneration,lockMs,queueDelayMs=input.ReceivedTicks==0?(double?)null:Stopwatch.GetElapsedTime(input.ReceivedTicks).TotalMilliseconds,queued=inputs.Count});
        try
        {
            if(hid.AbsoluteConnected && generation==captureGeneration)
                {
                // Settle at the final coordinate while held; F9 changes generation and aborts this wait.
                if(input.HoldBeforeReleaseMs>0)
                {
                    await hid.SendAt(input.AbsoluteX,input.AbsoluteY,stillCurrent:()=>generation==captureGeneration && capture?.Active==true);
                    var wait=System.Diagnostics.Stopwatch.StartNew();
                    while(wait.ElapsedMilliseconds<input.HoldBeforeReleaseMs)
                    {
                        if(generation!=captureGeneration || capture?.Active!=true)return;
                        await Task.Delay((int)Math.Clamp(input.HoldBeforeReleaseMs-wait.ElapsedMilliseconds,1,30));
                    }
                }
                if(generation!=captureGeneration || capture?.Active!=true)return;
                await hid.SendAt(input.AbsoluteX,input.AbsoluteY,input.Wheel,input.Buttons,
                    ()=>generation==captureGeneration && capture?.Active==true);
            }
        }
        catch(Exception ex)
        {
            capture?.Stop();captureGeneration++;inputs.Clear();
            changingMode=true;automaticMode.IsChecked=false;changingMode=false;
            DiagnosticEvent("send-error",input.GestureId,new {input.Sample,type=ex.GetType().Name,error=ex.Message,generation,currentGeneration=captureGeneration});
            SetStatus(ex.Message+" 同步控制已暂停。");
            try{if(hid.Connected)await hid.Send(pressed:0);}catch{}
        }
        finally{
            DiagnosticEvent("send-end",input.GestureId,new {input.Sample,totalMs=sendWatch.Elapsed.TotalMilliseconds,
                positionReports=hid.PositionCount-positionsBefore,buttonReports=hid.ButtonCount-buttonsBefore,
                generation,currentGeneration=captureGeneration,connected=hid.AbsoluteConnected,
                notifyMs=hid.LastNotifyMs,queued=inputs.Count});
            sendingGesture=0;reportLock.Release();
        }
    }
    private void Shell(string target)
    {try{Process.Start(new ProcessStartInfo(target){UseShellExecute=true});}catch(Exception ex){SetStatus(ex.Message);}}
}
