using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace CoreAppUWP.Helpers
{
    public static class UIHelper
    {
        public static bool HasStatusBar => ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar");

        public static int GetActualPixel(this double pixel)
        {
            double currentDpi = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            return Convert.ToInt32(pixel * currentDpi);
        }

        public static int GetActualPixel(this double pixel, IntPtr window)
        {
            uint currentDpi = PInvoke.GetDpiForWindow(new HWND(window));
            return Convert.ToInt32(pixel * (currentDpi / 96.0));
        }

        public static double GetDisplayPixel(this int pixel, IntPtr window)
        {
            uint currentDpi = PInvoke.GetDpiForWindow(new HWND(window));
            return pixel / (currentDpi / 96.0);
        }

        public static string ExceptionToMessage(this Exception ex)
        {
            StringBuilder builder = new();
            _ = builder.Append('\n');
            if (!string.IsNullOrWhiteSpace(ex.Message)) { _ = builder.AppendLine($"Message: {ex.Message}"); }
            _ = builder.AppendLine($"HResult: {ex.HResult} (0x{Convert.ToString(ex.HResult, 16).ToUpperInvariant()})");
            if (!string.IsNullOrWhiteSpace(ex.StackTrace)) { _ = builder.AppendLine(ex.StackTrace); }
            if (!string.IsNullOrWhiteSpace(ex.HelpLink)) { _ = builder.Append($"HelperLink: {ex.HelpLink}"); }
            return builder.ToString();
        }

        public static TResult AwaitByTaskCompleteSource<TResult>(this Task<TResult> function, CancellationToken cancellationToken = default)
        {
            TaskCompletionSource<TResult> taskCompletionSource = new();
            Task<TResult> task = taskCompletionSource.Task;
            _ = Task.Run(async () =>
            {
                try
                {
                    TResult result = await function.ConfigureAwait(false);
                    taskCompletionSource.SetResult(result);
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            }, cancellationToken);
            TResult taskResult = task.Result;
            return taskResult;
        }
    }
}
