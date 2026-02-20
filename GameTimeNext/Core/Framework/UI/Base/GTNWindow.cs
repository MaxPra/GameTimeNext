using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace GameTimeNext.Core.Framework.UI.Base
{
    public class GTNWindow : Window
    {
        private bool _allowClose;

        public GTNWindow()
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResize;

            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("PART_TitleBar") is FrameworkElement titleBar)
            {
                titleBar.MouseLeftButtonDown += TitleBar_MouseLeftButtonDown;
            }

            if (GetTemplateChild("PART_CloseButton") is Button closeBtn)
                closeBtn.Click += (_, __) => Close();

            if (GetTemplateChild("PART_MinButton") is Button minBtn)
                minBtn.Click += (_, __) => WindowState = WindowState.Minimized;

            if (GetTemplateChild("PART_MaxButton") is Button maxBtn)
                maxBtn.Click += (_, __) =>
                    WindowState = WindowState == WindowState.Maximized
                        ? WindowState.Normal
                        : WindowState.Maximized;
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
    }
}
