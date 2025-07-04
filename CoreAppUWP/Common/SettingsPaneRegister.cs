using CoreAppUWP.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Search;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.System;
using Windows.UI.ApplicationSettings;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace CoreAppUWP.Common
{
    public static class SettingsPaneRegister
    {
        public static bool IsSearchPaneSupported { get; } = ApiInformation.IsTypePresent("Windows.UI.ApplicationSettings.SettingsPane") && CheckSearchExtension();
        public static bool IsSettingsPaneSupported { get; } = ApiInformation.IsTypePresent("Windows.UI.ApplicationSettings.SettingsPane");

        public static void Register(Window window)
        {
            try
            {
                if (IsSettingsPaneSupported)
                {
                    SettingsPane settingsPane = SettingsPane.GetForCurrentView();
                    settingsPane.CommandsRequested -= OnCommandsRequested;
                    settingsPane.CommandsRequested += OnCommandsRequested;
                    window.Dispatcher.AcceleratorKeyActivated -= Dispatcher_AcceleratorKeyActivated;
                    window.Dispatcher.AcceleratorKeyActivated += Dispatcher_AcceleratorKeyActivated;
                }
            }
            catch (Exception ex)
            {
                SettingsHelper.LoggerFactory.CreateLogger(typeof(SettingsPaneRegister)).LogError(ex, "Failed to register settings pane. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
            }
        }

        public static void Unregister(Window window)
        {
            try
            {
                if (IsSettingsPaneSupported)
                {
                    SettingsPane.GetForCurrentView().CommandsRequested -= OnCommandsRequested;
                    window.Dispatcher.AcceleratorKeyActivated -= Dispatcher_AcceleratorKeyActivated;
                }
            }
            catch (Exception ex)
            {
                SettingsHelper.LoggerFactory.CreateLogger(typeof(SettingsPaneRegister)).LogError(ex, "Failed to unregister settings pane. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
            }
        }

        private static void OnCommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            args.Request.ApplicationCommands.Add(
                new SettingsCommand(
                    "Feedback",
                    "Feedback",
                    handler => _ = Launcher.LaunchUriAsync(new Uri("https://github.com/wherewhere/CoreAppUWP/issues"))));
            args.Request.ApplicationCommands.Add(
                new SettingsCommand(
                    "LogFolder",
                    "LogFolder",
                    async handler => _ = Launcher.LaunchFolderAsync(await ApplicationData.Current.LocalFolder.CreateFolderAsync("Logs", CreationCollisionOption.OpenIfExists))));
            args.Request.ApplicationCommands.Add(
                new SettingsCommand(
                    "Repository",
                    "Repository",
                    handler => _ = Launcher.LaunchUriAsync(new Uri("https://github.com/wherewhere/CoreAppUWP"))));
        }

        private static void Dispatcher_AcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
        {
            if (args.EventType is CoreAcceleratorKeyEventType.KeyDown or CoreAcceleratorKeyEventType.SystemKeyDown)
            {
                CoreWindow window = CoreWindow.GetForCurrentThread();
                CoreVirtualKeyStates ctrl = window.GetKeyState(VirtualKey.Control);
                if (ctrl.HasFlag(CoreVirtualKeyStates.Down))
                {
                    CoreVirtualKeyStates shift = window.GetKeyState(VirtualKey.Shift);
                    if (shift.HasFlag(CoreVirtualKeyStates.Down))
                    {
                        switch (args.VirtualKey)
                        {
                            case VirtualKey.X when IsSettingsPaneSupported:
                                SettingsPane.Show();
                                args.Handled = true;
                                break;
                            case VirtualKey.Q when IsSearchPaneSupported:
                                SearchPane.GetForCurrentView().Show();
                                args.Handled = true;
                                break;
                        }
                    }
                }
            }
        }

        private static bool CheckSearchExtension()
        {
            try
            {
                XDocument doc = XDocument.Load(Path.Combine(Package.Current.InstalledLocation.Path, "AppxManifest.xml"));
                XNamespace ns = XNamespace.Get("http://schemas.microsoft.com/appx/manifest/uap/windows10");
                IEnumerable<XElement> extensions = doc.Root.Descendants(ns + "Extension");
                if (extensions != null)
                {
                    foreach (XElement extension in extensions)
                    {
                        XAttribute category = extension.Attribute("Category");
                        if (category != null && category.Value == "windows.search")
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SettingsHelper.LoggerFactory.CreateLogger(typeof(SettingsPaneRegister)).LogWarning(ex, "Failed to check search pane supports. {message} (0x{hResult:X})", ex.GetMessage(), ex.HResult);
            }
            return false;
        }
    }
}
