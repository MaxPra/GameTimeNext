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

        // Passed by the application: the splash process has no debugger attached, so it can't detect development itself
        private readonly bool isDevelopment;

        public SplashScreen(bool isDevelopment)
        {
            this.isDevelopment = isDevelopment;
            DataContext = this;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FnControls.SetVisible(brdBeta, AppEnvironment.AppVersion.IsBeta);
            FnControls.SetVisible(brdDevBadge, isDevelopment);
            FnControls.SetVisible(txtDevelopmentPathActive, isDevelopment);
        }
    }
}
