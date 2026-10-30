using GameTimeNext.Core.Application.Profiles.Selectors;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Framework.LauncherIntegration;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    public class ProfilesLauncherImportViewController : UIXViewControllerBase<ProfilesLauncherImportApp, ProfilesLauncherImportView>
    {

        public ProfilesLauncherImportViewController(UIXApplication app) : base(app)
        {
        }

        public class ProfilesLauncherImportViewReturn : UIXViewReturn
        {
            public LauncherGame? Game { get; set; } = null;
        }

        public void ImportGame()
        {
            GetViewReturn<ProfilesLauncherImportViewReturn>().Canceled = false;
            GetViewReturn<ProfilesLauncherImportViewReturn>().Game = GetSelectedGame();
            Exit(true);
        }

        protected override void Init()
        {
            // Set loader texts
            GetApp().Loader.SetRandomTexts(
                                            "Searching your launchers for games...",
                                            "Asking Steam what you own...",
                                            "Looking through your game libraries...",
                                            "Finding games you haven't added yet...",
                                            "Peeking into every installed launcher...",
                                            "Counting your installed games..."
                                            );

            ViewReturn = new ProfilesLauncherImportViewReturn();
        }

        protected override void BuildImpl()
        {
            // Visibility of the import button
            GetView().btnImport.IsEnabled = GetSelectedGame() != null;
        }

        protected override void BuildFirstImpl()
        {
        }

        protected override async Task BuildFirstImplAsync()
        {
            await BuildGameGridAsync();
        }

        protected override void CheckImpl()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected override void Event_Closing()
        {
            GetViewReturn<ProfilesLauncherImportViewReturn>().Canceled = true;
            Exit(false);
        }

        protected override void Event_Maximize()
        {
        }

        protected override void Event_Minimize()
        {
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void FillViewImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected async Task EV_txbSearch()
        {
            await BuildGameGridAsync();
        }

        /// <summary>
        /// Import Button
        /// </summary>
        protected void EV_btnImport()
        {
            ImportGame();
        }

        protected void EV_btnCancel()
        {
            GetViewReturn<ProfilesLauncherImportViewReturn>().Canceled = true;

            Exit(true);
        }

        private async Task BuildGameGridAsync()
        {
            if (!await GetView().dgGames.LoadDataAsync())
                return;

            GetView().dgGames.SelectedItem = GetView().dgGames.Items.OfType<GameImportRow>().FirstOrDefault();
        }

        private LauncherGame? GetSelectedGame()
        {
            return (GetView().dgGames.SelectedItem as GameImportRow)?.RowObject as LauncherGame;
        }
    }
}
