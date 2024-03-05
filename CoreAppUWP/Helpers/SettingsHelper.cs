using MetroLog;
using MetroLog.Targets;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace CoreAppUWP.Helpers
{
    public static partial class SettingsHelper
    {
        public const string SelectedAppTheme = nameof(SelectedAppTheme);
        public const string IsExtendsTitleBar = nameof(IsExtendsTitleBar);

        public static Type Get<Type>(string key) => serializer.Deserialize<Type>(LocalObject.Values[key]?.ToString());
        public static void Set<Type>(string key, Type value) => LocalObject.Values[key] = serializer.Serialize(value);

        public static void SetDefaultSettings()
        {
            if (!LocalObject.Values.ContainsKey(SelectedAppTheme))
            {
                LocalObject.Values[SelectedAppTheme] = serializer.Serialize(ElementTheme.Default);
            }
            if (!LocalObject.Values.ContainsKey(IsExtendsTitleBar))
            {
                LocalObject.Values[IsExtendsTitleBar] = serializer.Serialize(true);
            }
        }
    }

    public static partial class SettingsHelper
    {
        private static readonly SystemTextJsonObjectSerializer serializer = new();

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

    public class SystemTextJsonObjectSerializer
    {
        public string Serialize<T>(T value) => value switch
        {
            bool => JsonSerializer.Serialize(value, SourceGenerationContext.Default.Boolean),
            ElementTheme => JsonSerializer.Serialize(value, SourceGenerationContext.Default.ElementTheme),
#if DEBUG
            _ => JsonSerializer.Serialize(value)
#else
            _ => value?.ToString(),
#endif
        };

        public T Deserialize<T>(string value)
        {
            if (string.IsNullOrEmpty(value)) { return default; }
            Type type = typeof(T);
            return type == typeof(bool) && JsonSerializer.Deserialize(value, SourceGenerationContext.Default.Boolean) is T @bool
                ? @bool
                : type == typeof(ElementTheme) && JsonSerializer.Deserialize(value, SourceGenerationContext.Default.ElementTheme) is T ElementTheme
                    ? ElementTheme
#if DEBUG
                    : JsonSerializer.Deserialize<T>(value);
#else
                    : default;
#endif
        }
    }

    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(ElementTheme))]
    public partial class SourceGenerationContext : JsonSerializerContext;
}
