using CoreAppUWP.Common;
using CoreAppUWP.Controls;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.Win32.Foundation;
using Windows.Win32.System.WinRT;
using WinRT;
using DispatcherQueueController = Microsoft.UI.Dispatching.DispatcherQueueController;

namespace CoreAppUWP.Helpers
{
    /// <summary>
    /// Helpers class to allow the app to find the Window that contains an
    /// arbitrary <see cref="UIElement"/> (GetWindowForElement(UIElement)).
    /// To do this, we keep track of all active Windows. The app code must call
    /// <see cref="CreateWindowAsync(Action{Window})"/> rather than "new <see cref="Window"/>()"
    /// so we can keep track of all the relevant windows.
    /// </summary>
    public static class WindowHelper
    {
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

        public static Window CreateWindow()
        {
            Window window = new();
            TrackWindow(window);
            return window;
        }

        public static Task<Window> CreateWindowAsync()
        {
            TaskCompletionSource<Window> taskCompletionSource = new();
            new Thread(() =>
            {
                DispatcherQueueController controller;
                HookWindowingModel hook = null;
                if (IsCoreWindow)
                {
                    hook = new HookWindowingModel();
                }

                try
                {
                    controller = DispatcherQueueController.CreateOnCurrentThread();
                    WindowsXamlManager.InitializeForCurrentThread();
                }
                finally
                {
                    hook?.Dispose();
                }

                Window window = new();
                TrackWindow(window);
                taskCompletionSource.SetResult(window);

                controller.DispatcherQueue.RunEventLoop();
            })
            {
                Name = nameof(Window)
            }.Start();
            return taskCompletionSource.Task;
        }

        public static async Task<DesktopWindow> CreateWindowAsync(Action<DesktopWindowXamlSource> launched)
        {
            DesktopWindow window = await DesktopWindow.CreateAsync(launched).ConfigureAwait(false);
            TrackWindow(window);
            return window;
        }

        public static async Task<DesktopWindow> CreateWindowAsync(this DispatcherQueue dispatcherQueue, Action<DesktopWindowXamlSource> launched)
        {
            DesktopWindow window = await DesktopWindow.CreateAsync(dispatcherQueue, launched);
            TrackWindow(window);
            return window;
        }

        public static void TrackWindow(this Window window)
        {
            if (!ActiveWindows.Contains(window))
            {
                SettingsPaneRegister.Register(window);
                window.Closed += (sender, args) =>
                {
                    ActiveWindows.Remove(window);
                    SettingsPaneRegister.Unregister(window);
                    window = null;
                };
                ActiveWindows.Add(window);
                BackdropHelper.Register(window);
            }
        }

        public static void TrackWindow(this DesktopWindow window)
        {
            if (!ActiveDesktopWindows.ContainsKey(window.XamlRoot))
            {
                window.AppWindow.Closing += (sender, args) =>
                {
                    ActiveDesktopWindows.Remove(window.XamlRoot);
                    window = null;
                };
                ActiveDesktopWindows[window.XamlRoot] = window;
                BackdropHelper.Register(window);
            }
        }

        public static Window GetWindowForElement(this UIElement element)
        {
            if (element.XamlRoot != null)
            {
                foreach (Window window in ActiveWindows)
                {
                    if (window.DispatcherQueue.HasThreadAccess && element.XamlRoot == window.Content.XamlRoot)
                    {
                        return window;
                    }
                }
            }
            return null;
        }

        public static DesktopWindow GetDesktopWindowForElement(this UIElement element) =>
            ActiveDesktopWindows.TryGetValue(element.XamlRoot, out DesktopWindow window) ? window : null;

        public static AppWindow GetAppWindow(this CoreWindow window)
        {
            if (!ActiveAppWindows.TryGetValue(window, out AppWindow appWindow))
            {
                HWND handle = window.As<ICoreWindowInterop>().WindowHandle;
                WindowId id = Win32Interop.GetWindowIdFromWindow(handle);
                appWindow = AppWindow.GetFromWindowId(id);
                window.Closed += (sender, args) =>
                {
                    ActiveAppWindows.Remove(window);
                    window = null;
                };
                ActiveAppWindows[window] = appWindow;
            }
            return appWindow;
        }

        public static HashSet<Window> ActiveWindows { get; } = [];
        public static Dictionary<CoreWindow, AppWindow> ActiveAppWindows { get; } = [];
        public static Dictionary<XamlRoot, DesktopWindow> ActiveDesktopWindows { get; } = [];
    }
}

namespace Windows.Win32.System.WinRT
{
    file static class Extensions
    {
        extension(ICoreWindowInterop interop)
        {
            public unsafe HWND WindowHandle
            {
                get
                {
                    HWND hwnd = default;
                    interop.get_WindowHandle(&hwnd);
                    return hwnd;
                }
            }
        }
    }
}
