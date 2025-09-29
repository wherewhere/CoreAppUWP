using CoreAppUWP.Common;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.Packaging.Appx;
using WinRT;

namespace CoreAppUWP
{
    public static partial class Program
    {
        public static bool IsPackagedApp
        {
            get
            {
                uint length = 0;
                Span<char> str = [];
                _ = PInvoke.GetCurrentPackageFullName(ref length, str);

                str = new char[(int)length];
                WIN32_ERROR result = PInvoke.GetCurrentPackageFullName(ref length, str);
                return result != WIN32_ERROR.APPMODEL_ERROR_NO_PACKAGE;
            }
        }

        public static bool IsCoreWindow
        {
            get
            {
                try
                {
                    if (PInvoke.AppPolicyGetWindowingModel(new DefaultSafeHandle(-6), out AppPolicyWindowingModel model) == WIN32_ERROR.ERROR_SUCCESS)
                    {
                        return model == AppPolicyWindowingModel.AppPolicyWindowingModel_Universal;
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        private static bool IsSupportCoreWindow
        {
            get
            {
                try
                {
                    RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\WinUI\Xaml");
                    return registryKey?.GetValue("EnableUWPWindow") is > 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        private static void Main()
        {
            ComWrappersSupport.InitializeComWrappers();
            if (!IsPackagedApp)
            {
                PInvoke.TryCreatePackageDependency(
                    new DefaultSafeHandle(0),
                    "Microsoft.WindowsAppRuntime.1.8_8wekyb3d8bbwe",
                    new PACKAGE_VERSION(),
                    RuntimeInformation.ProcessArchitecture switch
                    {
                        Architecture.X86 => PackageDependencyProcessorArchitectures.PackageDependencyProcessorArchitectures_X86,
                        Architecture.X64 => PackageDependencyProcessorArchitectures.PackageDependencyProcessorArchitectures_X64,
                        Architecture.Arm => PackageDependencyProcessorArchitectures.PackageDependencyProcessorArchitectures_Arm,
                        Architecture.Arm64 => PackageDependencyProcessorArchitectures.PackageDependencyProcessorArchitectures_Arm64,
                        _ => PackageDependencyProcessorArchitectures.PackageDependencyProcessorArchitectures_None
                    },
                    PackageDependencyLifetimeKind.PackageDependencyLifetimeKind_Process,
                    null,
                    CreatePackageDependencyOptions.CreatePackageDependencyOptions_None,
                    out PWSTR package).ThrowOnFailure();
                unsafe
                {
                    PWSTR packageFullName = new();
                    PInvoke.AddPackageDependency(
                        package.ToString(),
                        0,
                        AddPackageDependencyOptions.AddPackageDependencyOptions_PrependIfRankCollision,
                        out _,
                        &packageFullName).ThrowOnFailure();
                }
            }
            HookRegistry hookRegistry = null;
            try
            {
                if (IsCoreWindow && !IsSupportCoreWindow)
                {
                    hookRegistry = new HookRegistry();
                }
                XamlCheckProcessRequirements();
                Application.Start(p =>
                {
                    DispatcherQueueSynchronizationContext context = new(DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);
                    _ = new App();
                });
            }
            finally
            {
                hookRegistry?.Dispose();
            }
        }

        [LibraryImport("Microsoft.UI.Xaml.dll")]
        private static partial void XamlCheckProcessRequirements();

        private sealed partial class DefaultSafeHandle(nint invalidHandleValue, bool ownsHandle) : SafeHandle(invalidHandleValue, ownsHandle)
        {
            public DefaultSafeHandle(nint handle) : this(handle, true) => SetHandle(handle);

            public override bool IsInvalid => handle == 0;

            protected override bool ReleaseHandle() => true;
        }
    }
}
