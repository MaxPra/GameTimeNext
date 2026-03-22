using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.SteamGridDB;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    public class ProfilesSteamGridDBViewController : UIXWindowControllerBase
    {
        private ProfilesSteamGridDBViewModel _profilesSteamGridDBViewModel;

        private readonly string _coversFolder = AppEnvironment.GetAppConfig().AppDataLocalPathSteamGridDBCovers;
        private readonly int _coversPerPage = 12;

        private int _currentPage = 0;

        private readonly List<SteamGridDBPage> _sgdbPages = new List<SteamGridDBPage>();
        private SteamGridDBClient _client;

        public ProfilesSteamGridDBViewController(UIXApplication app) : base(app)
        {
        }

        public class ProfilesSteamGridDBViewReturn : UIXViewReturn
        {
            public string SelectedImagePath { get; set; }
        }

        protected override void BuildFirst()
        {
        }

        protected override async Task BuildFirstAsync()
        {
            GetApp().Loader.Begin();

            try
            {
                _client = new SteamGridDBClient(AppEnvironment.GetAppConfig().AppSettings.SteamGridDbKey.Trim());

                IReadOnlyList<SgdbGrid> grids;

                if (GetApp().SteamAppId != 0)
                    grids = await _client.GetGridsBySteamAppIdAsync((int)GetApp().SteamAppId, dimensions: "600x900");
                else
                    grids = await _client.GetGridsByNameAsync(GetApp().GameName, dimensions: "600x900");

                if (grids == null || grids.Count == 0)
                {
                    GetApp().GetApplication<CFMBOX>().Show(
                        "Attention",
                        "There were no covers found",
                        CFMBOXResult.Ok
                    );

                    Exit(true);
                    return;
                }

                Directory.CreateDirectory(_coversFolder);

                BuildSteamGridDBPagesDictionary(grids);

                if (_sgdbPages.Count == 0)
                {
                    GetApp().GetApplication<CFMBOX>().Show(
                        "Attention",
                        "There were no covers found",
                        CFMBOXResult.Ok
                    );

                    Exit(true);
                    return;
                }

                _currentPage = 0;
                await ShowCurrentPageAsync();
            }
            catch (Exception)
            {
                GetApp().GetApplication<CFMBOX>().Show(
                    "Error",
                    "An error occured!\nTry again later!",
                    CFMBOXResult.Ok,
                    CFMBOXIcon.Error
                );
            }
            finally
            {
                GetApp().Loader.Stop();
            }
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

        protected override void Build()
        {
            FnControls.SetEnabled(GetWnd().btnPreviousPage, _currentPage > 0);
            FnControls.SetEnabled(GetWnd().btnNextPage, _currentPage < _sgdbPages.Count - 1);
        }

        protected override void Check()
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

        protected void EV_btnApply()
        {
            SteamGridDBImage selected = _profilesSteamGridDBViewModel?.SelectedImage as SteamGridDBImage;

            if (selected == null)
                return;

            GetViewReturn<ProfilesSteamGridDBViewReturn>().SelectedImagePath = selected.Path;
            GetViewReturn<ProfilesSteamGridDBViewReturn>().Canceled = false;

            Exit(true);
        }

        protected void EV_btnCancel()
        {
            Exit(true);
        }

        protected async Task EV_btnPreviousPage()
        {
            if (_currentPage <= 0)
                return;

            _currentPage--;
            await ShowCurrentPageAsync();
        }

        protected async Task EV_btnNextPage()
        {
            if (_currentPage >= _sgdbPages.Count - 1)
                return;

            _currentPage++;
            await ShowCurrentPageAsync();
        }

        private async Task ShowCurrentPageAsync()
        {
            if (_currentPage < 0 || _currentPage >= _sgdbPages.Count)
                return;

            GetApp().Loader.Begin();

            try
            {
                SteamGridDBPage currentPage = _sgdbPages[_currentPage];

                if (!currentPage.Downloaded)
                    await DownloadPageAsync(currentPage);

                BuildSGCoverList(currentPage.Images);
                Build();
            }
            catch (Exception)
            {
                GetApp().GetApplication<CFMBOX>().Show(
                    "Error",
                    "An error occured while loading the page.",
                    CFMBOXResult.Ok,
                    CFMBOXIcon.Error
                );
            }
            finally
            {
                GetApp().Loader.Stop();
            }
        }

        private void BuildSGCoverList(List<SteamGridDBImage> images)
        {
            _profilesSteamGridDBViewModel = new ProfilesSteamGridDBViewModel();
            _profilesSteamGridDBViewModel.Images =
                new System.Collections.ObjectModel.ObservableCollection<SteamGridDBImage>(images);

            if (images.Count > 0)
                _profilesSteamGridDBViewModel.SelectedImage = images[0];

            View.DataContext = _profilesSteamGridDBViewModel;
        }

        private async Task DownloadPageAsync(SteamGridDBPage page)
        {
            if (page == null || page.Downloaded)
                return;

            using SemaphoreSlim semaphore = new SemaphoreSlim(6);

            SteamGridDBImage[] downloadedImages = new SteamGridDBImage[page.PageItems.Count];
            List<Task> downloadTasks = new List<Task>();

            for (int i = 0; i < page.PageItems.Count; i++)
            {
                int itemIndex = i;
                SgdbGrid grid = page.PageItems[itemIndex];

                if (grid == null || string.IsNullOrWhiteSpace(grid.Url))
                    continue;

                downloadTasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();

                    try
                    {
                        string fileName = BuildImageFileName(_currentPage, itemIndex, grid);
                        string path = Path.Combine(_coversFolder, fileName);

                        if (!File.Exists(path))
                            await _client.DownloadFileAsync(grid.Url, path);

                        SteamGridDBImage image = new SteamGridDBImage
                        {
                            BitmapImage = FnImage.LoadImageWithoutLock(path, 200, 300),
                            Path = path
                        };

                        downloadedImages[itemIndex] = image;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(downloadTasks);

            page.Images = downloadedImages
                .Where(x => x != null)
                .ToList();

            page.Downloaded = true;
        }

        private string BuildImageFileName(int pageIndex, int itemIndex, SgdbGrid grid)
        {
            string appPart = GetApp().SteamAppId != 0
                ? GetApp().SteamAppId.ToString()
                : SanitizeFileName(GetApp().GameName);

            string gridPart = "grid";

            try
            {
                object gridIdValue = grid.GetType().GetProperty("Id")?.GetValue(grid);
                if (gridIdValue != null)
                    gridPart = gridIdValue.ToString();
            }
            catch
            {
            }

            return $"{appPart}_p{pageIndex}_i{itemIndex}_{gridPart}.jpg";
        }

        private string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "unknown";

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                value = value.Replace(invalidChar, '_');

            return value.Replace(' ', '_');
        }

        private void BuildSteamGridDBPagesDictionary(IReadOnlyList<SgdbGrid> grids)
        {
            _sgdbPages.Clear();

            for (int i = 0; i < grids.Count; i += _coversPerPage)
            {
                List<SgdbGrid> pageItems = grids
                    .Skip(i)
                    .Take(_coversPerPage)
                    .ToList();

                if (pageItems.Count == 0)
                    continue;

                _sgdbPages.Add(new SteamGridDBPage
                {
                    PageItems = pageItems,
                    Images = new List<SteamGridDBImage>(),
                    Downloaded = false
                });
            }
        }

        private class SteamGridDBPage
        {
            public List<SgdbGrid> PageItems { get; set; } = new List<SgdbGrid>();
            public List<SteamGridDBImage> Images { get; set; } = new List<SteamGridDBImage>();
            public bool Downloaded { get; set; } = false;
        }

        private ProfilesSteamGridDBApp GetApp()
        {
            return (ProfilesSteamGridDBApp)App;
        }

        private ProfilesSteamGridDBView GetWnd()
        {
            return (ProfilesSteamGridDBView)View;
        }
    }
}