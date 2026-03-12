using GameTimeNext.Core.Application.Settings.Controller;
using GameTimeNext.Core.Application.Settings.Views;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Settings
{
    public class SettingsApp : UIXApplication
    {

        private SettingsView? _settingsView;
        private SettingsViewController? _settingsViewController;

        public SettingsView? SettingsView { get { return _settingsView; } set { _settingsView = value; } }

        public override void InitializeApplicationOutput()
        {
            _settingsView = new SettingsView();

            _settingsViewController = new SettingsViewController(this);
            _settingsView.ViewController = _settingsViewController;
        }

        public void Start(UIXApplication hostApplication, ContentPresenter presenter)
        {
            this.HostApplication = hostApplication;
            this.SettingsView.ContentPresenter = presenter;
            this.SettingsView.ViewController.Show(false);
            Loader = hostApplication.Loader;
        }
    }
}
