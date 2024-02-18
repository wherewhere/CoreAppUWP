using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace CoreAppUWP.Controls
{
    public partial class DesktopWindow
    {
        private readonly HWND hwnd;
        public nint Hwnd => hwnd;

        public DesktopWindow()
        {
            RegisterDesktopWindowClass();
            hwnd = CreateDesktopWindow();
        }

        public void Show() => PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_NORMAL);
    }

    public partial class DesktopWindow
    {
        private static readonly HINSTANCE g_hInstance = new(Process.GetCurrentProcess().Handle);

        // win32 window class name for top-level WinUI desktop windows
        private const string s_windowClassName = "WinUIDesktopWin32WindowClass";

        // Default window title for top-level WinUI desktop windows
        private const string s_defaultWindowTitle = "WinUI Desktop";

        public static unsafe void RegisterDesktopWindowClass()
        {
            if (!PInvoke.GetClassInfoEx(new DefaultSafeHandel(g_hInstance), s_windowClassName, out WNDCLASSEXW wndClassEx))
            {
                wndClassEx.cbSize = (uint)Marshal.SizeOf(wndClassEx);
                wndClassEx.style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW;
                wndClassEx.cbClsExtra = 0;
                wndClassEx.cbWndExtra = 0;
                wndClassEx.hCursor = PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_ARROW);
                wndClassEx.hbrBackground = (HBRUSH)((int)SYS_COLOR_INDEX.COLOR_WINDOW + 1);
                wndClassEx.hInstance = g_hInstance;

                fixed (char* lps_windowClassName = s_windowClassName)
                {
                    wndClassEx.lpszClassName = lps_windowClassName;
                }

                wndClassEx.lpfnWndProc = (hWnd, message, wParam, lParam) =>
                {
                    HDC hdc;
                    PAINTSTRUCT ps;
                    RECT rect;
                    switch (message)
                    {
                        case PInvoke.WM_PAINT:
                            hdc = PInvoke.BeginPaint(hWnd, out ps);
                            _ = PInvoke.GetClientRect(hWnd, out rect);
                            _ = PInvoke.FillRect(hdc, rect, new DefaultSafeHandel(PInvoke.GetStockObject(GET_STOCK_OBJECT_FLAGS.WHITE_BRUSH)));
                            _ = PInvoke.EndPaint(hWnd, ps);
                            return new LRESULT();
                        case PInvoke.WM_CREATE:
                        case PInvoke.WM_DESTROY:
                            return new LRESULT();
                        default:
                            return PInvoke.DefWindowProc(hWnd, message, wParam, lParam);
                    }
                };

                _ = PInvoke.RegisterClassEx(wndClassEx);
            }
        }

        internal static unsafe HWND CreateDesktopWindow() =>
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
                HWND.Null,                          // no owner window
                null,                               // use class menu
                new DefaultSafeHandel(g_hInstance),
                null);

        private class DefaultSafeHandel(nint invalidHandleValue, bool ownsHandle) : SafeHandle(invalidHandleValue, ownsHandle)
        {
            public DefaultSafeHandel(nint handle) : this(handle, true) => SetHandle(handle);

            public override bool IsInvalid => handle != nint.Zero;

            protected override bool ReleaseHandle() => true;
        }
    }
}
