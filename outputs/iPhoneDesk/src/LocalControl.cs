using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace iPhoneDesk;

public sealed partial class Desk
{
    private TcpListener? localServer;
    private CancellationTokenSource? remoteCancel;
    private string localToken="";
    private readonly SemaphoreSlim remoteLock=new(1);
    private void StartLocalControl()
    {
        localToken=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        localServer=new TcpListener(IPAddress.Loopback,0);localServer.Start();
        int port=((IPEndPoint)localServer.LocalEndpoint).Port;
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"control-interface.json"),JsonSerializer.Serialize(new{url=$"http://127.0.0.1:{port}",token=localToken}));
        _=ServeLocal();
    }
    private async Task ServeLocal()
    {
        while(localServer!=null)
        {
            try{var client=await localServer.AcceptTcpClientAsync();_=HandleLocal(client);}
            catch(SocketException){break;}catch(ObjectDisposedException){break;}
        }
    }
    private async Task HandleLocal(TcpClient client)
    {
        using(client)
        {
            client.ReceiveTimeout=5000;client.SendTimeout=5000;
            using var stream=client.GetStream();
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                var bytes=new List<byte>();var one=new byte[1];
                while(bytes.Count<8192)
                {
                    if(await stream.ReadAsync(one,timeout.Token)==0)return;
                    bytes.Add(one[0]);int n=bytes.Count;
                    if(n>=4 && bytes[n-4]==13 && bytes[n-3]==10 && bytes[n-2]==13 && bytes[n-1]==10)break;
                }
                var lines=Encoding.ASCII.GetString(bytes.ToArray()).Split("\r\n");
                string[] first=lines[0].Split(' ');
                var headers=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
                foreach(string line in lines.Skip(1)){int p=line.IndexOf(':');if(p>0)headers[line[..p]]=line[(p+1)..].Trim();}
                if(!headers.TryGetValue("Authorization",out var auth) || auth!="Bearer "+localToken)
                {await Reply(stream,403,"application/json",Encoding.UTF8.GetBytes("{\"error\":\"unauthorized\"}"));return;}
                if(headers.ContainsKey("Origin")){await Reply(stream,403,"application/json",Encoding.UTF8.GetBytes("{}"));return;}
                int length=headers.TryGetValue("Content-Length",out var raw)?int.Parse(raw):0;
                if(length<0 || length>8192)throw new InvalidOperationException("Request too large");
                byte[] body=new byte[length];await stream.ReadExactlyAsync(body,timeout.Token);
                if(first[0]=="GET" && first[1]=="/status")
                {
                    string result=await Dispatcher.InvokeAsync(()=>JsonSerializer.Serialize(new{mirrorConnected=MirrorConnected,transport="airplay",videoLayout=videoHost.LastResize,mouseConnected=hid.AbsoluteConnected,positionCount=hid.PositionCount,buttonCount=hid.ButtonCount,control=new {advertisement=hid.AdvertisementState,relativeClients=hid.RelativeClients,absoluteClients=hid.AbsoluteClients,reconnecting=hidStarting},diagnostics=new {session=diagnostics.Session,file=diagnostics.FilePath,dropped=diagnostics.Dropped,error=diagnostics.WriteError}}));
                    await Reply(stream,200,"application/json",Encoding.UTF8.GetBytes(result));
                }
                else if(first[0]=="GET" && first[1]=="/screenshot")
                {
                    byte[] png=await Dispatcher.InvokeAsync(CapturePhone);
                    await Reply(stream,200,"image/png",png);
                }
                else if(first[0]=="POST" && first[1]=="/control/reconnect")
                {
                    if(length!=0)throw new InvalidOperationException("重连接口不接受正文。");
                    await remoteLock.WaitAsync();
                    try{await (await Dispatcher.InvokeAsync(ReconnectControl));}
                    finally{remoteLock.Release();}
                    await Reply(stream,200,"application/json",Encoding.UTF8.GetBytes("{\"requested\":true,\"executionVerified\":false}"));
                }
                else if(first[0]=="POST" && first[1]=="/command")
                {
                    using var json=JsonDocument.Parse(body);var command=json.RootElement.Clone();
                    await remoteLock.WaitAsync();
                    try{await (await Dispatcher.InvokeAsync(()=>RunRemote(command)));}
                    finally{remoteLock.Release();}
                    await Reply(stream,200,"application/json",Encoding.UTF8.GetBytes("{\"sent\":true,\"executionVerified\":false}"));
                }
                else await Reply(stream,404,"application/json",Encoding.UTF8.GetBytes("{}"));
            }
            catch(Exception ex)
            {try{await Reply(stream,409,"application/json",Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{error=ex.Message})));}catch{}}
        }
    }
    private static async Task Reply(Stream stream,int code,string type,byte[] body)
    {
        byte[] head=Encoding.ASCII.GetBytes($"HTTP/1.1 {code} {(code==200?"OK":"Error")}\r\nContent-Type: {type}\r\nContent-Length: {body.Length}\r\nConnection: close\r\nCache-Control: no-store\r\n\r\n");
        await stream.WriteAsync(head);await stream.WriteAsync(body);
    }
    private async Task RunRemote(JsonElement command)
    {
        if(!hid.AbsoluteConnected || !MirrorConnected)throw new InvalidOperationException("请先连接投屏和蓝牙鼠标。");
        string action=command.GetProperty("action").GetString()??"";
        if(action is not ("tap" or "swipe" or "release"))throw new InvalidOperationException("Unsupported action");
        int Coord(string key){double v=command.GetProperty(key).GetDouble();if(!double.IsFinite(v)||v<0||v>1)throw new InvalidOperationException("Coordinates must be between 0 and 1");return (int)Math.Round(v*32767);}
        int x=action=="release"?0:Coord("x"),y=action=="release"?0:Coord("y");
        int endX=action=="swipe"?Coord("endX"):x,endY=action=="swipe"?Coord("endY"):y;
        int duration=command.TryGetProperty("durationMs",out var d)?d.GetInt32():350;
        if(duration<80 || duration>2000)throw new InvalidOperationException("Duration must be 80–2000 ms");
        byte? testButton=null;
        if(command.TryGetProperty("buttonMask",out var mask))
        {
            if(action!="swipe" || !mask.TryGetByte(out byte value) || value is not (1 or 2 or 4))throw new InvalidOperationException("buttonMask仅支持swipe及单按钮位1、2、4。");
            testButton=value;
        }
        bool auto=automaticMode.IsChecked==true;
        await ReleaseCapture();await reportLock.WaitAsync();
        using var cancel=new CancellationTokenSource();remoteCancel=cancel;
        long remoteGesture=++nextGesture;sendingGesture=remoteGesture;
        var remoteWatch=System.Diagnostics.Stopwatch.StartNew();
        DiagnosticEvent("api-start",remoteGesture,new {action,x,y,endX,endY,duration,testButton});
        try
        {
            if(action=="release"){await hid.Send(pressed:0);return;}
            byte gestureButton=action=="swipe"?(testButton ?? leftGesture?.ButtonForSwipe(y,endX-x,endY-y) ?? throw new InvalidOperationException("鼠标动作配置不支持滑动，请检查原配置。")):(byte)1;
            await hid.SendAt(x,y,pressed:0);
            await Task.Delay(60,cancel.Token); // Two 30ms BLE steps: settle the pointer before pressing.
            await hid.SendAt(x,y,pressed:gestureButton);
            if(action=="tap")await Task.Delay(90,cancel.Token);
            else
            {
                await Task.Delay(30,cancel.Token);
                int steps=Math.Max(2,duration/30);
                for(int i=1;i<=steps;i++)
                {
                    cancel.Token.ThrowIfCancellationRequested();
                    await hid.SendAt(x+(endX-x)*i/steps,y+(endY-y)*i/steps,pressed:gestureButton);
                    await Task.Delay(duration/steps,cancel.Token);
                }
            }
            if(action=="swipe" && testButton==null && leftGesture!=null)
                await Task.Delay(leftGesture.ReleaseHold(y,endY),cancel.Token);
            SetStatus("本机接口已发送："+action+"；手机执行效果需查看画面。");
        }
        catch(Exception ex){DiagnosticEvent("api-error",remoteGesture,new {type=ex.GetType().Name,error=ex.Message});throw;}
        finally
        {
            try{if(hid.Connected)await hid.Send(pressed:0);}finally{DiagnosticEvent("api-end",remoteGesture,new {elapsedMs=remoteWatch.Elapsed.TotalMilliseconds,cancelled=cancel.IsCancellationRequested,connected=hid.AbsoluteConnected});sendingGesture=0;remoteCancel=null;reportLock.Release();if(auto && !cancel.IsCancellationRequested){EnableHover();}}
        }
    }
    private byte[] CapturePhone()
    {
        nint hwnd=videoHost.Video;
        if(hwnd==0 || !LocalGetClientRect(hwnd,out var r))throw new InvalidOperationException("没有实时投屏画面。");
        int w=r.Right,h=r.Bottom;nint src=GetDC(hwnd),dc=CreateCompatibleDC(src),bitmap=CreateCompatibleBitmap(src,w,h),old=SelectObject(dc,bitmap);
        try
        {
            if(!BitBlt(dc,0,0,w,h,src,0,0,0x40CC0020))throw new InvalidOperationException("截图失败。");
            var source=Imaging.CreateBitmapSourceFromHBitmap(bitmap,0,Int32Rect.Empty,BitmapSizeOptions.FromEmptyOptions());
            var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(source));using var output=new MemoryStream();png.Save(output);return output.ToArray();
        }
        finally{SelectObject(dc,old);DeleteLocalObject(bitmap);DeleteDC(dc);ReleaseDC(hwnd,src);}
    }
    [StructLayout(LayoutKind.Sequential)]private struct LocalRect{public int Left,Top,Right,Bottom;}
    [DllImport("user32.dll",EntryPoint="GetClientRect")]private static extern bool LocalGetClientRect(nint hwnd,out LocalRect rect);
    [DllImport("user32.dll")]private static extern nint GetDC(nint hwnd);
    [DllImport("user32.dll")]private static extern int ReleaseDC(nint hwnd,nint dc);
    [DllImport("gdi32.dll")]private static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32.dll")]private static extern nint CreateCompatibleBitmap(nint dc,int w,int h);
    [DllImport("gdi32.dll")]private static extern nint SelectObject(nint dc,nint obj);
    [DllImport("gdi32.dll",EntryPoint="DeleteObject")]private static extern bool DeleteLocalObject(nint obj);
    [DllImport("gdi32.dll")]private static extern bool DeleteDC(nint dc);
    [DllImport("gdi32.dll")]private static extern bool BitBlt(nint target,int x,int y,int w,int h,nint source,int sx,int sy,uint operation);
}
