using CoreAppUWP.WinRT;
using Microsoft.UI.Content;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAppUWP.Controls
{
    public partial class DesktopWindow
    {
        /// <summary>
        /// Gets the <see cref="AppWindow"/> associated with this XAML Window.
        /// </summary>
        public AppWindow AppWindow { get; private set; }

        /// <summary>
        /// Gets or sets the visual root of an application window.
        /// </summary>
        public UIElement Content
        {
            get => WindowXamlSource.Content;
            set => WindowXamlSource.Content = value;
        }

        /// <summary>
        /// Gets the <see cref="DispatcherQueue"/> object for the window.
        /// </summary>
        public DispatcherQueue DispatcherQueue => WindowXamlSource.SiteBridge.DispatcherQueue;

        /// <summary>
        /// Gets or sets a value that specifies whether the default title bar of the window should be hidden to create space for app content.
        /// </summary>
        public bool ExtendsContentIntoTitleBar
        {
            get => AppWindow.TitleBar.ExtendsContentIntoTitleBar;
            set
            {
                AppWindow.TitleBar.ExtendsContentIntoTitleBar = value;
                Refresh();
            }
        }

        /// <summary>
        /// Gets or sets the system backdrop to apply to this Window. The backdrop is rendered behind the Window content.
        /// </summary>
        public SystemBackdrop SystemBackdrop
        {
            get => WindowXamlSource.SystemBackdrop;
            set => WindowXamlSource.SystemBackdrop = value;
        }

        /// <summary>
        /// Gets or sets a string used for the window title.
        /// </summary>
        public string Title
        {
            get => AppWindow.Title;
            set => AppWindow.Title = value;
        }

        /// <summary>
        /// Gets a value that reports whether the window is visible.
        /// </summary>
        public bool Visible => AppWindow.IsVisible;

        /// <summary>
        /// Gets or sets the XamlRoot in which this element is being viewed.
        /// </summary>
        public XamlRoot XamlRoot
        {
            get => WindowXamlSource.Content.XamlRoot;
            set => WindowXamlSource.Content.XamlRoot = value;
        }

        /// <summary>
        /// Gets the <see cref="DesktopWindowXamlSource"/> to provide XAML for this window.
        /// </summary>
        public DesktopWindowXamlSource WindowXamlSource { get; private set; }

        /// <summary>
        /// Attempts to activate the application window by bringing it to the foreground and setting the input focus to it.
        /// </summary>
        public void Activate()
        {
            AppWindow.Show();
            AppWindow.MoveInZOrderAtTop();
        }

        /// <summary>
        /// Closes the application window.
        /// </summary>
        public void Close() => AppWindow.Destroy();

        /// <summary>
        /// Refresh the <see cref="WindowXamlSource"/>.
        /// </summary>
        public void Refresh()
        {
            DesktopChildSiteBridge bridge = WindowXamlSource.SiteBridge;
            bridge.ResizePolicy = ContentSizePolicy.None;
            bridge.ResizePolicy = ContentSizePolicy.ResizeContentToParentWindow;
        }
    }

    public partial class DesktopWindow
    {
        /// <summary>
        /// Create a new <see cref="DesktopWindow"/> instance.
        /// </summary>
        /// <param name="launched">Do something after <see cref="DesktopWindowXamlSource"/> created.</param>
        /// <returns>The new instance of <see cref="DesktopWindow"/>.</returns>
        public static Task<DesktopWindow> CreateAsync(Action<DesktopWindowXamlSource> launched)
        {
            TaskCompletionSource<DesktopWindow> taskCompletionSource = new();

            new Thread(async () =>
            {
                try
                {
                    HookWindowingModel hook = new();
                    hook.StartHook();
                    DispatcherQueueController controller = DispatcherQueueController.CreateOnCurrentThread();
                    DispatcherQueue dispatcherQueue = controller.DispatcherQueue;
                    AppWindow window = AppWindow.Create();
                    window.AssociateWithDispatcherQueue(dispatcherQueue);
                    TrackWindow(window);
                    DesktopWindowXamlSource source = new();
                    source.Initialize(window.Id);
                    DesktopChildSiteBridge bridge = source.SiteBridge;
                    bridge.ResizePolicy = ContentSizePolicy.ResizeContentToParentWindow;
                    window.Changed += (sender, args) =>
                    {
                        if (args.DidPresenterChange)
                        {
                            bridge.ResizePolicy = ContentSizePolicy.None;
                            bridge.ResizePolicy = ContentSizePolicy.ResizeContentToParentWindow;
                        }
                    };
                    bridge.Show();
                    launched(source);
                    hook.EndHook();
                    DesktopWindow desktopWindow = new()
                    {
                        AppWindow = window,
                        WindowXamlSource = source
                    };
                    taskCompletionSource.SetResult(desktopWindow);
                    dispatcherQueue.RunEventLoop();
                    await controller.ShutdownQueueAsync();
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            })
            {
                Name = nameof(DesktopWindowXamlSource)
            }.Start();

            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Create a new <see cref="DesktopWindow"/> instance.
        /// </summary>
        /// <param name="dispatcherQueue">The <see cref="DispatcherQueue"/> to provide thread.</param>
        /// <param name="launched">Do something after <see cref="DesktopWindowXamlSource"/> created.</param>
        /// <returns>The new instance of <see cref="DesktopWindow"/>.</returns>
        public static Task<DesktopWindow> CreateAsync(DispatcherQueue dispatcherQueue, Action<DesktopWindowXamlSource> launched)
        {
            TaskCompletionSource<DesktopWindow> taskCompletionSource = new();

            _ = dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    HookWindowingModel hook = new();
                    hook.StartHook();
                    AppWindow window = AppWindow.Create();
                    window.AssociateWithDispatcherQueue(dispatcherQueue);
                    TrackWindow(window);
                    DesktopWindowXamlSource source = new();
                    source.Initialize(window.Id);
                    DesktopChildSiteBridge bridge = source.SiteBridge;
                    bridge.ResizePolicy = ContentSizePolicy.ResizeContentToParentWindow;
                    window.Changed += (sender, args) =>
                    {
                        if (args.DidPresenterChange)
                        {
                            bridge.ResizePolicy = ContentSizePolicy.None;
                            bridge.ResizePolicy = ContentSizePolicy.ResizeContentToParentWindow;
                        }
                    };
                    bridge.Show();
                    launched(source);
                    hook.EndHook();
                    DesktopWindow desktopWindow = new()
                    {
                        AppWindow = window,
                        WindowXamlSource = source
                    };
                    taskCompletionSource.SetResult(desktopWindow);
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            });

            return taskCompletionSource.Task;
        }

        private static void TrackWindow(AppWindow window)
        {
            if (ActiveDesktopWindows.ContainsKey(window.DispatcherQueue))
            {
                ActiveDesktopWindows[window.DispatcherQueue] += 1;
            }
            else
            {
                ActiveDesktopWindows[window.DispatcherQueue] = 1;
            }
            window.Destroying -= AppWindow_Destroying;
            window.Destroying += AppWindow_Destroying;
        }

        private static void AppWindow_Destroying(AppWindow sender, object args)
        {
            if (ActiveDesktopWindows.TryGetValue(sender.DispatcherQueue, out ulong num))
            {
                num--;
                if (num == 0)
                {
                    ActiveDesktopWindows.Remove(sender.DispatcherQueue);
                    sender.DispatcherQueue.EnqueueEventLoopExit();
                    return;
                }
                ActiveDesktopWindows[sender.DispatcherQueue] = num;
            }
        }

        private static Dictionary<DispatcherQueue, ulong> ActiveDesktopWindows { get; } = [];
    }
}
