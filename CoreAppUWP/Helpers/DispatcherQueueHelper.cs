using System.Runtime.InteropServices; // For DllImport
using Windows.System;

namespace CoreAppUWP.Helpers
{
    public partial class WindowsSystemDispatcherQueueHelper
    {
        /// <summary>
        /// Specifies the threading and apartment type for a new DispatcherQueueController.
        /// </summary>
        /// <remarks>Introduced in Windows 10, version 1709.</remarks>
        [StructLayout(LayoutKind.Sequential)]
        private struct DispatcherQueueOptions
        {
            /// <summary>
            /// Size of this <see cref="DispatcherQueueOptions"/> structure.
            /// </summary>
            public int DWSize;

            /// <summary>
            /// Thread affinity for the created <a href="https://docs.microsoft.com/uwp/api/windows.system.dispatcherqueuecontroller">DispatcherQueueController</a>.
            /// </summary>
            public int ThreadType;

            /// <summary>
            /// Specifies whether to initialize COM apartment on the new thread as an application single-threaded apartment (ASTA)
            /// or single-threaded apartment (STA). This field is only relevant if <b>threadType</b> is <b>DQTYPE_THREAD_DEDICATED</b>.
            /// Use <b>DQTAT_COM_NONE</b> when <b>DispatcherQueueOptions.threadType</b> is <b>DQTYPE_THREAD_CURRENT</b>.
            /// </summary>
            public int ApartmentType;
        }

        [LibraryImport("CoreMessaging.dll")]
        private static unsafe partial int CreateDispatcherQueueController(DispatcherQueueOptions options, nint* instance);

        private nint m_dispatcherQueueController = nint.Zero;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (DispatcherQueue.GetForCurrentThread() != null)
            {
                // one already exists, so we'll just use it.
                return;
            }

            if (m_dispatcherQueueController == nint.Zero)
            {
                DispatcherQueueOptions options;
                options.DWSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.ThreadType = 2;     // DQTYPE_THREAD_CURRENT
                options.ApartmentType = 2;  // DQTAT_COM_STA

                unsafe
                {
                    nint dispatcherQueueController;
                    _ = CreateDispatcherQueueController(options, &dispatcherQueueController);
                    m_dispatcherQueueController = dispatcherQueueController;
                }
            }
        }
    }
}
