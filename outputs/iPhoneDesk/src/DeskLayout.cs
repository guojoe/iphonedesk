using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shell;

namespace iPhoneDesk;

public sealed partial class Desk
{
    private readonly VideoHost videoHost=new();
    private readonly TextBlock mirrorState=new(){Text="等待屏幕镜像",FontSize=12};
    private readonly TextBlock inputState=new(){Text="尚未连接 iPhone 控制",FontSize=12};
    private readonly TextBlock metrics=new(){Text="尚未发送输入",FontSize=12,Foreground=Brushes.Gray};
    private static SolidColorBrush ColorBrush(string hex)=>new((Color)ColorConverter.ConvertFromString(hex));
    private void BuildLayout()
    {
        Title="iPhoneDesk";Width=1120;Height=840;MinWidth=920;MinHeight=680;
        WindowState=WindowState.Maximized;
        WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.CanResize;
        FontFamily=new FontFamily("Microsoft YaHei UI");FontSize=14;Background=ColorBrush("#F4F6FB");
        WindowChrome.SetWindowChrome(this,new WindowChrome{CaptionHeight=40,ResizeBorderThickness=new Thickness(6),CornerRadius=new CornerRadius(18),GlassFrameThickness=new Thickness(0),UseAeroCaptionButtons=false});
        string icon=Path.Combine(AppContext.BaseDirectory,"icon.ico");
        if(File.Exists(icon))Icon=BitmapFrame.Create(new Uri(icon));
        var page=new Grid{Background=ColorBrush("#F4F6FB")};page.RowDefinitions.Add(new RowDefinition{Height=new GridLength(44)});page.RowDefinitions.Add(new RowDefinition());Content=page;
        var header=new Grid{Background=Brushes.White};header.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(210)});header.ColumnDefinitions.Add(new ColumnDefinition());header.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(140)});page.Children.Add(header);
        var brand=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(24,0,0,0)};
        string preview=Path.Combine(AppContext.BaseDirectory,"icon-256.png");
        if(File.Exists(preview))brand.Children.Add(new Image{Source=new BitmapImage(new Uri(preview)),Width=28,Height=28,Margin=new Thickness(0,0,10,0)});
        brand.Children.Add(new TextBlock{Text="iPhoneDesk",FontSize=19,FontWeight=FontWeights.SemiBold,VerticalAlignment=VerticalAlignment.Center});header.Children.Add(brand);
        var nav=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(nav,1);header.Children.Add(nav);
        nav.Children.Add(new TextBlock{Text="设备工作台",Foreground=ColorBrush("#8E8E93"),FontSize=12,Margin=new Thickness(18,0,0,0),VerticalAlignment=VerticalAlignment.Center});
        var windowButtons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};Grid.SetColumn(windowButtons,2);header.Children.Add(windowButtons);
        foreach(int kind in new[]{0,1,2})
        {
            var b=CaptionButton(kind);WindowChrome.SetIsHitTestVisibleInChrome(b,true);windowButtons.Children.Add(b);
        }
        var body=new Grid();Grid.SetRow(body,1);page.Children.Add(body);
        body.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(280)});body.ColumnDefinitions.Add(new ColumnDefinition());
        var rail=new Border{Background=ColorBrush("#F8F9FC"),BorderBrush=ColorBrush("#E6E8ED"),BorderThickness=new Thickness(0,0,1,0)};body.Children.Add(rail);
        var screenColumn=new Grid{Margin=new Thickness(24,12,18,12)};Grid.SetColumn(screenColumn,1);body.Children.Add(screenColumn);
        screenColumn.RowDefinitions.Add(new RowDefinition{Height=new GridLength(48)});screenColumn.RowDefinitions.Add(new RowDefinition());screenColumn.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        var screenTitle=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};screenTitle.Children.Add(Label("iPhone",18,"#1C1C1E",true));screenTitle.Children.Add(new Border{Background=ColorBrush("#E6F7ED"),CornerRadius=new CornerRadius(12),Padding=new Thickness(10,4,10,4),Margin=new Thickness(12,8,0,8),Child=Label("屏幕镜像",11,"#248A3D")});screenColumn.Children.Add(screenTitle);
        var frame=new Border{CornerRadius=new CornerRadius(42),Background=ColorBrush("#151516"),BorderBrush=ColorBrush("#777B83"),BorderThickness=new Thickness(2),Padding=new Thickness(6),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Effect=new DropShadowEffect{BlurRadius=42,ShadowDepth=16,Opacity=0.12}};
        videoHost.Width=300;videoHost.Height=650;var display=new Grid();frame.Child=display;
        var idle=new Border{CornerRadius=new CornerRadius(34),Background=new LinearGradientBrush(Color.FromRgb(29,36,57),Color.FromRgb(70,83,111),65)};
        var idleContent=new Grid();idle.Child=idleContent;
        idleContent.Children.Add(new Border{Width=88,Height=24,CornerRadius=new CornerRadius(12),Background=Brushes.Black,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(0,12,0,0)});
        var welcome=new StackPanel{VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center};
        welcome.Children.Add(new TextBlock{Text="你好，iPhone",Foreground=Brushes.White,FontSize=23,FontWeight=FontWeights.Light,HorizontalAlignment=HorizontalAlignment.Center});
        welcome.Children.Add(new TextBlock{Text="连接屏幕镜像，开始使用",Foreground=ColorBrush("#B9C3D9"),FontSize=12,Margin=new Thickness(0,14,0,0),HorizontalAlignment=HorizontalAlignment.Center});idleContent.Children.Add(welcome);
        idleContent.Children.Add(new Border{Width=100,Height=4,CornerRadius=new CornerRadius(2),Background=Brushes.White,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(0,0,0,10)});
        display.Children.Add(idle);display.Children.Add(videoHost);var phone=new Grid{HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
        phone.Children.Add(frame);
        foreach(var spec in new[]{(-1,65,22),(-1,115,46),(-1,174,46),(1,135,66)})
        {
            var key=new Border{Width=4,Height=spec.Item3,CornerRadius=new CornerRadius(2),Background=ColorBrush("#A69E90"),HorizontalAlignment=spec.Item1<0?HorizontalAlignment.Left:HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(spec.Item1<0?-3:0,spec.Item2,spec.Item1>0?-3:0,0)};phone.Children.Add(key);
        }
        var viewport=new Grid();Grid.SetRow(viewport,1);screenColumn.Children.Add(viewport);viewport.Children.Add(phone);
        viewport.SizeChanged+=(_,_)=>{double aspect=1179.0/2556.0;double height=Math.Max(1,Math.Min(viewport.ActualHeight-16,(viewport.ActualWidth-24)/aspect));videoHost.Height=height;videoHost.Width=height*aspect;};
        var hint=Label("移入手机画面即可操作 · 移出恢复电脑",12,"#8E8E93");hint.HorizontalAlignment=HorizontalAlignment.Center;hint.VerticalAlignment=VerticalAlignment.Center;hint.Margin=new Thickness(0,10,0,4);Grid.SetRow(hint,2);screenColumn.Children.Add(hint);
        body.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)]=ThinScrollStyle();
        var cards=new StackPanel{Margin=new Thickness(18,24,18,24)};Grid.SetColumn(cards,0);body.Children.Add(new ScrollViewer{Content=cards,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Margin=new Thickness(0),HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});Grid.SetColumn((UIElement)body.Children[body.Children.Count-1],0);
        cards.Children.Add(Label("设备",11,"#8E8E93"));cards.Children.Add(Label("我的 iPhone",20,"#1C1C1E",true));cards.Children.Add(Label("画面与操作，随手连接",12,"#8E8E93"));
        var connection=Card("连接");cards.Children.Add(connection.Border);
        connection.Body.Children.Add(mirrorState);connection.Body.Children.Add(inputState);
        mirrorButton=IOSButton("启动 AirPlay 投屏",OpenReceiver,true);connection.Body.Children.Add(mirrorButton);
        connection.Body.Children.Add(Label("同一 Wi-Fi · iPhone 控制中心\n屏幕镜像 → 选择下方投屏名称\n无需手机 App，无需 USB 驱动",11,"#636366"));
        control=IOSButton("连接 iPhone 控制",StartHid,false);connection.Body.Children.Add(control);
        connection.Body.Children.Add(Label("使用电脑蓝牙连接 iPhone；有线鼠标、无线鼠标和触控板均可。",11,"#636366"));
        connectionNotice.TextWrapping=TextWrapping.Wrap;connectionNotice.FontSize=11;connectionNotice.Margin=new Thickness(0,8,0,0);connection.Body.Children.Add(connectionNotice);
        reconnectControl=IOSAsyncButton("重新连接",async()=>await ReconnectControl(),false);connection.Body.Children.Add(reconnectControl);
        stopControl=IOSAsyncButton("停止连接",async()=>await StopControl(),false);connection.Body.Children.Add(stopControl);
        connection.Body.Children.Add(IOSButton("电脑蓝牙设置",(_,_)=>Shell("ms-settings:bluetooth"),false));
        var naming=Card("投屏名称");cards.Children.Add(naming.Border);
        var nameBox=new TextBox{Text=ReadMirrorName(),FontSize=13,Padding=new Thickness(10,8,10,8),Margin=new Thickness(0,8,0,0),MaxLength=32,BorderBrush=ColorBrush("#E0E5ED"),BorderThickness=new Thickness(1)};
        naming.Body.Children.Add(nameBox);
        naming.Body.Children.Add(Label("显示在 iPhone 的屏幕镜像列表中",11,"#8E8E93"));
        naming.Body.Children.Add(IOSAsyncButton("保存并应用",async()=>await ApplyMirrorName(nameBox.Text),false,true));
        AddResolutionMenu(cards);
        var mouse=Card("鼠标与手势");cards.Children.Add(mouse.Border);
        automaticMode.Content=new TextBlock{Text="鼠标移入时自动控制",FontSize=12,TextWrapping=TextWrapping.Wrap};mouse.Body.Children.Add(automaticMode);
        automaticMode.Checked+=(_,_)=>{if(!changingMode)EnableHover();};automaticMode.Unchecked+=async (_,_)=>{if(!changingMode)await ReleaseCapture();};
        mouse.Body.Children.Add(Label("左键轻点确认，按住移动拖动\n移出恢复电脑 · F9 暂停",13,"#636366"));
        pauseControl=IOSAsyncButton("暂停控制",async()=>{if(capture?.Automatic==true || capture?.Active==true)await ReleaseCapture("控制已暂停；点击恢复控制可继续。");else EnableHover();},false);mouse.Body.Children.Add(pauseControl);
        connection.Body.Children.Add(IOSButton("连接说明",(_,_)=>OpenFile("使用说明.md"),false));
        var diag=Card("连接详情");cards.Children.Add(diag.Border);metrics.TextWrapping=TextWrapping.Wrap;diag.Body.Children.Add(metrics);
        status.FontSize=11;status.Foreground=ColorBrush("#636366");status.Margin=new Thickness(0,8,0,8);diag.Body.Children.Add(status);
        var details=new Expander{Header="查看连接日志",Margin=new Thickness(0,8,0,0)};log.Height=160;log.FontSize=11;details.Content=log;diag.Body.Children.Add(details);
        AddPlatformNavigation(page,body,nav);
    }
    private static string ReceiverConfig=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"leapbtw","uxplay-windows","arguments.txt");
    private static string ReadMirrorName()
    {
        if(!File.Exists(ReceiverConfig))return "iPhoneDesk";
        var match=System.Text.RegularExpressions.Regex.Match(File.ReadAllText(ReceiverConfig),@"(?:^|\s)-n\s+(\S+)");
        return match.Success?match.Groups[1].Value:"iPhoneDesk";
    }
    private async System.Threading.Tasks.Task ApplyMirrorName(string value)
    {
        value=value.Trim();
        if(value==ReadMirrorName()){SetStatus("投屏名称未改变，无需重新应用。");return;}
        if(value.Length==0 || value.Length>32 || value.StartsWith('-') || value.Any(char.IsWhiteSpace) || value.Any(char.IsControl))
        {SetStatus("名称需为 1–32 个字符，可用中文、英文、数字；请勿包含空格或以 - 开头。");return;}
        try
        {
            var config=ReceiverConfig;Directory.CreateDirectory(Path.GetDirectoryName(config)!);
            string args=File.Exists(config)?File.ReadAllText(config):"-nh -s 1280x720@60 -fps 60 -vd d3d11h264dec -vc d3d11convert -vsync no";
            args=System.Text.RegularExpressions.Regex.Replace(args,@"(?:^|\s)-n\s+\S+"," ").Trim();
            File.WriteAllText(config,"-n "+value+" "+args,new System.Text.UTF8Encoding(false));
            string dir=Path.Combine(AppContext.BaseDirectory,"receiver");
            foreach(string processName in new[]{"uxplay-windows","uxplay-bluetooth-beacon"})
                foreach(var process in Process.GetProcessesByName(processName))
                    using(process)
                    {
                        string expected=Path.Combine(dir,processName+".exe");
                        if(!string.Equals(process.MainModule?.FileName,expected,StringComparison.OrdinalIgnoreCase))continue;
                        process.Kill();await process.WaitForExitAsync();
                    }
            engine=null;OpenReceiver(this,new RoutedEventArgs());
            SetStatus("投屏名称已保存为“"+value+"”。接收器已重启，请在 iPhone 重新连接屏幕镜像。");
        }
        catch(Exception ex){SetStatus("应用投屏名称失败："+ex.Message);}
    }
    private Button CaptionButton(int kind)
    {
        var button=new Button{Width=38,Height=30,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,2,0),Background=Brushes.Transparent,Foreground=ColorBrush("#626872"),BorderThickness=new Thickness(0),FocusVisualStyle=null,ToolTip=kind==0?"最小化":kind==1?"最大化 / 还原":"关闭"};
        string shape=kind==0?"M 0,5 L 10,5":kind==1?"M 0,0 L 10,0 L 10,10 L 0,10 Z":"M 0,0 L 10,10 M 10,0 L 0,10";
        var border=new FrameworkElementFactory(typeof(Border));border.SetValue(Border.CornerRadiusProperty,new CornerRadius(6));border.SetValue(Border.BackgroundProperty,new TemplateBindingExtension(Control.BackgroundProperty));
        var path=new FrameworkElementFactory(typeof(System.Windows.Shapes.Path));path.SetValue(System.Windows.Shapes.Path.DataProperty,Geometry.Parse(shape));path.SetValue(System.Windows.Shapes.Path.StrokeProperty,new TemplateBindingExtension(Control.ForegroundProperty));path.SetValue(System.Windows.Shapes.Path.StrokeThicknessProperty,1.0);path.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Center);path.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);border.AppendChild(path);
        var template=new ControlTemplate(typeof(Button)){VisualTree=border};
        var hover=new Trigger{Property=IsMouseOverProperty,Value=true};hover.Setters.Add(new Setter(Control.BackgroundProperty,kind==2?ColorBrush("#FF453A"):ColorBrush("#EEF0F4")));if(kind==2)hover.Setters.Add(new Setter(Control.ForegroundProperty,Brushes.White));template.Triggers.Add(hover);button.Template=template;
        button.Click+=(_,_)=>{if(kind==0)SystemCommands.MinimizeWindow(this);else if(kind==1)WindowState=WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;else Close();};return button;
    }
    internal static Style ThinScrollStyle()=>(Style)System.Windows.Markup.XamlReader.Parse("""
        <Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="ScrollBar">
          <Setter Property="Width" Value="8"/><Setter Property="Background" Value="Transparent"/>
          <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="ScrollBar">
            <Grid Background="Transparent"><Track x:Name="PART_Track" IsDirectionReversed="True" Orientation="Vertical" Minimum="{TemplateBinding Minimum}" Maximum="{TemplateBinding Maximum}" Value="{TemplateBinding Value}" ViewportSize="{TemplateBinding ViewportSize}">
              <Track.DecreaseRepeatButton><RepeatButton Command="{x:Static ScrollBar.PageUpCommand}" Focusable="False"><RepeatButton.Template><ControlTemplate TargetType="RepeatButton"><Border Background="Transparent"/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.DecreaseRepeatButton>
              <Track.Thumb><Thumb MinHeight="24"><Thumb.Template><ControlTemplate TargetType="Thumb"><Border x:Name="Grip" Width="4" CornerRadius="2" Background="#BFC5D0" HorizontalAlignment="Center"/><ControlTemplate.Triggers><Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Grip" Property="Background" Value="#929BAA"/></Trigger><Trigger Property="IsDragging" Value="True"><Setter TargetName="Grip" Property="Background" Value="#768397"/></Trigger></ControlTemplate.Triggers></ControlTemplate></Thumb.Template></Thumb></Track.Thumb>
              <Track.IncreaseRepeatButton><RepeatButton Command="{x:Static ScrollBar.PageDownCommand}" Focusable="False"><RepeatButton.Template><ControlTemplate TargetType="RepeatButton"><Border Background="Transparent"/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.IncreaseRepeatButton>
            </Track></Grid>
          </ControlTemplate></Setter.Value></Setter>
        </Style>
        """);
    internal static TextBlock Label(string value,int size,string color,bool bold=false)=>new(){Text=value,FontSize=size,Foreground=ColorBrush(color),FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,5,0,5)};
    internal static (Border Border,StackPanel Body) Card(string title)
    {
        var content=new StackPanel();content.Children.Add(Label(title,13,"#1C1C1E",true));
        var border=new Border{Background=Brushes.White,CornerRadius=new CornerRadius(14),BorderBrush=ColorBrush("#E9ECF2"),BorderThickness=new Thickness(1),Padding=new Thickness(15),Margin=new Thickness(0,0,0,14),Child=content};return (border,content);
    }
    internal static Button IOSButton(string title,RoutedEventHandler handler,bool primary)
    {
        var b=new Button{Content=title,Margin=new Thickness(0,8,0,0),FontSize=12,FocusVisualStyle=null,Padding=new Thickness(12,10,12,10),Background=primary?ColorBrush("#007AFF"):ColorBrush("#F4F6FA"),Foreground=primary?Brushes.White:ColorBrush("#007AFF"),BorderThickness=new Thickness(0),Cursor=System.Windows.Input.Cursors.Hand};
        var border=new FrameworkElementFactory(typeof(Border));border.SetValue(Border.CornerRadiusProperty,new CornerRadius(9));border.SetValue(Border.BackgroundProperty,new TemplateBindingExtension(Control.BackgroundProperty));border.SetValue(Border.PaddingProperty,new TemplateBindingExtension(Control.PaddingProperty));
        var presenter=new FrameworkElementFactory(typeof(ContentPresenter));presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty,HorizontalAlignment.Center);presenter.SetValue(ContentPresenter.VerticalAlignmentProperty,VerticalAlignment.Center);border.AppendChild(presenter);var template=new ControlTemplate(typeof(Button)){VisualTree=border};
        var disabled=new Trigger{Property=IsEnabledProperty,Value=false};disabled.Setters.Add(new Setter(OpacityProperty,0.45));disabled.Setters.Add(new Setter(CursorProperty,System.Windows.Input.Cursors.Arrow));template.Triggers.Add(disabled);b.Template=template;b.Click+=handler;return b;
    }
    private Button IOSAsyncButton(string title,Func<Task> action,bool primary,bool changesReceiver=false)
    {
        Button? button=null;
        button=IOSButton(title,async(_,_)=>
        {
            if(busyButtons.Contains(button!) || (changesReceiver && receiverChanging))return;
            busyButtons.Add(button!);
            if(changesReceiver)receiverChanging=true;
            button!.IsEnabled=false;
            try{await action();}catch(Exception ex){SetStatus("操作失败："+ex.Message);}
            finally{busyButtons.Remove(button);if(changesReceiver)receiverChanging=false;button.IsEnabled=true;UpdateActionStates();}
        },primary);
        if(changesReceiver)receiverActions.Add(button);
        return button;
    }

}
