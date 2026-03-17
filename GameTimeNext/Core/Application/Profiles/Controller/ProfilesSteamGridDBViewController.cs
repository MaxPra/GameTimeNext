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

        private string _coversFolder = Path.Combine(
                                          Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                          "GameTimeNext",
                                          "Temp_SteamGridDBCovers"
                                          );

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

            List<SteamGridDBImage> images = new List<SteamGridDBImage>();

            await Task.Run(async () =>
            {
                try
                {
                    SteamGridDBClient client = new SteamGridDBClient(AppEnvironment.GetAppConfig().AppSettings.SteamGridDbKey.Trim());
                    IReadOnlyList<SgdbGrid> grids;

                    if (GetApp().SteamAppId != 0)
                        grids = await client.GetGridsBySteamAppIdAsync((int)GetApp().SteamAppId, dimensions: "600x900");
                    else
                        grids = await client.GetGridsByNameAsync(GetApp().GameName, dimensions: "600x900");

                    if (grids.Count == 0)
                    {
                        GetWnd().Dispatcher.Invoke(() =>
                        {
                            CFMBOX cfmbox = new CFMBOX();
                            cfmbox.Show("Attention", "There were no covers found", CFMBOXResult.Ok);
                            Exit(true);
                        });
                    }

                    int index = 1;

                    Directory.CreateDirectory(_coversFolder);

                    List<Task> downloadTasks = new List<Task>();
                    using SemaphoreSlim semaphore = new SemaphoreSlim(6);

                    //Thread.Sleep(10000);

                    foreach (var grid in grids.Where(g => !string.IsNullOrWhiteSpace(g.Url)))
                    {

                        int currentIndex = index;
                        string currentUrl = grid.Url;

                        downloadTasks.Add(Task.Run(async () =>
                        {

                            await semaphore.WaitAsync();

                            try
                            {
                                var fileName = $"{GetApp().SteamAppId}_cover_{currentIndex}.jpg";
                                var path = Path.Combine(_coversFolder, fileName);

                                await client.DownloadFileAsync(currentUrl!, path);

                                SteamGridDBImage image = new SteamGridDBImage();
                                image.BitmapImage = FnImage.LoadImageWithoutLock(path, 200, 300);
                                image.Path = path;

                                images.Add(image);
                            }
                            catch (Exception ex)
                            {
                                int j = 0;
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }
                        ));

                        index++;
                    }

                    await Task.WhenAll(downloadTasks);

                    View.Dispatcher.Invoke(() =>
                    {
                        BuildSGCoverList(images);
                    });
                }
                catch (Exception ex)
                {
                    int i = 0;
                }
                finally
                {
                    GetApp().Loader.Stop();
                }
            });
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
        }

        protected override void Check()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected override void Event_Closing()
        {
            //try { Directory.Delete(_coversFolder, true); } catch (Exception e) { /* Ignorieren */ }
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

            SteamGridDBImage selected = _profilesSteamGridDBViewModel.SelectedImage! as SteamGridDBImage;

            GetViewReturn<ProfilesSteamGridDBViewReturn>().SelectedImagePath = selected.Path;
            GetViewReturn<ProfilesSteamGridDBViewReturn>().Canceled = false;

            Exit(true);
        }

        protected void EV_btnCancel()
        {
            Exit(true);
        }

        private void BuildSGCoverList(List<SteamGridDBImage> images)
        {
            // Viewmodel befüllen
            _profilesSteamGridDBViewModel = new ProfilesSteamGridDBViewModel();
            _profilesSteamGridDBViewModel.Images = new System.Collections.ObjectModel.ObservableCollection<SteamGridDBImage>(images);


            if (images != null && images.Count > 0)
                _profilesSteamGridDBViewModel.SelectedImage = images.FirstOrDefault<SteamGridDBImage>()!;

            View.DataContext = _profilesSteamGridDBViewModel;
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
