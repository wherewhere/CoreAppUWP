using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;

namespace WASForCoreApp
{
    public class App : IFrameworkViewSource, IFrameworkView
    {
        private CoreWindow _window;
        private Compositor _compositor;
        private ContainerVisual _root;
        private CompositionTarget _compositionTarget;

        public IFrameworkView CreateView() => this;

        public void Initialize(CoreApplicationView applicationView)
        {
            applicationView.Activated += OnApplicationViewActivated;
        }

        public void SetWindow(CoreWindow window)
        {
            _window = window;
            ExtendViewIntoTitleBar(true);
        }

        public void Load(string entryPoint)
        {
        }

        public void Run()
        {
            _window.Activate();
            CoreWindow.GetForCurrentThread().Dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessUntilQuit);
        }

        public void Uninitialize()
        {
        }

        private void OnApplicationViewActivated(CoreApplicationView sender, IActivatedEventArgs e)
        {
            _compositor = new Compositor();
            _root = _compositor.CreateContainerVisual();
            _compositionTarget = _compositor.CreateTargetForCurrentView();
            _compositionTarget.Root = _root;
            SpriteVisual child = _compositor.CreateSpriteVisual();
            child.Size = new Vector2((float)_window.Bounds.Width, (float)_window.Bounds.Height);
            child.Brush = _compositor.CreateHostBackdropBrush();
            _root.Children.InsertAtTop(child);
            _window.SizeChanged += (sender, args) => child.Size = new Vector2((float)args.Size.Width, (float)args.Size.Height);

            StringBuilder builder = new();
            builder.AppendLine(RuntimeInformation.FrameworkDescription);
            builder.AppendLine(RuntimeInformation.OSDescription);
            builder.Append($"ProcessArchitecture: {RuntimeInformation.ProcessArchitecture.ToString()}");
            MessageDialog dialog = new(builder.ToString(), "Hello World!");
            _ = dialog.ShowAsync();
        }

        private static void ExtendViewIntoTitleBar(bool extendViewIntoTitleBar)
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = extendViewIntoTitleBar;

            if (extendViewIntoTitleBar)
            {
                bool IsDark = new UISettings().GetColorValue(UIColorType.Background) == Color.FromArgb(255, 0, 0, 0);

                Color ForegroundColor = IsDark ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(255, 0, 0, 0);
                Color BackgroundColor = IsDark ? Color.FromArgb(255, 32, 32, 32) : Color.FromArgb(255, 243, 243, 243);

                ApplicationViewTitleBar TitleBar = ApplicationView.GetForCurrentView().TitleBar;
                TitleBar.ForegroundColor = TitleBar.ButtonForegroundColor = ForegroundColor;
                TitleBar.BackgroundColor = TitleBar.InactiveBackgroundColor = BackgroundColor;
                TitleBar.ButtonBackgroundColor = TitleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 0, 0, 0);
            }
        }
    }
}
