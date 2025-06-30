using MetroLog;
using MetroLog.Targets;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Windows.Storage;
using IObjectSerializer = CommunityToolkit.Common.Helpers.IObjectSerializer;

namespace CoreAppUWP.Helpers
{
    public static partial class SettingsHelper
    {
        public const string SelectedAppTheme = nameof(SelectedAppTheme);
        public const string SelectedBackdrop = nameof(SelectedBackdrop);
        public const string IsExtendsTitleBar = nameof(IsExtendsTitleBar);

        public static Type Get<Type>(string key) => LocalObject.Read<Type>(key);
        public static void Set<Type>(string key, Type value) => LocalObject.Save(key, value);
        public static Task<Type> GetFile<Type>(string key) => LocalObject.ReadFileAsync<Type>($"Settings/{key}");
        public static Task SetFile<Type>(string key, Type value) => LocalObject.CreateFileAsync($"Settings/{key}", value);

        public static void SetDefaultSettings()
        {
            if (!LocalObject.KeyExists(SelectedAppTheme))
            {
                LocalObject.Save(SelectedAppTheme, ElementTheme.Default);
            }
            if (!LocalObject.KeyExists(SelectedBackdrop))
            {
                LocalObject.Save(SelectedBackdrop, BackdropType.DefaultColor);
            }
            if (!LocalObject.KeyExists(IsExtendsTitleBar))
            {
                LocalObject.Save(IsExtendsTitleBar, true);
            }
        }
    }

    public static partial class SettingsHelper
    {
        public static ILogManager LogManager { get; private set; }
        public static ApplicationDataStorageHelper LocalObject { get; } = ApplicationDataStorageHelper.GetCurrent(new SystemTextJsonObjectSerializer());

        static SettingsHelper() => SetDefaultSettings();

        public static void CreateLogManager()
        {
            if (LogManager == null)
            {
                if (WindowHelper.IsPackagedApp)
                {
                    string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "MetroLogs");
                    if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
                    LoggingConfiguration loggingConfiguration = new();
                    loggingConfiguration.AddTarget(LogLevel.Info, LogLevel.Fatal, new StreamingFileTarget(path, 7));
                    LogManager = LogManagerFactory.CreateLogManager(loggingConfiguration);
                }
                else
                {
                    LogManager = LogManagerFactory.CreateLogManager();
                }
            }
        }
    }

    public class SystemTextJsonObjectSerializer : IObjectSerializer
    {
        public string Serialize<T>(T value) => value switch
        {
            bool => JsonSerializer.Serialize(value, SourceGenerationContext.Default.Boolean),
            ElementTheme => JsonSerializer.Serialize(value, SourceGenerationContext.Default.ElementTheme),
            BackdropType => JsonSerializer.Serialize(value, SourceGenerationContext.Default.BackdropType),
            _ => value?.ToString(),
        };

        public T Deserialize<T>([StringSyntax(StringSyntaxAttribute.Json)] string value)
        {
            if (string.IsNullOrEmpty(value)) { return default; }
            Type type = typeof(T);
            return type == typeof(bool) ? Deserialize(value, SourceGenerationContext.Default.Boolean)
                : type == typeof(ElementTheme) ? Deserialize(value, SourceGenerationContext.Default.ElementTheme)
                : type == typeof(BackdropType) ? Deserialize(value, SourceGenerationContext.Default.BackdropType)
                : default;
            static T Deserialize<TValue>([StringSyntax(StringSyntaxAttribute.Json)] string json, JsonTypeInfo<TValue> jsonTypeInfo) => JsonSerializer.Deserialize(json, jsonTypeInfo) is T value ? value : default;
        }
    }

    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(ElementTheme))]
    [JsonSerializable(typeof(BackdropType))]
    public partial class SourceGenerationContext : JsonSerializerContext;
}
