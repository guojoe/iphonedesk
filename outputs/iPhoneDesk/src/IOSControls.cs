using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace iPhoneDesk;

internal static class IOSControls
{
    public static void Input(Control control)
    {
        control.FontSize=13;control.Padding=new Thickness(10,9,10,9);
        control.Background=Brushes.White;control.BorderBrush=new SolidColorBrush(Color.FromRgb(224,229,237));control.BorderThickness=new Thickness(1);
        var border=new FrameworkElementFactory(typeof(Border));border.SetValue(Border.CornerRadiusProperty,new CornerRadius(9));
        border.SetValue(Border.BackgroundProperty,new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty,new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty,new TemplateBindingExtension(Control.BorderThicknessProperty));
        border.SetValue(Border.PaddingProperty,new TemplateBindingExtension(Control.PaddingProperty));
        var content=new FrameworkElementFactory(typeof(ScrollViewer));content.Name="PART_ContentHost";border.AppendChild(content);
        control.Template=new ControlTemplate(control.GetType()){VisualTree=border};
    }
    public static Style ComboStyle()=>(Style)XamlReader.Parse("""
      <Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="ComboBox">
       <Setter Property="FontSize" Value="12"/><Setter Property="Foreground" Value="#1C1C1E"/><Setter Property="Background" Value="#F4F6FA"/>
       <Setter Property="Padding" Value="10,7"/><Setter Property="BorderBrush" Value="#E0E5ED"/><Setter Property="BorderThickness" Value="1"/>
       <Setter Property="ItemContainerStyle"><Setter.Value><Style TargetType="ComboBoxItem">
        <Setter Property="Padding" Value="10,7"/><Setter Property="FontSize" Value="12"/>
        <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="ComboBoxItem"><Border x:Name="Item" Background="Transparent" CornerRadius="6" Padding="{TemplateBinding Padding}"><ContentPresenter/></Border><ControlTemplate.Triggers><Trigger Property="IsHighlighted" Value="True"><Setter TargetName="Item" Property="Background" Value="#EAF2FF"/></Trigger><Trigger Property="IsSelected" Value="True"><Setter TargetName="Item" Property="Background" Value="#EAF2FF"/><Setter Property="Foreground" Value="#007AFF"/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
       </Style></Setter.Value></Setter>
       <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="ComboBox">
        <Grid>
         <ToggleButton Focusable="False" IsChecked="{Binding IsDropDownOpen,RelativeSource={RelativeSource TemplatedParent},Mode=TwoWay}" ClickMode="Press">
          <ToggleButton.Template><ControlTemplate TargetType="ToggleButton"><Border Background="{Binding Background,RelativeSource={RelativeSource AncestorType=ComboBox}}" BorderBrush="#E0E5ED" BorderThickness="1" CornerRadius="9"><Grid><TextBlock Text="⌄" Foreground="#8E8E93" HorizontalAlignment="Right" VerticalAlignment="Center" Margin="0,0,10,0"/></Grid></Border></ControlTemplate></ToggleButton.Template>
         </ToggleButton>
         <ContentPresenter IsHitTestVisible="False" Margin="10,6,26,6" VerticalAlignment="Center" Content="{TemplateBinding SelectionBoxItem}" ContentTemplate="{TemplateBinding SelectionBoxItemTemplate}"/>
         <Popup x:Name="PART_Popup" Placement="Bottom" IsOpen="{TemplateBinding IsDropDownOpen}" AllowsTransparency="True" Focusable="False" PopupAnimation="Fade">
          <Border Background="White" BorderBrush="#E0E5ED" BorderThickness="1" CornerRadius="9" Padding="4" MinWidth="{Binding ActualWidth,RelativeSource={RelativeSource TemplatedParent}}"><ScrollViewer MaxHeight="320"><ItemsPresenter KeyboardNavigation.DirectionalNavigation="Contained"/></ScrollViewer></Border>
         </Popup>
        </Grid>
        <ControlTemplate.Triggers><Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.45"/></Trigger></ControlTemplate.Triggers>
       </ControlTemplate></Setter.Value></Setter>
      </Style>
      """);
}
