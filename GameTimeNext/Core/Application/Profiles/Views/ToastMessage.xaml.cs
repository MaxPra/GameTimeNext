using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace GameTimeNext.Core.Framework.UI
{
    public partial class ToastMessage : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int SW_SHOWNOACTIVATE = 4;

        private readonly DispatcherTimer closeTimer = new();
        private bool allowClose;
        private readonly string toastTitle;
        private readonly string toastMessage;
        private readonly TimeSpan displayDuration;

        private double hiddenLeft;
        private double visibleLeft;
        private double visibleTop;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public ToastMessage(string title, string message)
            : this(title, message, TimeSpan.FromSeconds(4))
        {
        }

        public ToastMessage(string title, string message, TimeSpan duration)
        {
            InitializeComponent();

            toastTitle = title;
            toastMessage = message;
            displayDuration = duration;

            closeTimer.Interval = displayDuration;
            closeTimer.Tick += CloseTimer_Tick;
        }

        public new void Show()
        {
            base.Show();

            IntPtr handle = new WindowInteropHelper(this).Handle;
            ShowWindow(handle, SW_SHOWNOACTIVATE);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr handle = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(handle, GWL_EXSTYLE);
            SetWindowLong(handle, GWL_EXSTYLE, extendedStyle | WS_EX_NOACTIVATE);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TbTitle.Text = toastTitle;
            TbMessage.Text = toastMessage;

            CalculateWindowPositions();
            SetHiddenStartPosition();
            AnimateOpen();

            closeTimer.Start();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!allowClose)
            {
                e.Cancel = true;
                AnimateClose();
                return;
            }

            closeTimer.Stop();
            base.OnClosing(e);
        }

        private void CloseTimer_Tick(object? sender, EventArgs e)
        {
            closeTimer.Stop();
            Close();
        }

        private void CalculateWindowPositions()
        {
            Rect workArea = SystemParameters.WorkArea;
            const double marginTop = 16;
            const double marginRight = 16;

            visibleLeft = workArea.Right - Width - marginRight;
            visibleTop = workArea.Top + marginTop;
            hiddenLeft = workArea.Right + 12;
        }

        private void SetHiddenStartPosition()
        {
            Left = hiddenLeft;
            Top = visibleTop;
            Opacity = 0;
        }

        private void AnimateOpen()
        {
            DoubleAnimation leftAnimation = new()
            {
                From = hiddenLeft,
                To = visibleLeft,
                Duration = TimeSpan.FromMilliseconds(260),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            DoubleAnimation opacityAnimation = new()
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(220)
            };

            BeginAnimation(Window.LeftProperty, leftAnimation);
            BeginAnimation(Window.OpacityProperty, opacityAnimation);
        }

        private void AnimateClose()
        {
            DoubleAnimation leftAnimation = new()
            {
                From = Left,
                To = hiddenLeft,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            DoubleAnimation opacityAnimation = new()
            {
                From = Opacity,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(180)
            };

            opacityAnimation.Completed += (_, _) =>
            {
                allowClose = true;
                Close();
            };

            BeginAnimation(Window.LeftProperty, leftAnimation);
            BeginAnimation(Window.OpacityProperty, opacityAnimation);
        }
    }
}