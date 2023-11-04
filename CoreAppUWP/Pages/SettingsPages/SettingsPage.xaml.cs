using CommunityToolkit.WinUI.UI.Controls;
using CoreAppUWP.Common;
using CoreAppUWP.Helpers;
using CoreAppUWP.ViewModels.SettingsPages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Search;
using Windows.Storage;
using Windows.System;
using Windows.UI.ApplicationSettings;
using Windows.UI.ViewManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CoreAppUWP.Pages.SettingsPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        #region Provider

        /// <summary>
        /// Identifies the <see cref="Provider"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ProviderProperty =
            DependencyProperty.Register(
                nameof(Provider),
                typeof(SettingsViewModel),
                typeof(SettingsPage),
                null);

        /// <summary>
        /// Get the <see cref="SettingsViewModel"/> of current <see cref="Page"/>.
        /// </summary>
        public SettingsViewModel Provider
        {
            get => (SettingsViewModel)GetValue(ProviderProperty);
            private set => SetValue(ProviderProperty, value);
        }

        #endregion

        public SettingsPage() => InitializeComponent();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Provider ??= SettingsViewModel.Caches.TryGetValue(DispatcherQueue, out SettingsViewModel provider) ? provider : new SettingsViewModel(DispatcherQueue);
            _ = Refresh();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as FrameworkElement).Tag as string)
            {
                case "Reset":
                    SettingsHelper.LocalObject.Clear();
                    SettingsHelper.SetDefaultSettings();
                    if (Reset.Flyout is Flyout flyout_reset)
                    {
                        flyout_reset.Hide();
                    }
                    _ = Refresh(true);
                    break;
                case "OutPIP" when ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.Default):
                    _ = ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
                    break;
                case "EnterPIP" when ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay):
                    _ = ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay);
                    break;
                case "NewWindow":
                    bool IsExtendsTitleBar = Provider.IsExtendsTitleBar;
                    _ = await WindowHelper.CreateWindowAsync(window =>
                    {
                        if (IsExtendsTitleBar)
                        {
                            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
                        }
                        Frame _frame = new();
                        window.Content = _frame;
                        ThemeHelper.Initialize(window);
                        _ = _frame.Navigate(typeof(MainPage), null, new DrillInNavigationTransitionInfo());
                    });


                    break;
                case "NewDesktopWindow":
                    var window = AppWindow.Create();
                    window.Show();
                    window.Closing += Window_Closing;
                    break;
                case "SearchFlyout" when SettingsPaneRegister.IsSearchPaneSupported:
                    SearchPane.GetForCurrentView().Show();
                    break;
                case "SettingsFlyout" when SettingsPaneRegister.IsSettingsPaneSupported:
                    SettingsPane.Show();
                    break;
                default:
                    break;
            }
        }

        private void Window_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            sender.Destroy();
        }

        private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as FrameworkElement).Tag.ToString())
            {
                case "LogFolder":
                    _ = Launcher.LaunchFolderAsync(await ApplicationData.Current.LocalFolder.CreateFolderAsync("MetroLogs", CreationCollisionOption.OpenIfExists));
                    break;
                case "WindowsColor":
                    _ = Launcher.LaunchUriAsync(new Uri("ms-settings:colors"));
                    break;
                default:
                    break;
            }
        }

        public Task Refresh(bool reset = false) => Provider.Refresh(reset);

        private void MarkdownText_LinkClicked(object sender, LinkClickedEventArgs e) => _ = Launcher.LaunchUriAsync(new Uri(e.Link));
    }
}
