using CoreAppUWP.Common;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly HWND hwnd;

        public DesktopWindow()
        {
            RegisterDesktopWindowClass();
            hwnd = CreateDesktopWindow();
        }

        /// <summary>
        /// Get the handle of the window.
        /// </summary>
        public nint Hwnd => hwnd;

        public string Title
        {
            set => _ = PInvoke.SetWindowText(hwnd, value);
        }

        /// <summary>
        /// Gets the <see cref="DesktopWindowXamlSource"/> to provide XAML for this window.
        /// </summary>
        public DesktopWindowXamlSource WindowXamlSource { get; private set; }

        public void Show() => PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_NORMAL);

        public unsafe void SetIcon(string iconPath)
        {
            fixed (char* ptr = iconPath)
            {
                HANDLE icon = PInvoke.LoadImage(new HINSTANCE(), ptr, GDI_IMAGE_TYPE.IMAGE_ICON, 0, 0, IMAGE_FLAGS.LR_LOADFROMFILE);
                _ = PInvoke.SendMessage(hwnd, PInvoke.WM_SETICON, PInvoke.ICON_BIG, new LPARAM((nint)icon.Value));
            }
        }
    }

    public partial class DesktopWindow
    {
        private static readonly unsafe HINSTANCE g_hInstance = new((void*)Process.GetCurrentProcess().Handle);

        // win32 window class name for top-level WinUI desktop windows
        private const string s_windowClassName = "WinUIDesktopWin32WindowClass";

        // Default window title for top-level WinUI desktop windows
        private const string s_defaultWindowTitle = "WinUI Desktop";

        private static LRESULT WNDPROC(HWND hWnd, uint message, WPARAM wParam, LPARAM lParam)
        {
            switch (message)
            {
                case PInvoke.WM_PAINT:
                    HDC hdc = PInvoke.BeginPaint(hWnd, out PAINTSTRUCT ps);
                    _ = PInvoke.GetClientRect(hWnd, out RECT rect);
                    _ = PInvoke.FillRect(hdc, rect, new DefaultSafeHandel(PInvoke.GetStockObject(GET_STOCK_OBJECT_FLAGS.WHITE_BRUSH)));
                    _ = PInvoke.EndPaint(hWnd, ps);
                    return new LRESULT();
                case PInvoke.WM_CREATE:
                case PInvoke.WM_DESTROY:
                    return new LRESULT();
                default:
                    return PInvoke.DefWindowProc(hWnd, message, wParam, lParam);
            }
        }

        private static unsafe void RegisterDesktopWindowClass()
        {
            if (!PInvoke.GetClassInfoEx(new DefaultSafeHandel(g_hInstance), s_windowClassName, out WNDCLASSEXW wndClassEx))
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

                wndClassEx.lpfnWndProc = WNDPROC;

                _ = PInvoke.RegisterClassEx(wndClassEx);
            }
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
                new HWND(),                          // no owner window
                null,                               // use class menu
                new DefaultSafeHandel(g_hInstance),
                null);

        private partial class DefaultSafeHandel(nint invalidHandleValue, bool ownsHandle) : SafeHandle(invalidHandleValue, ownsHandle)
        {
            public DefaultSafeHandel(nint handle) : this(handle, true) => SetHandle(handle);

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
                    IDesktopWindowXamlSourceNative native = source.As<IDesktopWindowXamlSourceNative>();
                    native.AttachToWindow(window.hwnd);
                    UpdateHostSize();

                    launched(source);
                    taskCompletionSource.SetResult(window);

                    MSG msg = new();
                    while (msg.message != PInvoke.WM_QUIT)
                    {
                        if (PInvoke.PeekMessage(out msg, new HWND(), 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE))
                        {
                            _ = PInvoke.DispatchMessage(msg);
                            UpdateHostSize();
                        }
                    }

                    void UpdateHostSize()
                    {
                        _ = PInvoke.GetClientRect(window.hwnd, out RECT rect);
                        _ = PInvoke.SetWindowPos(
                            native.WindowHandle,
                            new HWND(),
                            0, 0,
                            rect.Width, rect.Height,
                            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
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
