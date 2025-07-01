using Microsoft.UI.Dispatching;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace CoreAppUWP.Helpers
{
    public static class UIHelper
    {
        public static int GetActualPixel(this double pixel)
        {
            double currentDpi = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            return Convert.ToInt32(pixel * currentDpi);
        }

        public static unsafe int GetActualPixel(this double pixel, nint window)
        {
            uint currentDpi = PInvoke.GetDpiForWindow(new HWND((void*)window));
            return Convert.ToInt32(pixel * (currentDpi / 96.0));
        }

        public static unsafe double GetDisplayPixel(this int pixel, nint window)
        {
            uint currentDpi = PInvoke.GetDpiForWindow(new HWND((void*)window));
            return pixel / (currentDpi / 96.0);
        }

        public static object GetMessage(this Exception ex) => ex.Message is { Length: > 0 } message ? message : ex.GetType();

        /// <summary>
        /// Extension method for <see cref="CoreDispatcher"/>. Offering an actual awaitable <see cref="Task{T}"/> with optional result that will be executed on the given dispatcher.
        /// </summary>
        /// <typeparam name="T">Returned data type of the function.</typeparam>
        /// <param name="dispatcher">Dispatcher of a thread to run <paramref name="function"/>.</param>
        /// <param name="function"> Function to be executed on the given dispatcher.</param>
        /// <param name="priority">Dispatcher execution priority, default is normal.</param>
        /// <returns>An awaitable <see cref="Task{T}"/> for the operation.</returns>
        /// <remarks>If the current thread has UI access, <paramref name="function"/> will be invoked directly.</remarks>
        public static Task<T> AwaitableRunAsync<T>(this CoreDispatcher dispatcher, Func<T> function, CoreDispatcherPriority priority = CoreDispatcherPriority.Normal)
        {
            ArgumentNullException.ThrowIfNull(function);

            // Skip the dispatch, if possible
            if (dispatcher.HasThreadAccess)
            {
                try
                {
                    return Task.FromResult(function());
                }
                catch (Exception e)
                {
                    return Task.FromException<T>(e);
                }
            }

            TaskCompletionSource<T> taskCompletionSource = new();

            _ = dispatcher.RunAsync(priority, () =>
            {
                try
                {
                    taskCompletionSource.SetResult(function());
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            });

            return taskCompletionSource.Task;
        }
    }
}
