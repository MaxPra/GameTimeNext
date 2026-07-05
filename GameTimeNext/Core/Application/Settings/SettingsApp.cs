using GameTimeNext.Core.Application.Settings.Controller;
using GameTimeNext.Core.Application.Settings.Views;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Settings
{
    public class SettingsApp : UIXApplication, IUIXApplicationStarter
    {

        private SettingsView? _settingsView;
        private SettingsViewController? _settingsViewController;

        public SettingsView? SettingsView { get { return _settingsView; } set { _settingsView = value; } }

        public override void InitializeApplicationOutput()
        {
            _settingsView = new SettingsView();

            _settingsViewController = new SettingsViewController(this);
            _settingsView.ViewController = _settingsViewController;

            Icon = UIXMdlIcons.Settings;
        }

        public void Start(UIXApplication hostApplication, ContentPresenter presenter)
        {
            Start(hostApplication, new UIXApplicationStartOptions
            {
                Target = UIXApplicationStartTarget.ContentPresenter,
                Presenter = presenter
            });
        }

        public void Start(UIXApplication hostApplication, UIXApplicationStartOptions options)
        {
            HostApplication = hostApplication;
            Loader = hostApplication.Loader;
            CallDispatcher = hostApplication.CallDispatcher;

            SettingsView!.ViewController.Show(options);
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
        }
    }
}
