using System.Windows;
using System.Windows.Controls.Primitives;
using Button = System.Windows.Controls.Button;
using Control = System.Windows.Controls.Control;
using System.Windows.Input;
using System.Windows.Media;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;
using UIX.ViewController.Engine.FrameworkElements.Markdown;
using UIX.ViewController.Engine.FrameworkElements.Windows;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using GameTimeNext.Core.Framework.Utils;

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

    public enum CFMBOXType
    {
        Default = 0,
        Memotext = 1,

        /// <summary>Markdown rendered like on GitHub (headings, lists, code, tables, links), e.g. the changelog.</summary>
        Markdown = 2,

        /// <summary>Like Markdown, in a larger window for long texts with images, e.g. the application help.</summary>
        MarkdownLarge = 3
    }

    internal class CFMBOXController : UIXWindowControllerBase<CFMBOX, CFMBOXView>
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

        // Memo text dialog (e.g. changelog): sized to the text within these bounds
        private const double MemotextMinWidth = 460;
        private const double MemotextMinHeight = 220;
        private const double MemotextMaxWidth = 1000;
        private const double MemotextMaxHeight = 800;
        private const double MemotextMaxScreenShare = 0.85;

        // Markdown dialog: comfortable reading width (wrapped text has no natural width)
        private const double MarkdownWidth = 780;

        // Large Markdown dialog (help): wider and higher, still limited to MemotextMaxScreenShare of the screen
        private const double MarkdownLargeWidth = 1100;
        private const double MarkdownLargeMaxWidth = 1400;
        private const double MarkdownLargeMaxHeight = 1000;

        public CFMBOXController(UIXApplication app) : base(app)
        {
        }

        public override WindowReturn GetWindowReturn()
        {
            return _windowReturn;
        }

        public CFMBOXResult ShowDialog(string title, string message, CFMBOXResult buttons, CFMBOXIcon icon, Window? owner, CFMBOXType type = CFMBOXType.Default)
        {
            _owner = owner;
            _windowReturn.Result = CFMBOXResult.None;

            ApplyContent(title, message, buttons, icon, owner, type);

            Show(true);

            return _windowReturn.Result;
        }

        protected override void Init()
        {
            GetView().Loaded += Wnd_Loaded;
            GetView().LocationChanged += Wnd_LocationChanged;
            GetView().PreviewKeyDown += Wnd_PreviewKeyDown;
            //GetView().HeaderRoot.MouseLeftButtonDown += HeaderRoot_MouseLeftButtonDown;
        }

        protected override void BuildFirstImpl()
        {
        }

        protected override void BuildImpl()
        {
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
            if (eventName != UIXEventNames.Button.Click)
                return;

            if (source == GetView().BtnButton1)
            {
                EV_Button1();
                return;
            }

            if (source == GetView().BtnButton2)
            {
                EV_Button2();
                return;
            }

            if (source == GetView().BtnButton3)
            {
                EV_Button3();
            }
        }

        protected override void CheckImpl()
        {
        }

        protected override void FillViewImpl()
        {
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
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
            GetView().Loaded -= Wnd_Loaded;
            GetView().LocationChanged -= Wnd_LocationChanged;
            GetView().PreviewKeyDown -= Wnd_PreviewKeyDown;
            //GetView().HeaderRoot.MouseLeftButtonDown -= HeaderRoot_MouseLeftButtonDown;
        }

        protected override void Event_Minimize()
        {
        }

        protected override void Event_Maximize()
        {
        }

        private void ApplyContent(string title, string message, CFMBOXResult buttons, CFMBOXIcon icon, Window? owner, CFMBOXType type = CFMBOXType.Default)
        {
            // Ensure dialog has correct owner so it stays on top of the main window
            GetView().Owner = owner;
            // Do not show dialog as separate taskbar window
            GetView().ShowInTaskbar = false;

            if (FnString.IsNullEmptyOrWhitespace(title))
                title = DeriveTitleFromIcon(icon);

            GetView().Title = title;

            GetView().BdMarkdown.Visibility = Visibility.Collapsed;

            if (type == CFMBOXType.Markdown || type == CFMBOXType.MarkdownLarge)
            {
                bool isLarge = type == CFMBOXType.MarkdownLarge;
                double width = isLarge ? MarkdownLargeWidth : MarkdownWidth;
                double maxWidth = isLarge ? MarkdownLargeMaxWidth : MemotextMaxWidth;
                double maxHeight = isLarge ? MarkdownLargeMaxHeight : MemotextMaxHeight;

                GetView().SpText.Visibility = Visibility.Collapsed;
                GetView().TxbMemotext.Visibility = Visibility.Collapsed;
                GetView().FdsvMarkdown.Document = UIXMarkdownRenderer.ToFlowDocument(message);
                GetView().BdMarkdown.Visibility = Visibility.Visible;

                // Wrapped text has no natural width: fixed reading width, the height follows the content up to a maximum
                Rect workArea = (_owner != null ? FnDisplay.GetWorkArea(_owner) : null) ?? SystemParameters.WorkArea;

                GetView().SizeToContent = SizeToContent.Height;
                GetView().ResizeMode = ResizeMode.CanResize;
                GetView().Width = Math.Max(MemotextMinWidth, Math.Min(width, workArea.Width * MemotextMaxScreenShare));
                GetView().Height = double.NaN;
                GetView().MinWidth = MemotextMinWidth;
                GetView().MinHeight = MemotextMinHeight;
                GetView().MaxWidth = Math.Max(MemotextMinWidth, Math.Min(maxWidth, workArea.Width * MemotextMaxScreenShare));
                GetView().MaxHeight = Math.Max(MemotextMinHeight, Math.Min(maxHeight, workArea.Height * MemotextMaxScreenShare));
            }
            else if (type == CFMBOXType.Memotext)
            {
                GetView().SpText.Visibility = Visibility.Collapsed;
                GetView().TxbMemotext.Text = message;
                GetView().TxbMemotext.Visibility = Visibility.Visible;

                // Size follows the text (longest line / number of lines), limited to a sensible maximum;
                // beyond that the text scrolls inside the window
                Rect workArea = (_owner != null ? FnDisplay.GetWorkArea(_owner) : null) ?? SystemParameters.WorkArea;

                GetView().SizeToContent = SizeToContent.WidthAndHeight;
                GetView().ResizeMode = ResizeMode.CanResize;
                GetView().Width = double.NaN;
                GetView().Height = double.NaN;
                GetView().MinWidth = MemotextMinWidth;
                GetView().MinHeight = MemotextMinHeight;
                GetView().MaxWidth = Math.Max(MemotextMinWidth, Math.Min(MemotextMaxWidth, workArea.Width * MemotextMaxScreenShare));
                GetView().MaxHeight = Math.Max(MemotextMinHeight, Math.Min(MemotextMaxHeight, workArea.Height * MemotextMaxScreenShare));
            }
            else
            {
                GetView().TxbMemotext.Visibility = Visibility.Collapsed;
                ApplyMessage(message);
                GetView().SpText.Visibility = Visibility.Visible;

                GetView().SizeToContent = SizeToContent.Height;
                GetView().ResizeMode = ResizeMode.NoResize;
                GetView().Width = 460;
                GetView().MinWidth = 460;
                GetView().MinHeight = 0;
                GetView().MaxWidth = 820;
                GetView().MaxHeight = 700;
            }

            ConfigureButtons(buttons);
            HighlightDefaultButton();
            ApplyIcon(icon);

            // Markdown brings its own headings, the icon column would only take away width
            if (type == CFMBOXType.Markdown || type == CFMBOXType.MarkdownLarge)
                GetView().BdIconHost.Visibility = Visibility.Collapsed;

            PrepareStartupPosition();
        }

        /// <summary>
        /// Multi-line messages with a short first line are split into a headline and a muted body text.
        /// </summary>
        private void ApplyMessage(string message)
        {
            message = (message ?? string.Empty).Replace("\r\n", "\n").Trim();

            int lineBreak = message.IndexOf('\n');
            if (lineBreak > 0 && lineBreak <= 120)
            {
                GetView().TbHeadline.Text = message.Substring(0, lineBreak).Trim();
                GetView().TbHeadline.Visibility = Visibility.Visible;
                GetView().TbMessage.Text = message.Substring(lineBreak + 1).Trim();
                GetView().TbMessage.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "Brush.TextSecondary");
                return;
            }

            GetView().TbHeadline.Visibility = Visibility.Collapsed;
            GetView().TbMessage.Text = message;
            GetView().TbMessage.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "Brush.TextPrimary");
        }

        private static string DeriveTitleFromIcon(CFMBOXIcon icon)
        {
            return icon switch
            {
                CFMBOXIcon.Info => "Information",
                CFMBOXIcon.Question => "Question",
                CFMBOXIcon.Warning => "Warning",
                CFMBOXIcon.Error => "Error",
                CFMBOXIcon.Success => "Success",
                _ => string.Empty,
            };
        }

        private void Wnd_Loaded(object? sender, RoutedEventArgs e)
        {
            // Now with the real size (SizeToContent), centered within the owner on its monitor
            CenterInOwnerArea();
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

            GetView().DragMove();
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
            GetView().Close();
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

            if (GetView().BtnButton1.Visibility != Visibility.Visible)
                SetButton1(CFMBOXResult.Ok);
        }

        private void ResetButtons()
        {
            GetView().BtnButton1.Visibility = Visibility.Collapsed;
            GetView().BtnButton2.Visibility = Visibility.Collapsed;
            GetView().BtnButton3.Visibility = Visibility.Collapsed;

            GetView().BtnButton1.Content = string.Empty;
            GetView().BtnButton2.Content = string.Empty;
            GetView().BtnButton3.Content = string.Empty;

            GetView().BtnButton1.IsDefault = false;
            GetView().BtnButton2.IsDefault = false;
            GetView().BtnButton3.IsDefault = false;

            GetView().BtnButton1.IsCancel = false;
            GetView().BtnButton2.IsCancel = false;
            GetView().BtnButton3.IsCancel = false;

            _button1Result = CFMBOXResult.None;
            _button2Result = CFMBOXResult.None;
            _button3Result = CFMBOXResult.None;
        }

        private void HighlightDefaultButton()
        {
            CFMBOXResult defaultResult = GetDefaultResult();

            foreach (Button button in new[] { GetView().BtnButton1, GetView().BtnButton2, GetView().BtnButton3 })
            {
                button.ClearValue(Control.BackgroundProperty);
                button.ClearValue(Control.BorderBrushProperty);
            }

            Button? defaultButton = defaultResult == _button1Result ? GetView().BtnButton1
                : defaultResult == _button2Result ? GetView().BtnButton2
                : defaultResult == _button3Result ? GetView().BtnButton3
                : null;

            if (defaultButton == null)
                return;

            defaultButton.SetResourceReference(Control.BackgroundProperty, "Brush.Accent");
            defaultButton.SetResourceReference(Control.BorderBrushProperty, "Brush.AccentHover");
        }

        private void SetNextButton(CFMBOXResult result)
        {
            if (GetView().BtnButton1.Visibility != Visibility.Visible)
            {
                SetButton1(result);
                return;
            }

            if (GetView().BtnButton2.Visibility != Visibility.Visible)
            {
                SetButton2(result);
                return;
            }

            if (GetView().BtnButton3.Visibility != Visibility.Visible)
            {
                SetButton3(result);
            }
        }

        private void SetButton1(CFMBOXResult result)
        {
            _button1Result = result;
            GetView().BtnButton1.Content = GetButtonText(result);
            GetView().BtnButton1.Visibility = Visibility.Visible;
        }

        private void SetButton2(CFMBOXResult result)
        {
            _button2Result = result;
            GetView().BtnButton2.Content = GetButtonText(result);
            GetView().BtnButton2.Visibility = Visibility.Visible;
        }

        private void SetButton3(CFMBOXResult result)
        {
            _button3Result = result;
            GetView().BtnButton3.Content = GetButtonText(result);
            GetView().BtnButton3.Visibility = Visibility.Visible;
        }

        private void ApplyIcon(CFMBOXIcon icon)
        {
            // Segoe Fluent / MDL2 glyphs: Info, Unknown, Warning, ErrorBadge, Completed
            switch (icon)
            {
                case CFMBOXIcon.Info:
                    GetView().TbIconGlyph.Text = "";
                    GetView().TbIconGlyph.Foreground = TryFindBrush("Brush.TextSecondary", Color.FromRgb(148, 148, 155));
                    break;

                case CFMBOXIcon.Question:
                    GetView().TbIconGlyph.Text = "";
                    GetView().TbIconGlyph.Foreground = TryFindBrush("Brush.TextSecondary", Color.FromRgb(148, 148, 155));
                    break;

                case CFMBOXIcon.Warning:
                    GetView().TbIconGlyph.Text = "";
                    GetView().TbIconGlyph.Foreground = TryFindBrush("Brush.Warning", Color.FromRgb(255, 167, 38));
                    break;

                case CFMBOXIcon.Error:
                    GetView().TbIconGlyph.Text = "";
                    GetView().TbIconGlyph.Foreground = TryFindBrush("Brush.Danger", Color.FromRgb(255, 76, 102));
                    break;

                case CFMBOXIcon.Success:
                    GetView().TbIconGlyph.Text = "";
                    GetView().TbIconGlyph.Foreground = TryFindBrush("Brush.Success", Color.FromRgb(46, 224, 137));
                    break;

                default:
                    GetView().TbIconGlyph.Text = string.Empty;
                    break;
            }

            GetView().BdIconHost.Visibility = icon == CFMBOXIcon.None ? Visibility.Collapsed : Visibility.Visible;
        }

        private void FocusDefaultButton()
        {
            CFMBOXResult defaultResult = GetDefaultResult();

            if (_button1Result == defaultResult)
            {
                GetView().BtnButton1.IsDefault = true;
                GetView().BtnButton1.Focus();
                return;
            }

            if (_button2Result == defaultResult)
            {
                GetView().BtnButton2.IsDefault = true;
                GetView().BtnButton2.Focus();
                return;
            }

            if (_button3Result == defaultResult)
            {
                GetView().BtnButton3.IsDefault = true;
                GetView().BtnButton3.Focus();
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
            if (GetOwnerArea() == null)
            {
                GetView().WindowStartupLocation = WindowStartupLocation.CenterScreen;
                return;
            }

            GetView().WindowStartupLocation = WindowStartupLocation.Manual;
            CenterInOwnerArea();
        }

        /// <summary>
        /// Area the dialog is centered in and kept inside: the owner's actual bounds (Window.Left/Top are the restore
        /// position for a maximized owner, e.g. on a monitor that was switched off), limited to the work area of the
        /// owner's monitor, so the modal dialog is always visible. Null if the owner can't be used (none, hidden, minimized).
        /// </summary>
        private Rect? GetOwnerArea()
        {
            if (_owner == null || !_owner.IsVisible || _owner.WindowState == WindowState.Minimized)
                return null;

            Rect? ownerBounds = FnDisplay.GetWindowBounds(_owner);
            Rect? workArea = FnDisplay.GetWorkArea(_owner);

            if (ownerBounds == null || workArea == null)
                return null;

            Rect visibleOwnerArea = Rect.Intersect(ownerBounds.Value, workArea.Value);

            // Owner (almost) outside its monitor: use the whole work area
            if (visibleOwnerArea.IsEmpty || visibleOwnerArea.Width < 100 || visibleOwnerArea.Height < 100)
                return workArea;

            return visibleOwnerArea;
        }

        private void CenterInOwnerArea()
        {
            if (GetOwnerArea() is not Rect area)
                return;

            double wndWidth = GetDialogWidth();
            double wndHeight = GetDialogHeight();

            _isInternalMove = true;
            GetView().Left = area.Left + ((area.Width - wndWidth) / 2d);
            GetView().Top = area.Top + ((area.Height - wndHeight) / 2d);
            _isInternalMove = false;

            ClampToOwner();
        }

        // Before the first layout the size may be NaN (SizeToContent): fall back to the minimum size
        private double GetDialogWidth()
        {
            if (GetView().ActualWidth > 0)
                return GetView().ActualWidth;

            return double.IsNaN(GetView().Width) ? GetView().MinWidth : GetView().Width;
        }

        private double GetDialogHeight()
        {
            if (GetView().ActualHeight > 0)
                return GetView().ActualHeight;

            return double.IsNaN(GetView().Height) ? Math.Max(GetView().MinHeight, 200) : GetView().Height;
        }

        private void ClampToOwner()
        {
            if (GetOwnerArea() is not Rect area)
                return;

            double currentWidth = GetDialogWidth();
            double currentHeight = GetDialogHeight();

            double minLeft = area.Left;
            double minTop = area.Top;
            double maxLeft = area.Right - currentWidth;
            double maxTop = area.Bottom - currentHeight;

            if (maxLeft < minLeft)
                maxLeft = minLeft;

            if (maxTop < minTop)
                maxTop = minTop;

            double newLeft = double.IsNaN(GetView().Left) ? minLeft : GetView().Left;
            double newTop = double.IsNaN(GetView().Top) ? minTop : GetView().Top;

            if (newLeft < minLeft)
                newLeft = minLeft;

            if (newLeft > maxLeft)
                newLeft = maxLeft;

            if (newTop < minTop)
                newTop = minTop;

            if (newTop > maxTop)
                newTop = maxTop;

            if (Math.Abs(newLeft - GetView().Left) < 0.1 && Math.Abs(newTop - GetView().Top) < 0.1)
                return;

            _isInternalMove = true;
            GetView().Left = newLeft;
            GetView().Top = newTop;
            _isInternalMove = false;
        }

        private SolidColorBrush TryFindBrush(string resourceKey, Color fallbackColor)
        {
            object resource = GetView().TryFindResource(resourceKey);
            if (resource is SolidColorBrush brush)
                return brush;

            return new SolidColorBrush(fallbackColor);
        }

    }
}
