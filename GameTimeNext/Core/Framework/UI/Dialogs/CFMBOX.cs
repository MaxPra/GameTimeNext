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

        public CFMBOXResult Show(string title, string message, CFMBOXResult buttons)
        {
            return Show(title, message, buttons, CFMBOXIcon.Info, System.Windows.Application.Current?.MainWindow);
        }

        public CFMBOXResult Show(string title, string message, CFMBOXResult buttons, CFMBOXIcon icon)
        {
            return Show(title, message, buttons, icon, System.Windows.Application.Current?.MainWindow);
        }

        public CFMBOXResult Show(string title, string message, CFMBOXResult buttons, Window? owner)
        {
            return Show(title, message, buttons, CFMBOXIcon.Info, owner);
        }

        public CFMBOXResult Show(string title, string message, CFMBOXResult buttons, CFMBOXIcon icon, Window? owner)
        {
            CreateFreshDialog();
            return _cfmboxController!.ShowDialog(title, message, buttons, icon, owner);
        }

        private void CreateFreshDialog()
        {
            _cfmboxView = new CFMBOXView();
            MainView = _cfmboxView;

            _cfmboxController = new CFMBOXController(this);
            _cfmboxView.WndController = _cfmboxController;
        }
    }
}