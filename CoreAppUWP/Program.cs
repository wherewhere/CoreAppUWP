using CoreAppUWP.Common;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Storage.Packaging.Appx;
using WinRT;
using AddPackageDependencyOptions = Windows.Win32.Storage.Packaging.Appx.AddPackageDependencyOptions;
using CreatePackageDependencyOptions = Windows.Win32.Storage.Packaging.Appx.CreatePackageDependencyOptions;
using PackageDependencyProcessorArchitectures = Microsoft.Windows.ApplicationModel.DynamicDependency.PackageDependencyProcessorArchitectures;

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
                    return PInvoke.AppPolicyGetWindowingModel(new DefaultSafeHandle(-6), out AppPolicyWindowingModel model) == WIN32_ERROR.ERROR_SUCCESS
                        && model == AppPolicyWindowingModel.AppPolicyWindowingModel_Universal;
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
                PackageManager manager = new();
                Package package = manager.FindPackagesForUser(string.Empty, "Microsoft.WindowsAppRuntime.2", "CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US").First();
                nint handle = NativeLibrary.Load(Path.Combine(package.InstalledLocation.Path, "Microsoft.WindowsAppRuntime.dll"));
                try
                {
                    _ = MddTryCreatePackageDependency(
                         default,
                         package.Id.FamilyName,
                         new PACKAGE_VERSION(),
                         PackageDependencyProcessorArchitectures.None,
                         PackageDependencyLifetimeArtifactKind.Process,
                         null,
                         CreatePackageDependencyOptions.CreatePackageDependencyOptions_None,
                         out PWSTR packageId).ThrowOnFailure();
                    _ = MddAddPackageDependency(
                        packageId,
                        0,
                        AddPackageDependencyOptions.AddPackageDependencyOptions_None,
                        out _, out _).ThrowOnFailure();
                }
                finally
                {
                    NativeLibrary.Free(handle);
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
                Application.Start(static p =>
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

        [LibraryImport("Microsoft.WindowsAppRuntime.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static partial HRESULT MddTryCreatePackageDependency(PSID user, string packageFamilyName, PACKAGE_VERSION minVersion, [Optional, DefaultParameterValue(PackageDependencyProcessorArchitectures.None)] PackageDependencyProcessorArchitectures architectures, [Optional, DefaultParameterValue(PackageDependencyLifetimeArtifactKind.Process)] PackageDependencyLifetimeArtifactKind lifetimeKind, string lifetimeArtifact, [Optional, DefaultParameterValue(CreatePackageDependencyOptions.CreatePackageDependencyOptions_None)] CreatePackageDependencyOptions options, out PWSTR packageDependencyId);

        [LibraryImport("Microsoft.WindowsAppRuntime.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static partial HRESULT MddAddPackageDependency(PWSTR packageDependencyId, [Optional, DefaultParameterValue(0)] int rank, [Optional, DefaultParameterValue(AddPackageDependencyOptions.AddPackageDependencyOptions_None)] AddPackageDependencyOptions options, out PACKAGEDEPENDENCY_CONTEXT packageDependencyContext, out PWSTR packageFullName);

        private sealed partial class DefaultSafeHandle(nint invalidHandleValue, bool ownsHandle) : SafeHandle(invalidHandleValue, ownsHandle)
        {
            public DefaultSafeHandle(nint handle) : this(handle, true) => SetHandle(handle);

            public override bool IsInvalid => handle == 0;

            protected override bool ReleaseHandle() => true;
        }
    }
}
