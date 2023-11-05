using CoreAppUWP.Common;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Content;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.Win32.Foundation;
using Windows.Win32.System.WinRT;
using WinRT;

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
        public static async Task<bool> CreateWindowAsync(Action<Window> launched)
        {
            CoreApplicationView newView = CoreApplication.CreateNewView();
            int newViewId = await newView.Dispatcher.AwaitableRunAsync(() =>
            {
                Window newWindow = Window.Current;
                newWindow.TrackWindow();
                launched(newWindow);
                Window.Current.Activate();
                return ApplicationView.GetForCurrentView().Id;
            });
            return await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
        }



        public static Task<AppWindow> CreateAppWindowAsync(Action<DesktopChildSiteBridge, Compositor> launched)
        {
            TaskCompletionSource<AppWindow> taskCompletionSource = new();

            Thread thread = new(async () =>
            {
                try
                {
                    DispatcherQueueController controller = DispatcherQueueController.CreateOnCurrentThread();
                    DispatcherQueue dispatcherQueue = controller.DispatcherQueue;
                    AppWindow window = AppWindow.Create();
                    window.AssociateWithDispatcherQueue(dispatcherQueue);
                    taskCompletionSource.SetResult(window);
                    window.Destroying += (sender, args) => dispatcherQueue.EnqueueEventLoopExit();
                    Compositor compositor = new();
                    DesktopChildSiteBridge bridge = DesktopChildSiteBridge.Create(compositor, window.Id);
                    bridge.Show();
                    bridge.ResizePolicy = ContentSizePolicy.ResizeContentToParentWindow;
                    launched(bridge, compositor);
                    dispatcherQueue.RunEventLoop();
                    await controller.ShutdownQueueAsync();
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            });
            thread.Start();

            return taskCompletionSource.Task;
        }

        public static void TrackWindow(this Window window)
        {
            if (!ActiveWindows.ContainsKey(window.Dispatcher))
            {
                SettingsPaneRegister register = SettingsPaneRegister.Register(window);
                window.Closed += (sender, args) =>
                {
                    ActiveWindows.Remove(window.Dispatcher);
                    register.Unregister();
                    window = null;
                };
                ActiveWindows[window.Dispatcher] = window;
                BackdropHelper.RegisterWindow(window);
            }
        }

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

        public static Dictionary<CoreDispatcher, Window> ActiveWindows { get; } = [];
        public static Dictionary<CoreWindow, AppWindow> ActiveAppWindows { get; } = [];
    }
}
