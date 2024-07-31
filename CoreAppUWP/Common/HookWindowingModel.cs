using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.Packaging.Appx;
using Detours = Microsoft.Detours.PInvoke;

namespace CoreAppUWP.Common
{
    public sealed partial class HookWindowingModel : IDisposable
    {
        private bool disposed;
        private static int refCount;
        private const int currentProcessToken = -6;
        private static unsafe delegate* unmanaged[Stdcall]<HANDLE, AppPolicyWindowingModel*, WIN32_ERROR> AppPolicyGetWindowingModel;

        public HookWindowingModel()
        {
            refCount++;
            StartHook();
        }

        ~HookWindowingModel()
        {
            Dispose();
        }

        public static bool IsHooked { get; private set; }
        internal static AppPolicyWindowingModel WindowingModel { get; set; } = AppPolicyWindowingModel.AppPolicyWindowingModel_ClassicDesktop;

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
