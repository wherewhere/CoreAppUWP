using CoreAppUWP.Common;
using CoreAppUWP.Helpers;
using CoreAppUWP.ViewModels.SettingsPages;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Search;
using Windows.Storage;
using Windows.System;
using Windows.UI.ApplicationSettings;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

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
            Provider ??= SettingsViewModel.Caches.TryGetValue(Dispatcher, out SettingsViewModel provider) ? provider : new SettingsViewModel(Dispatcher);
            _ = Refresh();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as FrameworkElement).Tag?.ToString())
            {
                case "Reset":
                    SettingsHelper.LocalObject.Values.Clear();
                    SettingsHelper.SetDefaultSettings();
                    if (Reset.Flyout is Flyout flyout_reset)
                    { flyout_reset.Hide(); }
                    _ = Refresh(true);
                    break;
                case "ExitPIP":
                    if (this.IsAppWindow())
                    { _ = this.GetWindowForElement().Presenter.RequestPresentation(AppWindowPresentationKind.Default); }
                    else if (ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.Default))
                    { _ = ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default); }
                    break;
                case "EnterPIP":
                    if (this.IsAppWindow())
                    { _ = this.GetWindowForElement().Presenter.RequestPresentation(AppWindowPresentationKind.CompactOverlay); }
                    else if (ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay))
                    { _ = ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay); }
                    break;
                case "NewWindow":
                    _ = await WindowHelper.CreateWindowAsync(window =>
                    {
                        if (SettingsHelper.Get<bool>(SettingsHelper.IsExtendsTitleBar))
                        { CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true; }
                        Frame _frame = new();
                        window.Content = _frame;
                        ThemeHelper.Initialize(window);
                        _ = _frame.Navigate(typeof(MainPage), null, new DrillInNavigationTransitionInfo());
                    });
                    break;
                case "NewAppWindow" when WindowHelper.IsAppWindowSupported:
                    (AppWindow appWindow, Frame frame) = await WindowHelper.CreateWindowAsync();
                    if (Provider.IsExtendsTitleBar)
                    {
                        appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                    }
                    ThemeHelper.Initialize(appWindow);
                    _ = frame.Navigate(typeof(MainPage), null, new DrillInNavigationTransitionInfo());
                    await appWindow.TryShowAsync();
                    break;
                case "SearchFlyout" when SettingsPaneRegister.IsSearchPaneSupported:
                    SearchPane.GetForCurrentView().Show();
                    break;
                case "ExitFullWindow":
                    if (this.IsAppWindow())
                    { _ = this.GetWindowForElement().Presenter.RequestPresentation(AppWindowPresentationKind.Default); }
                    else
                    { ApplicationView.GetForCurrentView().ExitFullScreenMode(); }
                    break;
                case "SettingsFlyout" when SettingsPaneRegister.IsSettingsPaneSupported:
                    SettingsPane.Show();
                    break;
                case "EnterFullWindow":
                    _ = this.IsAppWindow()
                        ? this.GetWindowForElement().Presenter.RequestPresentation(AppWindowPresentationKind.FullScreen)
                        : ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
                    break;
                default:
                    break;
            }
        }

        private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            string tag = (sender as FrameworkElement).Tag?.ToString();
            _ = tag switch
            {
                "LogFolder" => Launcher.LaunchFolderAsync(await ApplicationData.Current.LocalFolder.CreateFolderAsync("MetroLogs", CreationCollisionOption.OpenIfExists)),
                _ => Launcher.LaunchUriAsync(new Uri(tag)),
            };
        }

        public Task Refresh(bool reset = false) => Provider.Refresh(reset);
    }
}
