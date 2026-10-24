using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace GameTimeNext.Core.Framework.UI
{
    public static class TabControlBehaviors
    {
        public static readonly DependencyProperty EnableSelectionSwooshProperty =
            DependencyProperty.RegisterAttached(
                "EnableSelectionSwoosh",
                typeof(bool),
                typeof(TabControlBehaviors),
                new PropertyMetadata(false, OnEnableSelectionSwooshChanged));

        public static bool GetEnableSelectionSwoosh(DependencyObject obj)
            => (bool)obj.GetValue(EnableSelectionSwooshProperty);

        public static void SetEnableSelectionSwoosh(DependencyObject obj, bool value)
            => obj.SetValue(EnableSelectionSwooshProperty, value);

        /// <summary>
        /// Shows a "+" button after the last tab (template part BtnTabAdd). Its click is dispatched to the
        /// window controller like any other named button (EV_BtnTabAdd).
        /// </summary>
        public static readonly DependencyProperty ShowAddButtonProperty =
            DependencyProperty.RegisterAttached(
                "ShowAddButton",
                typeof(bool),
                typeof(TabControlBehaviors),
                new PropertyMetadata(false));

        public static bool GetShowAddButton(DependencyObject obj)
            => (bool)obj.GetValue(ShowAddButtonProperty);

        public static void SetShowAddButton(DependencyObject obj, bool value)
            => obj.SetValue(ShowAddButtonProperty, value);

        /// <summary>Tooltip of the "+" button.</summary>
        public static readonly DependencyProperty AddButtonToolTipProperty =
            DependencyProperty.RegisterAttached(
                "AddButtonToolTip",
                typeof(string),
                typeof(TabControlBehaviors),
                new PropertyMetadata(null));

        public static string? GetAddButtonToolTip(DependencyObject obj)
            => (string?)obj.GetValue(AddButtonToolTipProperty);

        public static void SetAddButtonToolTip(DependencyObject obj, string? value)
            => obj.SetValue(AddButtonToolTipProperty, value);

        private static readonly DependencyProperty LastSelectedItemProperty =
            DependencyProperty.RegisterAttached(
                "LastSelectedItem",
                typeof(object),
                typeof(TabControlBehaviors),
                new PropertyMetadata(null));

        private static object? GetLastSelectedItem(DependencyObject obj)
            => obj.GetValue(LastSelectedItemProperty);

        private static void SetLastSelectedItem(DependencyObject obj, object? value)
            => obj.SetValue(LastSelectedItemProperty, value);

        private static void OnEnableSelectionSwooshChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TabControl tabControl)
                return;

            if ((bool)e.NewValue)
            {
                tabControl.SelectionChanged += OnTabControlSelectionChanged;
                tabControl.Loaded += OnTabControlLoaded;
                tabControl.SizeChanged += OnTabControlSizeChanged;
            }
            else
            {
                tabControl.SelectionChanged -= OnTabControlSelectionChanged;
                tabControl.Loaded -= OnTabControlLoaded;
                tabControl.SizeChanged -= OnTabControlSizeChanged;
            }
        }

        private static void OnTabControlLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not TabControl tabControl)
                return;

            SetLastSelectedItem(tabControl, tabControl.SelectedItem);
            AnimateSelectionRunner(tabControl, animate: false);
        }

        private static void OnTabControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not TabControl tabControl)
                return;

            AnimateSelectionRunner(tabControl, animate: false);
        }

        private static void OnTabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not TabControl tabControl)
                return;

            // SelectionChanged bubbles: ignore events from nested selectors (inner TabControls, ComboBoxes, ...).
            if (!ReferenceEquals(e.OriginalSource, tabControl))
                return;

            // Only animate when a different tab actually became selected (not on item reorder/re-add of the same tab).
            var selectedItem = tabControl.SelectedItem;
            var lastSelectedItem = GetLastSelectedItem(tabControl);
            SetLastSelectedItem(tabControl, selectedItem);

            if (selectedItem is null)
            {
                // Last tab closed: hide the runner instead of leaving it at the old position
                AnimateSelectionRunner(tabControl, animate: false);
                return;
            }

            if (ReferenceEquals(selectedItem, lastSelectedItem))
                return;

            tabControl.Dispatcher.BeginInvoke(new Action(() => AnimateSelectionRunner(tabControl, animate: true)), System.Windows.Threading.DispatcherPriority.Render);

            if (tabControl.Template.FindName("SelectedContentHost", tabControl) is not FrameworkElement contentHost)
                return;

            if (contentHost.RenderTransform is not TranslateTransform contentTransform)
                return;

            contentHost.BeginAnimation(UIElement.OpacityProperty, null);
            contentTransform.BeginAnimation(TranslateTransform.XProperty, null);

            contentHost.Opacity = 0.9;
            contentTransform.X = 8;

            var fadeAnimation = new DoubleAnimation
            {
                From = 0.9,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var slideAnimation = new DoubleAnimation
            {
                From = 8,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            contentHost.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
            contentTransform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
        }

        private static void AnimateSelectionRunner(TabControl tabControl, bool animate)
        {
            if (tabControl.Template.FindName("SelectionRunner", tabControl) is not FrameworkElement runner)
                return;

            if (tabControl.Template.FindName("TabTrackContentHost", tabControl) is not FrameworkElement trackHost)
                return;

            if (runner.RenderTransform is not TranslateTransform runnerTransform)
            {
                runnerTransform = new TranslateTransform();
                runner.RenderTransform = runnerTransform;
            }

            if (tabControl.SelectedItem is null)
            {
                runner.BeginAnimation(FrameworkElement.WidthProperty, null);
                runnerTransform.BeginAnimation(TranslateTransform.XProperty, null);
                runner.Width = 0;
                runnerTransform.X = 0;
                return;
            }

            if (tabControl.ItemContainerGenerator.ContainerFromItem(tabControl.SelectedItem) is not FrameworkElement selectedTab)
                return;

            if (selectedTab.ActualWidth <= 0 || trackHost.ActualWidth <= 0)
                return;

            var selectedOrigin = selectedTab.TransformToVisual(trackHost).Transform(new Point(0, 0));
            var targetX = selectedOrigin.X;
            var targetWidth = selectedTab.ActualWidth;

            var currentRunnerWidth = (double)runner.GetValue(FrameworkElement.WidthProperty);
            var currentRunnerX = (double)runnerTransform.GetValue(TranslateTransform.XProperty);

            if (double.IsNaN(currentRunnerWidth) || currentRunnerWidth < 0)
                currentRunnerWidth = 0;

            if (double.IsNaN(currentRunnerX))
                currentRunnerX = 0;

            runner.BeginAnimation(FrameworkElement.WidthProperty, null);
            runnerTransform.BeginAnimation(TranslateTransform.XProperty, null);

            runner.Width = currentRunnerWidth;
            runnerTransform.X = currentRunnerX;

            if (!animate || runner.Width <= 0)
            {
                runner.Width = targetWidth;
                runnerTransform.X = targetX;
                return;
            }

            var duration = TimeSpan.FromMilliseconds(260);
            var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };

            var widthAnimation = new DoubleAnimation
            {
                To = targetWidth,
                Duration = duration,
                EasingFunction = easing
            };

            var moveAnimation = new DoubleAnimation
            {
                To = targetX,
                Duration = duration,
                EasingFunction = easing
            };

            runner.BeginAnimation(FrameworkElement.WidthProperty, widthAnimation);
            runnerTransform.BeginAnimation(TranslateTransform.XProperty, moveAnimation);
        }
    }
}
