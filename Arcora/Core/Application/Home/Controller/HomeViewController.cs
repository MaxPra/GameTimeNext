using GameTimeNext.Core.Application.BigPicture;
using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.Home.Views;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Framework.Config;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Home.Controller
{
    public class HomeViewController : UIXViewControllerBase<HomeApp, HomeView>
    {
        public HomeViewController(UIXApplication app) : base(app)
        {
        }

        #region Event-Pipeline-Methods
        protected override void Init()
        {
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {
            GetView().txWelcomeTitle.Text = $"Welcome to {AppConfig.Root.ApplicationName}";
            GetView().txHelpTitle.Text = $"Learn how {AppConfig.Root.ApplicationName} works";
        }

        protected override void BuildImpl()
        {
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
        #endregion

        // Help cards: the text comes from the application definition of the module
        protected void EV_btnHelpProfiles() => ShowHelpOf(nameof(ProfilesApp), "Profiles");
        protected void EV_btnHelpPlaythroughs() => ShowHelpOf("PlaythroughsApp", "Playthroughs");
        protected void EV_btnHelpDashboard() => ShowHelpOf("DashboardApp", "Dashboard");
        protected void EV_btnHelpRemoteMonitoring() => ShowHelpOf("RemoteMonitoringApp", "Remote Monitoring");
        protected void EV_btnHelpScreensetups() => ShowHelpOf("ScreensetupsApp", "Screensetups");
        protected void EV_btnHelpBigPicture() => ShowHelpOf(nameof(BigPictureApp), "Big Picture mode");
        protected void EV_btnHelpSettings() => ShowHelpOf("SettingsApp", "Settings");

        private void ShowHelpOf(string applicationId, string title)
        {
            CFMainApp.ShowApplicationHelp(GetApp(), applicationId, title);
        }
    }
}
