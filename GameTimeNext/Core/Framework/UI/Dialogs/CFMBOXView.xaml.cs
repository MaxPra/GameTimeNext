using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace GameTimeNext.Core.Framework.UI.Dialogs
{
    public partial class CFMBOXView
    {
        private CFMBOXResult _selectedResult = CFMBOXResult.None;

        private CFMBOXResult _button1Result = CFMBOXResult.None;
        private CFMBOXResult _button2Result = CFMBOXResult.None;
        private CFMBOXResult _button3Result = CFMBOXResult.None;

        private bool _isInternalMove;

        public CFMBOXView()
        {
            InitializeComponent();

            Loaded += CFMBOX_Loaded;
            LocationChanged += CFMBOX_LocationChanged;
            PreviewKeyDown += CFMBOX_PreviewKeyDown;
        }

        public CFMBOXResult Show(string title, string message, CFMBOXResult buttons)
        {
            return Show(title, message, buttons, CFMBOXIcon.Info, System.Windows.Application.Current?.MainWindow);
        }

        public CFMBOXResult Show(string title, string message, CFMBOXResult buttons, CFMBOXIcon icon)
        {
            return Show(title, message, buttons, icon, System.Windows.Application.Current?.MainWindow);
        }

        public CFMBOXResult Show(string title, string message, CFMBOXResult buttons, Window owner)
        {
            return Show(title, message, buttons, CFMBOXIcon.Info, owner);
        }

        public CFMBOXResult Show(string title, string message, CFMBOXResult buttons, CFMBOXIcon icon, Window owner)
        {
            Owner = owner;

            TbMessage.Text = message;

            ConfigureButtons(buttons);
            ApplyIcon(icon);
            PrepareStartupPosition();

            base.ShowDialog();
            return _selectedResult;
        }

        private void CFMBOX_Loaded(object sender, RoutedEventArgs e)
        {
            ClampToOwner();
            FocusDefaultButton();
        }

        private void CFMBOX_LocationChanged(object sender, EventArgs e)
        {
            if (_isInternalMove)
                return;

            ClampToOwner();
        }

        private void CFMBOX_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                var escapeResult = GetEscapeResult();
                if (escapeResult != CFMBOXResult.None)
                {
                    e.Handled = true;
                    CloseWithResult(escapeResult);
                }
            }
            else if (e.Key == Key.Enter)
            {
                var defaultResult = GetDefaultResult();
                if (defaultResult != CFMBOXResult.None)
                {
                    e.Handled = true;
                    CloseWithResult(defaultResult);
                }
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            DragMove();
            ClampToOwner();
        }

        private void BtnButton1_Click(object sender, RoutedEventArgs e)
        {
            CloseWithResult(_button1Result);
        }

        private void BtnButton2_Click(object sender, RoutedEventArgs e)
        {
            CloseWithResult(_button2Result);
        }

        private void BtnButton3_Click(object sender, RoutedEventArgs e)
        {
            CloseWithResult(_button3Result);
        }

        protected override void OnClosed(EventArgs e)
        {
            Loaded -= CFMBOX_Loaded;
            LocationChanged -= CFMBOX_LocationChanged;
            PreviewKeyDown -= CFMBOX_PreviewKeyDown;
            base.OnClosed(e);
        }

        private void CloseWithResult(CFMBOXResult result)
        {
            _selectedResult = result;
            Close();
        }

        private void ConfigureButtons(CFMBOXResult buttons)
        {
            ResetButtons();

            if (buttons == CFMBOXResult.None)
            {
                SetButton1(CFMBOXResult.Ok);
                return;
            }

            if (buttons == CFMBOXResult.Ok)
            {
                SetButton1(CFMBOXResult.Ok);
                return;
            }

            if (HasFlag(buttons, CFMBOXResult.Yes))
                SetNextButton(CFMBOXResult.Yes);

            if (HasFlag(buttons, CFMBOXResult.No))
                SetNextButton(CFMBOXResult.No);

            if (HasFlag(buttons, CFMBOXResult.Ok))
                SetNextButton(CFMBOXResult.Ok);

            if (HasFlag(buttons, CFMBOXResult.Cancel))
                SetNextButton(CFMBOXResult.Cancel);

            if (BtnButton1.Visibility != Visibility.Visible)
                SetButton1(CFMBOXResult.Ok);
        }

        private void ResetButtons()
        {
            BtnButton1.Visibility = Visibility.Collapsed;
            BtnButton2.Visibility = Visibility.Collapsed;
            BtnButton3.Visibility = Visibility.Collapsed;

            BtnButton1.Content = string.Empty;
            BtnButton2.Content = string.Empty;
            BtnButton3.Content = string.Empty;

            _button1Result = CFMBOXResult.None;
            _button2Result = CFMBOXResult.None;
            _button3Result = CFMBOXResult.None;

            BtnButton1.IsDefault = false;
            BtnButton2.IsDefault = false;
            BtnButton3.IsDefault = false;

            BtnButton1.IsCancel = false;
            BtnButton2.IsCancel = false;
            BtnButton3.IsCancel = false;
        }

        private void SetNextButton(CFMBOXResult result)
        {
            if (BtnButton1.Visibility != Visibility.Visible)
            {
                SetButton1(result);
                return;
            }

            if (BtnButton2.Visibility != Visibility.Visible)
            {
                SetButton2(result);
                return;
            }

            if (BtnButton3.Visibility != Visibility.Visible)
            {
                SetButton3(result);
            }
        }

        private void SetButton1(CFMBOXResult result)
        {
            _button1Result = result;
            BtnButton1.Content = GetButtonText(result);
            BtnButton1.Visibility = Visibility.Visible;
        }

        private void SetButton2(CFMBOXResult result)
        {
            _button2Result = result;
            BtnButton2.Content = GetButtonText(result);
            BtnButton2.Visibility = Visibility.Visible;
        }

        private void SetButton3(CFMBOXResult result)
        {
            _button3Result = result;
            BtnButton3.Content = GetButtonText(result);
            BtnButton3.Visibility = Visibility.Visible;
        }

        private void ApplyIcon(CFMBOXIcon icon)
        {
            switch (icon)
            {
                case CFMBOXIcon.Info:
                    TbIconGlyph.Text = "i";
                    BdIconHost.Background = new SolidColorBrush(Color.FromArgb(0x33, 0x4C, 0x9F, 0xFF));
                    BdIconHost.BorderBrush = new SolidColorBrush(Color.FromArgb(0x99, 0x4C, 0x9F, 0xFF));
                    TbIconGlyph.Foreground = Brushes.White;
                    break;

                case CFMBOXIcon.Question:
                    TbIconGlyph.Text = "?";
                    BdIconHost.Background = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xC1, 0x07));
                    BdIconHost.BorderBrush = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0xC1, 0x07));
                    TbIconGlyph.Foreground = Brushes.White;
                    break;

                case CFMBOXIcon.Warning:
                    TbIconGlyph.Text = "!";
                    BdIconHost.Background = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0x8C, 0x42));
                    BdIconHost.BorderBrush = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0x8C, 0x42));
                    TbIconGlyph.Foreground = Brushes.White;
                    break;

                case CFMBOXIcon.Error:
                    TbIconGlyph.Text = "×";
                    BdIconHost.Background = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0x4C, 0x4C));
                    BdIconHost.BorderBrush = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0x4C, 0x4C));
                    TbIconGlyph.Foreground = Brushes.White;
                    break;

                case CFMBOXIcon.Success:
                    TbIconGlyph.Text = "✓";
                    BdIconHost.Background = new SolidColorBrush(Color.FromArgb(0x33, 0x3D, 0xD5, 0x8A));
                    BdIconHost.BorderBrush = new SolidColorBrush(Color.FromArgb(0x99, 0x3D, 0xD5, 0x8A));
                    TbIconGlyph.Foreground = Brushes.White;
                    break;

                default:
                    TbIconGlyph.Text = "";
                    BdIconHost.Background = new SolidColorBrush(Color.FromArgb(0x18, 0xFF, 0x4C, 0x4C));
                    BdIconHost.BorderBrush = new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0x4C, 0x4C));
                    TbIconGlyph.Foreground = Brushes.White;
                    break;
            }
        }

        private void FocusDefaultButton()
        {
            var defaultResult = GetDefaultResult();

            if (_button1Result == defaultResult)
            {
                BtnButton1.IsDefault = true;
                BtnButton1.Focus();
                return;
            }

            if (_button2Result == defaultResult)
            {
                BtnButton2.IsDefault = true;
                BtnButton2.Focus();
                return;
            }

            if (_button3Result == defaultResult)
            {
                BtnButton3.IsDefault = true;
                BtnButton3.Focus();
            }
        }

        private CFMBOXResult GetDefaultResult()
        {
            if (_button1Result != CFMBOXResult.None)
                return _button1Result;

            if (_button2Result != CFMBOXResult.None)
                return _button2Result;

            if (_button3Result != CFMBOXResult.None)
                return _button3Result;

            return CFMBOXResult.None;
        }

        private CFMBOXResult GetEscapeResult()
        {
            if (ContainsResult(CFMBOXResult.Cancel))
                return CFMBOXResult.Cancel;

            if (ContainsResult(CFMBOXResult.No))
                return CFMBOXResult.No;

            if (ContainsResult(CFMBOXResult.Ok))
                return CFMBOXResult.Ok;

            return CFMBOXResult.None;
        }

        private bool ContainsResult(CFMBOXResult result)
        {
            return _button1Result == result || _button2Result == result || _button3Result == result;
        }

        private static bool HasFlag(CFMBOXResult value, CFMBOXResult flag)
        {
            return (value & flag) == flag;
        }

        private static string GetButtonText(CFMBOXResult result)
        {
            return result switch
            {
                CFMBOXResult.Ok => "OK",
                CFMBOXResult.Yes => "Yes",
                CFMBOXResult.No => "No",
                CFMBOXResult.Cancel => "Cancel",
                _ => "OK"
            };
        }

        private void PrepareStartupPosition()
        {
            if (Owner == null)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                return;
            }

            WindowStartupLocation = WindowStartupLocation.Manual;

            var ownerLeft = Owner.Left;
            var ownerTop = Owner.Top;
            var ownerWidth = Owner.ActualWidth > 0 ? Owner.ActualWidth : Owner.Width;
            var ownerHeight = Owner.ActualHeight > 0 ? Owner.ActualHeight : Owner.Height;

            Left = ownerLeft + ((ownerWidth - Width) / 2d);
            Top = ownerTop + ((ownerHeight - Height) / 2d);

            ClampToOwner();
        }

        private void ClampToOwner()
        {
            if (Owner == null)
                return;

            var ownerLeft = Owner.Left;
            var ownerTop = Owner.Top;
            var ownerWidth = Owner.ActualWidth > 0 ? Owner.ActualWidth : Owner.Width;
            var ownerHeight = Owner.ActualHeight > 0 ? Owner.ActualHeight : Owner.Height;

            var currentWidth = ActualWidth > 0 ? ActualWidth : Width;
            var currentHeight = ActualHeight > 0 ? ActualHeight : Height;

            var minLeft = ownerLeft;
            var minTop = ownerTop;
            var maxLeft = ownerLeft + ownerWidth - currentWidth;
            var maxTop = ownerTop + ownerHeight - currentHeight;

            if (maxLeft < minLeft)
                maxLeft = minLeft;

            if (maxTop < minTop)
                maxTop = minTop;

            var newLeft = Left;
            var newTop = Top;

            if (newLeft < minLeft)
                newLeft = minLeft;

            if (newLeft > maxLeft)
                newLeft = maxLeft;

            if (newTop < minTop)
                newTop = minTop;

            if (newTop > maxTop)
                newTop = maxTop;

            if (Math.Abs(newLeft - Left) < 0.1 && Math.Abs(newTop - Top) < 0.1)
                return;

            _isInternalMove = true;
            Left = newLeft;
            Top = newTop;
            _isInternalMove = false;
        }
    }
}