using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace iPhoneDesk;

// ADB statuses are preserved: unknown states must never be treated as authorized.
public sealed record AndroidDevice(string Serial, string State, string Model)
{
    public bool Ready => State == "device";
    public override string ToString() => $"{Model} · {Serial} · {State switch { "device" => "已授权", "unauthorized" => "请在手机确认调试授权", "offline" => "离线", _ => State }}";
}

public static class AndroidProtocol
{
    public static IReadOnlyList<AndroidDevice> ParseDevices(string text)
    {
        var devices = new List<AndroidDevice>();
        foreach (string line in text.Split('\n'))
        {
            string row = line.Trim();
            if (row.Length == 0 || row.StartsWith("List of devices") || row.StartsWith('*')) continue;
            string[] fields = Regex.Split(row, @"\s+");
            if (fields.Length < 2) continue;
            string model = fields.Skip(2).FirstOrDefault(x => x.StartsWith("model:"))?[6..].Replace('_', ' ') ?? "Android";
            devices.Add(new(fields[0], fields[1], model));
        }
        return devices;
    }

    public static string Endpoint(string value)
    {
        value = value.Trim();
        // Require the port shown by Android; pairing and connection ports differ.
        if (value.Any(char.IsWhiteSpace) || value.Contains('/') || value.Contains('@') || value.Contains('?') || value.Contains('#') ||
            !Uri.TryCreate("tcp://" + value, UriKind.Absolute, out var uri) ||
            uri.Port < 1 || uri.Port > 65535 || uri.HostNameType == UriHostNameType.Unknown ||
            !value.EndsWith(":" + uri.Port, StringComparison.Ordinal))
            throw new ArgumentException("请输入手机显示的地址和端口，例如 192.168.1.8:37123（IPv6 用 [地址]:端口）。");
        return value;
    }

    public static bool IsWirelessSerial(string serial)
    {
        try { Endpoint(serial); return true; } catch (ArgumentException) { }
        // ADB mDNS discovery serial (Android TLS service type), preserved verbatim.
        return Regex.IsMatch(serial, @"^[A-Za-z0-9._-]+\._adb-tls-connect\._tcp\.?$") && !serial.StartsWith('-');
    }

    public static int SelfTest()
    {
        var rows = ParseDevices("List of devices attached\r\nUSB-A device product:p model:Pixel_8 transport_id:1\n192.168.1.2:32100 unauthorized\nUSB-B offline\nUSB-C future_state model:Unknown\n");
        if (rows.Count != 4 || rows[0].Model != "Pixel 8" || !rows[0].Ready || rows.Skip(1).Any(x => x.Ready)) return 1;
        foreach (string value in new[] { "192.168.1.8:37123", "phone.local:5555", "[::1]:37123" }) if (Endpoint(value) != value) return 2;
        foreach (string value in new[] { "localhost", "host:0", "host:65536", "host:22/evil", "x@y:22", "host:22 --bad" })
        {
            try { Endpoint(value); return 3; } catch (ArgumentException) { }
        }
        if (!IsWirelessSerial("adb-abc._adb-tls-connect._tcp") || IsWirelessSerial("USB-A")) return 4;
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "android-self-test.txt"), "PASS: ADB device/status metadata; explicit authorization; IPv4/DNS/IPv6 endpoints; invalid arguments rejected. No phone commands sent.\n");
        return 0;
    }
}

public sealed class AndroidPanel : UserControl, IDisposable
{
    private readonly string runtime = Path.Combine(AppContext.BaseDirectory, "android", "scrcpy-win64-v5.0.1");
    private readonly ComboBox devices = new() { Margin = new(0, 8, 0, 8), MinHeight = 38 };
    private readonly TextBlock state = new() { Text = "准备安卓组件后刷新设备。", TextWrapping = TextWrapping.Wrap };
    private readonly TextBox connectionAddress = new() { MinHeight = 34 };
    private readonly TextBox pairingAddress = new() { MinHeight = 34 };
    private readonly PasswordBox pairingCode = new() { MinHeight = 34 };
    private readonly List<Button> actions = new();
    private readonly CancellationTokenSource lifetime = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Queue<string> recent = new();
    private readonly TextBox log = new() { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, Height = 110, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    private Button start = null!, stop = null!, install = null!, refresh = null!, pair = null!, connect = null!, disconnect = null!;
    private Process? mirror;
    private string? sessionSerial;
    private bool busy, closed, standalone, attachFailed, embeddedAnnounced;
    private readonly AndroidVideoHost videoHost = new();
    private readonly TextBlock screenName = Desk.Label("Android",18,"#1C1C1E",true);
    private readonly TextBlock videoState = Desk.Label("等待连接",11,"#248A3D");
    private Grid viewport = null!;
    private Button windowMode = null!;
    private void FitPhone()
    {
        if(viewport==null)return;
        double ratio=videoHost.AspectRatio;
        double height=Math.Max(1,Math.Min(viewport.ActualHeight-16,(viewport.ActualWidth-24)/ratio));
        videoHost.Height=height;videoHost.Width=height*ratio;
    }
    private string Adb => Path.Combine(runtime, "adb.exe");
    private bool Installed => File.Exists(Adb) && File.Exists(Path.Combine(runtime, "scrcpy.exe")) && File.Exists(Path.Combine(runtime, "scrcpy-server"));
    private bool Running { get { try { return mirror != null && !mirror.HasExited; } catch { return false; } } }

    public AndroidPanel(bool refreshOnLoad = true)
    {
        FontFamily = new("Microsoft YaHei UI"); FontSize = 14; Background = new SolidColorBrush(Color.FromRgb(244, 246, 251));
        var root = new Grid { Background = Background }; root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) }); root.ColumnDefinitions.Add(new ColumnDefinition()); Content = root;
        var cards = new StackPanel { Margin = new Thickness(18,24,18,24) };
        var rail = new Border { Background = new SolidColorBrush(Color.FromRgb(248,249,252)), BorderBrush = new SolidColorBrush(Color.FromRgb(230,232,237)), BorderThickness = new Thickness(0,0,1,0) };
        rail.Child = new ScrollViewer { Content = cards, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }; root.Children.Add(rail);
        root.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)] = Desk.ThinScrollStyle();
        cards.Children.Add(Desk.Label("设备",11,"#8E8E93")); cards.Children.Add(Desk.Label("我的 Android",20,"#1C1C1E",true)); cards.Children.Add(Desk.Label("画面与操作，随手连接",12,"#8E8E93"));
        var connection=Desk.Card("连接");cards.Children.Add(connection.Border);
        state.FontSize=12; connection.Body.Children.Add(state);
        devices.Style=IOSControls.ComboStyle(); devices.MinHeight=36; connection.Body.Children.Add(devices);
        devices.SelectionChanged += (_, _) => Update();
        refresh = Action(connection.Body,"刷新设备",Refresh);
        start = Action(connection.Body,"启动投屏与控制",StartMirror,true);
        stop = Action(connection.Body,"停止安卓投屏",StopMirror);
        windowMode=Action(connection.Body,"切换独立窗口",()=> { standalone=!standalone; if(standalone)videoHost.Detach();else attachFailed=false;Update();return Task.CompletedTask; });
        Text(connection.Body,"USB：开启 USB 调试，插线后在手机确认授权。\n鼠标和键盘直接控制，无需蓝牙配对。");
        var components=Desk.Card("安卓组件");cards.Children.Add(components.Border);
        install=Action(components.Body,"准备 / 校验组件",Install);
        Text(components.Body,"官方 scrcpy 组件，首次使用准备一次。");
        var wireless=Desk.Card("Wi-Fi 无线连接");cards.Children.Add(wireless.Border);
        Text(wireless.Body,"Android 11 及以上：开启无线调试，先配对，再连接。配对码不保存。");
        Text(wireless.Body,"配对地址（IP:端口）");IOSControls.Input(pairingAddress);wireless.Body.Children.Add(pairingAddress);
        Text(wireless.Body,"六位配对码");IOSControls.Input(pairingCode);wireless.Body.Children.Add(pairingCode);
        pair=Action(wireless.Body,"配对电脑",Pair);
        Text(wireless.Body,"连接地址（无线调试首页）");IOSControls.Input(connectionAddress);wireless.Body.Children.Add(connectionAddress);
        connect=Action(wireless.Body,"连接 Wi-Fi 设备",Connect); disconnect=Action(wireless.Body,"断开 Wi-Fi 设备",Disconnect);
        Text(wireless.Body,"配对端口与连接端口通常不同，以手机显示为准。");
        var gestures=Desk.Card("鼠标与手势");cards.Children.Add(gestures.Border);
        Text(gestures.Body,"左键轻点确认，按住拖动\n滚轮滚动 · 键盘输入\n右键返回 · 中键主页\nAlt+S 多任务\nCtrl+左键拖动：双指缩放 / 旋转");
        var diagnostic=Desk.Card("连接详情");cards.Children.Add(diagnostic.Border);
        log.FontSize=11;IOSControls.Input(log);diagnostic.Body.Children.Add(new Expander{Header="查看连接日志",Content=log});
        Text(diagnostic.Body,"部分手机需要 USB 调试（安全设置）。切换平台会停止安卓投屏，保留配对。");
        var screen=new Grid { Margin=new Thickness(24,12,18,12) };Grid.SetColumn(screen,1);root.Children.Add(screen);
        screen.RowDefinitions.Add(new RowDefinition{Height=new GridLength(48)});screen.RowDefinitions.Add(new RowDefinition());screen.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        var title=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};title.Children.Add(screenName);title.Children.Add(new Border{Background=new SolidColorBrush(Color.FromRgb(230,247,237)),CornerRadius=new CornerRadius(12),Padding=new Thickness(10,4,10,4),Margin=new Thickness(12,8,0,8),Child=videoState});screen.Children.Add(title);
        viewport=new Grid();Grid.SetRow(viewport,1);screen.Children.Add(viewport);
        var display=new Grid();display.Children.Add(new Border{Background=new System.Windows.Media.LinearGradientBrush(Color.FromRgb(29,36,57),Color.FromRgb(70,83,111),65),CornerRadius=new CornerRadius(28),Child=new StackPanel{VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center,Children={new TextBlock{Text="你好，Android",FontSize=23,Foreground=Brushes.White,HorizontalAlignment=HorizontalAlignment.Center},new TextBlock{Text="连接设备，开始投屏",FontSize=12,Foreground=Brushes.LightGray,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,14,0,0)}}}});display.Children.Add(videoHost);
        viewport.Children.Add(new Border{Background=new SolidColorBrush(Color.FromRgb(21,21,22)),BorderBrush=new SolidColorBrush(Color.FromRgb(119,123,131)),BorderThickness=new Thickness(2),Padding=new Thickness(6),CornerRadius=new CornerRadius(36),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Child=display});
        videoHost.Width=300;videoHost.Height=660;viewport.SizeChanged+=(_,_)=>FitPhone();videoHost.AspectChanged+=FitPhone;
        var hint=Desk.Label("左键点击与拖动 · 滚轮滚动 · 键盘输入",12,"#8E8E93");hint.HorizontalAlignment=HorizontalAlignment.Center;hint.Margin=new Thickness(0,10,0,4);Grid.SetRow(hint,2);screen.Children.Add(hint);
        timer.Tick += (_, _) => Update(); timer.Start();
        Loaded += async (_, _) => { if (refreshOnLoad && Installed) await Execute(Refresh); Update(); };

        Update();
    }

    public void Dispose() { if (closed) return; closed = true; timer.Stop(); lifetime.Cancel(); StopOwnedMirror(); lifetime.Dispose(); }

    private static void Heading(Panel panel, string value, int size) => panel.Children.Add(new TextBlock { Text = value, FontSize = size, FontWeight = FontWeights.SemiBold, Margin = new(0, 16, 0, 10) });
    private static void Text(Panel panel, string value) => panel.Children.Add(new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.DimGray, Margin = new(0, 8, 0, 8) });
    private Button Action(Panel panel, string title, Func<Task> task, bool primary=false)
    {
        var button=Desk.IOSButton(title,async(_,_)=>await Execute(task),primary);
        actions.Add(button);panel.Children.Add(button);return button;
    }
    private void Note(string message)
    {
        if (closed) return;
        state.Text = message;
        recent.Enqueue($"{DateTime.Now:HH:mm:ss} {message}");
        while (recent.Count > 30) recent.Dequeue(); // Bounded memory log, not a feature visibility threshold.
        log.Text = string.Join("\n", recent); log.ScrollToEnd();
    }
    private async Task Execute(Func<Task> action)
    {
        if (busy || closed) return;
        busy = true; Update();
        try { await action(); }
        catch (OperationCanceledException) { Note("操作取消或超时，请检查手机授权和连接。"); }
        catch (Exception ex) { Note("操作失败：" + ex.Message); }
        finally { busy = false; if (!closed) Update(); }
    }
    private void Update()
    {
        foreach (var action in actions) action.IsEnabled = !busy;
        refresh.IsEnabled = pair.IsEnabled = connect.IsEnabled = !busy && Installed;
        disconnect.IsEnabled = !busy && Installed && devices.SelectedItem is AndroidDevice selected && AndroidProtocol.IsWirelessSerial(selected.Serial);
        pairingAddress.IsEnabled = connectionAddress.IsEnabled = pairingCode.IsEnabled = !busy;
        install.IsEnabled = !busy && !Running;
        windowMode.IsEnabled=!busy && Running;
        windowMode.Content=standalone?"嵌入右侧手机画面":"切换独立窗口";
        if(Running && !standalone && !attachFailed)
        {
            try { videoHost.TryAttach(mirror!.Id); }
            catch(Exception ex) { attachFailed=true;standalone=true;Note("嵌入失败，保留独立窗口："+ex.Message); }
        }
        if(videoHost.Video!=0 && !embeddedAnnounced) { embeddedAnnounced=true;Note("安卓投屏已连接，可在右侧手机画面操作。"); }
        videoState.Text=videoHost.Video!=0?"已连接":Running?"正在启动":"等待连接";
        start.IsEnabled = !busy && Installed && !Running && devices.SelectedItem is AndroidDevice { Ready: true };
        stop.IsEnabled = !busy && Running;
        start.Content = Running ? "安卓投屏已启动" : "启动投屏与控制";
        devices.IsEnabled = !busy && !Running;
        if (mirror != null && !Running)
        {
            videoHost.Detach();
            int exitCode = mirror.ExitCode; mirror.Dispose(); mirror = null; sessionSerial = null;
            Note($"安卓投屏已结束（退出码 {exitCode}）。需要时可再次启动。");
        }
    }
    private async Task<string> Run(string executable, IEnumerable<string> args, string? input = null, int timeoutSeconds = 20)
    {
        var info = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = input != null, WorkingDirectory = Installed ? runtime : AppContext.BaseDirectory };
        foreach (string arg in args) info.ArgumentList.Add(arg);
        using var process = Process.Start(info) ?? throw new IOException("无法启动安卓组件。");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token); timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            if (input != null) { await process.StandardInput.WriteLineAsync(input); process.StandardInput.Close(); }
            await process.WaitForExitAsync(timeout.Token);
            string output = (await stdout) + "\n" + (await stderr);
            if (input != null) output = output.Replace(input, "[已隐藏]");
            if (process.ExitCode != 0) throw new IOException(output.Trim());
            return output.Trim();
        }
        catch { try { if (!process.HasExited) process.Kill(); } catch { } throw; }
    }
    private void RequireRuntime() { if (!Installed) throw new IOException("请先点“准备 / 校验安卓组件”。"); }
    private async Task Install()
    {
        if (Running) return;
        string script = Path.Combine(AppContext.BaseDirectory, "下载安卓组件.ps1");
        if (!File.Exists(script)) throw new IOException("缺少下载安卓组件.ps1，请恢复完整交付包。");
        Note("正在下载并校验官方 scrcpy 组件，请稍候…");
        string powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
        await Run(powershell, new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", script }, timeoutSeconds: 300);
        RequireRuntime(); Note("安卓组件就绪。未安装驱动、未改变手机调试设置。"); await Refresh();
    }
    private async Task Refresh()
    {
        RequireRuntime();
        string? selected = (devices.SelectedItem as AndroidDevice)?.Serial;
        var list = AndroidProtocol.ParseDevices(await Run(Adb, new[] { "devices", "-l" }));
        devices.ItemsSource = list;
        devices.SelectedItem = list.FirstOrDefault(d => d.Serial == (sessionSerial ?? selected));
        if (devices.SelectedItem == null && list.Count == 1) devices.SelectedIndex = 0;
        Note(list.Count == 0 ? "未发现安卓设备。请插线并开启 USB 调试，或配对并连接无线调试。" : $"发现 {list.Count} 台设备；只有“已授权”设备可启动。");
    }
    private Task StartMirror()
    {
        RequireRuntime(); if (Running) return Task.CompletedTask;
        if (devices.SelectedItem is not AndroidDevice { Ready: true } device) throw new IOException("请先选择已授权设备。");
        var info = new ProcessStartInfo(Path.Combine(runtime, "scrcpy.exe")) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = runtime, RedirectStandardError = true, RedirectStandardOutput = true };
        foreach (string arg in new[] { "--serial", device.Serial, "--window-title", "iPhoneDesk · Android", "--no-audio", "--no-clipboard-autosync" }) info.ArgumentList.Add(arg);
        standalone=false;attachFailed=false;embeddedAnnounced=false;screenName.Text=device.Model;
        mirror = Process.Start(info) ?? throw new IOException("安卓投屏启动失败。"); sessionSerial = device.Serial;
        mirror.OutputDataReceived += Output; mirror.ErrorDataReceived += Output;
        mirror.BeginOutputReadLine(); mirror.BeginErrorReadLine();
        Note("安卓投屏正在启动；画面会显示在右侧手机区域。重复启动已禁用。");
        return Task.CompletedTask;
    }
    private void Output(object sender, DataReceivedEventArgs e)
    {
        if(closed || e.Data==null)return;
        string message=e.Data;
        var size=Regex.Match(message,@"Texture:\s*(\d+)x(\d+)");
        try { Dispatcher.BeginInvoke(()=> {
            if(closed)return;
            if(size.Success && int.TryParse(size.Groups[1].Value,out int w) && int.TryParse(size.Groups[2].Value,out int h))videoHost.SetVideoSize(w,h);
            if(message.StartsWith("ERROR:") || message.StartsWith("WARN:"))Note(message);
        }); } catch(InvalidOperationException) { }
    }
    private void StopOwnedMirror()
    {
        videoHost.Detach();
        if (mirror == null) return;
        try { if (!mirror.HasExited) { if (!mirror.CloseMainWindow() || !mirror.WaitForExit(1000)) mirror.Kill(); } } catch { }
        mirror.Dispose(); mirror = null; sessionSerial = null;
    }
    private Task StopMirror() { StopOwnedMirror(); Note("安卓投屏已停止，配对保留。"); return Task.CompletedTask; }
    private async Task Pair()
    {
        RequireRuntime(); string address = AndroidProtocol.Endpoint(pairingAddress.Text);
        string code = pairingCode.Password; pairingCode.Clear();
        if (code.Length != 6 || !code.All(char.IsAsciiDigit)) throw new ArgumentException("请输入手机当前显示的六位配对码。");
        string result = await Run(Adb, new[] { "pair", address }, code, 60);
        if (!result.Contains("Successfully paired", StringComparison.OrdinalIgnoreCase)) throw new IOException(result);
        Note("配对成功。请用无线调试首页的连接端口继续连接。");
    }
    private async Task Connect()
    {
        RequireRuntime(); string result = await Run(Adb, new[] { "connect", AndroidProtocol.Endpoint(connectionAddress.Text) });
        if (!result.Contains("connected to", StringComparison.OrdinalIgnoreCase)) throw new IOException(result);
        Note(result); await Refresh();
    }
    private async Task Disconnect()
    {
        RequireRuntime(); if (devices.SelectedItem is not AndroidDevice device) throw new IOException("请先选择 Wi-Fi 设备。");
        if (!AndroidProtocol.IsWirelessSerial(device.Serial)) throw new IOException("所选设备不是已识别的 Wi-Fi 设备，未断开。USB 请拔线。");
        string address = device.Serial;
        if (Running && sessionSerial == device.Serial) await StopMirror();
        Note(await Run(Adb, new[] { "disconnect", address })); await Refresh();
    }
}

public sealed class AndroidWindow : Window
{
    public AndroidWindow(bool refreshOnLoad = true)
    {
        Title = "iPhoneDesk · 安卓投屏与控制"; Width = 1120; Height = 840; MinWidth = 920; MinHeight = 680;
        var panel = new AndroidPanel(refreshOnLoad); Content = panel;
        Closed += (_, _) => panel.Dispose();
    }
}

public sealed partial class Desk
{
    private bool applePageActive;
    private AndroidPanel? androidPanel;
    private void AddPlatformNavigation(Grid page, Grid appleBody, Panel nav)
    {
        appleBody.Visibility = Visibility.Collapsed;
        var selection = new ComboBox { Width = 175, Margin = new Thickness(18, 0, 0, 0), FontSize = 13 };
        foreach (string name in new[] { "请选择手机平台", "苹果手机 · iPhone", "安卓手机 · Android" }) selection.Items.Add(name);
        selection.Style=IOSControls.ComboStyle();
        selection.SelectedIndex = 0; nav.Children.Add(selection);
        var back = IOSButton("← 返回选择", (_, _) => selection.SelectedIndex = 0, false);
        back.Margin = new Thickness(12, 0, 0, 0); back.Padding = new Thickness(12, 6, 12, 6);
        back.Visibility = Visibility.Collapsed;
        System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(selection, true);
        System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(back, true);
        nav.Children.Add(back);
        var landing = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Width = 350 };
        landing.Children.Add(Label("选择手机平台", 26, "#1C1C1E", true));
        landing.Children.Add(Label("选择后显示对应的连接与控制菜单", 13, "#636366"));
        landing.Children.Add(IOSButton("苹果手机 · Wi-Fi AirPlay", (_, _) => selection.SelectedIndex = 1, true));
        landing.Children.Add(IOSButton("安卓手机 · USB / Wi-Fi", (_, _) => selection.SelectedIndex = 2, false));
        Grid.SetRow(landing, 1); page.Children.Add(landing);
        selection.SelectionChanged += async (_, _) =>
        {
            selection.IsEnabled = false; back.IsEnabled = false;
            try
            {
                await ReleaseCapture();
                androidPanel?.Dispose();
                if (androidPanel != null) { page.Children.Remove(androidPanel); androidPanel = null; }
                back.Visibility = selection.SelectedIndex == 0 ? Visibility.Collapsed : Visibility.Visible;
                applePageActive = selection.SelectedIndex == 1;
                appleBody.Visibility = applePageActive ? Visibility.Visible : Visibility.Collapsed;
                landing.Visibility = selection.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
                if (selection.SelectedIndex == 2)
                {
                    androidPanel = new AndroidPanel(); Grid.SetRow(androidPanel, 1); page.Children.Add(androidPanel);
                }
                if (applePageActive)
                {
                    EnableHover();
                    var args = Environment.GetCommandLineArgs();
                    if (args.Contains("--airplay") || !args.Skip(1).Any()) OpenReceiver(this, new RoutedEventArgs());
                    if (args.Contains("--resume-bluetooth") && !hid.Advertising) StartHid(this, new RoutedEventArgs());
                }
            }
            catch (Exception ex) { SetStatus("切换平台失败：" + ex.Message); }
            finally { selection.IsEnabled = true; back.IsEnabled = true; }
        };
        Closed += (_, _) => androidPanel?.Dispose();
        if (Environment.GetCommandLineArgs().Contains("--platform-ui-smoke"))
        {
            Loaded += async (_, _) =>
            {
                string outcome = "PASS: platform landing, Android-only panel, Apple-only panel, return to landing; no GATT start or phone input.";
                try
                {
                    if (appleBody.Visibility != Visibility.Collapsed || androidPanel != null) throw new Exception("initial landing");
                    selection.SelectedIndex = 2; await Task.Delay(250);
                    if (androidPanel == null || appleBody.Visibility != Visibility.Collapsed || applePageActive) throw new Exception("Android hierarchy");
                    selection.SelectedIndex = 1; await Task.Delay(250);
                    if (androidPanel != null || appleBody.Visibility != Visibility.Visible || !applePageActive) throw new Exception("Apple hierarchy");
                    if (back.Visibility != Visibility.Visible || !back.IsEnabled) throw new Exception("return button");
                    back.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent)); await Task.Delay(250);
                    if (appleBody.Visibility != Visibility.Collapsed || androidPanel != null || applePageActive || back.Visibility != Visibility.Collapsed) throw new Exception("return landing");
                }
                catch (Exception ex) { outcome = "FAIL: " + ex.Message; Environment.ExitCode = 1; }
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "platform-ui-smoke.txt"), outcome); Close();
            };
        }
    }
}
