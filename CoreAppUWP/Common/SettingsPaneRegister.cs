using Microsoft.UI.Xaml;
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

namespace CoreAppUWP.Common
{
    public class SettingsPaneRegister
    {
        private CoreDispatcher dispatcher;

        public static bool IsSearchPaneSupported { get; } = ApiInformation.IsTypePresent("Windows.UI.ApplicationSettings.SettingsPane") && CheckSearchExtension();
        public static bool IsSettingsPaneSupported { get; } = ApiInformation.IsTypePresent("Windows.UI.ApplicationSettings.SettingsPane");

        public SettingsPaneRegister(Window window)
        {
            dispatcher = window.Dispatcher;
            if (IsSettingsPaneSupported)
            {
                SettingsPane settingsPane = SettingsPane.GetForCurrentView();
                settingsPane.CommandsRequested -= OnCommandsRequested;
                settingsPane.CommandsRequested += OnCommandsRequested;
                dispatcher.AcceleratorKeyActivated -= Dispatcher_AcceleratorKeyActivated;
                dispatcher.AcceleratorKeyActivated += Dispatcher_AcceleratorKeyActivated;
            }
        }

        public static SettingsPaneRegister Register(Window window) => new(window);

        public void Unregister()
        {
            if (IsSettingsPaneSupported)
            {
                SettingsPane.GetForCurrentView().CommandsRequested -= OnCommandsRequested;
                dispatcher.AcceleratorKeyActivated -= Dispatcher_AcceleratorKeyActivated;
            }
            dispatcher = null;
        }

        private void OnCommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
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
                    async handler => _ = Launcher.LaunchFolderAsync(await ApplicationData.Current.LocalFolder.CreateFolderAsync("MetroLogs", CreationCollisionOption.OpenIfExists))));
            args.Request.ApplicationCommands.Add(
                new SettingsCommand(
                    "Repository",
                    "Repository",
                    handler => _ = Launcher.LaunchUriAsync(new Uri("https://github.com/wherewhere/CoreAppUWP"))));
        }

        private void Dispatcher_AcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
        {
            if (args.EventType is CoreAcceleratorKeyEventType.KeyDown or CoreAcceleratorKeyEventType.SystemKeyDown)
            {
                CoreVirtualKeyStates ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
                if (ctrl.HasFlag(CoreVirtualKeyStates.Down))
                {
                    CoreVirtualKeyStates shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
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
            return false;
        }
    }
}
