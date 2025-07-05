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
using System.Reflection;
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
        private readonly SettingsViewModel Provider;
        public bool IsCoreWindow => Dispatcher != null;

        public SettingsPage()
        {
            InitializeComponent();
            Provider ??= SettingsViewModel.Caches.TryGetValue(DispatcherQueue, out SettingsViewModel provider) ? provider : new SettingsViewModel(DispatcherQueue);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _ = Refresh();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element) { return; }
            switch (element.Tag?.ToString())
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
                        if (ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.Default))
                        { _ = ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default); }
                    }
                    else if (this.GetDesktopWindowForElement() is DesktopWindow _desktopWindow)
                    { _desktopWindow.AppWindow.SetPresenter(AppWindowPresenterKind.Default); }
                    else if (this.GetWindowForElement() is Window _window)
                    { _window.AppWindow.SetPresenter(AppWindowPresenterKind.Default); }
                    break;
                case "EnterPIP":
                    if (IsCoreWindow)
                    {
                        if (ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay))
                        { _ = ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay); }
                    }
                    else if (this.GetDesktopWindowForElement() is DesktopWindow _desktopWindow)
                    { _desktopWindow.AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay); }
                    else if (this.GetWindowForElement() is Window _window)
                    { _window.AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay); }
                    break;
                case "NewWindow" when WindowHelper.IsCoreWindow:
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
                case "NewWindow":
                    isProcessKept = Provider.IsProcessKept;
                    Window window = WindowHelper.CreateWindow();
                    AppWindow appWindow = window.AppWindow;
                    if (SettingsHelper.Get<bool>(SettingsHelper.IsExtendsTitleBar))
                    { appWindow.TitleBar.ExtendsContentIntoTitleBar = true; }
                    Frame _frame = new();
                    window.Content = _frame;
                    ThemeHelper.Initialize(window);
                    NavigationTransitionInfo transitionInfo = null;
                    if (!isProcessKept) { try { transitionInfo = new DrillInNavigationTransitionInfo(); } catch { } }
                    _ = _frame.Navigate(typeof(MainPage), null, transitionInfo);
                    BackdropHelper.SetBackdrop(window, SettingsHelper.Get<BackdropType>(SettingsHelper.SelectedBackdrop));
                    appWindow.Title = WindowHelper.IsPackagedApp ? Package.Current.DisplayName : Assembly.GetEntryAssembly().GetName().Name;
                    appWindow.SetIcon("favicon.ico");
                    window.Activate();
                    break;
                case "NewAppWindow":
                    isProcessKept = Provider.IsProcessKept;
                    DesktopWindow desktopWindow = IsCoreWindow || !WindowHelper.IsCoreWindow
                        ? await WindowHelper.CreateWindowAsync(OnLaunched).ConfigureAwait(false)
                        : await DispatcherQueue.CreateWindowAsync(OnLaunched);
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
                    { desktopWindow.ExtendsContentIntoTitleBar = true; }
                    ThemeHelper.Initialize(desktopWindow);
                    BackdropHelper.SetBackdrop(desktopWindow, SettingsHelper.Get<BackdropType>(SettingsHelper.SelectedBackdrop));
                    appWindow = desktopWindow.AppWindow;
                    appWindow.Title = WindowHelper.IsPackagedApp ? Package.Current.DisplayName : Assembly.GetEntryAssembly().GetName().Name;
                    appWindow.SetIcon("favicon.ico");
                    desktopWindow.Activate();
                    break;
                case "NewWin32Window":
                    isProcessKept = Provider.IsProcessKept;
                    window = IsCoreWindow || !WindowHelper.IsCoreWindow
                        ? await WindowHelper.CreateWindowAsync().ConfigureAwait(false)
                        : WindowHelper.CreateWindow();
                    appWindow = window.AppWindow;
                    if (SettingsHelper.Get<bool>(SettingsHelper.IsExtendsTitleBar))
                    { appWindow.TitleBar.ExtendsContentIntoTitleBar = true; }
                    _frame = new();
                    window.Content = _frame;
                    ThemeHelper.Initialize(window);
                    transitionInfo = null;
                    if (!isProcessKept) { try { transitionInfo = new DrillInNavigationTransitionInfo(); } catch { } }
                    _ = _frame.Navigate(typeof(MainPage), null, transitionInfo);
                    BackdropHelper.SetBackdrop(window, SettingsHelper.Get<BackdropType>(SettingsHelper.SelectedBackdrop));
                    appWindow.Title = WindowHelper.IsPackagedApp ? Package.Current.DisplayName : Assembly.GetEntryAssembly().GetName().Name;
                    appWindow.SetIcon("favicon.ico");
                    window.Activate();
                    break;
                case "SearchFlyout" when SettingsPaneRegister.IsSearchPaneSupported:
                    SearchPane.GetForCurrentView().Show();
                    break;
                case "ExitFullWindow":
                    if (IsCoreWindow)
                    { ApplicationView.GetForCurrentView().ExitFullScreenMode(); }
                    else if (this.GetDesktopWindowForElement() is DesktopWindow _desktopWindow)
                    { _desktopWindow.AppWindow.SetPresenter(AppWindowPresenterKind.Default); }
                    else if (this.GetWindowForElement() is Window _window)
                    { _window.AppWindow.SetPresenter(AppWindowPresenterKind.Default); }
                    break;
                case "SettingsFlyout" when SettingsPaneRegister.IsSettingsPaneSupported:
                    SettingsPane.Show();
                    break;
                case "EnterFullWindow":
                    if (IsCoreWindow)
                    { ApplicationView.GetForCurrentView().TryEnterFullScreenMode(); }
                    else if (this.GetDesktopWindowForElement() is DesktopWindow _desktopWindow)
                    { _desktopWindow.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen); }
                    else if (this.GetWindowForElement() is Window _window)
                    { _window.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen); }
                    break;
                case "KeepProcess" when IsCoreWindow:
                    Provider.KeepProcess();
                    break;
                default:
                    break;
            }
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element) { return; }
            switch (element.Tag?.ToString())
            {
                case "CleanLogs":
                    _ = Provider.CleanLogsAsync();
                    break;
                case "OpenLogFile":
                    _ = Provider.OpenLogFileAsync();
                    break;
                default:
                    break;
            }
        }

        private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            string tag = (sender as FrameworkElement).Tag?.ToString();
            if (!IsCoreWindow && WindowHelper.ActiveWindows.FirstOrDefault()?.DispatcherQueue is DispatcherQueue dispatcherQueue)
            {
                await dispatcherQueue.ResumeForegroundAsync();
            }
            _ = tag switch
            {
                "LogFolder" => Launcher.LaunchFolderAsync(await ApplicationData.Current.LocalFolder.CreateFolderAsync("Logs", CreationCollisionOption.OpenIfExists)),
                _ => Launcher.LaunchUriAsync(new Uri(tag)),
            };
        }

        public Task Refresh(bool reset = false) => Provider.Refresh(reset);
    }
}
