using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Data.Xml.Dom;
using Windows.Management.Deployment;
using Windows.Storage;
using WinRT;

namespace CoreAppUWP
{
    public class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                ComWrappersSupport.InitializeComWrappers();
                CoreApplication.Run(new App());
            }
            catch
            {
                StartCoreApplication();
            }
        }

        private static async void StartCoreApplication()
        {
            PackageManager manager = new();
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            XmlDocument manifest = await XmlDocument.LoadFromFileAsync(await StorageFile.GetFileFromPathAsync(Path.Combine(basePath, "AppxManifest.xml")));
            IXmlNode identity = manifest.GetElementsByTagName("Identity")[0];
            string name = identity.Attributes.FirstOrDefault((x) => x.NodeName == "Name")?.InnerText;
            IXmlNode application = manifest.GetElementsByTagName("Application")[0];
            string id = application.Attributes.FirstOrDefault((x) => x.NodeName == "Id")?.InnerText;
            IEnumerable<Package> packages = manager.FindPackagesForUser("").Where((x) => x.Id.FamilyName.StartsWith(name));
            if (packages.Any())
            {
                Process.Start("explorer.exe", $"shell:AppsFolder\\{packages.FirstOrDefault().Id.FamilyName}!{id}");
            }
        }
    }
}
