using MetroLog;
using MetroLog.Targets;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Windows.Storage;
using Windows.UI.Xaml;

namespace CoreAppUWP.Helpers
{
    public static partial class SettingsHelper
    {
        public const string SelectedAppTheme = nameof(SelectedAppTheme);
        public const string IsExtendsTitleBar = nameof(IsExtendsTitleBar);

        public static Type Get<Type>(string key) => SystemTextJsonObjectSerializer.Deserialize<Type>(LocalObject.Values[key]?.ToString());
        public static void Set<Type>(string key, Type value) => LocalObject.Values[key] = SystemTextJsonObjectSerializer.Serialize(value);

        public static void SetDefaultSettings()
        {
            if (!LocalObject.Values.ContainsKey(SelectedAppTheme))
            {
                LocalObject.Values[SelectedAppTheme] = SystemTextJsonObjectSerializer.Serialize(ElementTheme.Default);
            }
            if (!LocalObject.Values.ContainsKey(IsExtendsTitleBar))
            {
                LocalObject.Values[IsExtendsTitleBar] = SystemTextJsonObjectSerializer.Serialize(true);
            }
        }
    }

    public static partial class SettingsHelper
    {
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

    public static class SystemTextJsonObjectSerializer
    {
        public static string Serialize<T>(T value) => value switch
        {
            bool => JsonSerializer.Serialize(value, SourceGenerationContext.Default.Boolean),
            ElementTheme => JsonSerializer.Serialize(value, SourceGenerationContext.Default.ElementTheme),
            _ => value?.ToString(),
        };

        public static T Deserialize<T>(string value)
        {
            if (string.IsNullOrEmpty(value)) { return default; }
            Type type = typeof(T);
            return type == typeof(bool) ? Deserialize(value, SourceGenerationContext.Default.Boolean)
                : type == typeof(ElementTheme) ? Deserialize(value, SourceGenerationContext.Default.ElementTheme)
                : default;
            static T Deserialize<TValue>(string json, JsonTypeInfo<TValue> jsonTypeInfo) => JsonSerializer.Deserialize(json, jsonTypeInfo) is T value ? value : default;
        }
    }

    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(ElementTheme))]
    public partial class SourceGenerationContext : JsonSerializerContext;
}
