using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.SteamGridDB;
using GameTimeNext.Core.Framework.Utils;
using System.Globalization;
using System.IO;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Profiles.Selectors
{
    /// <summary>
    /// Covers of SteamGridDB, one page at a time (CurrentPage). The grids are searched on the first load; each page
    /// is downloaded when it is shown, the next one in advance.
    /// </summary>
    public class ProfilesSteamGridDBView_CoversSelectorAdapter : UIXSelectorAdapterBase<ProfilesSteamGridDBApp, ProfilesSteamGridDBView, UIXListBox, SteamGridDBImage>
    {
        private readonly string _coversFolder = AppConfig.Temp.SteamGridDBCoversDirectoryPath;
        private readonly int _coversPerPage = 12;

        // Shared by current page and prefetch, allows a whole page to load in parallel
        private readonly SemaphoreSlim _downloadSemaphore = new SemaphoreSlim(12);

        private readonly List<SteamGridDBPage> _sgdbPages = new List<SteamGridDBPage>();
        private SteamGridDBClient? _client;

        public ProfilesSteamGridDBView_CoversSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        /// <summary>
        /// Index of the page shown by the next load.
        /// </summary>
        public int CurrentPage { get; set; } = 0;

        /// <summary>
        /// Number of pages; 0 before the first load or when SteamGridDB has no covers for the game.
        /// </summary>
        public int PageCount => _sgdbPages.Count;

        /// <summary>
        /// Searches the grids on the first call, then returns the covers of the current page (empty without covers).
        /// Errors are passed on to the caller.
        /// </summary>
        protected override async Task<IEnumerable<SteamGridDBImage>> ExecuteAsync(CancellationToken token)
        {
            if (_client == null)
            {
                SteamGridDBClient client = new SteamGridDBClient(AppEnvironment.GetAppConfig().AppSettings.SteamGridDbKey.Trim());

                IReadOnlyList<SgdbGrid> grids;

                if (GetApp().SteamAppId != 0)
                    grids = await client.GetGridsBySteamAppIdAsync((int)GetApp().SteamAppId, dimensions: "600x900");
                else
                    grids = await client.GetGridsByNameAsync(GetApp().GameName, dimensions: "600x900");

                FnDirectory.EnsureDirectoryExists(_coversFolder);

                BuildSteamGridDBPages(grids ?? Array.Empty<SgdbGrid>());

                // Only after the search succeeded, otherwise the next load searches again
                _client = client;
                CurrentPage = 0;
            }

            if (CurrentPage < 0 || CurrentPage >= _sgdbPages.Count)
                return Array.Empty<SteamGridDBImage>();

            SteamGridDBPage currentPage = _sgdbPages[CurrentPage];

            try
            {
                // Reuses a running (prefetch) download instead of starting a second one
                currentPage.DownloadTask ??= DownloadPageAsync(currentPage);
                await currentPage.DownloadTask;
            }
            catch (Exception)
            {
                // Allow a retry the next time the page is shown
                currentPage.DownloadTask = null;
                throw;
            }

            PrefetchPage(CurrentPage + 1);

            return currentPage.Images;
        }

        /// <summary>
        /// Downloads the full resolution image of the cover, if not done yet (only needed for the chosen one).
        /// </summary>
        public async Task DownloadFullImageAsync(SteamGridDBImage image)
        {
            if (_client == null || File.Exists(image.FullImagePath))
                return;

            // In the background: it is decoded and saved as JPEG after the download, which would block the window on the UI thread
            await Task.Run(() => _client.DownloadCoverAsync(image.FullImageUrl, image.FullImagePath));
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

                if (grid == null || FnString.IsNullEmptyOrWhitespace(grid.Url))
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

            if (!FnString.IsNullEmptyOrWhitespace(grid.Thumb))
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
                        await _client!.DownloadFileAsync(url, path);

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
                ? GetApp().SteamAppId.ToString(CultureInfo.InvariantCulture)
                : SanitizeFileName(GetApp().GameName);

            string gridPart = "grid";

            try
            {
                object? gridIdValue = grid.GetType().GetProperty("Id")?.GetValue(grid);
                if (gridIdValue != null)
                    gridPart = Convert.ToString(gridIdValue, CultureInfo.InvariantCulture) ?? gridPart;
            }
            catch
            {
            }

            return $"{appPart}_p{pageIndex}_i{itemIndex}_{gridPart}.jpg";
        }

        private static string SanitizeFileName(string value)
        {
            if (FnString.IsNullEmptyOrWhitespace(value))
                return "unknown";

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                value = value.Replace(invalidChar, '_');

            return value.Replace(' ', '_');
        }

        private void BuildSteamGridDBPages(IReadOnlyList<SgdbGrid> grids)
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
    }
}
