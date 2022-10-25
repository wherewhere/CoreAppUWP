using System;
using System.Runtime.InteropServices;
using System.Text;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;

namespace UWPForCoreApp
{
    public class App : IFrameworkViewSource, IFrameworkView
    {
        public IFrameworkView CreateView() => this;

        public void Initialize(CoreApplicationView applicationView)
        {
            applicationView.Activated += OnApplicationViewActivated;
        }

        public void SetWindow(CoreWindow window)
        {
            ExtendViewIntoTitleBar(true);
        }

        public void Load(string entryPoint)
        {
        }

        public void Run()
        {
            CoreWindow.GetForCurrentThread().Dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessUntilQuit);
        }

        public void Uninitialize()
        {
        }

        private async void OnApplicationViewActivated(CoreApplicationView sender, IActivatedEventArgs e)
        {
            sender.CoreWindow.Activate();
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(RuntimeInformation.FrameworkDescription);
            builder.AppendLine(RuntimeInformation.OSDescription);
            builder.Append($"ProcessArchitecture: {RuntimeInformation.ProcessArchitecture.ToString()}");
            MessageDialog dialog = new MessageDialog(builder.ToString(), "Hellow World!");
            await dialog.ShowAsync();
        }

        private static void ExtendViewIntoTitleBar(bool extendViewIntoTitleBar)
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = extendViewIntoTitleBar;

            if (extendViewIntoTitleBar)
            {
                ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
                titleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
                titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 0, 0, 0);
            }
        }
    }
}
