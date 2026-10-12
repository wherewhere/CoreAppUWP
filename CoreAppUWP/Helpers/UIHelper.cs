using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace CoreAppUWP.Helpers
{
    public static class UIHelper
    {
        public static int GetActualPixel(this UIElement element, double pixel) => Convert.ToInt32(pixel * element.GetRasterizationScale());

        public static double GetDisplayPixel(this UIElement element, int pixel) => pixel / element.GetRasterizationScale();

        public static object GetMessage(this Exception ex) => ex.Message is { Length: > 0 } message ? message : ex.GetType();

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
