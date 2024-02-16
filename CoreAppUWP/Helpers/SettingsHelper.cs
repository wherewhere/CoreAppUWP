using MetroLog;
using MetroLog.Targets;
using System.IO;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace CoreAppUWP.Helpers
{
    public static partial class SettingsHelper
    {
        public const string SelectedAppTheme = nameof(SelectedAppTheme);
        public const string SelectedBackdrop = nameof(SelectedBackdrop);
        public const string IsExtendsTitleBar = nameof(IsExtendsTitleBar);

        public static Type Get<Type>(string key) => (Type)LocalObject.Values[key];
        public static void Set<Type>(string key, Type value) => LocalObject.Values[key] = value;

        public static void SetDefaultSettings()
        {
            if (!LocalObject.Values.ContainsKey(SelectedAppTheme))
            {
                LocalObject.Values[SelectedAppTheme] = (int)ElementTheme.Default;
            }
            if (!LocalObject.Values.ContainsKey(IsExtendsTitleBar))
            {
                LocalObject.Values[IsExtendsTitleBar] = true;
            }
        }
    }

    public static partial class SettingsHelper
    {
        public static UISettings UISettings { get; } = new();
        public static ILogManager LogManager { get; private set; }
        public static ApplicationDataContainer LocalObject { get; } = ApplicationData.Current.LocalSettings;

        static SettingsHelper() => SetDefaultSettings();

        public static void CreateLogManager()
        {
            if (LogManager == null)
            {
                string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "MetroLogs");
                if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
                LoggingConfiguration loggingConfiguration = new();
                loggingConfiguration.AddTarget(LogLevel.Info, LogLevel.Fatal, new StreamingFileTarget(path, 7));
                LogManager = LogManagerFactory.CreateLogManager(loggingConfiguration);
            }
        }
    }
}
