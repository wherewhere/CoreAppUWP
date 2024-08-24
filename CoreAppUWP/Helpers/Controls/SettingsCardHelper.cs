using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CoreAppUWP.Helpers
{
    public static class SettingsCardHelper
    {
        #region HeaderIcon

        /// <summary>
        /// Identifies the HeaderIcon dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderIconProperty =
            DependencyProperty.RegisterAttached(
                "HeaderIcon",
                typeof(object),
                typeof(SettingsExpanderHelper),
                new PropertyMetadata(null, OnHeaderIconChanged));

        /// <summary>
        /// Gets the HeaderIcon.
        /// </summary>
        /// <param name="control">The element from which to read the property value.</param>
        /// <returns>The HeaderIcon.</returns>
        public static object GetHeaderIcon(SettingsCard control)
        {
            return control.GetValue(HeaderIconProperty);
        }

        /// <summary>
        /// Sets the HeaderIcon.
        /// </summary>
        /// <param name="control">The element on which to set the attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetHeaderIcon(SettingsCard control, object value)
        {
            control.SetValue(HeaderIconProperty, value);
        }

        private static void OnHeaderIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SettingsCard element) { return; }
            if (element.IsLoaded)
            {
                OnElementLoaded(element, null);
            }
            else
            {
                element.Loaded -= OnElementLoaded;
                element.Loaded += OnElementLoaded;
            }
        }

        private static void OnElementLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not SettingsCard element) { return; }
            object content = GetHeaderIcon(element);
            element.HeaderIcon = content == null ? null : new SymbolIcon();
            if (element.FindDescendant("PART_HeaderIconPresenter") is ContentPresenter presenter)
            {
                presenter.Content = content;
            }
        }

        #endregion
    }
}
