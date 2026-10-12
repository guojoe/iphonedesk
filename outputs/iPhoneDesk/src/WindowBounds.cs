using System.Runtime.InteropServices;
namespace iPhoneDesk;
public sealed partial class Desk
{
    // WM_GETMINMAXINFO: borderless WPF maximization must respect this monitor's work area.
    private nint WorkAreaBounds(nint hwnd,int message,nint wParam,nint lParam,ref bool handled)
    {
        if(message!=0x0024)return 0;
        var monitor=MonitorFromWindow(hwnd,2); // MONITOR_DEFAULTTONEAREST
        var info=new MonitorArea{Size=Marshal.SizeOf<MonitorArea>()};
        if(monitor==0 || !GetMonitorInfo(monitor,ref info))return 0;
        var limits=Marshal.PtrToStructure<WindowLimits>(lParam);
        limits.MaxPosition=new NativePoint{X=info.Work.Left-info.Monitor.Left,Y=info.Work.Top-info.Monitor.Top};
        limits.MaxSize=new NativePoint{X=info.Work.Right-info.Work.Left,Y=info.Work.Bottom-info.Work.Top};
        Marshal.StructureToPtr(limits,lParam,false);handled=true;return 0;
    }
    [StructLayout(LayoutKind.Sequential)]private struct NativePoint{public int X,Y;}
    [StructLayout(LayoutKind.Sequential)]private struct NativeRect{public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)]private struct WindowLimits{public NativePoint Reserved,MaxSize,MaxPosition,MinTrackSize,MaxTrackSize;}
    [StructLayout(LayoutKind.Sequential)]private struct MonitorArea{public int Size;public NativeRect Monitor,Work;public uint Flags;}
    [DllImport("user32.dll")]private static extern nint MonitorFromWindow(nint hwnd,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Auto)]private static extern bool GetMonitorInfo(nint monitor,ref MonitorArea info);
}
