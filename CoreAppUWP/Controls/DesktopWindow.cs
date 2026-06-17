using CoreAppUWP.Common;
using CoreAppUWP.Helpers;
using Microsoft.UI.Windowing;
using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.WinRT.Xaml;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;

namespace CoreAppUWP.Controls
{
    public partial class DesktopWindow
    {
        [ThreadStatic]
        private static DesktopWindow current;
        public static DesktopWindow Current => current;

        /// <summary>
        /// Gets the <see cref="AppWindow"/> associated with this XAML Window.
        /// </summary>
        public AppWindow AppWindow { get; private init; }

        /// <summary>
        /// Gets or sets the visual root of an application window.
        /// </summary>
        public UIElement Content
        {
            get => WindowXamlSource.Content;
            set => WindowXamlSource.Content = value;
        }

        /// <summary>
        /// Gets the event dispatcher for the window.
        /// </summary>
        public CoreDispatcher Dispatcher { get; private init; }

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
        [SupportedOSPlatform("Windows10.0.18362.0")]
        public XamlRoot XamlRoot
        {
            get => WindowXamlSource.Content.XamlRoot;
            set => WindowXamlSource.Content.XamlRoot = value;
        }

        /// <summary>
        /// Gets the <see cref="DesktopWindowXamlSource"/> to provide XAML for this window.
        /// </summary>
        public DesktopWindowXamlSource WindowXamlSource { get; private init; }

        /// <summary>
        /// Occurs when a window is being closed through a system affordance.
        /// </summary>
        public event TypedEventHandler<AppWindow, AppWindowClosingEventArgs> Closing
        {
            add => AppWindow.Closing += value;
            remove => AppWindow.Closing -= value;
        }

        public DesktopWindow() => current = this;

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
            IDesktopWindowXamlSourceNative m_native = WindowXamlSource.As<IDesktopWindowXamlSourceNative>();
            SizeInt32 size = AppWindow.ClientSize;
            _ = PInvoke.SetWindowPos(
                m_native.WindowHandle,
                default,
                0, 0,
                size.Width, size.Height,
                SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
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

            new Thread(() =>
            {
                try
                {
                    DesktopWindowXamlSource source;
                    AppWindow window = AppWindow.Create();

                    HookWindowingModel hook = null;
                    if (WindowHelper.IsCoreWindow)
                    {
                        hook = new HookWindowingModel();
                    }

                    try
                    {
                        source = new DesktopWindowXamlSource();
                    }
                    finally
                    {
                        hook?.Dispose();
                    }

                    IDesktopWindowXamlSourceNative m_native = source.As<IDesktopWindowXamlSourceNative>();
                    m_native.AttachToWindow(new HWND((nint)window.Id.Value));

                    window.Changed += (sender, args) =>
                    {
                        if (args.DidPresenterChange || args.DidSizeChange || args.DidVisibilityChange)
                        {
                            SizeInt32 size = sender.ClientSize;
                            _ = PInvoke.SetWindowPos(
                                m_native.WindowHandle,
                                default,
                                0, 0,
                                size.Width, size.Height,
                                SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
                        }
                    };

                    CoreWindow coreWindow = CoreWindow.GetForCurrentThread();
                    CoreDispatcher dispatcher = coreWindow.Dispatcher;
                    window.Destroying += (sender, args) => coreWindow.Close();

                    launched(source);
                    DesktopWindow desktopWindow = new()
                    {
                        AppWindow = window,
                        WindowXamlSource = source,
                        Dispatcher = dispatcher
                    };
                    taskCompletionSource.SetResult(desktopWindow);

                    dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessUntilQuit);
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
        /// <param name="launched">Do something after <see cref="DesktopWindowXamlSource"/> created.</param>
        /// <returns>The new instance of <see cref="DesktopWindow"/>.</returns>
        public static DesktopWindow CreateMainWindow(Action<DesktopWindowXamlSource> launched)
        {
            AppWindow window = AppWindow.Create();
            DesktopWindowXamlSource source = new();

            IDesktopWindowXamlSourceNative m_native = source.As<IDesktopWindowXamlSourceNative>();
            m_native.AttachToWindow(new HWND((nint)window.Id.Value));
            
            window.Changed += (sender, args) =>
            {
                if (args.DidPresenterChange || args.DidSizeChange || args.DidVisibilityChange)
                {
                    SizeInt32 size = sender.ClientSize;
                    _ = PInvoke.SetWindowPos(
                        m_native.WindowHandle,
                        default,
                        0, 0,
                        size.Width, size.Height,
                        SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
                }
            };

            CoreWindow coreWindow = CoreWindow.GetForCurrentThread();
            window.Destroying += (sender, args) => coreWindow.Close();

            launched(source);
            DesktopWindow desktopWindow = new()
            {
                AppWindow = window,
                WindowXamlSource = source,
                Dispatcher = coreWindow.Dispatcher
            };
            return desktopWindow;
        }
    }
}

namespace Windows.Win32.System.WinRT.Xaml
{
    file static class Extensions
    {
        extension(IDesktopWindowXamlSourceNative source)
        {
            public HWND WindowHandle => source.get_WindowHandle();
        }
    }
}
