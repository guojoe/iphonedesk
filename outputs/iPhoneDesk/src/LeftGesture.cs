using System.IO;
using System.Text.Json;
namespace iPhoneDesk;

// Desktop left clicks and drags are separate actions, not a global button swap.
internal sealed class LeftGesture
{
    public byte DragButton {get;}
    public byte VerticalDragButton {get;}
    public byte BottomDragButton {get;}
    private byte activeDragButton;
    public double BottomEdgeStart {get;}
    public double BottomTravel {get;}
    public int BottomHoldMs {get;}
    public double? BottomAnchorY {get;}
    private bool bottomDrag;
    private int startY;
    public bool IsBottomSwipe(int y,int endY)=>BottomTravel<1 && y>=BottomEdgeStart*32767 && y-endY>=BottomTravel*32767;
    public int ReleaseHold(int y,int endY)=>IsBottomSwipe(y,endY)?BottomHoldMs:0;
    public byte ButtonForDirection(double x,double y)=>Math.Abs(y)>Math.Abs(x)?VerticalDragButton:DragButton;
    public byte ButtonForSwipe(int y,double dx,double dy)=>BottomTravel<1 && y>=BottomEdgeStart*32767?BottomDragButton:ButtonForDirection(dx,dy);
    public event Action<string>? Trace;
    private MouseCapture.Input? pending;
    private bool dragging;
    private int dx,dy;
    public LeftGesture(byte dragButton,byte? verticalDragButton=null,double bottomEdgeStart=1,double bottomTravel=1,int bottomHoldMs=0,double? bottomAnchorY=null,byte? bottomDragButton=null){DragButton=dragButton;VerticalDragButton=verticalDragButton??dragButton;BottomEdgeStart=bottomEdgeStart;BottomTravel=bottomTravel;BottomHoldMs=bottomHoldMs;BottomAnchorY=bottomAnchorY;BottomDragButton=bottomDragButton??VerticalDragButton;}
    public void Reset(){pending=null;dragging=false;bottomDrag=false;dx=dy=0;}
    public static LeftGesture? Load(out string? error)
    {
        error=null;
        string file=Path.Combine(AppContext.BaseDirectory,"mouse-actions.json");
        // Profile reflects this user's verified phone action: right button drags.
        if(!File.Exists(file))File.WriteAllText(file,"{\"dragButtonMask\":2}");
        string raw=File.ReadAllText(file);
        try
        {
            using var doc=JsonDocument.Parse(raw);
            if(!doc.RootElement.TryGetProperty("dragButtonMask",out var value) || !value.TryGetByte(out byte mask) || mask is not (1 or 2 or 4))throw new InvalidOperationException("dragButtonMask需为描述符支持的单按钮位1、2或4");
            byte vertical=mask;
            if(doc.RootElement.TryGetProperty("verticalDragButtonMask",out var verticalValue) && (!verticalValue.TryGetByte(out vertical) || vertical is not (1 or 2 or 4)))throw new InvalidOperationException("verticalDragButtonMask需为单按钮位1、2或4");
            double edge=1,travel=1;int hold=0;double? anchor=null;byte? bottomButton=null;
            if(doc.RootElement.TryGetProperty("bottomGesture",out var bottom))
            {
                if(bottom.TryGetProperty("buttonMask",out var bottomValue)){if(!bottomValue.TryGetByte(out byte b) || b is not (1 or 2 or 4))throw new InvalidOperationException("bottomGesture.buttonMask需为单按钮位1、2或4");bottomButton=b;}
                if(bottom.TryGetProperty("anchorY",out var anchorValue)){anchor=anchorValue.GetDouble();if(!double.IsFinite(anchor.Value)||anchor<0||anchor>1)throw new InvalidOperationException("anchorY需0–1");}
                edge=bottom.GetProperty("startY").GetDouble();travel=bottom.GetProperty("travel").GetDouble();hold=bottom.GetProperty("holdMs").GetInt32();
                if(!double.IsFinite(edge)||!double.IsFinite(travel)||edge<0||edge>1||travel<=0||travel>1||hold<0||hold>2000)throw new InvalidOperationException("bottomGesture坐标需归一化，holdMs需0–2000");
            }
            return new LeftGesture(mask,vertical,edge,travel,hold,anchor,bottomButton);
        }
        catch(Exception ex){error="鼠标动作配置只读，原值未修改："+raw+"；"+ex.Message;return null;}
    }
    public MouseCapture.Input[] Route(MouseCapture.Input input,double horizontalDistance,double verticalDistance)
    {
        if(!input.Absolute)return [input];
        if(pending.HasValue)
        {
            var start=pending.Value;
            if(input.Buttons==0)
            {
                Trace?.Invoke("左键分流：轻点确认，发送1→0。");Reset();return [start with {Buttons=1,IsMove=false},input with {IsMove=false}];
            }
            if(input.Buttons!=1 || input.Wheel!=0)
            {
                Reset();return [start with {Buttons=1,IsMove=false},input];
            }
            dx+=input.X;dy+=input.Y;
            if(Math.Abs(dx)<horizontalDistance && Math.Abs(dy)<verticalDistance)return [];
            pending=null;dragging=true;bottomDrag=BottomTravel<1 && start.AbsoluteY>=BottomEdgeStart*32767;
            startY=start.AbsoluteY;
            activeDragButton=bottomDrag?BottomDragButton:ButtonForDirection(dx,dy);Trace?.Invoke($"左键分流：已识别拖动，累计位移{dx},{dy}，配置按钮{activeDragButton}，起点{start.AbsoluteX},{start.AbsoluteY}，底部起手{bottomDrag}。");
            var press=start with {Buttons=activeDragButton,IsMove=false};
            if(bottomDrag && BottomAnchorY.HasValue)press=press with {AbsoluteY=(int)Math.Round(BottomAnchorY.Value*32767)};
            Trace?.Invoke($"拖动按下实际坐标：{press.AbsoluteX},{press.AbsoluteY}。");
            return [press,input with {Buttons=activeDragButton}];
        }
        if(dragging)
        {
            if((input.Buttons&1)==0){Trace?.Invoke($"左键分流：拖动松开，终点{input.AbsoluteX},{input.AbsoluteY}，起始Y{startY}，按钮{activeDragButton}，底部起手{bottomDrag}。");int hold=bottomDrag?ReleaseHold(startY,input.AbsoluteY):0;
                Reset();return [input with {IsMove=false,HoldBeforeReleaseMs=hold}];}
            return [input with {Buttons=(byte)((input.Buttons&~1)|activeDragButton)}];
        }
        if(!input.IsMove && input.Buttons==1 && input.Wheel==0)
        {pending=input;dx=dy=0;Trace?.Invoke("左键分流：已收到左键按下，等待移动或松开。");return [];}
        return [input];
    }
}
