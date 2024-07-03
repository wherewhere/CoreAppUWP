using System;
using System.Collections.Generic;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.Packaging.Appx;
using Windows.Win32.System.Registry;
using Detours = Microsoft.Detours.PInvoke;

namespace CoreAppUWP.Common
{
    public class HookWindowingModel : IDisposable
    {
        private unsafe delegate WIN32_ERROR AppPolicyGetWindowingModel(HANDLE processToken, AppPolicyWindowingModel* policy);

        //private static readonly HANDLE currentProcessToken = PInvoke.GetCurrentThreadEffectiveToken();

        [ThreadStatic]
        private static HANDLE currentThread;

        [ThreadStatic]
        private static unsafe FARPROC baseAppPolicyGetWindowingModel;
        [ThreadStatic]
        private static unsafe delegate*<HANDLE, AppPolicyWindowingModel*, WIN32_ERROR> overrideAppPolicyGetWindowingModel;

        ~HookWindowingModel()
        {
            Dispose(disposing: true);
        }

        [ThreadStatic]
        private static bool isHooked;
        public bool IsHooked
        {
            get => isHooked;
            set => isHooked = value;
        }

        [ThreadStatic]
        private static AppPolicyWindowingModel windowingModel;
        internal AppPolicyWindowingModel WindowingModel
        {
            get => windowingModel;
            set => windowingModel = value;
        }

        public unsafe void StartHook()
        {
            if (!IsHooked)
            {
                currentThread = PInvoke.GetCurrentThread();

                _ = Detours.DetourTransactionBegin();
                _ = Detours.DetourUpdateThread(currentThread);

                using (FreeLibrarySafeHandle library = PInvoke.LoadLibrary("KERNEL32.dll"))
                {
                    baseAppPolicyGetWindowingModel = PInvoke.GetProcAddress(library, "AppPolicyGetWindowingModel");
                    void* baseAppPolicyGetWindowingModelPointer = (void*)baseAppPolicyGetWindowingModel.Value;
                    overrideAppPolicyGetWindowingModel = &OverrideAppPolicyGetWindowingModel;
                    void* overrideAppPolicyGetWindowingModelPointer = overrideAppPolicyGetWindowingModel;
                    _ = Detours.DetourAttach(ref baseAppPolicyGetWindowingModelPointer, overrideAppPolicyGetWindowingModelPointer);
                }

                _ = Detours.DetourTransactionCommit();
                IsHooked = true;
            }
        }

        public unsafe void EndHook()
        {
            if (IsHooked)
            {
                _ = Detours.DetourTransactionBegin();
                _ = Detours.DetourUpdateThread(currentThread);

                void* baseAppPolicyGetWindowingModelPointer = (void*)baseAppPolicyGetWindowingModel.Value;
                void* overrideAppPolicyGetWindowingModelPointer = overrideAppPolicyGetWindowingModel;
                _ = Detours.DetourDetach(ref baseAppPolicyGetWindowingModelPointer, overrideAppPolicyGetWindowingModelPointer);
                baseAppPolicyGetWindowingModel = default;
                overrideAppPolicyGetWindowingModel = default;

                _ = Detours.DetourTransactionCommit();

                IsHooked = false;
            }
        }

        private static unsafe WIN32_ERROR OverrideAppPolicyGetWindowingModel(HANDLE processToken, AppPolicyWindowingModel* policy)
        {
            //if (processToken == currentProcessToken)
            {
                *policy = windowingModel;
                return WIN32_ERROR.ERROR_SUCCESS;
            }
            return baseAppPolicyGetWindowingModel.CreateDelegate<AppPolicyGetWindowingModel>()(processToken, policy);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && IsHooked)
            {
                EndHook();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
