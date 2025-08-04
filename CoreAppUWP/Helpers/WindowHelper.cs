using CoreAppUWP.Common;
using CoreAppUWP.Controls;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace CoreAppUWP.Helpers
{
    /// <summary>
    /// Helpers class to allow the app to find the Window that contains an
    /// arbitrary <see cref="UIElement"/> (<see cref="GetWindowForElement(UIElement)"/>).
    /// To do this, we keep track of all active Windows. The app code must call
    /// <see cref="CreateWindowAsync(Action{Window})"/> rather than "new <see cref="Window"/>()"
    /// so we can keep track of all the relevant windows.
    /// </summary>
    public static class WindowHelper
    {
#pragma warning disable CA1416
        [SupportedOSPlatformGuard("Windows10.0.18362.0")]
        public static bool IsAppWindowSupported { get; } = ApiInformation.IsTypePresent("Windows.UI.WindowManagement.AppWindow");

        [SupportedOSPlatformGuard("Windows10.0.18362.0")]
        public static bool IsXamlRootSupported { get; } = ApiInformation.IsPropertyPresent("Windows.UI.Xaml.UIElement", "XamlRoot");
#pragma warning restore CA1416

        public static bool IsCoreWindow { get; } = Program.IsCoreWindow;

        public static bool IsPackagedApp { get; } = Program.IsPackagedApp;

        public static async Task<bool> CreateWindowAsync(Action<Window> launched)
        {
            CoreApplicationView newView = CoreApplication.CreateNewView();
            int newViewId = await newView.Dispatcher.AwaitableRunAsync(() =>
            {
                Window window = Window.Current;
                TrackWindow(window);
                launched(window);
                window.Activate();
                return ApplicationView.GetForCurrentView().Id;
            });
            return await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
        }

        [SupportedOSPlatform("Windows10.0.18362.0")]
        public static async Task<(AppWindow, Frame)> CreateWindowAsync()
        {
            Frame frame = new();
            AppWindow window = await AppWindow.TryCreateAsync();
            ElementCompositionPreview.SetAppWindowContent(window, frame);
            window.TrackWindow(frame);
            return (window, frame);
        }

        public static async Task<DesktopWindow> CreateWindowAsync(Action<DesktopWindowXamlSource> launched)
        {
            DesktopWindow window = await DesktopWindow.CreateAsync(launched).ConfigureAwait(false);
            TrackWindow(window);
            return window;
        }

        public static void TrackWindow(this Window window)
        {
            if (!ActiveWindows.ContainsKey(window.Dispatcher))
            {
                SettingsPaneRegister.Register(window);
                window.Closed += (sender, args) =>
                {
                    ActiveWindows.Remove(window.Dispatcher);
                    SettingsPaneRegister.Unregister(window);
                    window = null;
                };
                ActiveWindows[window.Dispatcher] = window;
            }
        }

        public static void TrackWindow(this DesktopWindow window)
        {
            if (!ActiveDesktopWindows.ContainsKey(window.Dispatcher))
            {
                window.Closing += (sender, args) =>
                {
                    ActiveDesktopWindows.Remove(window.Dispatcher);
                    window = null;
                };
                ActiveDesktopWindows[window.Dispatcher] = window;
            }
        }

        [SupportedOSPlatform("Windows10.0.18362.0")]
        public static void TrackWindow(this AppWindow window, Frame frame)
        {
            if (!ActiveAppWindows.TryGetValue(frame.Dispatcher, out Dictionary<XamlRoot, AppWindow> windows))
            {
                ActiveAppWindows[frame.Dispatcher] = windows = [];
            }

            if (!windows.ContainsKey(frame.XamlRoot))
            {
                window.Closed += (sender, args) =>
                {
                    windows.Remove(frame.XamlRoot);
                    if (windows.Count <= 0)
                    { ActiveAppWindows.Remove(frame.Dispatcher); }
                    frame.Content = null;
                    window = null;
                };
                windows[frame.XamlRoot] = window;
            }
        }

        [SupportedOSPlatformGuard("Windows10.0.18362.0")]
        public static bool IsAppWindow(this UIElement element) =>
            IsAppWindowSupported
            && element?.XamlRoot is XamlRoot xamlRoot
            && ActiveAppWindows.TryGetValue(element.Dispatcher, out Dictionary<XamlRoot, AppWindow> windows)
            && windows.ContainsKey(xamlRoot);

        public static AppWindow GetWindowForElement(this UIElement element) =>
            IsAppWindowSupported
            && element?.XamlRoot is XamlRoot xamlRoot
            && ActiveAppWindows.TryGetValue(element.Dispatcher, out Dictionary<XamlRoot, AppWindow> windows)
            && windows.TryGetValue(xamlRoot, out AppWindow window)
                ? window : null;

        [SupportedOSPlatform("Windows10.0.18362.0")]
        public static UIElement GetXamlRootForWindow(this AppWindow window) =>
            ElementCompositionPreview.GetAppWindowContent(window);

        public static double GetRasterizationScale(this UIElement element) =>
            IsXamlRootSupported && element.XamlRoot is XamlRoot xamlRoot
                ? xamlRoot.RasterizationScale
                : DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

        public static Dictionary<CoreDispatcher, Window> ActiveWindows { get; } = [];
        public static Dictionary<CoreDispatcher, DesktopWindow> ActiveDesktopWindows { get; } = [];
        [SupportedOSPlatform("Windows10.0.18362.0")]
        public static Dictionary<CoreDispatcher, Dictionary<XamlRoot, AppWindow>> ActiveAppWindows { get; } = IsAppWindowSupported ? [] : null;
    }
}
