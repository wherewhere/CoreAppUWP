using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Data.Xml.Dom;
using Windows.Management.Deployment;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinRT;

namespace CoreAppUWP
{
    public partial class Program
    {
        private static unsafe bool IsPackagedApp
        {
            get
            {
                uint length = 0;
                PWSTR str = new();
                _ = PInvoke.GetCurrentPackageFullName(ref length, str);

                char[] array = new char[length];
                fixed (char* ptr = array)
                {
                    str = new(ptr);
                    WIN32_ERROR result = PInvoke.GetCurrentPackageFullName(ref length, str);
                    return result != WIN32_ERROR.APPMODEL_ERROR_NO_PACKAGE;
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
                    return registryKey.GetValue("EnableUWPWindow") is int value && value > 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        [MTAThread]
        private static void Main()
        {
            ComWrappersSupport.InitializeComWrappers();
            if (IsPackagedApp)
            {
                if (IsSupportCoreWindow)
                {
                    Application.Start(p =>
                    {
                        DispatcherQueueSynchronizationContext context = new(DispatcherQueue.GetForCurrentThread());
                        SynchronizationContext.SetSynchronizationContext(context);
                        _ = new App();
                    });
                }
            }
            else
            {
                StartCoreApplicationAsync().Wait();
            }
        }

        private static async Task StartCoreApplicationAsync()
        {
            PackageManager manager = new();
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            XmlDocument manifest = await XmlDocument.LoadFromFileAsync(await StorageFile.GetFileFromPathAsync(Path.Combine(basePath, "AppxManifest.xml")));
            IXmlNode identity = manifest.GetElementsByTagName("Identity")?[0];
            string name = identity.Attributes.FirstOrDefault((x) => x.NodeName == "Name")?.InnerText;
            IXmlNode application = manifest.GetElementsByTagName("Application")?[0];
            string id = application.Attributes.FirstOrDefault((x) => x.NodeName == "Id")?.InnerText;
            IEnumerable<Package> packages = manager.FindPackagesForUser("").Where((x) => x.Id.FamilyName.StartsWith(name));
            if (packages.FirstOrDefault() is Package package)
            {
                IReadOnlyList<AppListEntry> entries = await package.GetAppListEntriesAsync();
                if (entries?[0] is AppListEntry entry)
                {
                    _ = await entry.LaunchAsync();
                }
            }
        }
    }
}
