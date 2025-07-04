using CoreAppUWP.Controls;
using CoreAppUWP.Helpers;
using CoreAppUWP.Pages.SettingsPages;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.Graphics;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
#region using muxc = Microsoft.UI.Xaml.Controls;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewBackButtonVisible = Microsoft.UI.Xaml.Controls.NavigationViewBackButtonVisible;
using NavigationViewBackRequestedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewBackRequestedEventArgs;
using NavigationViewDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode;
using NavigationViewDisplayModeChangedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewDisplayModeChangedEventArgs;
using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using NavigationViewItemInvokedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs;
using NavigationViewPaneClosingEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewPaneClosingEventArgs;
using NavigationViewPaneDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode;
#endregion

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace CoreAppUWP.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly HashSet<(string Tag, Type Page)> _pages =
        [
            ("Home", typeof(HomePage))
        ];

        public MainPage()
        {
            InitializeComponent();
            TilesHelper.UpdateTile();
            NavigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            NavigationView_Navigate("Home", new EntranceNavigationTransitionInfo());
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (!this.IsAppWindow())
            {
                if (DesktopWindow.Current is DesktopWindow window)
                {
                    DragRegion.SizeChanged -= CustomTitleBar_SizeChanged;
                    window.AppWindow.Changed -= AppWindow_Changed;
                }
                else
                { Window.Current.SetTitleBar(null); }
                SystemNavigationManager.GetForCurrentView().BackRequested -= System_BackRequested;
                CoreApplication.GetCurrentView().TitleBar.LayoutMetricsChanged -= TitleBar_LayoutMetricsChanged;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!this.IsAppWindow())
            {
                if (DesktopWindow.Current is DesktopWindow window)
                {
                    DragRegion.SizeChanged += CustomTitleBar_SizeChanged;
                    window.AppWindow.Changed += AppWindow_Changed;
                    if (SettingsHelper.Get<bool>(SettingsHelper.IsExtendsTitleBar))
                    { window.ExtendsContentIntoTitleBar = true; }
                    ThemeHelper.UpdateSystemCaptionButtonColors(window);
                    CustomTitleBar_SizeChanged(DragRegion, null);
                }
                else
                {
                    Window.Current.SetTitleBar(DragRegion);
                    if (ApiInformation.IsMethodPresent("Windows.UI.Composition.Compositor", "TryCreateBlurredWallpaperBackdropBrush"))
                    { BackdropMaterial.SetApplyToRootOrPageBackground(this, true); }
                }
                SystemNavigationManager.GetForCurrentView().BackRequested += System_BackRequested;
                CoreApplication.GetCurrentView().TitleBar.LayoutMetricsChanged += TitleBar_LayoutMetricsChanged;
            }
        }

        private void TitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            UpdateAppTitle(sender);
        }

        private void AppWindow_Changed(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowChangedEventArgs args)
        {
            if (args.DidPresenterChange)
            {
                UpdateAppTitle(sender);
            }
        }

        private void NavigationView_Loaded(object sender, RoutedEventArgs e)
        {
            // Add handler for ContentFrame navigation.
            NavigationViewFrame.Navigated += On_Navigated;
            NavigationView.SelectedItem = NavigationView.MenuItems[0];
            NavigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.Auto;
        }

        private void NavigationView_Navigate(string NavItemTag, NavigationTransitionInfo TransitionInfo, object vs = null)
        {
            Type _page = null;

            (string Tag, Type Page) item = _pages.FirstOrDefault(p => p.Tag.Equals(NavItemTag, StringComparison.Ordinal));
            _page = item.Page;
            // Get the page type before navigation so you can prevent duplicate
            // entries in the back stack.
            Type PreNavPageType = NavigationViewFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (_page != null && !Equals(PreNavPageType, _page))
            {
                _ = NavigationViewFrame.Navigate(_page, vs, TransitionInfo);
            }
        }

        private void NavigationView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args) => _ = TryGoBack();

        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                _ = NavigationViewFrame.Navigate(typeof(SettingsPage), null, args.RecommendedNavigationTransitionInfo);
            }
            else if (args.InvokedItemContainer != null)
            {
                string NavItemTag = args.InvokedItemContainer.Tag.ToString();
                NavigationView_Navigate(NavItemTag, args.RecommendedNavigationTransitionInfo);
            }
        }

        private bool TryGoBack()
        {
            if (!NavigationViewFrame.CanGoBack)
            { return false; }

            // Don't go back if the nav pane is overlayed.
            if (NavigationView.IsPaneOpen &&
                (NavigationView.DisplayMode == NavigationViewDisplayMode.Compact ||
                 NavigationView.DisplayMode == NavigationViewDisplayMode.Minimal))
            { return false; }

            NavigationViewFrame.GoBack();
            return true;
        }

        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            if (sender is not Frame NavigationViewFrame) { return; }
            NavigationView.IsBackEnabled = NavigationViewFrame.CanGoBack;
            if (NavigationViewFrame.SourcePageType == typeof(SettingsPage))
            {
                // SettingsItem is not part of NavView.MenuItems, and doesn't have a Tag.
                NavigationView.SelectedItem = (NavigationViewItem)NavigationView.SettingsItem;
            }
            else if (NavigationViewFrame.SourcePageType != null)
            {
                (string Tag, Type Page) item = _pages.FirstOrDefault(p => p.Page == e.SourcePageType);
                if (item.Tag != null)
                {
                    NavigationViewItem SelectedItem = NavigationView.MenuItems
                        .OfType<NavigationViewItem>()
                        .FirstOrDefault(n => n.Tag.Equals(item.Tag))
                            ?? NavigationView.FooterMenuItems
                                .OfType<NavigationViewItem>()
                                .FirstOrDefault(n => n.Tag.Equals(item.Tag));
                    NavigationView.SelectedItem = SelectedItem;
                }
            }
        }

        private void NavigationViewControl_PaneClosing(NavigationView sender, NavigationViewPaneClosingEventArgs args)
        {
            UpdateTitlePaddingColumn();
        }

        private void NavigationViewControl_PaneOpening(NavigationView sender, object args)
        {
            UpdateTitlePaddingColumn();
        }

        private void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {
            UpdateLeftPaddingColumn();
        }

        private void UpdateLeftPaddingColumn()
        {
            LeftPaddingColumn.Width = NavigationView.IsBackButtonVisible == NavigationViewBackButtonVisible.Collapsed
                ? NavigationView.DisplayMode == NavigationViewDisplayMode.Minimal
                    ? NavigationView.IsPaneToggleButtonVisible
                        ? new GridLength(48)
                        : new GridLength(0)
                    : new GridLength(0)
                : NavigationView.DisplayMode == NavigationViewDisplayMode.Minimal
                    ? NavigationView.IsPaneToggleButtonVisible
                        ? new GridLength(88)
                        : new GridLength(44)
                    : new GridLength(44);
            UpdateTitlePaddingColumn();
        }

        private void UpdateTitlePaddingColumn()
        {
            TitlePaddingColumn.Width =
                NavigationView.IsBackButtonVisible != NavigationViewBackButtonVisible.Collapsed
                && NavigationView.DisplayMode != NavigationViewDisplayMode.Minimal
                && !NavigationView.IsPaneOpen
                    ? new GridLength(16)
                    : new GridLength(0);
        }

        private void UpdateAppTitle(CoreApplicationViewTitleBar coreTitleBar)
        {
            //ensure the custom title bar does not overlap window caption controls
            RightPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayRightInset);
        }

        private void UpdateAppTitle(Microsoft.UI.Windowing.AppWindow appWindow)
        {
            nint hwnd = (nint)appWindow.Id.Value;
            RightPaddingColumn.Width = new GridLength(Math.Max(0, appWindow.TitleBar.RightInset.GetDisplayPixel(hwnd)));
        }

        private void System_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = TryGoBack();
            }
        }

        private void CustomTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DesktopWindow.Current is DesktopWindow window)
            {
                nint hwnd = (nint)window.AppWindow.Id.Value;
                RectInt32 Rect = new((AppTitleBar.ActualWidth - DragRegion.ActualWidth).GetActualPixel(hwnd), 0, DragRegion.ActualWidth.GetActualPixel(hwnd), DragRegion.ActualHeight.GetActualPixel(hwnd));
                window.AppWindow?.TitleBar.SetDragRectangles([Rect]);
            }
        }
    }
}
