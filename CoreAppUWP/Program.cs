using CoreAppUWP.Controls;
using CoreAppUWP.Helpers;
using CoreAppUWP.Pages;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.ApplicationModel;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.Packaging.Appx;
using WinRT;

namespace CoreAppUWP
{
    public static partial class Program
    {
        public static unsafe bool IsPackagedApp
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

        public unsafe static bool IsCoreWindow
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

        private static void Main()
        {
            ComWrappersSupport.InitializeComWrappers();
            bool isPackagedApp = IsPackagedApp;
            if (!isPackagedApp)
            {
                PInvoke.TryCreatePackageDependency(
                    new DefaultSafeHandle(0),
                    "Microsoft.UI.Xaml.2.8_8wekyb3d8bbwe",
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
            if (IsCoreWindow)
            {
                Application.Start(p =>
                {
                    DispatcherQueueSynchronizationContext context = new(DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);
                    _ = new App();
                });
            }
            else
            {
                App app = new();
                DesktopWindow window = DesktopWindow.CreateMainWindow(source =>
                {
                    Frame frame = new();
                    source.Content = frame;
                    _ = frame.Navigate(typeof(MainPage), null, new DrillInNavigationTransitionInfo());
                    ThemeHelper.Initialize(frame);
                });
                WindowHelper.TrackWindow(window);
                window.Title = isPackagedApp ? Package.Current.DisplayName : Assembly.GetEntryAssembly().GetName().Name;
                window.AppWindow.SetIcon("favicon.ico");
                window.Activate();
                DesktopWindow.RunEventLoop();
            }
        }

        private partial class DefaultSafeHandle(nint invalidHandleValue, bool ownsHandle) : SafeHandle(invalidHandleValue, ownsHandle)
        {
            public DefaultSafeHandle(nint handle) : this(handle, true) => SetHandle(handle);

            public override bool IsInvalid => handle != nint.Zero;

            protected override bool ReleaseHandle() => true;
        }
    }
}
