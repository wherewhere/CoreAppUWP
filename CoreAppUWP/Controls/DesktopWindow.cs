using CoreAppUWP.Common;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.WinRT.Xaml;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;

namespace CoreAppUWP.Controls
{
    public partial class DesktopWindow
    {
        private bool m_bMinimizedOrHidden = false;
        private bool m_closed = false;
        private DesktopWindowXamlSource m_source;
        private IDesktopWindowXamlSourceNative m_native;

        private readonly HWND m_hwnd;
        private readonly WNDCLASSEXW m_wndClassEx;

        public DesktopWindow()
        {
            m_wndClassEx = RegisterDesktopWindowClass(WNDPROC);
            m_hwnd = CreateDesktopWindow();
        }

        /// <summary>
        /// Get the handle of the window.
        /// </summary>
        public nint Hwnd => m_hwnd;

        /// <summary>
        /// Gets the event dispatcher for the window.
        /// </summary>
        public CoreDispatcher Dispatcher { get; private set; }

        /// <summary>
        /// Gets or sets the visual root of an application window.
        /// </summary>
        public UIElement Content
        {
            get => WindowXamlSource.Content;
            set => WindowXamlSource.Content = value;
        }

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
        /// Gets or sets a string used for the window title.
        /// </summary>
        public unsafe string Title
        {
            get
            {
                int windowTextLength = PInvoke.GetWindowTextLength(m_hwnd);
                char* windowText = stackalloc char[windowTextLength + 1];
                _ = PInvoke.GetWindowText(m_hwnd, windowText, windowTextLength + 1);
                return new string(windowText);
            }
            set => _ = PInvoke.SetWindowText(m_hwnd, value);
        }

        /// <summary>
        /// Gets the <see cref="DesktopWindowXamlSource"/> to provide XAML for this window.
        /// </summary>
        public DesktopWindowXamlSource WindowXamlSource
        {
            get => m_source;
            private init
            {
                if (m_source != value)
                {
                    m_source = value;
                    if (value != null)
                    {
                        Dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
                        m_native = value.As<IDesktopWindowXamlSourceNative>();
                        m_native.AttachToWindow(m_hwnd);
                        ResizeWindowToDesktopWindowXamlSourceWindowDimensions();
                    }
                    else
                    {
                        m_native = null;
                    }
                }
            }
        }

        public event TypedEventHandler<DesktopWindow, object> Closed;

        public void Show() => _ = PInvoke.ShowWindow(m_hwnd, SHOW_WINDOW_CMD.SW_NORMAL);

        public unsafe void SetIcon(string iconPath)
        {
            fixed (char* ptr = iconPath)
            {
                HANDLE icon = PInvoke.LoadImage(new HINSTANCE(), ptr, GDI_IMAGE_TYPE.IMAGE_ICON, 0, 0, IMAGE_FLAGS.LR_LOADFROMFILE);
                _ = PInvoke.SendMessage(m_hwnd, PInvoke.WM_SETICON, PInvoke.ICON_BIG, new LPARAM((nint)icon.Value));
            }
        }

        private LRESULT WNDPROC(HWND hWnd, uint message, WPARAM wParam, LPARAM lParam)
        {
            switch (message)
            {
                case PInvoke.WM_PAINT:
                    HDC hdc = PInvoke.BeginPaint(hWnd, out PAINTSTRUCT ps);
                    _ = PInvoke.GetClientRect(hWnd, out RECT rect);
                    _ = PInvoke.FillRect(hdc, rect, new DefaultSafeHandle(PInvoke.GetStockObject(GET_STOCK_OBJECT_FLAGS.WHITE_BRUSH)));
                    _ = PInvoke.EndPaint(hWnd, ps);
                    return new LRESULT();
                case PInvoke.WM_CLOSE when m_closed:
                    goto default;
                case PInvoke.WM_CLOSE:
                    m_closed = true;
                    Closed?.Invoke(this, null);
                    goto default;
                case PInvoke.WM_SIZE:
                    return OnSizeChanged(wParam);
                case PInvoke.WM_CREATE:
                case PInvoke.WM_DESTROY:
                    return new LRESULT();
                default:
                    return PInvoke.DefWindowProc(hWnd, message, wParam, lParam);
            }
        }

        private LRESULT OnSizeChanged(WPARAM wParam)
        {
            ResizeWindowToDesktopWindowXamlSourceWindowDimensions();
            //RaiseWindowSizeChangedEvent();

            switch (wParam.Value)
            {
                case PInvoke.SIZE_RESTORED:
                case PInvoke.SIZE_MAXIMIZED:
                    {
                        if (m_bMinimizedOrHidden)
                        {
                            m_bMinimizedOrHidden = false;
                            //RaiseWindowVisibilityChangedEvent(true /* visible */);
                        }
                    }
                    break;
                case PInvoke.SIZE_MINIMIZED:
                    {
                        if (!m_bMinimizedOrHidden)
                        {
                            m_bMinimizedOrHidden = true;
                            //RaiseWindowVisibilityChangedEvent(false /* visible */);
                        }
                    }
                    break;
            }

            return new LRESULT();
        }

        private void ResizeWindowToDesktopWindowXamlSourceWindowDimensions()
        {
            _ = PInvoke.GetClientRect(m_hwnd, out RECT rect);
            _ = PInvoke.SetWindowPos(
                m_native.WindowHandle,
                new HWND(),
                0, 0,
                rect.Width, rect.Height,
                SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
        }
    }

    public partial class DesktopWindow
    {
        private static readonly unsafe HINSTANCE g_hInstance = new((void*)Process.GetCurrentProcess().Handle);

        // win32 window class name for top-level WinUI desktop windows
        private const string s_windowClassName = "WinUIDesktopWin32WindowClass";

        // Default window title for top-level WinUI desktop windows
        private const string s_defaultWindowTitle = "WinUI Desktop";

        private static unsafe WNDCLASSEXW RegisterDesktopWindowClass(WNDPROC lpfnWndProc)
        {
            if (!PInvoke.GetClassInfoEx(new DefaultSafeHandle(g_hInstance), s_windowClassName, out WNDCLASSEXW wndClassEx))
            {
                wndClassEx.cbSize = (uint)Marshal.SizeOf(wndClassEx);
                wndClassEx.style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW;
                wndClassEx.cbClsExtra = 0;
                wndClassEx.cbWndExtra = 0;
                wndClassEx.hCursor = PInvoke.LoadCursor(new HINSTANCE(), PInvoke.IDC_ARROW);
                wndClassEx.hbrBackground = (HBRUSH)((nint)SYS_COLOR_INDEX.COLOR_WINDOW + 1);
                wndClassEx.hInstance = g_hInstance;

                fixed (char* lps_windowClassName = s_windowClassName)
                {
                    wndClassEx.lpszClassName = lps_windowClassName;
                }

                wndClassEx.lpfnWndProc = lpfnWndProc;
                _ = PInvoke.RegisterClassEx(wndClassEx);

                return wndClassEx;
            }
            return default;
        }

        private static unsafe HWND CreateDesktopWindow() =>
            PInvoke.CreateWindowEx(
                0,                                  // Extended Style
                s_windowClassName,                  // name of window class
                s_defaultWindowTitle,               // title-bar string
                WINDOW_STYLE.WS_OVERLAPPEDWINDOW | WINDOW_STYLE.WS_VISIBLE,  // top-level window
                int.MinValue,                       // default horizontal position
                (int)SHOW_WINDOW_CMD.SW_HIDE,       // If the y parameter is some other value,
                                                    // then the window manager calls ShowWindow with that value as the nCmdShow parameter
                int.MinValue,                       // default width
                int.MinValue,                       // default height
                new HWND(),                         // no owner window
                null,                               // use class menu
                new DefaultSafeHandle(g_hInstance),
                null);

        private partial class DefaultSafeHandle(nint invalidHandleValue, bool ownsHandle) : SafeHandle(invalidHandleValue, ownsHandle)
        {
            public DefaultSafeHandle(nint handle) : this(handle, true) => SetHandle(handle);

            public override bool IsInvalid => handle != nint.Zero;

            protected override bool ReleaseHandle() => true;
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
                    using (HookWindowingModel hook = new())
                    {
                        source = new DesktopWindowXamlSource();
                    }

                    DesktopWindow window = new() { WindowXamlSource = source };

                    launched(source);
                    taskCompletionSource.SetResult(window);

                    MSG msg = new();
                    while (msg.message != PInvoke.WM_QUIT)
                    {
                        if (PInvoke.PeekMessage(out msg, new HWND(), 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE))
                        {
                            _ = PInvoke.DispatchMessage(msg);
                        }
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
