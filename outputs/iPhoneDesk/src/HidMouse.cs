using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace iPhoneDesk;

// Experimental HID over GATT mouse. No private iOS API or Windows driver.
public sealed class HidMouse : IDisposable
{
    private GattServiceProvider? provider;
    private GattLocalCharacteristic? input;
    private GattLocalCharacteristic? absoluteInput;
    private byte buttons;
    private int absoluteX=-1,absoluteY=-1;
    private long lastPress;
    public long PositionCount {get;private set;}
    public long ButtonCount {get;private set;}
    public double LastNotifyMs {get;private set;}
    public event Action<string>? Status;
    internal event Action<string,object>? Diagnostic;
    public string AdvertisementState => provider?.AdvertisementStatus.ToString()??"NotCreated";
    public int RelativeClients => input?.SubscribedClients.Count??0;
    public int AbsoluteClients => absoluteInput?.SubscribedClients.Count??0;
    public bool Advertising => provider?.AdvertisementStatus==GattServiceProviderAdvertisementStatus.Started;
    public bool Connected => input?.SubscribedClients.Count > 0;
    public bool AbsoluteConnected => Connected && absoluteInput?.SubscribedClients.Count > 0;
    private static Guid Uuid(int n) => Guid.Parse($"0000{n:x4}-0000-1000-8000-00805f9b34fb");
    // Report ID 3: buttons, signed relative X/Y, wheel.
    internal static readonly byte[] RelativeMap = [
        0x05,0x01,0x09,0x02,0xA1,0x01,0x85,0x03,0x09,0x01,0xA1,0x00,
        0x05,0x09,0x19,0x01,0x29,0x03,0x15,0x00,0x25,0x01,0x95,0x03,
        0x75,0x01,0x81,0x02,0x95,0x01,0x75,0x05,0x81,0x03,0x05,0x01,
        0x09,0x30,0x09,0x31,0x09,0x38,0x15,0x81,0x25,0x7F,0x75,0x08,
        0x95,0x03,0x81,0x06,0xC0,0xC0];
    // Separate absolute pointer and relative-button collections: iOS can map the
    // absolute X/Y directly, while mouse buttons are supplied by report 3.
    // Interoperability approach: https://github.com/xhoantran/iphone-use (MIT).
    internal static readonly byte[] AbsoluteMap = [
        0x05,0x01,0x09,0x02,0xA1,0x01,0x85,0x02,0x09,0x01,0xA1,0x00,
        0x05,0x09,0x19,0x01,0x29,0x03,0x15,0x00,0x25,0x01,0x95,0x03,
        0x75,0x01,0x81,0x02,0x95,0x01,0x75,0x05,0x81,0x03,
        0x05,0x01,0x09,0x30,0x09,0x31,0x16,0x00,0x00,0x26,0xFF,0x7F,
        0x75,0x10,0x95,0x02,0x81,0x02,0xC0,0xC0];
    internal static readonly byte[] ReportMap = [..AbsoluteMap,..RelativeMap];
    public static byte[] AbsoluteReport(int x,int y,byte heldButtons=0)
    {
        x=Math.Clamp(x,0,32767);y=Math.Clamp(y,0,32767);
        return [(byte)(heldButtons&7),(byte)(x&255),(byte)(x>>8),(byte)(y&255),(byte)(y>>8)];
    }

    public static byte[] Report(byte buttons, int x, int y, int wheel) =>
        [buttons, unchecked((byte)(sbyte)Math.Clamp(x,-127,127)),
         unchecked((byte)(sbyte)Math.Clamp(y,-127,127)),
         unchecked((byte)(sbyte)Math.Clamp(wheel,-127,127))];

    public async Task Start()
    {
        if(provider!=null && input!=null && absoluteInput!=null)
        {
            if(provider.AdvertisementStatus!=GattServiceProviderAdvertisementStatus.Started)
                provider.StartAdvertising(new GattServiceProviderAdvertisingParameters { IsConnectable=true, IsDiscoverable=true });
            Status?.Invoke("鼠标服务已存在，保留当前报告与连接，不重复创建。请在手机辅助触控的蓝牙设备中连接电脑。");
            ConnectionStatus();
            return;
        }
        Dispose();
        var adapter = await BluetoothAdapter.GetDefaultAsync();
        if (adapter == null) throw new InvalidOperationException("没有检测到蓝牙适配器。");
        if (!adapter.IsPeripheralRoleSupported)
            throw new InvalidOperationException("此蓝牙适配器不支持 BLE 外设模式，无法用本原型控制 iPhone。投屏仍可使用。");
        var result = await GattServiceProvider.CreateAsync(Uuid(0x1812));
        if (result.Error != BluetoothError.Success)
            throw new InvalidOperationException($"Windows 无法创建 HID 服务：{result.Error}。");
        provider = result.ServiceProvider;
        try
        {
            await Constant(0x2A4A, [0x11,0x01,0x00,0x02]); // HID v1.11, normally connectable
            await Constant(0x2A4B, ReportMap);
            var control = await Characteristic(0x2A4C,
                GattCharacteristicProperties.WriteWithoutResponse);
            control.WriteRequested += async (s,e) =>
            {
                var d=e.GetDeferral();
                try { var r=await e.GetRequestAsync(); if(r?.Option==GattWriteOption.WriteWithResponse) r.Respond(); }
                finally { d.Complete(); }
            };
            input = await Characteristic(0x2A4D,
                GattCharacteristicProperties.Read | GattCharacteristicProperties.Notify);
            var descriptor=await input.CreateDescriptorAsync(Uuid(0x2908),
                new GattLocalDescriptorParameters { StaticValue = new byte[]{3,1}.AsBuffer(),
                    ReadProtectionLevel=GattProtectionLevel.Plain });
            if(descriptor.Error!=BluetoothError.Success)
                throw new InvalidOperationException($"报告描述符创建失败：{descriptor.Error}");
            input.ReadRequested += async (s,e) =>
            {
                var d=e.GetDeferral();
                try { var r=await e.GetRequestAsync(); r?.RespondWithValue(Report(buttons,0,0,0).AsBuffer()); }
                finally { d.Complete(); }
            };
            absoluteInput = await Characteristic(0x2A4D,
                GattCharacteristicProperties.Read | GattCharacteristicProperties.Notify);
            var absoluteDescriptor=await absoluteInput.CreateDescriptorAsync(Uuid(0x2908),
                new GattLocalDescriptorParameters { StaticValue=new byte[]{2,1}.AsBuffer(),
                    ReadProtectionLevel=GattProtectionLevel.Plain });
            if(absoluteDescriptor.Error!=BluetoothError.Success)
                throw new InvalidOperationException($"绝对坐标描述符创建失败：{absoluteDescriptor.Error}");
            absoluteInput.ReadRequested += async (s,e)=>
            {
                var d=e.GetDeferral();
                try{var r=await e.GetRequestAsync();r?.RespondWithValue(AbsoluteReport(Math.Max(0,absoluteX),Math.Max(0,absoluteY),buttons).AsBuffer());}
                finally{d.Complete();}
            };
            input.SubscribedClientsChanged += (s,e)=>ConnectionStatus();
            absoluteInput.SubscribedClientsChanged += (s,e)=>ConnectionStatus();
            provider.AdvertisementStatusChanged += (s,e) => Status?.Invoke(
                $"蓝牙广播：{e.Status} / {e.Error}。未订阅前不能控制。");
            provider.StartAdvertising(new GattServiceProviderAdvertisingParameters
            { IsConnectable=true, IsDiscoverable=true });
        }
        catch { Dispose(); throw; }
    }

    public async Task ReconnectAdvertising()
    {
        if(AbsoluteConnected)return;
        if(provider==null || input==null || absoluteInput==null){await Start();return;}
        var current=provider;
        Diagnostic?.Invoke("reconnect-start",new {advertisement=AdvertisementState,relative=RelativeClients,absolute=AbsoluteClients});
        current.StopAdvertising();
        Diagnostic?.Invoke("reconnect-stop-requested",new {advertisement=AdvertisementState});
        if(provider!=current)throw new InvalidOperationException("控制服务已改变，重连已取消。");
        if(AbsoluteConnected)return;
        current.StartAdvertising(new GattServiceProviderAdvertisingParameters {IsConnectable=true,IsDiscoverable=true});
        Diagnostic?.Invoke("reconnect-published",new {advertisement=AdvertisementState,relative=RelativeClients,absolute=AbsoluteClients});
        Status?.Invoke("已重新发布原控制服务，等待手机订阅定位与点击报告；没有新建服务或删除配对。");
    }

    private async Task<GattLocalCharacteristic> Characteristic(int id, GattCharacteristicProperties flags,
        byte[]? value=null)
    {
        var p=new GattLocalCharacteristicParameters
        { CharacteristicProperties=flags, ReadProtectionLevel=GattProtectionLevel.EncryptionRequired,
          WriteProtectionLevel=GattProtectionLevel.EncryptionRequired };
        if(value!=null) p.StaticValue=value.AsBuffer();
        var r=await provider!.Service.CreateCharacteristicAsync(Uuid(id),p);
        if(r.Error!=BluetoothError.Success) throw new InvalidOperationException($"GATT {id:x4}：{r.Error}");
        return r.Characteristic;
    }
    private Task<GattLocalCharacteristic> Constant(int id,byte[] value) =>
        Characteristic(id,GattCharacteristicProperties.Read,value);
    private void ConnectionStatus() => Status?.Invoke(AbsoluteConnected
        ? "绝对坐标和点击报告均已订阅。请验证电脑箭头与手机圆点是否对齐。"
        : Connected ? "仅相对鼠标已连接，绝对坐标尚未订阅。请在 iPhone 忽略此电脑并重新配对；同步模式暂不发送点击。"
        : "等待 iPhone 连接；控制暂不可用。");

    internal static byte MapDesktopButtons(byte value)=>value;

    public async Task Send(int x=0,int y=0,int wheel=0,byte? pressed=null)
    {
        if(!Connected) throw new InvalidOperationException("尚未有客户端订阅鼠标报告。");
        byte before=buttons;
        double? heldMs=before!=0?System.Diagnostics.Stopwatch.GetElapsedTime(lastPress).TotalMilliseconds:null;
        if(pressed==0 && before!=0)
        {
            double held=System.Diagnostics.Stopwatch.GetElapsedTime(lastPress).TotalMilliseconds;
            if(held<80)await Task.Delay((int)Math.Ceiling(80-held));
        }
        if(pressed.HasValue) buttons=MapDesktopButtons(pressed.Value);
        var watch=System.Diagnostics.Stopwatch.StartNew();
        var results=await input!.NotifyValueAsync(Report(buttons,x,y,wheel).AsBuffer());
        LastNotifyMs=watch.Elapsed.TotalMilliseconds;
        Diagnostic?.Invoke("relative-report",new {before,buttons,x,y,wheel,notifyMs=LastNotifyMs,
            clients=results.Count,statuses=results.Select(r=>r.Status.ToString()).ToArray(),heldMs});
        if(results.Count==0 || results.Any(r=>r.Status!=GattCommunicationStatus.Success))
            throw new InvalidOperationException("鼠标报告发送失败，请重新连接蓝牙。");
        // Clear both collections on release, even when the pointer does not move.
        if(pressed==0 && before!=0 && AbsoluteConnected && absoluteX>=0 && absoluteY>=0)
        {
            var released=await absoluteInput!.NotifyValueAsync(AbsoluteReport(absoluteX,absoluteY,0).AsBuffer());
            Diagnostic?.Invoke("absolute-release",new {x=absoluteX,y=absoluteY,buttons=0,
                clients=released.Count,statuses=released.Select(r=>r.Status.ToString()).ToArray()});
            if(released.Count==0 || released.Any(r=>r.Status!=GattCommunicationStatus.Success))throw new InvalidOperationException("绝对定位报告释放失败。");
        }
        if(pressed.HasValue && before!=buttons)
        {
            ButtonCount++;
            if(buttons!=0)lastPress=System.Diagnostics.Stopwatch.GetTimestamp();
            Status?.Invoke($"按钮报告：{before} → {buttons}，发送 {LastNotifyMs:0.0} ms，客户端 {results.Count}。这不是手机执行确认。");
        }
    }
    public async Task SendAt(int x,int y,int wheel=0,byte? pressed=null,Func<bool>? stillCurrent=null)
    {
        if(!AbsoluteConnected)throw new InvalidOperationException("绝对坐标协议尚未连接，请重新配对手机。");
        x=Math.Clamp(x,0,32767);y=Math.Clamp(y,0,32767);
        bool changed=x!=absoluteX || y!=absoluteY;
        Diagnostic?.Invoke("position-decision",new {x,y,previousX=absoluteX,previousY=absoluteY,
            changed,buttons,pressed,wheel});
        if(changed)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();
            var results=await absoluteInput!.NotifyValueAsync(AbsoluteReport(x,y,buttons).AsBuffer());
            LastNotifyMs=watch.Elapsed.TotalMilliseconds;
            Diagnostic?.Invoke("absolute-report",new {x,y,buttons,notifyMs=LastNotifyMs,
                clients=results.Count,statuses=results.Select(r=>r.Status.ToString()).ToArray()});
            if(results.Count==0 || results.Any(r=>r.Status!=GattCommunicationStatus.Success))
                throw new InvalidOperationException("绝对坐标发送失败。");
            absoluteX=x;absoluteY=y;
            PositionCount++;
            if(PositionCount%120==0)Status?.Invoke($"定位报告 {PositionCount} 次，最近发送 {LastNotifyMs:0.0} ms。这是蓝牙发送耗时，不包含投屏延迟。");
        }
        if(wheel!=0 || (pressed.HasValue && MapDesktopButtons(pressed.Value)!=buttons))
        {
            // A newly positioned click needs a short settling interval; motion
            // itself has no additional delay and queued positions are replaced.
            if(changed && pressed.HasValue && pressed.Value!=0 && MapDesktopButtons(pressed.Value)!=buttons)
            {Diagnostic?.Invoke("position-settle",new {x,y,pressed,delayMs=45});await Task.Delay(45);}
            if(stillCurrent!=null && !stillCurrent()){Diagnostic?.Invoke("button-skipped",new {reason="generation-or-capture-changed",x,y,pressed});return;}
            await Send(wheel:wheel,pressed:pressed);
        }
    }
    public void Dispose()
    {
        provider?.StopAdvertising(); provider=null; input=null;absoluteInput=null; buttons=0;absoluteX=-1;absoluteY=-1;
    }
}
