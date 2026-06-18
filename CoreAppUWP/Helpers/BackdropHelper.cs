using CoreAppUWP.Common;
using CoreAppUWP.Controls;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.UI;
using Windows.Win32;
using Windows.Win32.System.WinRT;
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

    public abstract class BackdropHelper<T>
    {
        protected readonly T window;
        private readonly WindowsSystemDispatcherQueueHelper m_wsdqHelper;
        private ISystemBackdropControllerWithTargets m_controller;
        protected SystemBackdropConfiguration m_configurationSource;

        public BackdropType? Backdrop { get; private set; } = null;
        protected abstract FrameworkElement Content { get; }
        protected abstract ICompositionSupportsSystemBackdrop CompositionSupportsSystemBackdrop { get; }

        #region BackdropTypeChanged

        private readonly WeakEvent<BackdropType?> actions = [];

        public event Action<BackdropType?> BackdropTypeChanged
        {
            add => actions.Add(value);
            remove => actions.Remove(value);
        }

        private void InvokeBackdropTypeChanged(BackdropType? value) => actions.Invoke(value);

        #endregion

        public BackdropHelper(T window)
        {
            this.window = window;
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

            Unregister();

            m_configurationSource = null;

            if (type is BackdropType.Mica or BackdropType.MicaAlt)
            {
                if (TrySetMicaBackdrop(type == BackdropType.MicaAlt ? MicaKind.BaseAlt : MicaKind.Base))
                {
                    Backdrop = type;
                }
            }
            if (type == BackdropType.DesktopAcrylic)
            {
                if (TrySetAcrylicBackdrop())
                {
                    Backdrop = type;
                }
            }

            InvokeBackdropTypeChanged(Backdrop);
        }

        private bool TrySetMicaBackdrop(MicaKind kind = MicaKind.Base)
        {
            if (MicaController.IsSupported())
            {
                // Hooking up the policy object
                m_configurationSource = new SystemBackdropConfiguration();

                Register();

                // Initial configuration state.
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                m_controller = new MicaController { Kind = kind };

                // Enable the system backdrop.
                if (window != null)
                {
                    // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
                    m_controller.AddSystemBackdropTarget(CompositionSupportsSystemBackdrop);
                }

                m_controller.SetSystemBackdropConfiguration(m_configurationSource);
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
                Register();

                // Initial configuration state.
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                Color BackgroundColor = ThemeHelper.IsDarkTheme() ? Color.FromArgb(255, 32, 32, 32) : Color.FromArgb(255, 243, 243, 243);
                m_controller = new DesktopAcrylicController { TintColor = BackgroundColor, FallbackColor = BackgroundColor };

                // Enable the system backdrop.
                if (window != null)
                {
                    // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
                    m_controller.AddSystemBackdropTarget(CompositionSupportsSystemBackdrop);
                }

                m_controller.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // succeeded
            }

            return false; // Acrylic is not supported on this system
        }

        protected void FrameworkElement_ThemeChanged(FrameworkElement sender, object args)
        {
            if (m_configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
            if (m_controller is DesktopAcrylicController m_acrylicController)
            {
                Color BackgroundColor = ThemeHelper.IsDarkTheme(sender.ActualTheme) ? Color.FromArgb(255, 32, 32, 32) : Color.FromArgb(255, 243, 243, 243);
                m_acrylicController.TintColor = m_acrylicController.FallbackColor = BackgroundColor;
            }
        }

        private void SetConfigurationSourceTheme()
        {
            m_configurationSource.Theme = Content?.ActualTheme switch
            {
                ElementTheme.Dark => SystemBackdropTheme.Dark,
                ElementTheme.Light => SystemBackdropTheme.Light,
                ElementTheme.Default => SystemBackdropTheme.Default,
                _ => SystemBackdropTheme.Default,
            };
        }

        protected virtual void Register() => Content.ActualThemeChanged += FrameworkElement_ThemeChanged;

        protected virtual void Unregister()
        {
            Content.ActualThemeChanged -= FrameworkElement_ThemeChanged;
            if (m_controller != null)
            {
                m_controller.Dispose();
                m_controller = null;
            }
        }

        public abstract DispatcherQueueThreadSwitcher ResumeForegroundAsync();
    }

    public class WindowBackdropHelper(Window window) : BackdropHelper<Window>(window)
    {
        protected override FrameworkElement Content => window.Content as FrameworkElement;
        protected override ICompositionSupportsSystemBackdrop CompositionSupportsSystemBackdrop => window.As<ICompositionSupportsSystemBackdrop>();

        protected override void Register()
        {
            base.Register();
            window.Closed += Window_Closed;
            window.Activated += Window_Activated;
        }

        protected override void Unregister()
        {
            window.Closed -= Window_Closed;
            window.Activated -= Window_Activated;
            base.Unregister();
        }

        public override DispatcherQueueThreadSwitcher ResumeForegroundAsync() => window.DispatcherQueue.ResumeForegroundAsync();

        private void Window_Activated(object sender, WindowActivatedEventArgs args) => m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            Unregister();
            m_configurationSource = null;
        }
    }

    public class DesktopWindowBackdropHelper(DesktopWindow window) : BackdropHelper<DesktopWindow>(window)
    {
        protected override FrameworkElement Content => window.Content as FrameworkElement;
        protected override DesktopWindow CompositionSupportsSystemBackdrop => window;

        protected override void Register()
        {
            base.Register();
            window.Closing += AppWindow_Closing;
        }

        protected override void Unregister()
        {
            window.Closing -= AppWindow_Closing;
            base.Unregister();
        }

        public override DispatcherQueueThreadSwitcher ResumeForegroundAsync() => window.DispatcherQueue.ResumeForegroundAsync();

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            Unregister();
            m_configurationSource = null;
        }
    }

    public static class BackdropHelper
    {
        public static void Register(Window window)
        {
            if (!ActiveWindows.ContainsKey(window))
            {
                window.Closed += (sender, args) =>
                {
                    ActiveWindows.Remove(window);
                    window = null;
                };
                ActiveWindows[window] = new WindowBackdropHelper(window);
            }
        }

        public static void Register(DesktopWindow window)
        {
            if (!ActiveDesktopWindows.ContainsKey(window))
            {
                window.Closing += (sender, args) =>
                {
                    ActiveDesktopWindows.Remove(window);
                    window = null;
                };
                ActiveDesktopWindows[window] = new DesktopWindowBackdropHelper(window);
            }
        }

        public static void SetBackdrop(this Window window, BackdropType type)
        {
            if (ActiveWindows.TryGetValue(window, out BackdropHelper<Window> backdrop))
            {
                backdrop.SetBackdrop(type);
            }
        }

        public static void SetBackdrop(this DesktopWindow window, BackdropType type)
        {
            if (ActiveDesktopWindows.TryGetValue(window, out BackdropHelper<DesktopWindow> backdrop))
            {
                backdrop.SetBackdrop(type);
            }
        }

        public static void SetAllBackdrop(this BackdropType type)
        {
            ActiveWindows.Values.ForEach(async x =>
            {
                await x.ResumeForegroundAsync();
                x.SetBackdrop(type);
            });

            ActiveDesktopWindows.Values.ForEach(async x =>
            {
                await x.ResumeForegroundAsync();
                x.SetBackdrop(type);
            });
        }

        public static BackdropType? GetBackdrop(this Window window)
        {
            return ActiveWindows.TryGetValue(window, out BackdropHelper<Window> backdrop) ? backdrop.Backdrop : null;
        }

        public static BackdropType? GetBackdrop(this DesktopWindow window)
        {
            return ActiveDesktopWindows.TryGetValue(window, out BackdropHelper<DesktopWindow> backdrop) ? backdrop.Backdrop : null;
        }

        public static void AddBackdropTypeChanged(this Window window, Action<BackdropType?> typedEventHandler)
        {
            if (ActiveWindows.TryGetValue(window, out BackdropHelper<Window> backdrop))
            {
                backdrop.BackdropTypeChanged += typedEventHandler;
            }
        }

        public static void AddBackdropTypeChanged(this DesktopWindow window, Action<BackdropType?> typedEventHandler)
        {
            if (ActiveDesktopWindows.TryGetValue(window, out BackdropHelper<DesktopWindow> backdrop))
            {
                backdrop.BackdropTypeChanged += typedEventHandler;
            }
        }

        public static void RemoveBackdropTypeChanged(this Window window, Action<BackdropType?> typedEventHandler)
        {
            if (ActiveWindows.TryGetValue(window, out BackdropHelper<Window> backdrop))
            {
                backdrop.BackdropTypeChanged -= typedEventHandler;
            }
        }

        public static void RemoveBackdropTypeChanged(this DesktopWindow window, Action<BackdropType?> typedEventHandler)
        {
            if (ActiveDesktopWindows.TryGetValue(window, out BackdropHelper<DesktopWindow> backdrop))
            {
                backdrop.BackdropTypeChanged -= typedEventHandler;
            }
        }

        public static Dictionary<Window, BackdropHelper<Window>> ActiveWindows { get; } = [];
        public static Dictionary<DesktopWindow, BackdropHelper<DesktopWindow>> ActiveDesktopWindows { get; } = [];
    }

    public partial class WindowsSystemDispatcherQueueHelper
    {
        [LibraryImport("CoreMessaging.dll")]
        private static partial int CreateDispatcherQueueController(DispatcherQueueOptions options, out nint instance);

        private nint m_dispatcherQueueController = 0;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
            {
                // one already exists, so we'll just use it.
                return;
            }

            if (m_dispatcherQueueController == 0)
            {
                DispatcherQueueOptions options = new()
                {
                    dwSize = (uint)Unsafe.SizeOf<DispatcherQueueOptions>(),
                    threadType = DISPATCHERQUEUE_THREAD_TYPE.DQTYPE_THREAD_CURRENT,     // DQTYPE_THREAD_CURRENT
                    apartmentType = DISPATCHERQUEUE_THREAD_APARTMENTTYPE.DQTAT_COM_STA, // DQTAT_COM_STA
                };

                _ = CreateDispatcherQueueController(options, out nint dispatcherQueueController);
                m_dispatcherQueueController = dispatcherQueueController;
            }
        }
    }
}
