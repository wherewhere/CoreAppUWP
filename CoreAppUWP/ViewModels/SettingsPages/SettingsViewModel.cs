using CoreAppUWP.Common;
using CoreAppUWP.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Xaml;
using WinRT;

namespace CoreAppUWP.ViewModels.SettingsPages
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public static ConditionalWeakTable<CoreDispatcher, SettingsViewModel> Caches { get; } = [];

        public static string SDKVersion { get; } = Assembly.GetAssembly(typeof(PackageSignatureKind)).GetName().Version.ToString();

        public static string WinRTVersion { get; } = Assembly.GetAssembly(typeof(TrustLevel)).GetName().Version.ToString(3);

        public static string DeviceFamily { get; } = AnalyticsInfo.VersionInfo.DeviceFamily.Replace('.', ' ');

        public static string VersionTextBlockText { get; } = $"{Package.Current.DisplayName} v{Package.Current.Id.Version.ToFormattedString(3)}";

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
                Uri dataUri = new($"ms-appx:///Assets/About/About.{langCode}.md");
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);
                if (file != null)
                {
                    string markdown = await FileIO.ReadTextAsync(file);
                    AboutTextBlockText = markdown;
                }
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
