using CoreAppUWP.Helpers;
using CoreAppUWP.Pages.SettingsPages;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.Win32.System.WinRT;
using WinRT.Interop;
using WinRT;
using System.Runtime.InteropServices;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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
            SettingsHelper.CreateLogManager();
            NavigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            NavigationView_Navigate("Home", new EntranceNavigationTransitionInfo());
            SystemNavigationManager.GetForCurrentView().BackRequested += System_BackRequested;
            CoreApplication.GetCurrentView().TitleBar.LayoutMetricsChanged += TitleBar_LayoutMetricsChanged;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            SystemNavigationManager.GetForCurrentView().BackRequested -= System_BackRequested;
            CoreApplication.GetCurrentView().TitleBar.LayoutMetricsChanged -= TitleBar_LayoutMetricsChanged;
        }

        private void TitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            UpdateAppTitle(sender);
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
            UpdateLeftPaddingColumn();
        }

        private void NavigationViewControl_PaneOpening(NavigationView sender, object args)
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
                    : NavigationView.IsPaneOpen ? new GridLength(44) : new GridLength(60);
        }

        private void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {
            UpdateLeftPaddingColumn();
        }

        private void UpdateAppTitle(CoreApplicationViewTitleBar coreTitleBar)
        {
            //ensure the custom title bar does not overlap window caption controls
            RightPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayRightInset);
        }

        private void System_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = TryGoBack();
            }
        }
        [DllImport("Shcore.dll", SetLastError = true)]
        internal static extern int GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);

        internal enum Monitor_DPI_Type : int
        {
            MDT_Effective_DPI = 0,
            MDT_Angular_DPI = 1,
            MDT_Raw_DPI = 2,
            MDT_Default = MDT_Effective_DPI
        }

        private double GetScaleAdjustment(IntPtr hWnd)
        {
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            DisplayArea displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
            IntPtr hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

            // Get DPI.
            int result = GetDpiForMonitor(hMonitor, Monitor_DPI_Type.MDT_Default, out uint dpiX, out uint _);
            if (result != 0)
            {
                throw new Exception("Could not get DPI for monitor.");
            }

            uint scaleFactorPercent = (uint)(((long)dpiX * 100 + (96 >> 1)) / 96);
            return scaleFactorPercent / 100.0;
        }

        private void DragRegion_Loaded(object sender, RoutedEventArgs e)
        {

            var m_AppWindow = GetAppWindowForCurrentWindow();
            double scaleAdjustment = GetScaleAdjustment(GetWindowhWnd());

            List<Windows.Graphics.RectInt32> dragRectsList = new();

            Windows.Graphics.RectInt32 dragRect;
            dragRect.X = (int)(LeftPaddingColumn.ActualWidth * scaleAdjustment);
            dragRect.Y = 0;
            dragRect.Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment);
            dragRect.Width = (int)(DragColumn.ActualWidth * scaleAdjustment); ;
            dragRectsList.Add(dragRect);



            Windows.Graphics.RectInt32[] dragRects = dragRectsList.ToArray();

            m_AppWindow.TitleBar.SetDragRectangles(dragRects);
        }
        private Microsoft.UI.Windowing.AppWindow GetAppWindowForCurrentWindow()
        {
            WindowId wndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(GetWindowhWnd());
            return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(wndId);
        }
        private IntPtr GetWindowhWnd()
        {
            var appcoreWindow = CoreWindow.GetForCurrentThread();
            var interop = appcoreWindow.As<ICoreWindowInterop>();
            return interop.WindowHandle;
        }
    }
}
