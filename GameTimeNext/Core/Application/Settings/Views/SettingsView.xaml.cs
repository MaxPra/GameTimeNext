using System.Diagnostics;
using System.Windows.Navigation;
using UIX.ViewController.Engine.FrameworkElements.UserControls;

namespace GameTimeNext.Core.Application.Settings.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UIXUserControlBase
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void OnDeveloperLinkRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true
            });

            e.Handled = true;
        }
    }
}
