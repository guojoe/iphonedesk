using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Interop;

namespace iPhoneDesk;

// Reparents the original GPU-rendered window, without copying/re-encoding frames.
public sealed class VideoHost : HwndHost
{
    private nint host,video;
    private nint originalStyle,originalParent;
    private Rect originalRect;
    private int lastWidth=-1,lastHeight=-1;
    public object? LastResize {get;private set;}
    public nint Video => video!=0 && IsWindow(video)?video:0;
    public VideoHost(){SizeChanged+=(_,_)=>ResizeVideo();}
    protected override HandleRef BuildWindowCore(HandleRef parent)
    {
        host=CreateWindowEx(0,"STATIC","",0x40000004|0x02000000,0,0,1,1,parent.Handle,0,0,0);
        if(host==0)throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        nint empty=CreateRoundRectRgn(0,0,0,0,0,0);
        if(empty!=0 && SetWindowRgn(host,empty,true)==0)DeleteObject(empty);
        return new HandleRef(this,host);
    }
    public bool TryAttach(string receiverPath)
    {
        if(host==0)return false;
        if(Video!=0){ResizeVideo();return true;}
        if(video!=0)
        {
            video=0;
            nint empty=CreateRoundRectRgn(0,0,0,0,0,0);
            if(empty!=0 && SetWindowRgn(host,empty,true)==0)DeleteObject(empty);
        }
        var ids=new HashSet<uint>();
        foreach(var p in Process.GetProcessesByName("uxplay-windows"))
            using(p){try{if(string.Equals(p.MainModule?.FileName,receiverPath,StringComparison.OrdinalIgnoreCase))ids.Add((uint)p.Id);}catch{}}
        nint candidate=0;
        EnumWindows((hwnd,_)=>
        {
            GetWindowThreadProcessId(hwnd,out uint pid);
            if(!ids.Contains(pid)||!IsWindowVisible(hwnd))return true;
            var text=new StringBuilder(512);GetWindowText(hwnd,text,512);var title=text.ToString();
            if(title=="AirPlay Video Stream"||(title.Contains("Direct")&&title.Contains("enderer"))){candidate=hwnd;return false;}
            return true;
        },0);
        if(candidate==0)return false;
        originalStyle=GetWindowLongPtr(candidate,-16);originalParent=GetParent(candidate);
        GetWindowRect(candidate,out originalRect);ShowWindow(candidate,9);
        long style=originalStyle.ToInt64();
        style&=~(0x00C00000L|0x00040000L|0x80000000L|0x01000000L|0x20000000L);
        style|=0x50000000L;
        SetWindowLongPtr(candidate,-16,(nint)style);SetParent(candidate,host);
        if(GetParent(candidate)!=host)
        {
            SetWindowLongPtr(candidate,-16,originalStyle);
            throw new InvalidOperationException("投屏窗口嵌入失败，请保留独立窗口并查看日志。");
        }
        video=candidate;lastWidth=-1;lastHeight=-1;ResizeVideo();ShowWindow(host,5);ShowWindow(video,5);return true;
    }
    private void ResizeVideo()
    {
        if(Video==0||host==0||!GetClientRect(host,out var rect))return;
        int width=Math.Max(1,rect.Right),height=Math.Max(1,rect.Bottom);
        // A codec/photo transition can resize the renderer without changing HwndHost.
        bool actualKnown=GetClientRect(video,out var actual);
        var rendererOrigin=new NativePoint();var hostOrigin=new NativePoint();
        bool positionKnown=ClientToScreen(video,ref rendererOrigin) && ClientToScreen(host,ref hostOrigin);
        int offsetX=rendererOrigin.X-hostOrigin.X,offsetY=rendererOrigin.Y-hostOrigin.Y;
        if(width==lastWidth && height==lastHeight && actualKnown && positionKnown &&
            actual.Right==width && actual.Bottom==height && offsetX==0 && offsetY==0)return;
        bool resized=MoveWindow(video,0,0,width,height,true);
        LastResize=new {renderer=video.ToInt64(),width,height,actualKnown,beforeWidth=actual.Right,beforeHeight=actual.Bottom,
            positionKnown,offsetX,offsetY,resized};
        int diameter=Math.Max(2,(int)(width*0.19));
        nint region=CreateRoundRectRgn(0,0,width+1,height+1,diameter,diameter);
        if(region!=0 && SetWindowRgn(video,region,true)==0)DeleteObject(region);
        nint hostRegion=CreateRoundRectRgn(0,0,width+1,height+1,diameter,diameter);
        if(hostRegion!=0 && SetWindowRgn(host,hostRegion,true)==0)DeleteObject(hostRegion);
        lastWidth=width;lastHeight=height;
    }
    private void RestoreVideo()
    {
        if(video!=0 && IsWindow(video))
        {
            SetWindowRgn(video,0,true);
            SetParent(video,originalParent);SetWindowLongPtr(video,-16,originalStyle);
            MoveWindow(video,originalRect.Left,originalRect.Top,Math.Max(1,originalRect.Right-originalRect.Left),Math.Max(1,originalRect.Bottom-originalRect.Top),true);
            ShowWindow(video,9);
        }
        video=0;
    }
    protected override void DestroyWindowCore(HandleRef handle)
    {
        RestoreVideo();DestroyWindow(handle.Handle);host=0;
    }
    [StructLayout(LayoutKind.Sequential)]private struct NativePoint{public int X,Y;}
    [DllImport("user32.dll")]private static extern bool ClientToScreen(nint hwnd,ref NativePoint point);
    [DllImport("gdi32.dll")]private static extern nint CreateRoundRectRgn(int left,int top,int right,int bottom,int ellipseWidth,int ellipseHeight);
    [DllImport("gdi32.dll")]private static extern bool DeleteObject(nint obj);
    [DllImport("user32.dll")]private static extern int SetWindowRgn(nint hwnd,nint region,bool redraw);
    private delegate bool EnumProc(nint hwnd,nint data);
    [StructLayout(LayoutKind.Sequential)]private struct Rect{public int Left,Top,Right,Bottom;}
    [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]private static extern nint CreateWindowEx(uint ex,string cls,string title,uint style,int x,int y,int w,int h,nint parent,nint menu,nint module,nint data);
    [DllImport("user32.dll")]private static extern bool DestroyWindow(nint hwnd);
    [DllImport("user32.dll")]private static extern bool IsWindow(nint hwnd);
    [DllImport("user32.dll")]private static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")]private static extern bool EnumWindows(EnumProc proc,nint data);
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint hwnd,out uint pid);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern int GetWindowText(nint hwnd,StringBuilder text,int max);
    [DllImport("user32.dll")]private static extern nint GetWindowLongPtr(nint hwnd,int index);
    [DllImport("user32.dll")]private static extern nint SetWindowLongPtr(nint hwnd,int index,nint value);
    [DllImport("user32.dll")]private static extern nint SetParent(nint hwnd,nint parent);
    [DllImport("user32.dll")]private static extern nint GetParent(nint hwnd);
    [DllImport("user32.dll")]private static extern bool ShowWindow(nint hwnd,int how);
    [DllImport("user32.dll")]private static extern bool GetWindowRect(nint hwnd,out Rect rect);
    [DllImport("user32.dll")]private static extern bool GetClientRect(nint hwnd,out Rect rect);
    [DllImport("user32.dll")]private static extern bool MoveWindow(nint hwnd,int x,int y,int w,int h,bool redraw);
}
