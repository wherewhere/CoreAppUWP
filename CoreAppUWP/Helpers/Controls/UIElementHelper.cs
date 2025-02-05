using CoreAppUWP.Common;
using System;
using System.ComponentModel;
using Windows.UI.Xaml;

namespace CoreAppUWP.Helpers
{
    public static class UIElementHelper
    {
        /// <summary>
        /// A helper function—for use within a coroutine—that you can <see langword="await"/> to wait for the <see cref="FrameworkElement.Loaded"/> event fired.
        /// </summary>
        /// <param name="element">A <see cref="FrameworkElement"/> whose <see cref="FrameworkElement.Loaded"/> event to wait for.</param>
        /// <returns>An object that you can <see langword="await"/>.</returns>
        public static ElementLoadedSwitcher ResumeOnLoadedAsync(this FrameworkElement element) => new(element);
    }

    /// <summary>
    /// A helper type for wait for <see cref="FrameworkElement.Loaded"/> event. This type is not intended to be used directly from your code.
    /// </summary>
    /// <param name="Element">A <see cref="FrameworkElement"/> whose <see cref="FrameworkElement.Loaded"/> event to wait for.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly record struct ElementLoadedSwitcher(FrameworkElement Element) : IThreadSwitcher<ElementLoadedSwitcher>
    {
        /// <inheritdoc/>
        public bool IsCompleted => Element.IsLoaded;

        /// <inheritdoc/>
        public void GetResult() { }

        /// <inheritdoc/>
        public ElementLoadedSwitcher GetAwaiter() => this;

        /// <inheritdoc/>
        IThreadSwitcher IThreadSwitcher.GetAwaiter() => this;

        /// <inheritdoc/>
        public void OnCompleted(Action continuation)
        {
            FrameworkElement element = Element;
            element.Loaded -= OnLoaded;
            element.Loaded += OnLoaded;
            void OnLoaded(object sender, RoutedEventArgs e)
            {
                element.Loaded -= OnLoaded;
                continuation();
            }
        }
    }
}
