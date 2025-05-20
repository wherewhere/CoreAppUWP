using CoreAppUWP.Common;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
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
            set => AppWindow.TitleBar.ExtendsContentIntoTitleBar = value;
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

                    using (HookWindowingModel hook = new())
                    {
                        source = new DesktopWindowXamlSource();
                    }

                    IDesktopWindowXamlSourceNative m_native = source.As<IDesktopWindowXamlSourceNative>();
                    m_native.AttachToWindow((nint)window.Id.Value);

                    window.Changed += (sender, args) =>
                    {
                        if (args.DidPresenterChange)
                        {
                            SizeInt32 size = sender.ClientSize;
                            _ = PInvoke.SetWindowPos(
                                m_native.WindowHandle(),
                                new HWND(),
                                0, 0,
                                size.Width, size.Height,
                                SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
                        }
                    };

                    launched(source);
                    DesktopWindow desktopWindow = new()
                    {
                        AppWindow = window,
                        WindowXamlSource = source,
                        Dispatcher = CoreWindow.GetForCurrentThread().Dispatcher
                    };
                    taskCompletionSource.SetResult(desktopWindow);

                    MSG msg = new();
                    while (msg.message != PInvoke.WM_QUIT)
                    {
                        if (PInvoke.PeekMessage(out msg, new HWND(), 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE))
                        { _ = PInvoke.DispatchMessage(msg); }
                    }
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
    }
}

namespace Windows.Win32.System.WinRT.Xaml
{
    [GeneratedComInterface]
    [Guid("3CBCF1BF-2F76-4E9C-96AB-E84B37972554")]
    internal partial interface IDesktopWindowXamlSourceNative
    {
        /// <summary>
        /// Attaches the current **IDesktopWindowXamlSourceNative** instance to a parent UI element in your desktop app that is associated with a window handle.
        /// </summary>
        /// <param name="parentWnd">
        /// <para>Type: **HWND** The window handle of the parent UI element in which you want to host a WinRT XAML control.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.xaml.hosting.desktopwindowxamlsource/nf-windows-ui-xaml-hosting-desktopwindowxamlsource-idesktopwindowxamlsourcenative-attachtowindow#parameters">Read more on docs.microsoft.com</see>.</para>
        /// </param>
        /// <returns>If this method succeeds, it returns S_OK. Otherwise, it returns an **HRESULT** error code.</returns>
        /// <remarks>
        /// <para>For a code example that demonstrates how to use this method, see [XamlBridge.cpp](https://github.com/microsoft/Xaml-Islands-Samples/blob/master/Samples/Win32/SampleCppApp/XamlBridge.cpp) in the SampleCppApp sample in the XAML Island samples repo. > [!IMPORTANT] > Make sure that your code calls the **AttachToWindow** method only once per [DesktopWindowXamlSource](/uwp/api/windows.ui.xaml.hosting.desktopwindowxamlsource) object. Calling this method more than once for a **DesktopWindowXamlSource** object could result in a memory leak.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.xaml.hosting.desktopwindowxamlsource/nf-windows-ui-xaml-hosting-desktopwindowxamlsource-idesktopwindowxamlsourcenative-attachtowindow#">Read more on docs.microsoft.com</see>.</para>
        /// </remarks>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Error)]
        int AttachToWindow(nint parentWnd);

        /// <summary>
        /// Gets the window handle of the parent UI element that is associated with the current IDesktopWindowXamlSourceNative instance.
        /// </summary>
        /// <returns>If this method succeeds, it returns S_OK. Otherwise, it returns an **HRESULT** error code.</returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Error)]
        int get_WindowHandle(out nint hWnd);
    }

    file static class Extensions
    {
        public static HWND WindowHandle(this IDesktopWindowXamlSourceNative source)
        {
            _ = source.get_WindowHandle(out nint hWnd);
            return new HWND(hWnd);
        }
    }
}
