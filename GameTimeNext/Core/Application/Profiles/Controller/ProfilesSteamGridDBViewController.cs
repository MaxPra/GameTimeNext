using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.SteamGridDB;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    public class ProfilesSteamGridDBViewController : UIXViewControllerBase
    {
        private ProfilesSteamGridDBViewModel _profilesSteamGridDBViewModel;

        private readonly string _coversFolder = AppConfig.Temp.SteamGridDBCoversDirectoryPath;
        private readonly int _coversPerPage = 12;

        // Shared by current page and prefetch, allows a whole page to load in parallel
        private readonly SemaphoreSlim _downloadSemaphore = new SemaphoreSlim(12);

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

        protected override void BuildFirstImpl()
        {
        }

        protected override async Task BuildFirstImplAsync()
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
                    GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(
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
                    GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(
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
                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(
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

        protected override void BuildImpl()
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

        protected async Task EV_btnApply()
        {
            SteamGridDBImage selected = _profilesSteamGridDBViewModel?.SelectedImage as SteamGridDBImage;

            if (selected == null)
                return;

            GetApp().Loader.Begin();

            try
            {
                // Only now download the full resolution image of the chosen cover
                if (!File.Exists(selected.FullImagePath))
                    await _client.DownloadCoverAsync(selected.FullImageUrl, selected.FullImagePath);
            }
            catch (Exception)
            {
                GetApp().Loader.Stop();

                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(
                    "Error",
                    "An error occured while downloading the cover.",
                    CFMBOXResult.Ok,
                    CFMBOXIcon.Error
                );

                return;
            }

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

                // Reuses a running (prefetch) download instead of starting a second one
                currentPage.DownloadTask ??= DownloadPageAsync(currentPage);
                await currentPage.DownloadTask;

                BuildSGCoverList(currentPage.Images);
                Build();

                PrefetchPage(_currentPage + 1);
            }
            catch (Exception)
            {
                // Allow a retry the next time the page is shown
                _sgdbPages[_currentPage].DownloadTask = null;

                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(
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

        /// <summary>
        /// Downloads the given page in the background, so switching to it is instant.
        /// </summary>
        private void PrefetchPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _sgdbPages.Count)
                return;

            SteamGridDBPage page = _sgdbPages[pageIndex];

            if (page.DownloadTask != null)
                return;

            page.DownloadTask = DownloadPageAsync(page);

            // On error reset, the page is downloaded again when it gets shown
            page.DownloadTask.ContinueWith(t => page.DownloadTask = null,
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async Task DownloadPageAsync(SteamGridDBPage page)
        {
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
                    await _downloadSemaphore.WaitAsync();

                    try
                    {
                        string fileName = BuildImageFileName(page.PageIndex, itemIndex, grid);

                        SteamGridDBImage image = new SteamGridDBImage
                        {
                            BitmapImage = await DownloadThumbnailAsync(grid, Path.GetFileNameWithoutExtension(fileName)),
                            FullImageUrl = grid.Url,
                            FullImagePath = Path.Combine(_coversFolder, fileName)
                        };

                        downloadedImages[itemIndex] = image;
                    }
                    catch (Exception)
                    {
                        // Skip single broken covers instead of failing the whole page
                    }
                    finally
                    {
                        _downloadSemaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(downloadTasks);

            page.Images = downloadedImages
                .Where(x => x != null)
                .ToList();
        }

        /// <summary>
        /// Downloads the small preview of a grid for the selection list.
        /// Falls back to the full image if there is no thumbnail or it can't be decoded.
        /// </summary>
        private async Task<BitmapImage> DownloadThumbnailAsync(SgdbGrid grid, string baseFileName)
        {
            List<string> urls = new List<string>();

            if (!string.IsNullOrWhiteSpace(grid.Thumb))
                urls.Add(grid.Thumb);

            urls.Add(grid.Url!);

            Exception? lastError = null;

            foreach (string url in urls)
            {
                // Keep the original file extension, the thumbnail is stored without re-encoding
                string extension = Path.GetExtension(new Uri(url).AbsolutePath);
                string path = Path.Combine(_coversFolder, $"{baseFileName}_thumb{extension}");

                try
                {
                    if (!File.Exists(path))
                        await _client.DownloadFileAsync(url, path);

                    return FnImage.LoadImageWithoutLock(path, 200, 300)!;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    try { File.Delete(path); } catch { }
                }
            }

            throw lastError!;
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
                    PageIndex = _sgdbPages.Count,
                    PageItems = pageItems,
                    Images = new List<SteamGridDBImage>()
                });
            }
        }

        private class SteamGridDBPage
        {
            public int PageIndex { get; set; }
            public List<SgdbGrid> PageItems { get; set; } = new List<SgdbGrid>();
            public List<SteamGridDBImage> Images { get; set; } = new List<SteamGridDBImage>();

            /// <summary>
            /// Running or finished download of this page, null if not started yet
            /// </summary>
            public Task? DownloadTask { get; set; }
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
