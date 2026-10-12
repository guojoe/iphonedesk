using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Diagnostics;
using System.Text;

namespace iPhoneDesk;

// Captures physical mouse events only while explicitly enabled. No Bluetooth work
// occurs inside the low-level callback; consumers enqueue and send asynchronously.
public sealed class MouseCapture : IDisposable
{
    public readonly record struct Input(int X,int Y,int Wheel,byte Buttons,bool IsMove,
        bool Absolute=false,int AbsoluteX=0,int AbsoluteY=0,int HoldBeforeReleaseMs=0,long GestureId=0,long ReceivedTicks=0,long Sample=0);
    private readonly HookProc callback;
    private nint hook;
    private byte buttons;
    private readonly HwndSource source;
    private readonly nint window;
    private bool hotkeys;
    private bool manual,hovering,blockedGesture;
    private NativePoint? lastPoint;
    private byte physicalButtons;
    private readonly HashSet<nint> videoWindows=new();
    public bool Active => hook!=0 && (manual || hovering);
    public bool Installed => hook!=0;
    public bool Automatic {get;private set;}
    public bool ClientReady {get;set;}
    public double ScreenAspect {get;set;}=9.0/19.5;
    public int VideoWindowCount => videoWindows.Count;
    public nint EmbeddedVideo {get;set;}
    private int viewportLeft,viewportTop,viewportWidth,viewportHeight;
    internal object DiagnosticViewport=>new {left=viewportLeft,top=viewportTop,width=viewportWidth,
        height=viewportHeight,aspect=ScreenAspect,embedded=EmbeddedVideo!=0};
    public event Action<Input>? Received;
    public event Action<bool>? HoverChanged;
    public event Action? ToggleRequested;
    public event Action? ReleaseRequested;
    public MouseCapture(nint hwnd)
    {
        callback=OnMouse;window=hwnd;source=HwndSource.FromHwnd(hwnd);
        source.AddHook(WindowMessage);
        bool f8=RegisterHotKey(window,81,0x4000,0x77);
        bool f9=RegisterHotKey(window,82,0x4000,0x78);
        if(!f8 || !f9)
        {
            UnregisterHotKey(window,81);UnregisterHotKey(window,82);
            source.RemoveHook(WindowMessage);
            throw new InvalidOperationException("F8/F9 已被其他程序占用，无法启用全局鼠标接管。");
        }
        hotkeys=true;
    }
    public void Start()
    {
        Stop();manual=true;
        // Refuse capture if the release key cannot be registered.
        if(!RegisterHotKey(window,83,0x4000,0x1B))
        {
            manual=false;
            throw new InvalidOperationException("Esc 被占用，暂不接管鼠标。请使用原触控板模式。");
        }
        buttons=0;
        hook=SetWindowsHookEx(14,callback,GetModuleHandle(null),0);
        if(hook==0){UnregisterHotKey(window,83);manual=false;throw new Win32Exception(Marshal.GetLastWin32Error());}
    }
    public void EnableAutomatic()
    {
        Stop();Automatic=true;
        physicalButtons=(byte)(((GetAsyncKeyState(1)&0x8000)!=0?1:0)|((GetAsyncKeyState(2)&0x8000)!=0?2:0)|((GetAsyncKeyState(4)&0x8000)!=0?4:0));
        blockedGesture=physicalButtons!=0;
        hook=SetWindowsHookEx(14,callback,GetModuleHandle(null),0);
        if(hook==0){Automatic=false;throw new Win32Exception(Marshal.GetLastWin32Error());}
    }
    public void LeaveHover()
    {
        if(!hovering)return;
        hovering=false;buttons=0;lastPoint=null;
        if(physicalButtons!=0)blockedGesture=true;
        HoverChanged?.Invoke(false);
    }
    public void RefreshVideoWindows(string receiverPath)
    {
        var ids=new HashSet<uint>();
        foreach(var p in Process.GetProcessesByName("uxplay-windows"))
        {
            using(p){try{if(string.Equals(p.MainModule?.FileName,receiverPath,StringComparison.OrdinalIgnoreCase))ids.Add((uint)p.Id);}catch{}}
        }
        var found=new HashSet<nint>();
        EnumWindows((hwnd,_)=>
        {
            GetWindowThreadProcessId(hwnd,out uint id);
            if(!ids.Contains(id) || !IsWindowVisible(hwnd))return true;
            var title=new StringBuilder(512);GetWindowText(hwnd,title,title.Capacity);
            string name=title.ToString();
            if(name=="AirPlay Video Stream" || (name.Contains("Direct") && name.Contains("enderer")))found.Add(hwnd);
            return true;
        },0);
        videoWindows.Clear();videoWindows.UnionWith(found);
        if(EmbeddedVideo!=0 && IsWindow(EmbeddedVideo))videoWindows.Add(EmbeddedVideo);
    }
    internal static bool Contains(int x,int y,int left,int top,int right,int bottom) =>
        x>=left && x<right && y>=top && y<bottom;
    internal static bool RouteHover(bool candidate,bool wasInside,byte physical,ref bool blocked)
    {
        if(!wasInside && physical!=0 && !candidate)blocked=true;
        bool inside=candidate && !blocked;
        if(wasInside && !inside && physical!=0)blocked=true;
        if(physical==0)blocked=false;
        return inside;
    }
    internal static (int Left,int Top,int Width,int Height) FitContent(int width,int height,double ratio)
    {
        if(width<=0 || height<=0 || ratio<=0)return (0,0,0,0);
        int w=width,h=height;
        if((double)width/height>ratio)w=Math.Max(1,(int)Math.Round(height*ratio));
        else h=Math.Max(1,(int)Math.Round(width/ratio));
        return ((width-w)/2,(height-h)/2,w,h);
    }
    internal static int MapAbsolute(int offset,int size) =>
        size<=1?0:(int)Math.Round(Math.Clamp(offset,0,size-1)*32767.0/(size-1));
    private bool OverVideo(NativePoint point,out int absoluteX,out int absoluteY)
    {
        absoluteX=0;absoluteY=0;
        var hit=WindowFromPoint(point);
        var root=videoWindows.FirstOrDefault(h=>h==hit || IsChild(h,hit) || GetAncestor(h,2)==GetAncestor(hit,2));
        if(root==0)root=GetAncestor(hit,2);
        if(!videoWindows.Contains(root) || !GetClientRect(root,out var rect))return false;
        var origin=new NativePoint();if(!ClientToScreen(root,ref origin))return false;
        var area=FitContent(rect.Right,rect.Bottom,ScreenAspect);
        int left=origin.X+area.Left,top=origin.Y+area.Top;
        if(!Contains(point.X,point.Y,left,top,left+area.Width,top+area.Height))return false;
        viewportLeft=left;viewportTop=top;viewportWidth=area.Width;viewportHeight=area.Height;
        absoluteX=MapAbsolute(point.X-left,area.Width);absoluteY=MapAbsolute(point.Y-top,area.Height);
        return true;
    }
    public void Stop()
    {
        if(hook!=0){UnhookWindowsHookEx(hook);hook=0;}
        UnregisterHotKey(window,83);buttons=0;physicalButtons=0;
        manual=false;hovering=false;Automatic=false;blockedGesture=false;lastPoint=null;
    }
    private nint WindowMessage(nint hwnd,int msg,nint wParam,nint lParam,ref bool handled)
    {
        if(msg!=0x0312)return 0;
        if(wParam==81){handled=true;ToggleRequested?.Invoke();}
        else if(wParam==82 || wParam==83){handled=true;ReleaseRequested?.Invoke();}
        return 0;
    }
    internal static byte ButtonState(byte state,int msg) => msg switch
    {
        0x201=>(byte)(state|1),0x202=>(byte)(state&~1),
        0x204=>(byte)(state|2),0x205=>(byte)(state&~2),
        0x207=>(byte)(state|4),0x208=>(byte)(state&~4),_=>state
    };
    private nint OnMouse(int code,nint message,nint data)
    {
        if(code<0 || !Installed)return CallNextHookEx(hook,code,message,data);
        var m=Marshal.PtrToStructure<MouseData>(data);
        // Do not turn synthetic / accessibility input into remote phone input.
        if((m.Flags&1)!=0)return CallNextHookEx(hook,code,message,data);
        int msg=(int)message;
        if(msg<0x200 || msg>0x20E)return CallNextHookEx(hook,code,message,data);
        var previous=lastPoint;lastPoint=m.Point;
        int absoluteX=0,absoluteY=0;
        if(Automatic)
        {
            physicalButtons=ButtonState(physicalButtons,msg);
            bool candidate=ClientReady && OverVideo(m.Point,out absoluteX,out absoluteY);
            // Do not steal a Windows drag that started on a title bar or elsewhere.
            bool inside=RouteHover(candidate,hovering,physicalButtons,ref blockedGesture);
            if(inside!=hovering)
            {
                hovering=inside;buttons=0;previous=null;
                HoverChanged?.Invoke(inside);
            }
            if(!hovering)return CallNextHookEx(hook,code,message,data);
        }
        int x=0,y=0,wheel=0;
        if(msg==0x200)
        {
            if(Automatic && previous.HasValue){x=m.Point.X-previous.Value.X;y=m.Point.Y-previous.Value.Y;}
            else if(manual && GetCursorPos(out var anchor)){x=m.Point.X-anchor.X;y=m.Point.Y-anchor.Y;}
        }
        if(msg==0x20A)wheel=unchecked((short)(m.Data>>16))/120;
        buttons=ButtonState(buttons,msg);
        try{Received?.Invoke(new Input(x,y,wheel,buttons,msg==0x200,Automatic,absoluteX,absoluteY));}
        catch{Stop();ReleaseRequested?.Invoke();return CallNextHookEx(0,code,message,data);}
        // In hover mode the Windows pointer moves normally so it can leave the
        // video area. Clicks/wheel inside are delivered exclusively to the phone.
        if(Automatic && msg==0x200)return CallNextHookEx(hook,code,message,data);
        return 1;
    }
    public void Dispose()
    {
        Stop();source.RemoveHook(WindowMessage);
        if(hotkeys){UnregisterHotKey(window,81);UnregisterHotKey(window,82);hotkeys=false;}
    }
    [StructLayout(LayoutKind.Sequential)]private struct NativePoint{public int X,Y;}
    [StructLayout(LayoutKind.Sequential)]private struct NativeRect{public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)]private struct MouseData
    {public NativePoint Point;public uint Data,Flags,Time;public nuint Extra;}
    private delegate nint HookProc(int code,nint message,nint data);
    private delegate bool EnumProc(nint hwnd,nint data);
    [DllImport("user32.dll",SetLastError=true)]private static extern nint SetWindowsHookEx(int id,HookProc callback,nint module,uint thread);
    [DllImport("user32.dll")]private static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")]private static extern nint CallNextHookEx(nint hook,int code,nint message,nint data);
    [DllImport("user32.dll")]private static extern bool GetCursorPos(out NativePoint point);
    [DllImport("user32.dll")]private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")]private static extern nint WindowFromPoint(NativePoint point);
    [DllImport("user32.dll")]private static extern nint GetAncestor(nint hwnd,uint flags);
    [DllImport("user32.dll")]private static extern bool GetClientRect(nint hwnd,out NativeRect rect);
    [DllImport("user32.dll")]private static extern bool ClientToScreen(nint hwnd,ref NativePoint point);
    [DllImport("user32.dll")]private static extern bool EnumWindows(EnumProc callback,nint data);
    [DllImport("user32.dll")]private static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")]private static extern bool IsWindow(nint hwnd);
    [DllImport("user32.dll")]private static extern bool IsChild(nint parent,nint child);
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint hwnd,out uint id);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern int GetWindowText(nint hwnd,StringBuilder text,int length);
    [DllImport("user32.dll",SetLastError=true)]private static extern bool RegisterHotKey(nint hwnd,int id,uint modifiers,uint key);
    [DllImport("user32.dll")]private static extern bool UnregisterHotKey(nint hwnd,int id);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]private static extern nint GetModuleHandle(string? name);
}
