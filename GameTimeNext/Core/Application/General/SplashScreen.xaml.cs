using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.General
{
    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : Window
    {
        public string AppVersion
        {
            get
            {
                string suffix = AppEnvironment.AppVersion.IsBeta ? "-beta" : "";
                return $"v{AppEnvironment.AppVersion.VersionText}{suffix}";
            }
        }

        public SplashScreen()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FnControls.SetVisible(DevBadge, FnSystem.IsDebug());
            FnControls.SetVisible(txtDevelopmentPathActive, FnSystem.IsDebug());
        }
    }
}
