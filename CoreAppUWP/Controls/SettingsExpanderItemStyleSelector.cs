using CommunityToolkit.WinUI.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CoreAppUWP.Controls
{
    public partial class SettingsExpanderItemStyleSelector : CommunityToolkit.WinUI.Controls.SettingsExpanderItemStyleSelector
    {
        public Style GridStyle { get; set; }
        public Style BorderStyle { get; set; }
        public Style StackPanelStyle { get; set; }

        protected override Style SelectStyleCore(object item, DependencyObject container) =>
            container switch
            {
                SettingsCard card => card.IsClickEnabled ? ClickableStyle : DefaultStyle,
                Grid => GridStyle,
                Border => BorderStyle,
                StackPanel => StackPanelStyle,
                FrameworkElement element => element.Style,
                _ => null
            };
    }
}
