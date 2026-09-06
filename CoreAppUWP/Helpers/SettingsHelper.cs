using CommunityToolkit.Common.Helpers;
using Karambolo.Extensions.Logging.File;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace CoreAppUWP.Helpers
{
    public static partial class SettingsHelper
    {
        public const string SelectedAppTheme = nameof(SelectedAppTheme);
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
            if (!LocalObject.KeyExists(IsExtendsTitleBar))
            {
                LocalObject.Save(IsExtendsTitleBar, true);
            }
        }
    }

    public static partial class SettingsHelper
    {
        public static ApplicationDataStorageHelper LocalObject { get; } = ApplicationDataStorageHelper.GetCurrent(new SystemTextJsonObjectSerializer());
        public static ILoggerFactory LoggerFactory { get; } = CreateLoggerFactory();

        static SettingsHelper() => SetDefaultSettings();

        public static ILoggerFactory CreateLoggerFactory() =>
            Microsoft.Extensions.Logging.LoggerFactory.Create(x =>
            {
                if (WindowHelper.IsPackagedApp)
                {
                    _ = x.AddFile(x =>
                    {
                        x.RootPath = LocalObject.Folder.Path;
                        x.IncludeScopes = true;
                        x.BasePath = "Logs";
                        x.Files = [
                            new LogFileOptions()
                            {
                                Path = "Log - <date>.log"
                            }
                        ];
                    });
                }
                _ = x.AddDebug();
            });
    }

    public sealed class SystemTextJsonObjectSerializer : IObjectSerializer
    {
        public string Serialize<T>(T value) => value switch
        {
            bool => JsonSerializer.Serialize(value, SourceGenerationContext.Default.Boolean),
            ElementTheme => JsonSerializer.Serialize(value, SourceGenerationContext.Default.ElementTheme),
            _ => JsonSerializer.Serialize(value, typeof(T), SourceGenerationContext.Default)
        };

        public T Deserialize<T>([StringSyntax(StringSyntaxAttribute.Json)] string value)
        {
            if (string.IsNullOrEmpty(value)) { return default; }
            Type type = typeof(T);
            return type == typeof(bool) ? Deserialize(value, SourceGenerationContext.Default.Boolean)
                : type == typeof(ElementTheme) ? Deserialize(value, SourceGenerationContext.Default.ElementTheme)
                : JsonSerializer.Deserialize(value, type, SourceGenerationContext.Default) is T result ? result : default;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T Deserialize<TValue>([StringSyntax(StringSyntaxAttribute.Json)] string json, JsonTypeInfo<TValue> jsonTypeInfo)
            {
                TValue value = JsonSerializer.Deserialize(json, jsonTypeInfo);
                return Unsafe.As<TValue, T>(ref value);
            }
        }
    }

    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(ElementTheme))]
    public sealed partial class SourceGenerationContext : JsonSerializerContext;
}
