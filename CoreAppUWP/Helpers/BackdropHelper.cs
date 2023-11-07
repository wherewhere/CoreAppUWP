using CoreAppUWP.Common;
using CoreAppUWP.Controls;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using Windows.UI;
using WinRT; // required to support Window.As<ICompositionSupportsSystemBackdrop>()

namespace CoreAppUWP.Helpers
{
    public enum BackdropType
    {
        Mica,
        MicaAlt,
        DesktopAcrylic,
        DefaultColor,
    }

    public partial class BackdropHelper
    {
        private readonly Window window;
        private readonly DesktopWindow desktopWindow;
        private readonly WindowsSystemDispatcherQueueHelper m_wsdqHelper;
        private MicaController m_micaController;
        private DesktopAcrylicController m_acrylicController;
        private SystemBackdropConfiguration m_configurationSource;

        public BackdropType? Backdrop { get; private set; } = null;
        public WeakEvent<BackdropType?> BackdropTypeChanged { get; } = [];

        public BackdropHelper(Window window)
        {
            this.window = window;
            m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
            m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();
        }

        public BackdropHelper(DesktopWindow desktopWindow)
        {
            this.desktopWindow = desktopWindow;
            m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
            m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();
        }

        public void SetBackdrop(BackdropType type)
        {
            if (type == Backdrop) { return; }

            // Reset to default color. If the requested type is supported, we'll update to that.
            // Note: This sample completely removes any previous controller to reset to the default
            //       state. This is done so this sample can show what is expected to be the most
            //       common pattern of an app simply choosing one controller type which it sets at
            //       startup. If an app wants to toggle between Mica and Acrylic it could simply
            //       call RemoveSystemBackdropTarget() on the old controller and then setup the new
            //       controller, reusing any existing m_configurationSource and Activated/Closed
            //       event handlers.
            Backdrop = BackdropType.DefaultColor;
            if (m_micaController != null)
            {
                m_micaController.Dispose();
                m_micaController = null;
            }
            if (m_acrylicController != null)
            {
                m_acrylicController.Dispose();
                m_acrylicController = null;
            }

            if (window != null)
            {
                window.Closed -= Window_Closed;
                window.Activated -= Window_Activated;
                ((FrameworkElement)window.Content).ActualThemeChanged -= FrameworkElement_ThemeChanged;
            }
            else
            {
                desktopWindow.AppWindow.Closing -= AppWindow_Closing;
                ((FrameworkElement)desktopWindow.Content).ActualThemeChanged -= FrameworkElement_ThemeChanged;
            }

            m_configurationSource = null;

            if (type is BackdropType.Mica or BackdropType.MicaAlt)
            {
                if (TrySetMicaBackdrop(type == BackdropType.MicaAlt ? MicaKind.BaseAlt : MicaKind.Base))
                {
                    Backdrop = type;
                }
                //else
                //{
                //    // Mica isn't supported. Try Acrylic.
                //    type = BackdropType.DesktopAcrylic;
                //}
            }
            if (type == BackdropType.DesktopAcrylic)
            {
                if (TrySetAcrylicBackdrop())
                {
                    Backdrop = type;
                }
            }

            BackdropTypeChanged.Invoke(Backdrop);
        }

        private bool TrySetMicaBackdrop(MicaKind kind = MicaKind.Base)
        {
            if (MicaController.IsSupported())
            {
                // Hooking up the policy object
                m_configurationSource = new SystemBackdropConfiguration();

                if (window != null)
                {
                    window.Closed += Window_Closed;
                    window.Activated += Window_Activated;
                    ((FrameworkElement)window.Content).ActualThemeChanged += FrameworkElement_ThemeChanged;
                }
                else
                {
                    desktopWindow.AppWindow.Closing += AppWindow_Closing;
                    ((FrameworkElement)desktopWindow.Content).ActualThemeChanged += FrameworkElement_ThemeChanged;
                }

                // Initial configuration state.
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                m_micaController = new MicaController { Kind = kind };

                // Enable the system backdrop.
                if (window != null)
                {
                    // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
                    m_micaController.AddSystemBackdropTarget(window.As<ICompositionSupportsSystemBackdrop>());
                }
                else if (desktopWindow != null)
                {
                    m_micaController.AddSystemBackdropTarget(desktopWindow.WindowXamlSource.As<ICompositionSupportsSystemBackdrop>());
                }

                m_micaController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // succeeded
            }

            return false; // Mica is not supported on this system
        }

        private bool TrySetAcrylicBackdrop()
        {
            if (DesktopAcrylicController.IsSupported())
            {
                // Hooking up the policy object
                m_configurationSource = new SystemBackdropConfiguration();

                if (window != null)
                {
                    window.Closed += Window_Closed;
                    window.Activated += Window_Activated;
                    ((FrameworkElement)window.Content).ActualThemeChanged += FrameworkElement_ThemeChanged;
                }
                else
                {
                    desktopWindow.AppWindow.Closing += AppWindow_Closing;
                    ((FrameworkElement)desktopWindow.Content).ActualThemeChanged += FrameworkElement_ThemeChanged;
                }

                // Initial configuration state.
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                Color BackgroundColor = ThemeHelper.IsDarkTheme() ? Color.FromArgb(255, 32, 32, 32) : Color.FromArgb(255, 243, 243, 243);
                m_acrylicController = new DesktopAcrylicController { TintColor = BackgroundColor, FallbackColor = BackgroundColor };

                // Enable the system backdrop.
                if (window != null)
                {
                    // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
                    m_acrylicController.AddSystemBackdropTarget(window.As<ICompositionSupportsSystemBackdrop>());
                }
                else if (desktopWindow != null)
                {
                    m_acrylicController.AddSystemBackdropTarget(desktopWindow.WindowXamlSource.As<ICompositionSupportsSystemBackdrop>());
                }

                m_acrylicController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // succeeded
            }

            return false; // Acrylic is not supported on this system
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed so it doesn't try to
            // use this closed window.
            if (m_micaController != null)
            {
                m_micaController.Dispose();
                m_micaController = null;
            }
            if (m_acrylicController != null)
            {
                m_acrylicController.Dispose();
                m_acrylicController = null;
            }
            ((FrameworkElement)window.Content).ActualThemeChanged -= FrameworkElement_ThemeChanged;
            window.Activated -= Window_Activated;
            m_configurationSource = null;
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed so it doesn't try to
            // use this closed window.
            if (m_micaController != null)
            {
                m_micaController.Dispose();
                m_micaController = null;
            }
            if (m_acrylicController != null)
            {
                m_acrylicController.Dispose();
                m_acrylicController = null;
            }
            ((FrameworkElement)desktopWindow.Content).ActualThemeChanged -= FrameworkElement_ThemeChanged;
            m_configurationSource = null;
        }

        private void FrameworkElement_ThemeChanged(FrameworkElement sender, object args)
        {
            if (m_configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
            if (m_acrylicController != null)
            {
                Color BackgroundColor = sender.ActualTheme.IsDarkTheme() ? Color.FromArgb(255, 32, 32, 32) : Color.FromArgb(255, 243, 243, 243);
                m_acrylicController.TintColor = m_acrylicController.FallbackColor = BackgroundColor;
            }
        }

        private void SetConfigurationSourceTheme()
        {
            m_configurationSource.Theme = (((window == null ? desktopWindow?.Content : window.Content) as FrameworkElement)?.ActualTheme) switch
            {
                ElementTheme.Dark => SystemBackdropTheme.Dark,
                ElementTheme.Light => SystemBackdropTheme.Light,
                ElementTheme.Default => SystemBackdropTheme.Default,
                _ => SystemBackdropTheme.Default,
            };
        }
    }

    public partial class BackdropHelper
    {
        public static void RegisterWindow(Window window)
        {
            if (!ActiveWindows.ContainsKey(window))
            {
                window.Closed += (sender, args) =>
                {
                    ActiveWindows.Remove(window);
                    window = null;
                };
                ActiveWindows[window] = new BackdropHelper(window);
            }
        }

        public static void RegisterWindow(DesktopWindow window)
        {
            if (!ActiveDesktopWindows.ContainsKey(window))
            {
                window.AppWindow.Closing += (sender, args) =>
                {
                    ActiveDesktopWindows.Remove(window);
                    window = null;
                };
                ActiveDesktopWindows[window] = new BackdropHelper(window);
            }
        }

        public static void SetBackdrop(Window window, BackdropType type)
        {
            if (ActiveWindows.TryGetValue(window, out BackdropHelper backdrop))
            {
                backdrop.SetBackdrop(type);
            }
        }

        public static void SetBackdrop(DesktopWindow window, BackdropType type)
        {
            if (ActiveDesktopWindows.TryGetValue(window, out BackdropHelper backdrop))
            {
                backdrop.SetBackdrop(type);
            }
        }

        public static void SetAllBackdrop(BackdropType type)
        {
            ActiveWindows.Values.ForEach(async x =>
            {
                if (x.window.DispatcherQueue?.HasThreadAccess == false)
                {
                    await x.window.DispatcherQueue.ResumeForegroundAsync();
                }
                x.SetBackdrop(type);
            });

            ActiveDesktopWindows.Values.ForEach(async x =>
            {
                if (x.desktopWindow.DispatcherQueue?.HasThreadAccess == false)
                {
                    await x.desktopWindow.DispatcherQueue.ResumeForegroundAsync();
                }
                x.SetBackdrop(type);
            });
        }

        public static BackdropType? GetBackdrop(Window window)
        {
            return ActiveWindows.TryGetValue(window, out BackdropHelper backdrop) ? backdrop.Backdrop : null;
        }

        public static BackdropType? GetBackdrop(DesktopWindow window)
        {
            return ActiveDesktopWindows.TryGetValue(window, out BackdropHelper backdrop) ? backdrop.Backdrop : null;
        }

        public static void AddBackdropTypeChanged(Window window, Action<BackdropType?> typedEventHandler)
        {
            if (ActiveWindows.TryGetValue(window, out BackdropHelper backdrop))
            {
                backdrop.BackdropTypeChanged.Add(typedEventHandler);
            }
        }

        public static void AddBackdropTypeChanged(DesktopWindow window, Action<BackdropType?> typedEventHandler)
        {
            if (ActiveDesktopWindows.TryGetValue(window, out BackdropHelper backdrop))
            {
                backdrop.BackdropTypeChanged.Add(typedEventHandler);
            }
        }

        public static void RemoveBackdropTypeChanged(Window window, Action<BackdropType?> typedEventHandler)
        {
            if (ActiveWindows.TryGetValue(window, out BackdropHelper backdrop))
            {
                backdrop.BackdropTypeChanged.Remove(typedEventHandler);
            }
        }

        public static void RemoveBackdropTypeChanged(DesktopWindow window, Action<BackdropType?> typedEventHandler)
        {
            if (ActiveDesktopWindows.TryGetValue(window, out BackdropHelper backdrop))
            {
                backdrop.BackdropTypeChanged.Remove(typedEventHandler);
            }
        }

        public static Dictionary<Window, BackdropHelper> ActiveWindows { get; } = [];
        public static Dictionary<DesktopWindow, BackdropHelper> ActiveDesktopWindows { get; } = [];
    }
}
