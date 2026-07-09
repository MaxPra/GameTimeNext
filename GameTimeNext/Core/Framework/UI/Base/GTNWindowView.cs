using System.Collections;
using System.Windows;
using UIX.ViewController.Engine.FrameworkElements.UserControls;

namespace GameTimeNext.Core.Framework.UI.Base
{
    public class GTNWindowView : UIXUserControlBase
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(GTNWindowView),
                new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty ShowMinimizeButtonProperty =
            DependencyProperty.Register(
                nameof(ShowMinimizeButton),
                typeof(bool),
                typeof(GTNWindowView),
                new PropertyMetadata(true));

        public bool ShowMinimizeButton
        {
            get => (bool)GetValue(ShowMinimizeButtonProperty);
            set => SetValue(ShowMinimizeButtonProperty, value);
        }

        public static readonly DependencyProperty ShowMaximizeButtonProperty =
            DependencyProperty.Register(
                nameof(ShowMaximizeButton),
                typeof(bool),
                typeof(GTNWindowView),
                new PropertyMetadata(true));

        public bool ShowMaximizeButton
        {
            get => (bool)GetValue(ShowMaximizeButtonProperty);
            set => SetValue(ShowMaximizeButtonProperty, value);
        }

        public static readonly DependencyProperty ShowCloseButtonProperty =
            DependencyProperty.Register(
                nameof(ShowCloseButton),
                typeof(bool),
                typeof(GTNWindowView),
                new PropertyMetadata(true));

        public bool ShowCloseButton
        {
            get => (bool)GetValue(ShowCloseButtonProperty);
            set => SetValue(ShowCloseButtonProperty, value);
        }

        public static readonly DependencyProperty ShowApplicationSearchProperty =
            DependencyProperty.Register(
                nameof(ShowApplicationSearch),
                typeof(bool),
                typeof(GTNWindowView),
                new PropertyMetadata(false));

        public bool ShowApplicationSearch
        {
            get => (bool)GetValue(ShowApplicationSearchProperty);
            set => SetValue(ShowApplicationSearchProperty, value);
        }

        public static readonly DependencyProperty ApplicationSearchItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ApplicationSearchItemsSource),
                typeof(IEnumerable),
                typeof(GTNWindowView),
                new PropertyMetadata(null));

        public IEnumerable? ApplicationSearchItemsSource
        {
            get => (IEnumerable?)GetValue(ApplicationSearchItemsSourceProperty);
            set => SetValue(ApplicationSearchItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ApplicationSearchSelectedItemProperty =
            DependencyProperty.Register(
                nameof(ApplicationSearchSelectedItem),
                typeof(object),
                typeof(GTNWindowView),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public object? ApplicationSearchSelectedItem
        {
            get => GetValue(ApplicationSearchSelectedItemProperty);
            set => SetValue(ApplicationSearchSelectedItemProperty, value);
        }

        public static readonly DependencyProperty ApplicationSearchTextProperty =
            DependencyProperty.Register(
                nameof(ApplicationSearchText),
                typeof(string),
                typeof(GTNWindowView),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string ApplicationSearchText
        {
            get => (string)GetValue(ApplicationSearchTextProperty);
            set => SetValue(ApplicationSearchTextProperty, value);
        }

        public static readonly DependencyProperty ApplicationSearchPlaceholderProperty =
            DependencyProperty.Register(
                nameof(ApplicationSearchPlaceholder),
                typeof(string),
                typeof(GTNWindowView),
                new PropertyMetadata("Suchen..."));

        public string ApplicationSearchPlaceholder
        {
            get => (string)GetValue(ApplicationSearchPlaceholderProperty);
            set => SetValue(ApplicationSearchPlaceholderProperty, value);
        }

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(
                nameof(Subtitle),
                typeof(string),
                typeof(GTNWindowView),
                new PropertyMetadata(string.Empty));

        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        public static readonly DependencyProperty ResizeModeProperty =
            DependencyProperty.Register(
                nameof(ResizeMode),
                typeof(ResizeMode),
                typeof(GTNWindowView),
                new PropertyMetadata(ResizeMode.CanResize));

        public ResizeMode ResizeMode
        {
            get => (ResizeMode)GetValue(ResizeModeProperty);
            set => SetValue(ResizeModeProperty, value);
        }

        public static readonly DependencyProperty WindowStartupLocationProperty =
            DependencyProperty.Register(
                nameof(WindowStartupLocation),
                typeof(WindowStartupLocation),
                typeof(GTNWindowView),
                new PropertyMetadata(WindowStartupLocation.CenterOwner));

        public WindowStartupLocation WindowStartupLocation
        {
            get => (WindowStartupLocation)GetValue(WindowStartupLocationProperty);
            set => SetValue(WindowStartupLocationProperty, value);
        }

        public static readonly DependencyProperty ShowInTaskbarProperty =
            DependencyProperty.Register(
                nameof(ShowInTaskbar),
                typeof(bool),
                typeof(GTNWindowView),
                new PropertyMetadata(false));

        public bool ShowInTaskbar
        {
            get => (bool)GetValue(ShowInTaskbarProperty);
            set => SetValue(ShowInTaskbarProperty, value);
        }

        public static readonly DependencyProperty SizeToContentProperty =
            DependencyProperty.Register(
                nameof(SizeToContent),
                typeof(SizeToContent),
                typeof(GTNWindowView),
                new PropertyMetadata(SizeToContent.Manual));

        public SizeToContent SizeToContent
        {
            get => (SizeToContent)GetValue(SizeToContentProperty);
            set => SetValue(SizeToContentProperty, value);
        }
    }
}
