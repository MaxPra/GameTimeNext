using GameTimeNext.Core.Application.Profiles.Selectors;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Framework.UI;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    public class ProfilesSteamGridDBViewController : UIXViewControllerBase<ProfilesSteamGridDBApp, ProfilesSteamGridDBView>
    {
        public ProfilesSteamGridDBViewController(UIXApplication app) : base(app)
        {
        }

        public class ProfilesSteamGridDBViewReturn : UIXViewReturn
        {
            public string SelectedImagePath { get; set; }
        }

        protected override void BuildFirstImpl()
        {
        }

        protected override async Task BuildFirstImplAsync()
        {
            try
            {
                await GetView().lbSteamGridCovers.LoadDataAsync();
            }
            catch (Exception)
            {
                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("GLO0012", "loading the covers from SteamGridDB"), CFMBOXResult.Ok, CFMBOXIcon.Error);
                return;
            }

            if (GetCoversAdapter().PageCount == 0)
            {
                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("PRO0005"), CFMBOXResult.Ok, CFMBOXIcon.Info);

                Exit(true);
                return;
            }

            SelectFirstCover();
        }

        protected override void Init()
        {
            ViewReturn = new ProfilesSteamGridDBViewReturn();

            GetApp().Loader.SetRandomTexts(
                "Fetching artwork from SteamGridDB...",
                "Searching SteamGridDB for the perfect covers...",
                "Looting shiny new artwork...",
                "Downloading cosmetic upgrades for your games...",
                "Consulting the SteamGridDB archives...",
                "Preparing beautiful covers...",
                "Equipping your games with fresh artwork...",
                "Scanning SteamGridDB for legendary covers...",
                "Polishing your game library with new art...",
                "Importing community-made artwork...",
                "Grabbing some fancy covers...",
                "Synchronizing with SteamGridDB...",
                "Collecting high-resolution artwork...",
                "Looking for the perfect grid...",
                "Upgrading your game library visuals...",
                "Exploring the SteamGridDB vault...",
                "Checking the artwork armory...",
                "Gathering premium covers...",
                "Searching for epic grid art...",
                "Hunting for legendary artwork...",
                "Inspecting community masterpieces...",
                "Fetching visual upgrades...",
                "Preparing visual enhancements...",
                "Scanning for rare covers...",
                "Downloading fresh grid artwork...",
                "Exploring the cover dimension...",
                "Finding the perfect look for your games...",
                "Collecting artwork like rare loot...",
                "Opening the SteamGridDB treasure chest...",
                "Summoning high-resolution covers...",
                "Unlocking cosmetic upgrades...",
                "Searching for artistic perfection...",
                "Exploring new visual styles...",
                "Browsing the cover archives...",
                "Importing premium grid artwork...",
                "Gathering stylish covers...",
                "Preparing some visual magic...",
                "Checking the artwork inventory...",
                "Downloading the finest covers...",
                "Looking for next-level artwork...",
                "Scanning for community favorites...",
                "Collecting the best grids available...",
                "Equipping next-gen artwork...",
                "Summoning new library visuals...",
                "Retrieving rare visual upgrades...",
                "Discovering hidden cover gems...",
                "Upgrading your games' wardrobe...",
                "Finding the ultimate cover art...",
                "Inspecting SteamGridDB treasures...",
                "Bringing beautiful artwork to your library...",
                "Fetching premium community covers...",
                "Collecting stylish visual upgrades..."
            );
        }

        protected override void BuildImpl()
        {
            ProfilesSteamGridDBView_CoversSelectorAdapter coversAdapter = GetCoversAdapter();

            FnControls.SetEnabled(GetView().btnPreviousPage, coversAdapter.CurrentPage > 0);
            FnControls.SetEnabled(GetView().btnNextPage, coversAdapter.CurrentPage < coversAdapter.PageCount - 1);

            // Preview of the selected cover as case and disc (only set on a change: a new cover rolls the disc out again).
            // Its disc section is taken over by the profile cover on Apply (DiscPrint.AdoptSectionAsync).
            BitmapImage? previewCover = (GetView().lbSteamGridCovers.SelectedItem as SteamGridDBImage)?.BitmapImage;

            if (!ReferenceEquals(GetView().ccCoverPreview.Content, previewCover))
                GetView().ccCoverPreview.Content = previewCover;
        }

        protected override void CheckImpl()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected override void Event_Closing()
        {
            // try { Directory.Delete(_coversFolder, true); } catch (Exception) { }
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

        protected async Task EV_btnApply()
        {
            if (GetView().lbSteamGridCovers.SelectedItem is not SteamGridDBImage selected)
                return;

            GetApp().Loader.Begin();

            try
            {
                // Only now download the full resolution image of the chosen cover
                await GetCoversAdapter().DownloadFullImageAsync(selected);
            }
            catch (Exception)
            {
                GetApp().Loader.Stop();

                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("GLO0012", "downloading the cover"), CFMBOXResult.Ok, CFMBOXIcon.Error);

                return;
            }

            // The profile cover (a copy of this file) shows the disc exactly like the preview, everywhere
            await DiscPrint.AdoptSectionAsync(selected.BitmapImage, selected.FullImagePath);

            GetApp().Loader.Stop();

            GetViewReturn<ProfilesSteamGridDBViewReturn>().SelectedImagePath = selected.FullImagePath;
            GetViewReturn<ProfilesSteamGridDBViewReturn>().Canceled = false;

            Exit(true);
        }

        protected void EV_btnCancel()
        {
            Exit(true);
        }

        protected async Task EV_btnPreviousPage()
        {
            if (GetCoversAdapter().CurrentPage <= 0)
                return;

            await ShowPageAsync(GetCoversAdapter().CurrentPage - 1);
        }

        protected async Task EV_btnNextPage()
        {
            if (GetCoversAdapter().CurrentPage >= GetCoversAdapter().PageCount - 1)
                return;

            await ShowPageAsync(GetCoversAdapter().CurrentPage + 1);
        }

        private ProfilesSteamGridDBView_CoversSelectorAdapter GetCoversAdapter()
        {
            return GetView().lbSteamGridCovers.GetSelectorAdapter<ProfilesSteamGridDBView_CoversSelectorAdapter>();
        }

        private async Task ShowPageAsync(int page)
        {
            GetCoversAdapter().CurrentPage = page;

            try
            {
                if (!await GetView().lbSteamGridCovers.LoadDataAsync())
                    return;

                SelectFirstCover();
                Build();
            }
            catch (Exception)
            {
                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("GLO0012", "loading the page"), CFMBOXResult.Ok, CFMBOXIcon.Error);
            }
        }

        private void SelectFirstCover()
        {
            GetView().lbSteamGridCovers.SelectedItem = GetView().lbSteamGridCovers.Items.OfType<SteamGridDBImage>().FirstOrDefault();
        }
    }
}
