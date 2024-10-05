using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.Packaging.Appx;
using Detours = Microsoft.Detours.PInvoke;

namespace CoreAppUWP.Common
{
    /// <summary>
    /// Represents a hook for the <see cref="PInvoke.AppPolicyGetWindowingModel(HANDLE, AppPolicyWindowingModel*)"/> function.
    /// </summary>
    public sealed partial class HookWindowingModel : IDisposable
    {
        /// <summary>
        /// The value that indicates whether the class has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// The reference count for the hook.
        /// </summary>
        private static int refCount;

        /// <summary>
        /// The value that represents the current process token.
        /// </summary>
        private const int currentProcessToken = -6;

        /// <remarks>The original <see cref="PInvoke.AppPolicyGetWindowingModel(HANDLE, AppPolicyWindowingModel*)"/> function.</remarks>
        /// <inheritdoc cref="PInvoke.AppPolicyGetWindowingModel(HANDLE, AppPolicyWindowingModel*)"/>
        private static unsafe delegate* unmanaged[Stdcall]<HANDLE, AppPolicyWindowingModel*, WIN32_ERROR> AppPolicyGetWindowingModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="HookWindowingModel"/> class.
        /// </summary>
        public HookWindowingModel()
        {
            refCount++;
            StartHook();
        }

        /// <summary>
        /// Finalizes this instance of the <see cref="HookWindowingModel"/> class.
        /// </summary>
        ~HookWindowingModel()
        {
            Dispose();
        }

        /// <summary>
        /// Gets the value that indicates whether the hook is active.
        /// </summary>
        public static bool IsHooked { get; private set; }

        /// <summary>
        /// Gets or sets the windowing model to use when the hooked <see cref="PInvoke.AppPolicyGetWindowingModel(HANDLE, AppPolicyWindowingModel*)"/> function is called.
        /// </summary>
        internal static AppPolicyWindowingModel WindowingModel { get; set; } = AppPolicyWindowingModel.AppPolicyWindowingModel_ClassicDesktop;

        /// <summary>
        /// Starts the hook for the <see cref="PInvoke.AppPolicyGetWindowingModel(HANDLE, AppPolicyWindowingModel*)"/> function.
        /// </summary>
        private static unsafe void StartHook()
        {
            if (!IsHooked)
            {
                using FreeLibrarySafeHandle library = PInvoke.GetModuleHandle("KERNEL32.dll");
                if (!library.IsInvalid && NativeLibrary.TryGetExport(library.DangerousGetHandle(), nameof(PInvoke.AppPolicyGetWindowingModel), out nint appPolicyGetWindowingModel))
                {
                    void* appPolicyGetWindowingModelPtr = (void*)appPolicyGetWindowingModel;
                    delegate* unmanaged[Stdcall]<HANDLE, AppPolicyWindowingModel*, WIN32_ERROR> overrideAppPolicyGetWindowingModel = &OverrideAppPolicyGetWindowingModel;

                    _ = Detours.DetourRestoreAfterWith();

                    _ = Detours.DetourTransactionBegin();
                    _ = Detours.DetourUpdateThread(PInvoke.GetCurrentThread());
                    _ = Detours.DetourAttach(ref appPolicyGetWindowingModelPtr, overrideAppPolicyGetWindowingModel);
                    _ = Detours.DetourTransactionCommit();

                    AppPolicyGetWindowingModel = (delegate* unmanaged[Stdcall]<HANDLE, AppPolicyWindowingModel*, WIN32_ERROR>)appPolicyGetWindowingModelPtr;
                    IsHooked = true;
                }
            }
        }

        /// <summary>
        /// Ends the hook for the <see cref="PInvoke.AppPolicyGetWindowingModel(HANDLE, AppPolicyWindowingModel*)"/> function.
        /// </summary>
        private static unsafe void EndHook()
        {
            if (--refCount == 0 && IsHooked)
            {
                void* appPolicyGetWindowingModelPtr = AppPolicyGetWindowingModel;
                delegate* unmanaged[Stdcall]<HANDLE, AppPolicyWindowingModel*, WIN32_ERROR> overrideAppPolicyGetWindowingModel = &OverrideAppPolicyGetWindowingModel;

                _ = Detours.DetourTransactionBegin();
                _ = Detours.DetourUpdateThread(PInvoke.GetCurrentThread());
                _ = Detours.DetourDetach(&appPolicyGetWindowingModelPtr, overrideAppPolicyGetWindowingModel);
                _ = Detours.DetourTransactionCommit();

                AppPolicyGetWindowingModel = null;
                IsHooked = false;
            }
        }

        /// <param name="policy">A pointer to a variable of the <a href="https://docs.microsoft.com/windows/win32/api/appmodel/ne-appmodel-apppolicywindowingmodel">AppPolicyWindowingModel</a> enumerated type.
        /// When the function returns successfully, the variable contains the <see cref="WindowingModel"/> when the identified process is current; otherwise, the windowing model of the identified process.</param>
        /// <remarks>The overridden <see cref="PInvoke.AppPolicyGetWindowingModel(HANDLE, AppPolicyWindowingModel*)"/> function.</remarks>
        /// <inheritdoc cref="PInvoke.AppPolicyGetWindowingModel(HANDLE, AppPolicyWindowingModel*)"/>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static unsafe WIN32_ERROR OverrideAppPolicyGetWindowingModel(HANDLE processToken, AppPolicyWindowingModel* policy)
        {
            if ((int)processToken.Value == currentProcessToken)
            {
                *policy = WindowingModel;
                return WIN32_ERROR.ERROR_SUCCESS;
            }
            return AppPolicyGetWindowingModel(processToken, policy);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!disposed && IsHooked)
            {
                EndHook();
            }
            GC.SuppressFinalize(this);
            disposed = true;
        }
    }
}
