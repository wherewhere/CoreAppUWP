using CommunityToolkit.WinUI;
using CoreAppUWP.Common;
using CoreAppUWP.Controls;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace CoreAppUWP.Helpers
{
    /// <summary>
    /// Class providing functionality around switching and restoring theme settings
    /// </summary>
    public static class ThemeHelper
    {
        public static Window CurrentApplicationWindow { get; private set; }

        // Keep reference so it does not get optimized/garbage collected
        public static UISettings UISettings { get; } = new UISettings();
        public static AccessibilitySettings AccessibilitySettings { get; } = new AccessibilitySettings();

        #region UISettingChanged

        private static readonly WeakEvent<bool> actions = [];

        public static event Action<bool> UISettingChanged
        {
            add => actions.Add(value);
            remove => actions.Remove(value);
        }

        private static void InvokeUISettingChanged(bool value) => actions.Invoke(value);

        #endregion

        #region ActualTheme

        /// <summary>
        /// Gets the current actual theme of the app based on the requested theme of the
        /// root element, or if that value is Default, the requested theme of the Application.
        /// </summary>
        public static ElementTheme ActualTheme => GetActualTheme();

        public static ElementTheme GetActualTheme() =>
            GetActualTheme(Window.Current ?? CurrentApplicationWindow);

        public static ElementTheme GetActualTheme(Window window) =>
            window == null
                ? SettingsHelper.Get<ElementTheme>(SettingsHelper.SelectedAppTheme)
                : window.DispatcherQueue?.HasThreadAccess == false
                    ? window.DispatcherQueue.EnqueueAsync(() =>
                        window.Content is FrameworkElement _rootElement
                            && _rootElement.RequestedTheme != ElementTheme.Default
                                ? _rootElement.RequestedTheme
                                : SettingsHelper.Get<ElementTheme>(SettingsHelper.SelectedAppTheme),
                        DispatcherQueuePriority.High)?.AwaitByTaskCompleteSource()
                        ?? SettingsHelper.Get<ElementTheme>(SettingsHelper.SelectedAppTheme)
                    : window.Content is FrameworkElement rootElement
                        && rootElement.RequestedTheme != ElementTheme.Default
                            ? rootElement.RequestedTheme
                            : SettingsHelper.Get<ElementTheme>(SettingsHelper.SelectedAppTheme);

        public static ValueTask<ElementTheme> GetActualThemeAsync() =>
            GetActualThemeAsync(Window.Current ?? CurrentApplicationWindow);

        public static async ValueTask<ElementTheme> GetActualThemeAsync(Window window) =>
            window == null
                ? SettingsHelper.Get<ElementTheme>(SettingsHelper.SelectedAppTheme)
                : window.DispatcherQueue?.HasThreadAccess == false
                    ? await window.DispatcherQueue.EnqueueAsync(() =>
                        window.Content is FrameworkElement _rootElement
                            && _rootElement.RequestedTheme != ElementTheme.Default
                                ? _rootElement.RequestedTheme
                                : SettingsHelper.Get<ElementTheme>(SettingsHelper.SelectedAppTheme),
                            DispatcherQueuePriority.High)
                    : window.Content is FrameworkElement rootElement
                        && rootElement.RequestedTheme != ElementTheme.Default
                            ? rootElement.RequestedTheme
                            : SettingsHelper.Get<ElementTheme>(SettingsHelper.SelectedAppTheme);

        #endregion

        #region RootTheme

        /// <summary>
        /// Gets or sets (with LocalSettings persistence) the RequestedTheme of the root element.
        /// </summary>
        public static ElementTheme RootTheme
        {
            get => GetRootTheme();
            set => SetRootTheme(value);
        }

        public static ElementTheme GetRootTheme() =>
            GetRootTheme(Window.Current ?? CurrentApplicationWindow);

        public static ElementTheme GetRootTheme(Window window) =>
            window == null
                ? ElementTheme.Default
                : window.DispatcherQueue?.HasThreadAccess == false
                    ? window.DispatcherQueue.EnqueueAsync(() =>
                        window.Content is FrameworkElement _rootElement
                            ? _rootElement.RequestedTheme
                            : ElementTheme.Default,
                        DispatcherQueuePriority.High).AwaitByTaskCompleteSource()
                    : window.Content is FrameworkElement rootElement
                        ? rootElement.RequestedTheme
                        : ElementTheme.Default;

        public static ValueTask<ElementTheme> GetRootThemeAsync() =>
            GetRootThemeAsync(Window.Current ?? CurrentApplicationWindow);

        public static async ValueTask<ElementTheme> GetRootThemeAsync(Window window) =>
            window == null
                ? ElementTheme.Default
                : window.DispatcherQueue?.HasThreadAccess == false
                    ? await window.DispatcherQueue.EnqueueAsync(() =>
                        window.Content is FrameworkElement _rootElement
                            ? _rootElement.RequestedTheme
                            : ElementTheme.Default,
                        DispatcherQueuePriority.High)
                    : window.Content is FrameworkElement rootElement
                        ? rootElement.RequestedTheme
                        : ElementTheme.Default;

        public static async void SetRootTheme(ElementTheme value)
        {
            WindowHelper.ActiveWindows.Values.ForEach(async window =>
            {
                await window.DispatcherQueue.ResumeForegroundAsync();
                if (window.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = value;
                }
            });

            WindowHelper.ActiveDesktopWindows.Values.ForEach(async window =>
            {
                await window.DispatcherQueue.ResumeForegroundAsync();
                if (window.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = value;
                }
            });

            SettingsHelper.Set(SettingsHelper.SelectedAppTheme, value);
            UpdateSystemCaptionButtonColors();
            InvokeUISettingChanged(await IsDarkThemeAsync());
        }

        public static async ValueTask SetRootThemeAsync(ElementTheme value)
        {
            await Task.WhenAll(WindowHelper.ActiveWindows.Values.Select(async window =>
            {
                await window.DispatcherQueue.ResumeForegroundAsync();
                if (window.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = value;
                }
            }));

            await Task.WhenAll(WindowHelper.ActiveDesktopWindows.Values.Select(async window =>
            {
                await window.DispatcherQueue.ResumeForegroundAsync();
                if (window.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = value;
                }
            }));

            SettingsHelper.Set(SettingsHelper.SelectedAppTheme, value);
            UpdateSystemCaptionButtonColors();
            InvokeUISettingChanged(await IsDarkThemeAsync());
        }

        #endregion

        static ThemeHelper()
        {
            // Registering to color changes, thus we notice when user changes theme system wide
            UISettings.ColorValuesChanged += UISettings_ColorValuesChanged;
        }

        public static void Initialize()
        {
            // Save reference as this might be null when the user is in another app
            CurrentApplicationWindow = Window.Current;
            RootTheme = SettingsHelper.Get<ElementTheme>(SettingsHelper.SelectedAppTheme);
        }

        public static async void Initialize(Window window)
        {
            CurrentApplicationWindow ??= window;
            if (window?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = await GetActualThemeAsync();
            }
            UpdateSystemCaptionButtonColors(window);
        }

        public static async void Initialize(DesktopWindow window)
        {
            if (window?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = await GetActualThemeAsync();
            }
            UpdateSystemCaptionButtonColors(window);
        }

        private static async void UISettings_ColorValuesChanged(UISettings sender, object args)
        {
            UpdateSystemCaptionButtonColors();
            InvokeUISettingChanged(await IsDarkThemeAsync());
        }

        public static bool IsDarkTheme()
        {
            return Window.Current != null
                ? ActualTheme == ElementTheme.Default
                    ? Application.Current.RequestedTheme == ApplicationTheme.Dark
                    : ActualTheme == ElementTheme.Dark
                : ActualTheme == ElementTheme.Default
                    ? UISettings?.GetColorValue(UIColorType.Foreground).IsColorLight() == true
                    : ActualTheme == ElementTheme.Dark;
        }

        public static async ValueTask<bool> IsDarkThemeAsync()
        {
            ElementTheme ActualTheme = await GetActualThemeAsync();
            return Window.Current != null
                ? ActualTheme == ElementTheme.Default
                    ? Application.Current.RequestedTheme == ApplicationTheme.Dark
                    : ActualTheme == ElementTheme.Dark
                : ActualTheme == ElementTheme.Default
                    ? UISettings?.GetColorValue(UIColorType.Foreground).IsColorLight() == true
                    : ActualTheme == ElementTheme.Dark;
        }

        public static bool IsDarkTheme(this ElementTheme ActualTheme)
        {
            return Window.Current != null
                ? ActualTheme == ElementTheme.Default
                    ? Application.Current.RequestedTheme == ApplicationTheme.Dark
                    : ActualTheme == ElementTheme.Dark
                : ActualTheme == ElementTheme.Default
                    ? UISettings?.GetColorValue(UIColorType.Foreground).IsColorLight() == true
                    : ActualTheme == ElementTheme.Dark;
        }

        public static bool IsColorLight(this Color color) => ((5 * color.G) + (2 * color.R) + color.B) > (8 * 128);

        public static void UpdateExtendViewIntoTitleBar(bool IsExtendsTitleBar)
        {
            WindowHelper.ActiveWindows.Values.ForEach(async window =>
            {
                await window.DispatcherQueue.ResumeForegroundAsync();
                CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = IsExtendsTitleBar;
            });

            if (!AppWindowTitleBar.IsCustomizationSupported()) { return; }

            WindowHelper.ActiveDesktopWindows.Values.ForEach(async window =>
            {
                await window.DispatcherQueue.ResumeForegroundAsync();
                window.ExtendsContentIntoTitleBar = IsExtendsTitleBar;
            });
        }

        public static async void UpdateSystemCaptionButtonColors()
        {
            bool IsDark = await IsDarkThemeAsync();
            bool IsHighContrast = AccessibilitySettings.HighContrast;

            Color ForegroundColor = IsDark || IsHighContrast ? Colors.White : Colors.Black;
            Color BackgroundColor = IsHighContrast ? Color.FromArgb(255, 0, 0, 0) : IsDark ? Color.FromArgb(255, 32, 32, 32) : Color.FromArgb(255, 243, 243, 243);

            WindowHelper.ActiveWindows.Values.ForEach(async window =>
            {
                await window.DispatcherQueue.ResumeForegroundAsync();
                bool ExtendViewIntoTitleBar = CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar;
                ApplicationViewTitleBar TitleBar = ApplicationView.GetForCurrentView().TitleBar;
                TitleBar.ForegroundColor = TitleBar.ButtonForegroundColor = ForegroundColor;
                TitleBar.BackgroundColor = TitleBar.InactiveBackgroundColor = BackgroundColor;
                TitleBar.ButtonBackgroundColor = TitleBar.ButtonInactiveBackgroundColor = ExtendViewIntoTitleBar ? Colors.Transparent : BackgroundColor;
            });

            if (!AppWindowTitleBar.IsCustomizationSupported()) { return; }

            WindowHelper.ActiveDesktopWindows.Values.ForEach(async window =>
            {
                await window.DispatcherQueue.ResumeForegroundAsync();
                bool ExtendViewIntoTitleBar = window.ExtendsContentIntoTitleBar;
                AppWindowTitleBar TitleBar = window.AppWindow.TitleBar;
                TitleBar.ForegroundColor = TitleBar.ButtonForegroundColor = ForegroundColor;
                TitleBar.BackgroundColor = TitleBar.InactiveBackgroundColor = BackgroundColor;
                TitleBar.ButtonBackgroundColor = TitleBar.ButtonInactiveBackgroundColor = ExtendViewIntoTitleBar ? Colors.Transparent : BackgroundColor;
            });
        }

        public static async void UpdateSystemCaptionButtonColors(Window window)
        {
            await window.DispatcherQueue.ResumeForegroundAsync();

            bool IsDark = window?.Content is FrameworkElement rootElement ? rootElement.RequestedTheme.IsDarkTheme() : await IsDarkThemeAsync();
            bool IsHighContrast = AccessibilitySettings.HighContrast;

            Color ForegroundColor = IsDark || IsHighContrast ? Colors.White : Colors.Black;
            Color BackgroundColor = IsHighContrast ? Color.FromArgb(255, 0, 0, 0) : IsDark ? Color.FromArgb(255, 32, 32, 32) : Color.FromArgb(255, 243, 243, 243);

            bool ExtendViewIntoTitleBar = CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar;
            ApplicationViewTitleBar TitleBar = ApplicationView.GetForCurrentView().TitleBar;
            TitleBar.ForegroundColor = TitleBar.ButtonForegroundColor = ForegroundColor;
            TitleBar.BackgroundColor = TitleBar.InactiveBackgroundColor = BackgroundColor;
            TitleBar.ButtonBackgroundColor = TitleBar.ButtonInactiveBackgroundColor = ExtendViewIntoTitleBar ? Colors.Transparent : BackgroundColor;
        }

        public static async void UpdateSystemCaptionButtonColors(DesktopWindow window)
        {
            if (!AppWindowTitleBar.IsCustomizationSupported()) { return; }

            await window.DispatcherQueue.ResumeForegroundAsync();

            bool IsDark = window?.Content is FrameworkElement rootElement ? rootElement.RequestedTheme.IsDarkTheme() : await IsDarkThemeAsync();
            bool IsHighContrast = AccessibilitySettings.HighContrast;

            Color ForegroundColor = IsDark || IsHighContrast ? Colors.White : Colors.Black;
            Color BackgroundColor = IsHighContrast ? Color.FromArgb(255, 0, 0, 0) : IsDark ? Color.FromArgb(255, 32, 32, 32) : Color.FromArgb(255, 243, 243, 243);

            bool ExtendViewIntoTitleBar = window.ExtendsContentIntoTitleBar;
            AppWindowTitleBar TitleBar = window.AppWindow.TitleBar;
            TitleBar.ForegroundColor = TitleBar.ButtonForegroundColor = ForegroundColor;
            TitleBar.BackgroundColor = TitleBar.InactiveBackgroundColor = BackgroundColor;
            TitleBar.ButtonBackgroundColor = TitleBar.ButtonInactiveBackgroundColor = ExtendViewIntoTitleBar ? Colors.Transparent : BackgroundColor;
        }
    }
}
