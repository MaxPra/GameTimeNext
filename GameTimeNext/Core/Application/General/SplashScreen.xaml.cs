using GameTimeNext.Core.Framework;
using System.Windows;

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
    }
}
