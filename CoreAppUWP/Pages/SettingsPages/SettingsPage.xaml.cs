using CommunityToolkit.WinUI.UI.Controls;
using CoreAppUWP.Common;
using CoreAppUWP.Controls;
using CoreAppUWP.Helpers;
using CoreAppUWP.ViewModels.SettingsPages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Search;
using Windows.Storage;
using Windows.System;
using Windows.UI.ApplicationSettings;
using Windows.UI.ViewManagement;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CoreAppUWP.Pages.SettingsPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public bool IsCoreWindow => Dispatcher != null;

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
            switch ((sender as FrameworkElement).Tag?.ToString())
            {
                case "Reset":
                    SettingsHelper.LocalObject.Clear();
                    SettingsHelper.SetDefaultSettings();
                    if (Reset.Flyout is Flyout flyout_reset)
                    { flyout_reset.Hide(); }
                    _ = Refresh(true);
                    break;
                case "ExitPIP":
                    if (IsCoreWindow)
                    {
                        ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.Default);
                        _ = ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
                    }
                    else if (this.GetWindowForElement() is DesktopWindow desktopWindow)
                    { desktopWindow.AppWindow.SetPresenter(AppWindowPresenterKind.Default); }
                    break;
                case "EnterPIP":
                    if (IsCoreWindow)
                    {
                        ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay);
                        _ = ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay);
                    }
                    else if (this.GetWindowForElement() is DesktopWindow desktopWindow)
                    { desktopWindow.AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay); }
                    break;
                case "NewWindow":
                    bool isProcessKept = Provider.IsProcessKept;
                    _ = await WindowHelper.CreateWindowAsync(window =>
                    {
                        if (SettingsHelper.Get<bool>(SettingsHelper.IsExtendsTitleBar))
                        { CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true; }
                        Frame _frame = new();
                        window.Content = _frame;
                        ThemeHelper.Initialize(window);
                        NavigationTransitionInfo transitionInfo = null;
                        if (!isProcessKept) { try { transitionInfo = new DrillInNavigationTransitionInfo(); } catch { } }
                        _ = _frame.Navigate(typeof(MainPage), null, transitionInfo);
                        BackdropHelper.SetBackdrop(window, SettingsHelper.Get<BackdropType>(SettingsHelper.SelectedBackdrop));
                    });
                    break;
                case "NewAppWindow":
                    isProcessKept = Provider.IsProcessKept;
                    DesktopWindow window = await (IsCoreWindow
                        ? WindowHelper.CreateDesktopWindowAsync(OnLaunched)
                        : DispatcherQueue.CreateDesktopWindowAsync(OnLaunched)).ConfigureAwait(false);
                    void OnLaunched(DesktopWindowXamlSource source)
                    {
                        Frame _frame = new();
                        source.Content = _frame;
                        NavigationTransitionInfo transitionInfo = null;
                        if (!isProcessKept) { try { transitionInfo = new DrillInNavigationTransitionInfo(); } catch { } }
                        _ = _frame.Navigate(typeof(MainPage), null, transitionInfo);
                    }
                    if (AppWindowTitleBar.IsCustomizationSupported()
                        && SettingsHelper.Get<bool>(SettingsHelper.IsExtendsTitleBar))
                    { window.ExtendsContentIntoTitleBar = true; }
                    ThemeHelper.Initialize(window);
                    BackdropHelper.SetBackdrop(window, SettingsHelper.Get<BackdropType>(SettingsHelper.SelectedBackdrop));
                    AppWindow appWindow = window.AppWindow;
                    appWindow.Title = Package.Current.DisplayName;
                    appWindow.SetIcon("favicon.ico");
                    window.Activate();
                    break;
                case "SearchFlyout" when SettingsPaneRegister.IsSearchPaneSupported:
                    SearchPane.GetForCurrentView().Show();
                    break;
                case "ExitFullWindow":
                    if (IsCoreWindow)
                    { ApplicationView.GetForCurrentView().ExitFullScreenMode(); }
                    else if (this.GetWindowForElement() is DesktopWindow desktopWindow)
                    { desktopWindow.AppWindow.SetPresenter(AppWindowPresenterKind.Default); }
                    break;
                case "SettingsFlyout" when SettingsPaneRegister.IsSettingsPaneSupported:
                    SettingsPane.Show();
                    break;
                case "EnterFullWindow":
                    if (IsCoreWindow)
                    { ApplicationView.GetForCurrentView().TryEnterFullScreenMode(); }
                    else if (this.GetWindowForElement() is DesktopWindow desktopWindow)
                    { desktopWindow.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen); }
                    break;
                case "KeepProcess" when IsCoreWindow:
                    Provider.KeepProcess();
                    break;
                default:
                    break;
            }
        }

        private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            string tag = (sender as FrameworkElement).Tag?.ToString();
            if (!IsCoreWindow && WindowHelper.ActiveWindows.Values.FirstOrDefault()?.DispatcherQueue is DispatcherQueue dispatcherQueue)
            {
                await dispatcherQueue.ResumeForegroundAsync();
            }
            _ = tag switch
            {
                "LogFolder" => Launcher.LaunchFolderAsync(await ApplicationData.Current.LocalFolder.CreateFolderAsync("MetroLogs", CreationCollisionOption.OpenIfExists)),
                _ => Launcher.LaunchUriAsync(new Uri(tag)),
            };
        }

        public Task Refresh(bool reset = false) => Provider.Refresh(reset);

        private void MarkdownText_LinkClicked(object sender, LinkClickedEventArgs e) => _ = Launcher.LaunchUriAsync(new Uri(e.Link));
    }
}
