using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using UIX.ViewController.Engine.FrameworkElements.Windows;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.UI.Base
{
    public class GTNWindow : UIXWindowBase
    {
        private bool _allowClose;
        private bool _suppressSearchTextSync;

        private const int WM_GETMINMAXINFO = 0x0024;
        private const int MONITOR_DEFAULTTONEAREST = 0x00000002;

        private IntPtr _hwnd;
        private HwndSource? _hwndSource;

        private FrameworkElement? _titleBar;
        private Button? _closeButton;
        private Button? _minButton;
        private Button? _maxButton;
        private ComboBox? _applicationSearchBox;
        private TextBox? _applicationSearchTextBox;

        public static readonly DependencyProperty ShowMinimizeButtonProperty =
            DependencyProperty.Register(
                nameof(ShowMinimizeButton),
                typeof(bool),
                typeof(GTNWindow),
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
                typeof(GTNWindow),
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
                typeof(GTNWindow),
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
                typeof(GTNWindow),
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
                typeof(GTNWindow),
                new PropertyMetadata(null, OnApplicationSearchItemsSourceChanged));

        public IEnumerable? ApplicationSearchItemsSource
        {
            get => (IEnumerable?)GetValue(ApplicationSearchItemsSourceProperty);
            set => SetValue(ApplicationSearchItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ApplicationSearchSelectedItemProperty =
            DependencyProperty.Register(
                nameof(ApplicationSearchSelectedItem),
                typeof(object),
                typeof(GTNWindow),
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
                typeof(GTNWindow),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnApplicationSearchTextChanged));

        public string ApplicationSearchText
        {
            get => (string)GetValue(ApplicationSearchTextProperty);
            set => SetValue(ApplicationSearchTextProperty, value);
        }

        public static readonly DependencyProperty ApplicationSearchPlaceholderProperty =
            DependencyProperty.Register(
                nameof(ApplicationSearchPlaceholder),
                typeof(string),
                typeof(GTNWindow),
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
                typeof(GTNWindow),
                new PropertyMetadata(string.Empty));

        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        /// <summary>
        /// Translucent window with a blurred backdrop (Windows 11 acrylic). The main window turns it off while the
        /// dynamic background is shown. Can be switched at runtime.
        /// </summary>
        public static readonly DependencyProperty UseBackdropBlurProperty =
            DependencyProperty.Register(
                nameof(UseBackdropBlur),
                typeof(bool),
                typeof(GTNWindow),
                new PropertyMetadata(true, OnUseBackdropBlurChanged));

        public bool UseBackdropBlur
        {
            get => (bool)GetValue(UseBackdropBlurProperty);
            set => SetValue(UseBackdropBlurProperty, value);
        }

        /// <summary>
        /// Uses the blurred desktop wallpaper (DesktopWallpaper) as base of the window background, tinted with the
        /// normal background. Meant for the main window; has no effect while there is no wallpaper.
        /// </summary>
        public static readonly DependencyProperty UseWallpaperBackgroundProperty =
            DependencyProperty.Register(
                nameof(UseWallpaperBackground),
                typeof(bool),
                typeof(GTNWindow),
                new PropertyMetadata(false));

        public bool UseWallpaperBackground
        {
            get => (bool)GetValue(UseWallpaperBackgroundProperty);
            set => SetValue(UseWallpaperBackgroundProperty, value);
        }

        private static readonly DependencyPropertyKey IsBackdropActivePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsBackdropActive),
                typeof(bool),
                typeof(GTNWindow),
                new PropertyMetadata(false));

        /// <summary>
        /// True when the blurred backdrop could be enabled (the window style switches to translucent surfaces then).
        /// </summary>
        public static readonly DependencyProperty IsBackdropActiveProperty = IsBackdropActivePropertyKey.DependencyProperty;

        public bool IsBackdropActive => (bool)GetValue(IsBackdropActiveProperty);

        private static readonly DependencyPropertyKey IsNativeFrameActivePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsNativeFrameActive),
                typeof(bool),
                typeof(GTNWindow),
                new PropertyMetadata(false));

        /// <summary>
        /// True when DWM rounds the corners of the opaque window (Windows 11). The window style then keeps a minimal
        /// DWM frame instead of a rounded window region, so DWM draws the native drop shadow.
        /// </summary>
        public static readonly DependencyProperty IsNativeFrameActiveProperty = IsNativeFrameActivePropertyKey.DependencyProperty;

        public bool IsNativeFrameActive => (bool)GetValue(IsNativeFrameActiveProperty);

        public GTNWindow()
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResize;

            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _hwnd = new WindowInteropHelper(this).Handle;
            _hwndSource = HwndSource.FromHwnd(_hwnd);
            _hwndSource?.AddHook(WndProc);

            UpdateFrame();
        }

        private static void OnUseBackdropBlurChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GTNWindow window)
                window.UpdateFrame();
        }

        /// <summary>
        /// Applies UseBackdropBlur to the window frame: acrylic backdrop if possible, otherwise the opaque window with
        /// the native frame (Windows 11). Does nothing before the window handle exists.
        /// </summary>
        private void UpdateFrame()
        {
            if (_hwndSource == null)
                return;

            bool backdropActive = UseBackdropBlur && TryEnableBackdropBlur(_hwnd, _hwndSource);

            if (!backdropActive && IsBackdropActive)
                DisableBackdropBlur(_hwnd);

            // Style triggers switch the WindowChrome (glass frame) and the surfaces accordingly
            SetValue(IsBackdropActivePropertyKey, backdropActive);
            SetValue(IsNativeFrameActivePropertyKey, !backdropActive && TryEnableNativeFrame(_hwnd));
        }

        /// <summary>Removes the acrylic system backdrop again.</summary>
        private static void DisableBackdropBlur(IntPtr hwnd)
        {
            try
            {
                int backdrop = DWMSBT_NONE;
                DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        /// <summary>
        /// Lets DWM round the corners of the opaque window (Windows 11). Without it the rounding needs a window region,
        /// which suppresses the DWM drop shadow.
        /// </summary>
        private static bool TryEnableNativeFrame(IntPtr hwnd)
        {
            if (Environment.OSVersion.Version.Build < MinBuildRoundedCorners)
                return false;

            try
            {
                int darkMode = 1;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

                int cornerPreference = DWMWCP_ROUND;
                return DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int)) == 0;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }

        /// <summary>
        /// Lets DWM draw the acrylic system backdrop behind the window (Windows 11 22H2+).
        /// WPF then renders onto a transparent surface; the window style provides the translucent tint.
        /// </summary>
        private static bool TryEnableBackdropBlur(IntPtr hwnd, HwndSource source)
        {
            if (Environment.OSVersion.Version.Build < MinBuildSystemBackdrop)
                return false;

            try
            {
                int darkMode = 1;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

                int cornerPreference = DWMWCP_ROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));

                int backdrop = DWMSBT_TRANSIENTWINDOW;
                if (DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int)) != 0)
                    return false;

                // The backdrop is only visible where the frame extends into the client area and WPF doesn't paint
                var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
                DwmExtendFrameIntoClientArea(hwnd, ref margins);

                if (source.CompositionTarget != null)
                    source.CompositionTarget.BackgroundColor = Colors.Transparent;

                return true;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }

        public override void OnApplyTemplate()
        {
            DetachTemplateEvents();

            base.OnApplyTemplate();

            _titleBar = GetTemplateChild("PART_TitleBar") as FrameworkElement;
            _closeButton = GetTemplateChild("PART_CloseButton") as Button;
            _minButton = GetTemplateChild("PART_MinButton") as Button;
            _maxButton = GetTemplateChild("PART_MaxButton") as Button;
            _applicationSearchBox = GetTemplateChild("PART_ApplicationSearchBox") as ComboBox;

            if (_titleBar != null)
                _titleBar.MouseLeftButtonDown += TitleBar_MouseLeftButtonDown;

            if (_closeButton != null)
            {
                _closeButton.Click += CloseButton_Click;
                _closeButton.Visibility = ShowCloseButton ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_minButton != null)
            {
                _minButton.Click += MinButton_Click;
                _minButton.Visibility = ShowMinimizeButton ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_maxButton != null)
            {
                _maxButton.Click += MaxButton_Click;
                _maxButton.Visibility = ShowMaximizeButton ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_applicationSearchBox != null)
            {
                _applicationSearchBox.ApplyTemplate();
                _applicationSearchBox.DropDownOpened += ApplicationSearchBox_DropDownOpened;
                _applicationSearchBox.GotKeyboardFocus += ApplicationSearchBox_GotKeyboardFocus;
                _applicationSearchBox.LostKeyboardFocus += ApplicationSearchBox_LostKeyboardFocus;

                _applicationSearchTextBox = _applicationSearchBox.Template.FindName("PART_EditableTextBox", _applicationSearchBox) as TextBox;

                if (_applicationSearchTextBox != null)
                {
                    _applicationSearchTextBox.TextChanged += ApplicationSearchTextBox_TextChanged;
                    _applicationSearchTextBox.GotKeyboardFocus += ApplicationSearchTextBox_GotKeyboardFocus;
                    _applicationSearchTextBox.PreviewMouseLeftButtonDown += ApplicationSearchTextBox_PreviewMouseLeftButtonDown;
                    _applicationSearchTextBox.Text = ApplicationSearchText ?? string.Empty;
                }

                RefreshApplicationSearchFilter();
            }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            AnimateStateChange();
        }

        private void AnimateStateChange()
        {
            if (GetTemplateChild("RootBorder") is FrameworkElement root &&
                root.RenderTransform is ScaleTransform scale)
            {
                var from = 0.985;
                var to = 1.0;

                if (WindowState == WindowState.Minimized)
                    from = 0.97;

                var anim = new DoubleAnimation
                {
                    From = from,
                    To = to,
                    Duration = TimeSpan.FromMilliseconds(120),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source)
            {
                if (FindParent<TextBoxBase>(source) != null ||
                    FindParent<ComboBox>(source) != null ||
                    FindParent<ComboBoxItem>(source) != null ||
                    FindParent<Button>(source) != null)
                {
                    return;
                }
            }

            if (e.ClickCount == 2 && ResizeMode != ResizeMode.NoResize)
            {
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            else
            {
                DragMove();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Opacity = 0;

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(160),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            BeginAnimation(OpacityProperty, fadeIn);
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            if (_allowClose)
                return;

            e.Cancel = true;

            var fadeOut = new DoubleAnimation
            {
                From = Opacity,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(140),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            fadeOut.Completed += (_, __) =>
            {
                _allowClose = true;
                Close();
            };

            BeginAnimation(OpacityProperty, fadeOut);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaxButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void ApplicationSearchBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            OpenSearchDropDown(showAllWhenEmpty: true);
        }

        private void ApplicationSearchBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (_applicationSearchBox == null)
                return;

            if (!_applicationSearchBox.IsKeyboardFocusWithin)
                _applicationSearchBox.IsDropDownOpen = false;
        }

        private void ApplicationSearchTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            OpenSearchDropDown(showAllWhenEmpty: true);
        }

        private void ApplicationSearchTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_applicationSearchTextBox == null)
                return;

            if (!_applicationSearchTextBox.IsKeyboardFocusWithin)
                _applicationSearchTextBox.Focus();

            OpenSearchDropDown(showAllWhenEmpty: true);
            e.Handled = false;
        }

        private void ApplicationSearchBox_DropDownOpened(object? sender, EventArgs e)
        {
            RefreshApplicationSearchFilter();
        }

        private void ApplicationSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressSearchTextSync || _applicationSearchTextBox == null || _applicationSearchBox == null)
                return;

            ApplicationSearchText = _applicationSearchTextBox.Text;
            _applicationSearchBox.SelectedItem = null;
            _applicationSearchBox.SelectedIndex = -1;
            ApplicationSearchSelectedItem = null;

            RefreshApplicationSearchFilter();

            if (_applicationSearchBox.IsKeyboardFocusWithin || _applicationSearchTextBox.IsKeyboardFocusWithin)
                _applicationSearchBox.IsDropDownOpen = HasVisibleItems();
        }

        private static void OnApplicationSearchItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GTNWindow window)
                window.RefreshApplicationSearchFilter();
        }

        private static void OnApplicationSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GTNWindow window)
                window.SyncApplicationSearchTextFromProperty();
        }

        private void SyncApplicationSearchTextFromProperty()
        {
            if (_applicationSearchTextBox == null)
                return;

            var newText = ApplicationSearchText ?? string.Empty;

            if (_applicationSearchTextBox.Text != newText)
            {
                _suppressSearchTextSync = true;
                _applicationSearchTextBox.Text = newText;
                _applicationSearchTextBox.CaretIndex = _applicationSearchTextBox.Text.Length;
                _suppressSearchTextSync = false;
            }

            RefreshApplicationSearchFilter();
        }

        private void RefreshApplicationSearchFilter()
        {
            if (_applicationSearchBox == null)
                return;

            var source = ApplicationSearchItemsSource;
            if (source == null)
            {
                _applicationSearchBox.IsDropDownOpen = false;
                return;
            }

            var view = CollectionViewSource.GetDefaultView(source);
            if (view == null)
            {
                _applicationSearchBox.IsDropDownOpen = false;
                return;
            }

            var searchText = ApplicationSearchText ?? string.Empty;

            view.Filter = item =>
            {
                if (item == null)
                    return false;

                if (FnString.IsNullEmptyOrWhitespace(searchText))
                    return true;

                var text = item.ToString() ?? string.Empty;
                return text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
            };

            view.Refresh();
        }

        private void OpenSearchDropDown(bool showAllWhenEmpty)
        {
            if (_applicationSearchBox == null)
                return;

            if (!ShowApplicationSearch)
            {
                _applicationSearchBox.IsDropDownOpen = false;
                return;
            }

            if (showAllWhenEmpty && FnString.IsNullEmptyOrWhitespace(ApplicationSearchText))
                RefreshApplicationSearchFilter();

            _applicationSearchBox.IsDropDownOpen = HasVisibleItems();
        }

        private bool HasVisibleItems()
        {
            if (_applicationSearchBox == null)
                return false;

            return _applicationSearchBox.Items.Count > 0;
        }

        private void DetachTemplateEvents()
        {
            if (_titleBar != null)
                _titleBar.MouseLeftButtonDown -= TitleBar_MouseLeftButtonDown;

            if (_closeButton != null)
                _closeButton.Click -= CloseButton_Click;

            if (_minButton != null)
                _minButton.Click -= MinButton_Click;

            if (_maxButton != null)
                _maxButton.Click -= MaxButton_Click;

            if (_applicationSearchBox != null)
            {
                _applicationSearchBox.DropDownOpened -= ApplicationSearchBox_DropDownOpened;
                _applicationSearchBox.GotKeyboardFocus -= ApplicationSearchBox_GotKeyboardFocus;
                _applicationSearchBox.LostKeyboardFocus -= ApplicationSearchBox_LostKeyboardFocus;
            }

            if (_applicationSearchTextBox != null)
            {
                _applicationSearchTextBox.TextChanged -= ApplicationSearchTextBox_TextChanged;
                _applicationSearchTextBox.GotKeyboardFocus -= ApplicationSearchTextBox_GotKeyboardFocus;
                _applicationSearchTextBox.PreviewMouseLeftButtonDown -= ApplicationSearchTextBox_PreviewMouseLeftButtonDown;
            }

            _titleBar = null;
            _closeButton = null;
            _minButton = null;
            _maxButton = null;
            _applicationSearchBox = null;
            _applicationSearchTextBox = null;
        }

        private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T typedChild)
                    return typedChild;

                child = VisualTreeHelper.GetParent(child);
            }

            return null;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                AdjustMaximizedSize(hwnd, lParam);
                handled = false;
            }

            return IntPtr.Zero;
        }

        private static void AdjustMaximizedSize(IntPtr hwnd, IntPtr lParam)
        {
            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);

            var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero)
                return;

            var mi = new MONITORINFO();
            mi.cbSize = Marshal.SizeOf<MONITORINFO>();

            if (!GetMonitorInfo(monitor, ref mi))
                return;

            var rcWork = mi.rcWork;
            var rcMonitor = mi.rcMonitor;

            mmi.ptMaxPosition.x = Math.Abs(rcWork.left - rcMonitor.left);
            mmi.ptMaxPosition.y = Math.Abs(rcWork.top - rcMonitor.top);
            mmi.ptMaxSize.x = Math.Abs(rcWork.right - rcWork.left);
            mmi.ptMaxSize.y = Math.Abs(rcWork.bottom - rcWork.top);

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        // --- Backdrop blur (DWM) ---

        private const int MinBuildSystemBackdrop = 22621;
        private const int MinBuildRoundedCorners = 22000;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        private const int DWMWCP_ROUND = 2;
        private const int DWMSBT_NONE = 1;
        private const int DWMSBT_TRANSIENTWINDOW = 3;

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);
    }
}
