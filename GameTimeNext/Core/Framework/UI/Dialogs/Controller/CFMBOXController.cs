using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;
using UIX.ViewController.Engine.FrameworkElements.Windows;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Framework.UI.Dialogs
{
    [Flags]
    public enum CFMBOXResult
    {
        None = 0,
        Ok = 1,
        Yes = 2,
        No = 4,
        Cancel = 8
    }

    public enum CFMBOXIcon
    {
        None = 0,
        Info = 1,
        Question = 2,
        Warning = 3,
        Error = 4,
        Success = 5
    }

    internal class CFMBOXController : UIXWindowControllerBase
    {
        public class WindowReturn : UIXWindowReturn
        {
            public CFMBOXResult Result { get; set; } = CFMBOXResult.None;
        }

        private readonly WindowReturn _windowReturn = new WindowReturn();

        private CFMBOXResult _button1Result = CFMBOXResult.None;
        private CFMBOXResult _button2Result = CFMBOXResult.None;
        private CFMBOXResult _button3Result = CFMBOXResult.None;

        private bool _isInternalMove;
        private Window? _owner;

        public CFMBOXController(UIXApplication app) : base(app)
        {
        }

        public override WindowReturn GetWindowReturn()
        {
            return _windowReturn;
        }

        public CFMBOXResult ShowDialog(string title, string message, CFMBOXResult buttons, CFMBOXIcon icon, Window? owner)
        {
            _owner = owner;
            _windowReturn.Result = CFMBOXResult.None;

            ApplyContent(title, message, buttons, icon, owner);
            GetWnd().ShowDialog();

            return _windowReturn.Result;
        }

        protected override void Init()
        {
            GetWnd().Loaded += Wnd_Loaded;
            GetWnd().LocationChanged += Wnd_LocationChanged;
            GetWnd().PreviewKeyDown += Wnd_PreviewKeyDown;
            GetWnd().HeaderRoot.MouseLeftButtonDown += HeaderRoot_MouseLeftButtonDown;
        }

        protected override void BuildFirst()
        {
        }

        protected override void Build()
        {
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
            if (eventName != UIXEventNames.Button.Click)
                return;

            if (source == GetWnd().BtnButton1)
            {
                EV_Button1();
                return;
            }

            if (source == GetWnd().BtnButton2)
            {
                EV_Button2();
                return;
            }

            if (source == GetWnd().BtnButton3)
            {
                EV_Button3();
            }
        }

        protected override void Check()
        {
        }

        protected override void FillViewImpl()
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected override void Event_Closing()
        {
            GetWnd().Loaded -= Wnd_Loaded;
            GetWnd().LocationChanged -= Wnd_LocationChanged;
            GetWnd().PreviewKeyDown -= Wnd_PreviewKeyDown;
            GetWnd().HeaderRoot.MouseLeftButtonDown -= HeaderRoot_MouseLeftButtonDown;
        }

        protected override void Event_Minimize()
        {
        }

        protected override void Event_Maximize()
        {
        }

        private void ApplyContent(string title, string message, CFMBOXResult buttons, CFMBOXIcon icon, Window? owner)
        {
            GetWnd().Owner = owner;
            GetWnd().Title = title;
            GetWnd().TbMessage.Text = message;

            ConfigureButtons(buttons);
            ApplyIcon(icon);
            PrepareStartupPosition();
        }

        private void Wnd_Loaded(object? sender, RoutedEventArgs e)
        {
            ClampToOwner();
            FocusDefaultButton();
        }

        private void Wnd_LocationChanged(object? sender, EventArgs e)
        {
            if (_isInternalMove)
                return;

            ClampToOwner();
        }

        private void Wnd_PreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CFMBOXResult escapeResult = GetEscapeResult();
                if (escapeResult != CFMBOXResult.None)
                {
                    e.Handled = true;
                    CloseWithResult(escapeResult);
                }

                return;
            }

            if (e.Key == Key.Enter)
            {
                CFMBOXResult defaultResult = GetDefaultResult();
                if (defaultResult != CFMBOXResult.None)
                {
                    e.Handled = true;
                    CloseWithResult(defaultResult);
                }
            }
        }

        private void HeaderRoot_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            GetWnd().DragMove();
            ClampToOwner();
        }

        private void EV_Button1()
        {
            CloseWithResult(_button1Result);
        }

        private void EV_Button2()
        {
            CloseWithResult(_button2Result);
        }

        private void EV_Button3()
        {
            CloseWithResult(_button3Result);
        }

        private void CloseWithResult(CFMBOXResult result)
        {
            _windowReturn.Result = result;
            GetWnd().Close();
        }

        private void ConfigureButtons(CFMBOXResult buttons)
        {
            ResetButtons();

            if (buttons == CFMBOXResult.None || buttons == CFMBOXResult.Ok)
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

            if (GetWnd().BtnButton1.Visibility != Visibility.Visible)
                SetButton1(CFMBOXResult.Ok);
        }

        private void ResetButtons()
        {
            GetWnd().BtnButton1.Visibility = Visibility.Collapsed;
            GetWnd().BtnButton2.Visibility = Visibility.Collapsed;
            GetWnd().BtnButton3.Visibility = Visibility.Collapsed;

            GetWnd().BtnButton1.Content = string.Empty;
            GetWnd().BtnButton2.Content = string.Empty;
            GetWnd().BtnButton3.Content = string.Empty;

            GetWnd().BtnButton1.IsDefault = false;
            GetWnd().BtnButton2.IsDefault = false;
            GetWnd().BtnButton3.IsDefault = false;

            GetWnd().BtnButton1.IsCancel = false;
            GetWnd().BtnButton2.IsCancel = false;
            GetWnd().BtnButton3.IsCancel = false;

            _button1Result = CFMBOXResult.None;
            _button2Result = CFMBOXResult.None;
            _button3Result = CFMBOXResult.None;
        }

        private void SetNextButton(CFMBOXResult result)
        {
            if (GetWnd().BtnButton1.Visibility != Visibility.Visible)
            {
                SetButton1(result);
                return;
            }

            if (GetWnd().BtnButton2.Visibility != Visibility.Visible)
            {
                SetButton2(result);
                return;
            }

            if (GetWnd().BtnButton3.Visibility != Visibility.Visible)
            {
                SetButton3(result);
            }
        }

        private void SetButton1(CFMBOXResult result)
        {
            _button1Result = result;
            GetWnd().BtnButton1.Content = GetButtonText(result);
            GetWnd().BtnButton1.Visibility = Visibility.Visible;
        }

        private void SetButton2(CFMBOXResult result)
        {
            _button2Result = result;
            GetWnd().BtnButton2.Content = GetButtonText(result);
            GetWnd().BtnButton2.Visibility = Visibility.Visible;
        }

        private void SetButton3(CFMBOXResult result)
        {
            _button3Result = result;
            GetWnd().BtnButton3.Content = GetButtonText(result);
            GetWnd().BtnButton3.Visibility = Visibility.Visible;
        }

        private void ApplyIcon(CFMBOXIcon icon)
        {
            GetWnd().BdIconHost.BorderBrush = TryFindBrush("Brush.Border", Color.FromRgb(96, 100, 108));
            GetWnd().TbIconGlyph.Foreground = TryFindBrush("Brush.TextPrimary", Color.FromRgb(242, 245, 248));

            switch (icon)
            {
                case CFMBOXIcon.Info:
                    GetWnd().TbIconGlyph.Text = "i";
                    GetWnd().BdIconHost.Background = TryFindBrush("Brush.Accent", Color.FromRgb(139, 30, 36));
                    break;

                case CFMBOXIcon.Question:
                    GetWnd().TbIconGlyph.Text = "?";
                    GetWnd().BdIconHost.Background = TryFindBrush("Brush.Warning", Color.FromRgb(241, 196, 15));
                    break;

                case CFMBOXIcon.Warning:
                    GetWnd().TbIconGlyph.Text = "!";
                    GetWnd().BdIconHost.Background = TryFindBrush("Brush.Warning", Color.FromRgb(241, 196, 15));
                    break;

                case CFMBOXIcon.Error:
                    GetWnd().TbIconGlyph.Text = "×";
                    GetWnd().BdIconHost.Background = TryFindBrush("Brush.Danger", Color.FromRgb(231, 76, 60));
                    break;

                case CFMBOXIcon.Success:
                    GetWnd().TbIconGlyph.Text = "✓";
                    GetWnd().BdIconHost.Background = TryFindBrush("Brush.Success", Color.FromRgb(46, 204, 113));
                    break;

                default:
                    GetWnd().TbIconGlyph.Text = string.Empty;
                    GetWnd().BdIconHost.Background = TryFindBrush("Brush.Accent", Color.FromRgb(139, 30, 36));
                    break;
            }
        }

        private void FocusDefaultButton()
        {
            CFMBOXResult defaultResult = GetDefaultResult();

            if (_button1Result == defaultResult)
            {
                GetWnd().BtnButton1.IsDefault = true;
                GetWnd().BtnButton1.Focus();
                return;
            }

            if (_button2Result == defaultResult)
            {
                GetWnd().BtnButton2.IsDefault = true;
                GetWnd().BtnButton2.Focus();
                return;
            }

            if (_button3Result == defaultResult)
            {
                GetWnd().BtnButton3.IsDefault = true;
                GetWnd().BtnButton3.Focus();
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
            if (_owner == null)
            {
                GetWnd().WindowStartupLocation = WindowStartupLocation.CenterScreen;
                return;
            }

            GetWnd().WindowStartupLocation = WindowStartupLocation.Manual;

            double ownerLeft = _owner.Left;
            double ownerTop = _owner.Top;
            double ownerWidth = _owner.ActualWidth > 0 ? _owner.ActualWidth : _owner.Width;
            double ownerHeight = _owner.ActualHeight > 0 ? _owner.ActualHeight : _owner.Height;

            double wndWidth = GetWnd().Width;
            double wndHeight = GetWnd().Height;

            GetWnd().Left = ownerLeft + ((ownerWidth - wndWidth) / 2d);
            GetWnd().Top = ownerTop + ((ownerHeight - wndHeight) / 2d);

            ClampToOwner();
        }

        private void ClampToOwner()
        {
            if (_owner == null)
                return;

            double ownerLeft = _owner.Left;
            double ownerTop = _owner.Top;
            double ownerWidth = _owner.ActualWidth > 0 ? _owner.ActualWidth : _owner.Width;
            double ownerHeight = _owner.ActualHeight > 0 ? _owner.ActualHeight : _owner.Height;

            double currentWidth = GetWnd().ActualWidth > 0 ? GetWnd().ActualWidth : GetWnd().Width;
            double currentHeight = GetWnd().ActualHeight > 0 ? GetWnd().ActualHeight : GetWnd().Height;

            double minLeft = ownerLeft;
            double minTop = ownerTop;
            double maxLeft = ownerLeft + ownerWidth - currentWidth;
            double maxTop = ownerTop + ownerHeight - currentHeight;

            if (maxLeft < minLeft)
                maxLeft = minLeft;

            if (maxTop < minTop)
                maxTop = minTop;

            double newLeft = GetWnd().Left;
            double newTop = GetWnd().Top;

            if (newLeft < minLeft)
                newLeft = minLeft;

            if (newLeft > maxLeft)
                newLeft = maxLeft;

            if (newTop < minTop)
                newTop = minTop;

            if (newTop > maxTop)
                newTop = maxTop;

            if (Math.Abs(newLeft - GetWnd().Left) < 0.1 && Math.Abs(newTop - GetWnd().Top) < 0.1)
                return;

            _isInternalMove = true;
            GetWnd().Left = newLeft;
            GetWnd().Top = newTop;
            _isInternalMove = false;
        }

        private SolidColorBrush TryFindBrush(string resourceKey, Color fallbackColor)
        {
            object resource = GetWnd().TryFindResource(resourceKey);
            if (resource is SolidColorBrush brush)
                return brush;

            return new SolidColorBrush(fallbackColor);
        }

        private CFMBOXView GetWnd()
        {
            return (CFMBOXView)View;
        }
    }
}