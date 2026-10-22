using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
namespace iPhoneDesk;

internal static class MirrorResolution
{
    internal static bool Valid(string value)
    {
        var m=Regex.Match(value,@"^(\+?\d{1,4})x(\+?\d{1,4})(?:@(\+?\d{1,3}))?$");
        return m.Success && m.Groups[1].Length<=4 && m.Groups[2].Length<=4 && int.Parse(m.Groups[1].Value)>0 && int.Parse(m.Groups[2].Value)>0 && (!m.Groups[3].Success || (m.Groups[3].Length<=3 && int.Parse(m.Groups[3].Value) is >0 and <=255));
    }
    internal static string? Current(string raw)
    {
        var tokens=Regex.Matches(raw.TrimEnd('\r','\n'),"[^ ]+");string? current=null;
        for(int i=0;i<tokens.Count;i++)
        {
            if(tokens[i].Value.StartsWith("-s="))throw new InvalidOperationException("当前 -s= 写法未适配，保留原配置。");
            if(tokens[i].Value!="-s")continue;
            if(++i>=tokens.Count || !Valid(tokens[i].Value))throw new InvalidOperationException("当前分辨率参数无法解析，保留原配置。");
            current=tokens[i].Value;
        }
        return current;
    }
    internal static string Change(string raw,string? value)
    {
        Current(raw);
        if(value!=null && !Valid(value))throw new InvalidOperationException("请输入宽x高或宽x高@刷新率；宽高1–9999，刷新率1–255。");
        var tokens=Regex.Matches(raw.TrimEnd('\r','\n'),"[^ ]+");var spans=new List<(int Start,int Length)>();
        for(int i=0;i<tokens.Count;i++)if(tokens[i].Value=="-s")
        {
            var option=tokens[i];var parameter=tokens[++i];
            spans.Add(value==null?(option.Index,parameter.Index+parameter.Length-option.Index):(parameter.Index,parameter.Length));
        }
        foreach(var span in spans.AsEnumerable().Reverse())raw=raw.Remove(span.Start,span.Length).Insert(span.Start,value??"");
        if(spans.Count==0 && value!=null)
        {
            string core=raw.TrimEnd('\r','\n');raw=core+(core.Length>0?" ":"")+"-s "+value+raw[core.Length..];
        }
        return raw;
    }
}

public sealed partial class Desk
{
    private void AddResolutionMenu(StackPanel cards)
    {
        var card=Card("投屏分辨率");cards.Children.Add(card.Border);
        var choice=new ComboBox{IsEditable=true,FontSize=12,Padding=new Thickness(8),Margin=new Thickness(0,8,0,0)};
        // Presets are shortcuts; arbitrary receiver-supported sizes remain editable.
        foreach(var spec in new[]{("自动（接收器默认）",""),("720p · 竖屏流畅","720x1280@60"),("1080p · 竖屏清晰","1080x1920@60"),("1440p · 竖屏高清","1440x2560@60"),("4K · 竖屏超清","2160x3840@60"),("720p · 横屏流畅","1280x720@60"),("1080p · 横屏清晰","1920x1080@60"),("1440p · 横屏高清","2560x1440@60"),("4K · 横屏超清","3840x2160@60")})choice.Items.Add(new ComboBoxItem{Content=spec.Item1,Tag=spec.Item2});
        string raw="";string? error=null;
        try
        {
            raw=File.Exists(ReceiverConfig)?File.ReadAllText(ReceiverConfig):"";
            string? current=MirrorResolution.Current(raw);
            choice.SelectedItem=choice.Items.Cast<ComboBoxItem>().FirstOrDefault(i=>(string)i.Tag==(current??""));
            if(choice.SelectedItem==null)choice.Text=current??"";
        }
        catch(Exception ex){error=ex.Message;choice.Text=raw;choice.IsReadOnly=true;choice.IsEnabled=false;}
        card.Body.Children.Add(choice);
        card.Body.Children.Add(Label(error??"可自定义，如 1179x2556@60。实际尺寸由手机协商。",11,"#8E8E93"));
        var apply=IOSAsyncButton("应用分辨率",async()=>
        {
            try
            {
                var selected=choice.SelectedItem as ComboBoxItem;
                bool preset=selected!=null && string.Equals(choice.Text,selected.Content?.ToString(),StringComparison.Ordinal);
                string value=preset && selected?.Tag is string tag?tag:choice.Text.Trim();
                if(!preset && value.Length==0)throw new InvalidOperationException("自定义分辨率不能为空；恢复默认请明确选择自动。");
                string next=MirrorResolution.Change(raw,value.Length==0?null:value);
                string actual=File.Exists(ReceiverConfig)?File.ReadAllText(ReceiverConfig):"";
                if(actual!=raw)throw new InvalidOperationException("配置已被其他操作修改，请重新打开程序读取后再应用。");
                if(next==raw){SetStatus("分辨率未改变。");return;}
                Directory.CreateDirectory(Path.GetDirectoryName(ReceiverConfig)!);
                string temp=ReceiverConfig+"."+Guid.NewGuid().ToString("N")+".tmp";
                File.WriteAllText(temp,next,new System.Text.UTF8Encoding(false));
                if(File.Exists(ReceiverConfig))File.Replace(temp,ReceiverConfig,ReceiverConfig+"."+DateTime.Now.ToString("yyyyMMddHHmmssfff")+".bak");else File.Move(temp,ReceiverConfig);
                raw=next;
                await RestartReceiver();SetStatus("分辨率已保存，接收器已重启。请重新连接屏幕镜像。");
            }
            catch(Exception ex){SetStatus("分辨率未应用："+ex.Message);}
        },false,true);
        if(error!=null)unsupportedActions.Add(apply);apply.IsEnabled=error==null;card.Body.Children.Add(apply);
    }
}
