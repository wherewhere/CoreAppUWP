using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Helpers;
using CoreAppUWP.Common;
using CoreAppUWP.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Xaml;
using WinRT;

namespace CoreAppUWP.ViewModels.SettingsPages
{
    public partial class SettingsViewModel : INotifyPropertyChanged
    {
        public static ConditionalWeakTable<CoreDispatcher, SettingsViewModel> Caches { get; } = [];

        public static string SDKVersion { get; } = Assembly.GetAssembly(typeof(PackageSignatureKind)).GetName().Version.ToString();

        public static string WinRTVersion { get; } = Assembly.GetAssembly(typeof(TrustLevel)).GetName().Version.ToString(3);

        public static string WinUIVersion { get; } = Assembly.GetAssembly(typeof(ControlsResourcesVersion)).GetName().Version.ToString(3);

        public static string DeviceFamily { get; } = WindowHelper.IsPackagedApp ? AnalyticsInfo.VersionInfo.DeviceFamily.Replace('.', ' ') : "Unpackage";

        public static string ToolkitVersion { get; } = Assembly.GetAssembly(typeof(HsvColor)).GetName().Version.ToString(3);

        public static string VersionTextBlockText { get; } = WindowHelper.IsPackagedApp ? $"{Package.Current.DisplayName} v{Package.Current.Id.Version.ToFormattedString(3)}" : Assembly.GetEntryAssembly()?.GetName() is AssemblyName name ? $"{name.Name} {name.Version.ToString(3)}" : "Unknown";

        public CoreDispatcher Dispatcher { get; }

        public int SelectedTheme
        {
            get => 2 - (int)ThemeHelper.ActualTheme;
            set
            {
                if (SelectedTheme != value)
                {
                    ThemeHelper.RootTheme = (ElementTheme)(2 - value);
                    RaisePropertyChangedEvent();
                }
            }
        }

        public bool IsExtendsTitleBar
        {
            get => SettingsHelper.Get<bool>(SettingsHelper.IsExtendsTitleBar);
            set
            {
                if (IsExtendsTitleBar != value)
                {
                    SettingsHelper.Set(SettingsHelper.IsExtendsTitleBar, value);
                    ThemeHelper.UpdateExtendViewIntoTitleBar(value);
                    ThemeHelper.UpdateSystemCaptionButtonColors();
                    RaisePropertyChangedEvent();
                }
            }
        }

        private static bool isCleanLogsButtonEnabled = true;
        public bool IsCleanLogsButtonEnabled
        {
            get => isCleanLogsButtonEnabled;
            set => SetProperty(ref isCleanLogsButtonEnabled, value);
        }

        private static string _aboutTextBlockText;
        public string AboutTextBlockText
        {
            get => _aboutTextBlockText;
            set => SetProperty(ref _aboutTextBlockText, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected static async void RaisePropertyChangedEvent([CallerMemberName] string name = null)
        {
            if (name != null)
            {
                foreach (KeyValuePair<CoreDispatcher, SettingsViewModel> cache in Caches)
                {
                    await cache.Key.ResumeForegroundAsync();
                    cache.Value.PropertyChanged?.Invoke(cache.Value, new PropertyChangedEventArgs(name));
                }
            }
        }

        protected static async void RaisePropertyChangedEvent(params string[] names)
        {
            if (names != null)
            {
                foreach (KeyValuePair<CoreDispatcher, SettingsViewModel> cache in Caches)
                {
                    await cache.Key.ResumeForegroundAsync();
                    names.ForEach(name => cache.Value.PropertyChanged?.Invoke(cache.Value, new PropertyChangedEventArgs(name)));
                }
            }
        }

        [SuppressMessage("Performance", "CA1822:将成员标记为 static", Justification = "<挂起>")]
        protected void SetProperty<TProperty>(ref TProperty property, TProperty value, [CallerMemberName] string name = null)
        {
            if (property == null ? value != null : !property.Equals(value))
            {
                property = value;
                RaisePropertyChangedEvent(name);
            }
        }

        public SettingsViewModel(CoreDispatcher dispatcher)
        {
            Dispatcher = dispatcher;
            Caches.AddOrUpdate(dispatcher, this);
        }

        private async ValueTask GetAboutTextBlockTextAsync(bool reset)
        {
            if (reset || string.IsNullOrWhiteSpace(_aboutTextBlockText))
            {
                await ThreadSwitcher.ResumeBackgroundAsync();
                const string langCode = "en-US";
                if (WindowHelper.IsPackagedApp)
                {
                    Uri dataUri = new($"ms-appx:///Assets/About/About.{langCode}.md");
                    StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);
                    if (file != null)
                    {
                        string markdown = await FileIO.ReadTextAsync(file);
                        AboutTextBlockText = markdown;
                    }
                }
                else
                {
                    string filePath = Path.Combine(AppContext.BaseDirectory, "Assets", "About", $"About.{langCode}.md");
                    if (File.Exists(filePath))
                    {
                        string markdown = await File.ReadAllTextAsync(filePath);
                        AboutTextBlockText = markdown;
                    }
                }
            }
        }

        public async Task<bool> OpenLogFileAsync()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            StorageFolder folder = await SettingsHelper.LocalObject.Folder.CreateFolderAsync("Logs", CreationCollisionOption.OpenIfExists);
            IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();
            if (files is [StorageFile file, ..])
            {
                await Dispatcher.ResumeForegroundAsync();
                return await Launcher.LaunchFileAsync(file);
            }
            return false;
        }

        public async Task CleanLogsAsync()
        {
            IsCleanLogsButtonEnabled = false;
            try
            {
                await ThreadSwitcher.ResumeBackgroundAsync();
                StorageFolder folder = await SettingsHelper.LocalObject.Folder.CreateFolderAsync("Logs", CreationCollisionOption.OpenIfExists);
                await folder.DeleteAsync();
            }
            catch (Exception ex)
            {
                SettingsHelper.LoggerFactory.CreateLogger<SettingsViewModel>().LogError(ex, "Failed to clean the logs. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
            }
            finally
            {
                IsCleanLogsButtonEnabled = true;
            }
        }

        public async Task Refresh(bool reset)
        {
            if (reset)
            {
                RaisePropertyChangedEvent(
                    nameof(SelectedTheme),
                    nameof(IsExtendsTitleBar));
            }
            await GetAboutTextBlockTextAsync(reset);
        }
    }
}
