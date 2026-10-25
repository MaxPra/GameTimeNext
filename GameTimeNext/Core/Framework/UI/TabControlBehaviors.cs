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

            if (contentHost.RenderTransform is not TranslateTransform)
                return;

            TranslateTransform contentTransform = GetAnimatableTranslateTransform(contentHost);

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

        /// <summary>
        /// Returns the element's TranslateTransform in an animatable state: unnamed freezables in templates are frozen,
        /// so a frozen (or missing) transform is replaced by a modifiable one.
        /// </summary>
        private static TranslateTransform GetAnimatableTranslateTransform(UIElement element)
        {
            if (element.RenderTransform is TranslateTransform { IsFrozen: false } transform)
                return transform;

            var modifiable = element.RenderTransform is TranslateTransform frozen
                ? frozen.Clone()
                : new TranslateTransform();

            element.RenderTransform = modifiable;
            return modifiable;
        }

        /// <summary>
        /// Moves the template part SelectionRunner to the selected tab (inside TabTrackContentHost).
        /// Horizontal tab strips animate its width and X, vertical ones (TabStripPlacement Left / Right) its height and Y.
        /// </summary>
        private static void AnimateSelectionRunner(TabControl tabControl, bool animate)
        {
            if (tabControl.Template.FindName("SelectionRunner", tabControl) is not FrameworkElement runner)
                return;

            if (tabControl.Template.FindName("TabTrackContentHost", tabControl) is not FrameworkElement trackHost)
                return;

            TranslateTransform runnerTransform = GetAnimatableTranslateTransform(runner);

            bool vertical = tabControl.TabStripPlacement is Dock.Left or Dock.Right;
            DependencyProperty sizeProperty = vertical ? FrameworkElement.HeightProperty : FrameworkElement.WidthProperty;
            DependencyProperty offsetProperty = vertical ? TranslateTransform.YProperty : TranslateTransform.XProperty;

            if (tabControl.SelectedItem is null)
            {
                runner.BeginAnimation(sizeProperty, null);
                runnerTransform.BeginAnimation(offsetProperty, null);
                runner.SetValue(sizeProperty, 0d);
                runnerTransform.SetValue(offsetProperty, 0d);
                return;
            }

            if (tabControl.ItemContainerGenerator.ContainerFromItem(tabControl.SelectedItem) is not FrameworkElement selectedTab)
                return;

            double selectedSize = vertical ? selectedTab.ActualHeight : selectedTab.ActualWidth;
            double trackSize = vertical ? trackHost.ActualHeight : trackHost.ActualWidth;

            if (selectedSize <= 0 || trackSize <= 0)
                return;

            var selectedOrigin = selectedTab.TransformToVisual(trackHost).Transform(new Point(0, 0));
            var targetOffset = vertical ? selectedOrigin.Y : selectedOrigin.X;
            var targetSize = selectedSize;

            var currentRunnerSize = (double)runner.GetValue(sizeProperty);
            var currentRunnerOffset = (double)runnerTransform.GetValue(offsetProperty);

            if (double.IsNaN(currentRunnerSize) || currentRunnerSize < 0)
                currentRunnerSize = 0;

            if (double.IsNaN(currentRunnerOffset))
                currentRunnerOffset = 0;

            runner.BeginAnimation(sizeProperty, null);
            runnerTransform.BeginAnimation(offsetProperty, null);

            runner.SetValue(sizeProperty, currentRunnerSize);
            runnerTransform.SetValue(offsetProperty, currentRunnerOffset);

            if (!animate || currentRunnerSize <= 0)
            {
                runner.SetValue(sizeProperty, targetSize);
                runnerTransform.SetValue(offsetProperty, targetOffset);
                return;
            }

            var duration = TimeSpan.FromMilliseconds(260);
            var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };

            var sizeAnimation = new DoubleAnimation
            {
                To = targetSize,
                Duration = duration,
                EasingFunction = easing
            };

            var moveAnimation = new DoubleAnimation
            {
                To = targetOffset,
                Duration = duration,
                EasingFunction = easing
            };

            runner.BeginAnimation(sizeProperty, sizeAnimation);
            runnerTransform.BeginAnimation(offsetProperty, moveAnimation);
        }
    }
}
