using System.Windows;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Framework.UI.Dialogs
{
    public class CFMBOX : UIXApplication
    {
        private CFMBOXView? _cfmboxView;
        private CFMBOXController? _cfmboxController;

        public override void InitializeApplicationOutput()
        {
            _cfmboxView = new CFMBOXView();
            MainView = _cfmboxView;

            _cfmboxController = new CFMBOXController(this);
            _cfmboxView.WndController = _cfmboxController;
        }

        public CFMBOXResult Show(string title, string message, CFMBOXResult buttons, CFMBOXType type = CFMBOXType.Default)
        {
            return Show(title, message, buttons, CFMBOXIcon.Info, this.WndParent ?? System.Windows.Application.Current?.MainWindow, type);
        }

        public CFMBOXResult Show(string message, CFMBOXResult buttons, CFMBOXIcon icon, CFMBOXType type = CFMBOXType.Default)
        {
            return Show(message, buttons, icon, this.WndParent ?? System.Windows.Application.Current?.MainWindow, type);
        }

        public CFMBOXResult Show(string message, CFMBOXResult buttons, CFMBOXIcon icon, Window? owner, CFMBOXType type = CFMBOXType.Default)
        {
            string title = DeriveTitleFromIcon(icon);
            return Show(title, message, buttons, icon, owner ?? this.WndParent ?? System.Windows.Application.Current?.MainWindow, type);
        }

        public CFMBOXResult Show(string title, string message, CFMBOXResult buttons, CFMBOXIcon icon, CFMBOXType type = CFMBOXType.Default)
        {
            return Show(title, message, buttons, icon, System.Windows.Application.Current?.MainWindow, type);
        }

        public CFMBOXResult Show(string title, string message, CFMBOXResult buttons, Window? owner, CFMBOXType type = CFMBOXType.Default)
        {
            return Show(title, message, buttons, CFMBOXIcon.Info, owner, type);
        }

        public CFMBOXResult Show(string title, string message, CFMBOXResult buttons, CFMBOXIcon icon, Window? owner, CFMBOXType type = CFMBOXType.Default)
        {
            CreateFreshDialog();
            return _cfmboxController!.ShowDialog(title, message, buttons, icon, owner, type);
        }

        private void CreateFreshDialog()
        {
            _cfmboxView = new CFMBOXView();
            MainView = _cfmboxView;

            _cfmboxController = new CFMBOXController(this);
            _cfmboxView.WndController = _cfmboxController;
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
    }
}