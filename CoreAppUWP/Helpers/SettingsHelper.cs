using CommunityToolkit.WinUI.Helpers;
using MetroLog;
using MetroLog.Targets;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.ViewManagement;
using IObjectSerializer = CommunityToolkit.Common.Helpers.IObjectSerializer;

namespace CoreAppUWP.Helpers
{
    public static partial class SettingsHelper
    {
        public const string UpdateDate = nameof(UpdateDate);
        public const string SelectedAppTheme = nameof(SelectedAppTheme);

        public static Type Get<Type>(string key) => LocalObject.Read<Type>(key);
        public static void Set<Type>(string key, Type value) => LocalObject.Save(key, value);
        public static Task<Type> GetFile<Type>(string key) => LocalObject.ReadFileAsync<Type>($"Settings/{key}");
        public static Task SetFile<Type>(string key, Type value) => LocalObject.CreateFileAsync($"Settings/{key}", value);

        public static void SetDefaultSettings()
        {
            if (!LocalObject.KeyExists(UpdateDate))
            {
                LocalObject.Save(UpdateDate, new DateTime());
            }
            if (!LocalObject.KeyExists(SelectedAppTheme))
            {
                LocalObject.Save(SelectedAppTheme, ElementTheme.Default);
            }
        }
    }

    public static partial class SettingsHelper
    {
        public static readonly UISettings UISettings = new();
        public static readonly ILogManager LogManager = LogManagerFactory.CreateLogManager(GetDefaultReleaseConfiguration());
        public static OSVersion OperatingSystemVersion => SystemInformation.Instance.OperatingSystemVersion;
        private static readonly ApplicationDataStorageHelper LocalObject = ApplicationDataStorageHelper.GetCurrent(new SystemTextJsonObjectSerializer());

        static SettingsHelper() => SetDefaultSettings();

        private static LoggingConfiguration GetDefaultReleaseConfiguration()
        {
            string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "MetroLogs");
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            LoggingConfiguration loggingConfiguration = new();
            loggingConfiguration.AddTarget(LogLevel.Info, LogLevel.Fatal, new StreamingFileTarget(path, 7));
            return loggingConfiguration;
        }
    }

    public class SystemTextJsonObjectSerializer : IObjectSerializer
    {
        public string Serialize<T>(T value) => JsonSerializer.Serialize(value);

        public T Deserialize<T>(string value) => JsonSerializer.Deserialize<T>(value);
    }
}
