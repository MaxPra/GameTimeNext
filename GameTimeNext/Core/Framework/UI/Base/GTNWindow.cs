using GameTimeNext.Core.Framework.Config;
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
using System.Windows.Threading;
using UIX.ViewController.Engine.FrameworkElements.Windows;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.UI.Base
{
    public class GTNWindow : UIXWindowBase
    {
        private bool _suppressSearchTextSync;
        private WindowState _windowStateBeforeFullScreen = WindowState.Maximized;
        private ResizeMode _resizeModeBeforeFullScreen = ResizeMode.CanResize;
        private Rect _boundsBeforeFullScreen = Rect.Empty;

        private const int WM_GETMINMAXINFO = 0x0024;
        private const int WM_WINDOWPOSCHANGED = 0x0047;
        private const int WM_STYLECHANGING = 0x007C;
        private const int WM_DISPLAYCHANGE = 0x007E;
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x00080000;
        private const int MONITOR_DEFAULTTOPRIMARY = 0x00000001;
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

        /// <summary>
        /// Shows a help button in the title bar (template part BtnHelp). Its click is dispatched to the
        /// window controller like any other named button (EV_BtnHelp).
        /// </summary>
        public static readonly DependencyProperty ShowHelpButtonProperty =
            DependencyProperty.Register(
                nameof(ShowHelpButton),
                typeof(bool),
                typeof(GTNWindow),
                new PropertyMetadata(false));

        public bool ShowHelpButton
        {
            get => (bool)GetValue(ShowHelpButtonProperty);
            set => SetValue(ShowHelpButtonProperty, value);
        }

        /// <summary>
        /// Shows the Big Picture button in the title bar (template part btnBigPicture). Like the help button, its click is
        /// dispatched to the window controller (EV_btnBigPicture).
        /// </summary>
        public static readonly DependencyProperty ShowBigPictureButtonProperty =
            DependencyProperty.Register(
                nameof(ShowBigPictureButton),
                typeof(bool),
                typeof(GTNWindow),
                new PropertyMetadata(false));

        public bool ShowBigPictureButton
        {
            get => (bool)GetValue(ShowBigPictureButtonProperty);
            set => SetValue(ShowBigPictureButtonProperty, value);
        }

        /// <summary>Shows the app icon in the title bar, in front of the title.</summary>
        public static readonly DependencyProperty ShowAppIconProperty =
            DependencyProperty.Register(
                nameof(ShowAppIcon),
                typeof(bool),
                typeof(GTNWindow),
                new PropertyMetadata(false));

        public bool ShowAppIcon
        {
            get => (bool)GetValue(ShowAppIconProperty);
            set => SetValue(ShowAppIconProperty, value);
        }

        /// <summary>
        /// Borderless full screen over the whole monitor (also over the taskbar), without title bar. The window state
        /// before is restored when it is switched off again.
        /// </summary>
        public static readonly DependencyProperty IsFullScreenProperty =
            DependencyProperty.Register(
                nameof(IsFullScreen),
                typeof(bool),
                typeof(GTNWindow),
                new PropertyMetadata(false, OnIsFullScreenChanged));

        public bool IsFullScreen
        {
            get => (bool)GetValue(IsFullScreenProperty);
            set => SetValue(IsFullScreenProperty, value);
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

        private static readonly DependencyPropertyKey MaximizedInsetPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(MaximizedInset),
                typeof(Thickness),
                typeof(GTNWindow),
                new PropertyMetadata(new Thickness()));

        /// <summary>
        /// The part of the maximized window that lies outside the work area; zero when not maximized.
        /// </summary>
        public static readonly DependencyProperty MaximizedInsetProperty = MaximizedInsetPropertyKey.DependencyProperty;

        public Thickness MaximizedInset => (Thickness)GetValue(MaximizedInsetProperty);

        /// <summary>
        /// The caption style is what makes DWM animate the window when it opens and closes (fade and scale of the whole
        /// window with backdrop and shadow, drawn by the compositor, so a busy UI thread can't make it stutter). The
        /// WindowChrome of the style removes the native caption itself.
        /// </summary>
        public GTNWindow()
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _hwnd = new WindowInteropHelper(this).Handle;
            _hwndSource = HwndSource.FromHwnd(_hwnd);
            _hwndSource?.AddHook(WndProc);

            // Runs through WM_STYLECHANGING, which removes the system menu style
            SetWindowLong(_hwnd, GWL_STYLE, GetWindowLong(_hwnd, GWL_STYLE));

            UpdateFrame();
        }

        private static void OnUseBackdropBlurChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GTNWindow window)
                window.UpdateFrame();
        }

        private static void OnIsFullScreenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GTNWindow window)
                return;

            if ((bool)e.NewValue)
                window.EnterFullScreen();
            else
                window.LeaveFullScreen();
        }

        /// <summary>
        /// Full screen is a normal (not maximized) window without resize frame, placed exactly over the monitor:
        /// Windows maximizes a resizable window only to the work area, the taskbar area would stay uncovered.
        /// </summary>
        private void EnterFullScreen()
        {
            _windowStateBeforeFullScreen = WindowState == WindowState.Minimized ? WindowState.Maximized : WindowState;
            _resizeModeBeforeFullScreen = ResizeMode;
            _boundsBeforeFullScreen = RestoreBounds;

            // Windows only treats a window without caption as full screen (hides the taskbar for it)
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Normal;

            if (_hwnd == IntPtr.Zero)
                return;

            // A normal window would get rounded corners from DWM (Windows 11)
            SetCornerPreference(_hwnd, DWMWCP_DONOTROUND);

            PlaceFullScreen(MonitorFromWindow(_hwnd, MONITOR_DEFAULTTONEAREST));
        }

        /// <summary>
        /// Moves the full screen window to the primary monitor, e.g. after a screensetup switched the monitors.
        /// Does nothing outside full screen.
        /// </summary>
        public void MoveFullScreenToPrimaryMonitor()
        {
            if (IsFullScreen && _hwnd != IntPtr.Zero)
                PlaceFullScreen(MonitorFromPoint(new POINT(), MONITOR_DEFAULTTOPRIMARY));
        }

        private void PlaceFullScreen(IntPtr monitor)
        {
            var monitorInfo = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };

            if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref monitorInfo))
                return;

            RECT bounds = monitorInfo.rcMonitor;
            SetWindowPos(_hwnd, IntPtr.Zero, bounds.left, bounds.top, bounds.right - bounds.left, bounds.bottom - bounds.top, SWP_NOZORDER | SWP_FRAMECHANGED);
        }

        private void LeaveFullScreen()
        {
            if (_hwnd != IntPtr.Zero)
                SetCornerPreference(_hwnd, DWMWCP_ROUND);

            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = _resizeModeBeforeFullScreen;

            // Back to the normal bounds first, so restoring a maximized window later returns to them
            if (!_boundsBeforeFullScreen.IsEmpty)
            {
                Left = _boundsBeforeFullScreen.Left;
                Top = _boundsBeforeFullScreen.Top;
                Width = _boundsBeforeFullScreen.Width;
                Height = _boundsBeforeFullScreen.Height;
            }

            WindowState = _windowStateBeforeFullScreen;
        }

        private static void SetCornerPreference(IntPtr hwnd, int preference)
        {
            if (Environment.OSVersion.Version.Build < MinBuildRoundedCorners)
                return;

            try
            {
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
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

                // The caption fill would show in the system accent color before the first frame is rendered
                int captionColor = DWMWA_COLOR_NONE;
                DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

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

                // No native caption fill behind the translucent title bar (shows as a differently tinted band
                // above the bottom of the title bar); the title bar is drawn by the template
                int captionColor = DWMWA_COLOR_NONE;
                DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

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

            if (GetTemplateChild("BtnHelp") is Button helpButton)
                helpButton.ToolTip = $"Help for {AppConfig.Root.ApplicationName}";

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

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            RefitSizeToContent();
        }

        /// <summary>
        /// With WindowChrome the first SizeToContent pass still includes the standard window frame, which the chrome
        /// removes afterwards: the window ends up a few pixels too large and the glass shows as a strip at the right /
        /// bottom edge. Re-triggering SizeToContent once the chrome is applied fits the window to its content; the center
        /// is kept so the window doesn't shift.
        /// </summary>
        private void RefitSizeToContent()
        {
            SizeToContent mode = SizeToContent;
            if (mode == SizeToContent.Manual || WindowState != WindowState.Normal)
                return;

            double oldWidth = ActualWidth;
            double oldHeight = ActualHeight;

            SizeToContent = SizeToContent.Manual;
            SizeToContent = mode;
            UpdateLayout();

            if (!double.IsNaN(Left))
                Left += (oldWidth - ActualWidth) / 2d;

            if (!double.IsNaN(Top))
                Top += (oldHeight - ActualHeight) / 2d;
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

        /// <summary>
        /// DWM only plays its close animation when the window is hidden, destroying it makes it vanish at once. The animation
        /// keeps running on the last rendered frame after the window is destroyed, so nothing has to wait for it.
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!e.Cancel)
                HideWithCloseAnimation();
        }

        private void HideWithCloseAnimation()
        {
            if (_hwnd == IntPtr.Zero)
                return;

            // Hiding the active window makes Windows activate the next one, often a window of another program. The owner of
            // a dialog can't take over yet, WPF enables it only when the dialog is destroyed.
            if (IsActive && Owner is { IsVisible: true } owner && owner.WindowState != WindowState.Minimized)
            {
                EnableWindow(new WindowInteropHelper(owner).Handle, true);
                owner.Activate();
            }

            ShowWindow(_hwnd, SW_HIDE);
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

            if (msg == WM_WINDOWPOSCHANGED)
                UpdateMaximizedInset(hwnd);

            if (msg == WM_ERASEBKGND && IsNativeFrameActive)
            {
                FillWithBackground(hwnd, wParam);
                handled = true;
                return new IntPtr(1);
            }

            // With a system menu, DWM draws the native caption buttons on the glass, where they show through the title bar
            if (msg == WM_STYLECHANGING && unchecked((int)wParam.ToInt64()) == GWL_STYLE)
            {
                var style = Marshal.PtrToStructure<STYLESTRUCT>(lParam);
                style.styleNew &= ~WS_SYSMENU;
                Marshal.StructureToPtr(style, lParam, false);
            }

            // Monitors switched on / off or resolution changed: Windows moves the window off a switched off monitor,
            // but keeps its old size; full screen covers the monitor it is on now (after Windows has moved it)
            if (msg == WM_DISPLAYCHANGE && IsFullScreen)
                Dispatcher.BeginInvoke(() =>
                {
                    if (IsFullScreen)
                        PlaceFullScreen(MonitorFromWindow(_hwnd, MONITOR_DEFAULTTONEAREST));
                }, DispatcherPriority.Background);

            return IntPtr.Zero;
        }

        /// <summary>
        /// A window that grows has no rendered frame for the new size until WPF has laid it out and rendered it (about 100 ms
        /// when it is maximized), and WPF doesn't erase the background: DWM shows the whole window in the system accent color
        /// until then. Filled with the background of the app instead. Not with the acrylic backdrop: it shows through there.
        /// </summary>
        private static void FillWithBackground(IntPtr hwnd, IntPtr hdc)
        {
            if (_eraseBrush == IntPtr.Zero)
                _eraseBrush = CreateSolidBrush(EraseColor);

            if (GetClientRect(hwnd, out RECT client))
                FillRect(hdc, ref client, _eraseBrush);
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

        /// <summary>
        /// Windows lets a maximized window with caption overhang the work area by its frame (it can't be changed: neither
        /// WM_GETMINMAXINFO nor WM_WINDOWPOSCHANGING is respected), so the template insets its content by that part.
        /// </summary>
        private void UpdateMaximizedInset(IntPtr hwnd)
        {
            var inset = new Thickness();
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };

            if (IsZoomed(hwnd) && GetWindowRect(hwnd, out RECT window) && GetMonitorInfo(MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST), ref mi))
            {
                DpiScale dpi = VisualTreeHelper.GetDpi(this);
                RECT work = mi.rcWork;

                inset = new Thickness(
                    Math.Max(work.left - window.left, 0) / dpi.DpiScaleX,
                    Math.Max(work.top - window.top, 0) / dpi.DpiScaleY,
                    Math.Max(window.right - work.right, 0) / dpi.DpiScaleX,
                    Math.Max(window.bottom - work.bottom, 0) / dpi.DpiScaleY);
            }

            if (MaximizedInset != inset)
                SetValue(MaximizedInsetPropertyKey, inset);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct STYLESTRUCT
        {
            public int styleOld;
            public int styleNew;
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

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, int dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        private const int SW_HIDE = 0;
        private const int WM_ERASEBKGND = 0x0014;

        /// <summary>COLORREF (0x00BBGGRR) of Color.Background.</summary>
        private const int EraseColor = 0x00141010;

        // One brush for all windows, for the whole run of the app
        private static IntPtr _eraseBrush;

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateSolidBrush(int color);

        [DllImport("user32.dll")]
        private static extern int FillRect(IntPtr hdc, ref RECT rect, IntPtr brush);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hwnd, out RECT rect);


        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        // --- Backdrop blur (DWM) ---

        private const int MinBuildSystemBackdrop = 22621;
        private const int MinBuildRoundedCorners = 22000;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_COLOR_NONE = unchecked((int)0xFFFFFFFE);
        private const int DWMWCP_DONOTROUND = 1;
        private const int DWMWCP_ROUND = 2;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
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
