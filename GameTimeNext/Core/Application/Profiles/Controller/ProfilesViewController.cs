using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.Playthroughs;
using GameTimeNext.Core.Application.Profiles.BackgroundProcesses;
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using GameTimeNext.Core.Application.Profiles.Types;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Application.TimeMonitoring;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.LauncherIntegration;
using GameTimeNext.Core.Framework.UI;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.Utils;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static UIX.ViewController.Engine.FrameworkElements.UIXContextMenuFactory;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    public class ProfilesViewController : UIXViewControllerBase
    {
        private ProfilesSubViewDataWrapper? _dataWrapper;
        private ProfilesSubGridViewModel? _profilesSubGridViewModel;

        public ProfilesViewController(UIXApplication app) : base(app)
        {
        }

        #region Event-Pipeline-Methods
        protected override void Init()
        {
            _dataWrapper = GetDataWrapper<ProfilesSubViewDataWrapper>();
            GetApp().CallDispatcher.Register(this, nameof(EXEV_GameTimeMonitoringStarted));
            GetApp().CallDispatcher.Register(this, nameof(EXEV_GameTimeMonitoringStopped));
            GetApp().CallDispatcher.Register(this, nameof(EXEV_SwitchProfile));
            GetApp().CallDispatcher.Register(this, nameof(EXEV_GameLaunched));
            GetApp().CallDispatcher.Register(this, nameof(EXEV_GameClosed));

            // Set loader texts
            GetApp().Loader.SetRandomTexts(
                                            "Preparing your gaming library...",
                                            "Checking which worlds you visited last...",
                                            "Counting unfinished games...",
                                            "Connecting to the gaming multiverse...",

                                            "Summoning your game profiles...",
                                            "Dusting off your backlog...",
                                            "Loading your saved adventures...",
                                            "Scanning for epic quests...",
                                            "Synchronizing your gaming timeline...",
                                            "Restoring your digital adventures...",
                                            "Checking which worlds still need saving...",
                                            "Preparing your next journey...",
                                            "Warming up your game collection...",
                                            "Looking for unfinished side quests...",
                                            "Loading your legendary backlog...",
                                            "Rebuilding your gaming universe...",
                                            "Gathering your heroic achievements...",
                                            "Preparing countless hours of gameplay...",
                                            "Consulting the archives of your adventures..."
                                            );

            GetApp().IsInitialized = true;

        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {
        }

        protected override async Task BuildFirstImplAsync()
        {
            await BuildProfilesListBoxAsync(AppEnvironment.CurrentPfid);
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

        protected override void FillDBOImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
            ProfilesListBoxItem selectedProfi = source.SelectedItem as ProfilesListBoxItem;
            T1PROFI selectedTableObject = selectedProfi?.ItemObject as T1PROFI;

            if (selectedProfi == null || selectedTableObject == null)
                return;

            AppEnvironment.CurrentPfid = selectedTableObject.PFID;

            FnTheme.ApplyProfileBackground(selectedTableObject);

            GetView().ListBoxProfiles.ScrollIntoView(selectedProfi);

            // Falls back to the default accent, so a profile without own colors doesn't keep the previous profile's
            FnTheme.ApplyProfileAccentColors(selectedTableObject);
        }
        #endregion

        #region Profiles List View
        private async Task BuildProfilesListBoxAsync(long pfid)
        {
            GetApp().Loader.Begin();

            // Stop in finally: an error while building the list must not leave the loader visible
            try
            {
                (List<ProfilesListBoxItem> profileItems, bool filterActive) = await Task.Run(() =>
                {
                    (List<T1PROFI> t1profis, bool isFiltered) = GetAllFilteredProfis();

                    return (BuildProfileListBoxItems(t1profis), isFiltered);
                });

                // On the UI thread, also when called from a background thread
                await View.Dispatcher.InvokeAsync(() => FillProfilesListBoxAsync(profileItems, filterActive, pfid)).Task.Unwrap();
            }
            finally
            {
                GetApp().Loader.Stop();
            }
        }

        // Increased by every fill of the list: an older fill still adding tiles stops when a newer one has started
        private int _listFillVersion;

        // UI thread time per portion of tiles; the rest of a frame stays free for rendering and the loader animation
        private const double TilePortionBudgetMs = 8;

        /// <summary>
        /// Shows the profiles in the list. The tiles are added in portions, each followed by layout and a rendered frame:
        /// a game case (3D template) is expensive to create, and creating all of them in one layout pass blocked the UI
        /// thread, so the loader animation (driven by the UI thread) stood still while the library was loading.
        /// The portion size adapts to the measured time per tile. The profile is selected once all tiles exist.
        /// </summary>
        private async Task FillProfilesListBoxAsync(List<ProfilesListBoxItem> profileItems, bool filterActive, long pfid)
        {
            int version = ++_listFillVersion;

            // Indicator on the filter button if the list is filtered
            GetView().ElFilterActive.Visibility = filterActive ? Visibility.Visible : Visibility.Collapsed;

            ProfilesListBoxItem? selectedItem = profileItems.FirstOrDefault(p => (p.ItemObject as T1PROFI)?.PFID == pfid)
                                                ?? profileItems.FirstOrDefault();

            _profilesSubGridViewModel = new ProfilesSubGridViewModel();

            _dataWrapper!.TableObject = selectedItem?.ItemObject as T1PROFI;

            View.DataContext = _profilesSubGridViewModel;

            if (_dataWrapper!.TableObject == null)
            {
                _dataWrapper!.TableObject = new TXPROFI().CreateNew();
                GetApp().ProfilesDetailView.Visibility = Visibility.Hidden;
                AppEnvironment.CurrentPfid = 0;
                FnTheme.ApplyProfileBackground(null);
                FnTheme.ApplyProfileAccentColors(null);
            }
            else
            {
                GetApp().ProfilesDetailView.Visibility = Visibility.Visible;
                AppEnvironment.CurrentPfid = (selectedItem?.ItemObject as T1PROFI)?.PFID ?? 0;
                FnTheme.ApplyProfileBackground(_dataWrapper!.TableObject);
                FnTheme.ApplyProfileAccentColors(_dataWrapper!.TableObject);
            }

            ProfilesSubGridViewModel viewModel = _profilesSubGridViewModel;
            Stopwatch watch = new Stopwatch();
            int portionSize = 1;
            int index = 0;

            while (index < profileItems.Count)
            {
                int portionEnd = Math.Min(profileItems.Count, index + portionSize);
                int added = portionEnd - index;

                watch.Restart();

                for (; index < portionEnd; index++)
                    viewModel.T1Profis.Add(profileItems[index]);

                // Layout (creates the new cases) and rendering have a higher priority: they run before this continues
                await Dispatcher.Yield(DispatcherPriority.Background);

                if (version != _listFillVersion)
                    return;

                double msPerTile = Math.Max(0.25, watch.Elapsed.TotalMilliseconds / added);
                portionSize = Math.Clamp((int)(TilePortionBudgetMs / msPerTile), 1, 24);
            }

            if (selectedItem != null)
                viewModel.SelectedT1Profi = selectedItem;
        }

        /// <summary>
        /// Reads all profiles matching the library filter and the name search (TFPROFI.GetFilteredProfiles).
        /// </summary>
        private (List<T1PROFI> Profiles, bool FilterActive) GetAllFilteredProfis()
        {
            string nameSearch = GetView().Dispatcher.Invoke(() => GetView().tbSearchProfileName.Text);

            return TFPROFI.GetFilteredProfiles(GetApp().FilterCache, nameSearch);
        }

        private void BuildContextMenu(ListBoxItem lbi, T1PROFI t1profi)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("ProfilesListBoxContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            // Always Edit and Delete in principle
            contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon("\uE70F"), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon("\uE74D"), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            // Game Folder
            ProfilesContextMenuBuilder.BuildContextShowGameFolder(contextBuilder, t1profi);

            // Playthrough
            ProfilesContextMenuBuilder.BuildContextPlayhtrougths(contextBuilder, t1profi);

            // Launcher
            ProfilesContextMenuBuilder.BuildContextLauncher(contextBuilder, t1profi);

            // Archive
            ProfilesContextMenuBuilder.BuildContextArchive(contextBuilder, t1profi);


            if (contextBuilder.HasItems())
                lbi.ContextMenu = contextBuilder.Build();
            else
                lbi.ContextMenu = null;

        }
        #endregion

        private List<ProfilesListBoxItem> BuildProfileListBoxItems(List<T1PROFI> t1profiles)
        {
            List<ProfilesListBoxItem> result = new List<ProfilesListBoxItem>(t1profiles.Count);
            TXCTABD txctabd = new TXCTABD();

            for (int i = 0; i < t1profiles.Count; i++)
            {
                T1PROFI prof = t1profiles[i];
                try
                {
                    T1CTABD? t1ctabdPf = txctabd.Read("pF", prof.PLAFO);
                    T1CTABD? t1ctabdIm = t1ctabdPf == null
                        ? null
                        : txctabd.Read("iM", t1ctabdPf.PARM2);

                    string platformSymbolFile = t1ctabdIm?.PARM1 ?? string.Empty;
                    string platformSymbolFullPath =
                        AppConfig.Storage.ImagesSymbolsDirectoryPath +
                        platformSymbolFile;

                    ProfilesListBoxItem item = new ProfilesListBoxItem
                    {
                        ItemObject = prof,
                        COCOVIM = FnImage.LoadImageWithoutLock(
                            Path.Combine(AppConfig.Storage.ProfileCoversDirectoryPath ?? string.Empty, prof.PPFN),
                            300,
                            450),
                        COISPLA = FnSystem.IsExeFoundInPath(prof.EXGF),
                        COPLFPA = File.Exists(platformSymbolFullPath) ? FnImage.LoadImageWithoutLock(platformSymbolFullPath, 50, 50) : null
                    };

                    result.Add(item);
                }
                catch (Exception ex)
                {
                    FnLog.AddError(GetApp(), $"Error while building profile list box item {prof.GANA}. Continued with next item", ex);
                }
            }

            return result;
        }

        private async Task AdjustFiltersToStartedGameProfile(long pfid)
        {
            // Determine the assigned groups
            T1PROFI t1profi = new TXPROFI().Read(pfid);

            List<long> linkedTagIds = TFPROFI.GetAllLinkedTags(t1profi).Select(t => t.GRID).ToList();

            // Reset everything else, so the started game is guaranteed to be visible
            FilterCache filter = new FilterCache();
            filter.ShowArchived = t1profi.ARCH;

            // Only show games sharing a tag with the started game (if it has tags)
            if (linkedTagIds.Count > 0)
            {
                filter.ExcludeUntagged = true;
                filter.ExcludedTagIds = new TXGROUP().ReadAll()
                    .Where(g => g.GTYP == GroupType.Tag && !linkedTagIds.Contains(g.GRID))
                    .Select(g => g.GRID)
                    .ToList();
            }

            ApplyFilter(filter);

            await BuildProfilesListBoxAsync(pfid);
        }

        /// <summary>
        /// Sets the filter and stores it in the config (restored on next start)
        /// </summary>
        private void ApplyFilter(FilterCache filter)
        {
            GetApp().FilterCache = filter;

            AppEnvironment.GetAppConfig().FilterCache = filter;
            AppEnvironment.SaveAppConfig();
        }

        #region Getter App & View
        private ProfilesApp GetApp()
        {
            return (ProfilesApp)App;
        }

        private ProfilesView GetView()
        {
            return (ProfilesView)View;
        }
        #endregion

        #region External Events
        protected async Task EXEV_GameTimeMonitoringStarted()
        {
            GetView().MonitoringOverlay.Visibility = Visibility.Visible;

            if (AppEnvironment.GetAppConfig().AppSettings.ShowMonitoringIndicator)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    CFMonitoringIndicator.ToggleIndicator(System.Windows.Application.Current.MainWindow);
                });
            }

            ProfilesDetailSubViewController profileDetailViewController = (ProfilesDetailSubViewController)GetApp().ProfilesDetailView.ViewController;
            profileDetailViewController.UpdateUIMonitoringStarted();
        }

        protected async Task EXEV_GameTimeMonitoringStopped()
        {
            CFGameTimeMonitoring.UpdateTableObject();

            GetView().MonitoringOverlay.Visibility = Visibility.Hidden;

            if (AppEnvironment.GetAppConfig().AppSettings.ShowMonitoringIndicator)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    CFMonitoringIndicator.ToggleIndicator(System.Windows.Application.Current.MainWindow);
                });
            }

            await BuildProfilesListBoxAsync(AppEnvironment.CurrentPfid);

            ProfilesDetailSubViewController profileDetailViewController = (ProfilesDetailSubViewController)GetApp().ProfilesDetailView.ViewController;
            profileDetailViewController.UpdateUIMonitoringStopped();
        }

        protected async Task EXEV_SwitchProfile()
        {
            if (_profilesSubGridViewModel == null)
                return;

            List<ProfilesListBoxItem> currentProfis = _profilesSubGridViewModel.T1Profis.Where(p => (p.ItemObject as T1PROFI)?.PFID == AppEnvironment.CurrentPfid).ToList();

            if (currentProfis == null || currentProfis.Count == 0)
                await AdjustFiltersToStartedGameProfile(AppEnvironment.CurrentPfid);

            _profilesSubGridViewModel.SelectedT1Profi = _profilesSubGridViewModel.T1Profis.Where(p => (p.ItemObject as T1PROFI)?.PFID == AppEnvironment.CurrentPfid).ToList()[0];

            ToastMessage tm = new ToastMessage("Switched profile...", (_profilesSubGridViewModel!.SelectedT1Profi.ItemObject as T1PROFI)?.GANA ?? string.Empty);
            tm.Show();
        }

        protected async Task EXEV_GameLaunched()
        {
            ProfilesDetailSubViewController profileDetailViewController = (ProfilesDetailSubViewController)GetApp().ProfilesDetailView.ViewController;
            GetView().LaunchingOverlay.Visibility = Visibility.Visible;

            profileDetailViewController.UpdateUIGameRunning();
        }

        protected async Task EXEV_GameClosed()
        {
            ProfilesDetailSubViewController profileDetailViewController = (ProfilesDetailSubViewController)GetApp().ProfilesDetailView.ViewController;
            profileDetailViewController.UpdateUIGameClosed();
        }
        #endregion

        #region Internal Events
        protected async Task EV_tbSearchProfileName()
        {
            await BuildProfilesListBoxAsync(AppEnvironment.CurrentPfid);
        }

        /// <summary>
        /// Button Refresh
        /// </summary>
        protected async Task EV_BtnRefresh()
        {
            await BuildProfilesListBoxAsync(AppEnvironment.CurrentPfid);
        }

        /// <summary>
        /// Button Filter
        /// </summary>
        public void EV_BtnFilter()
        {
            if (!GetApp().ProfilesFilterView.IsShown)
            {
                GetApp().ProfilesFilterView.ShowView(true);

                // Closed via Apply
                GetApp().ProfilesFilterView.ViewController.SetResultCallback<ProfilesFilterViewController.ProfileFilterViewReturn>(async r =>
                {
                    if (r.Applied)
                    {
                        ApplyFilter(r.Filter);

                        await BuildProfilesListBoxAsync(AppEnvironment.CurrentPfid);
                    }
                });
            }

            else
                GetApp().ProfilesFilterView.CloseView();
        }

        protected void EV_BtnAddProfile()
        {
            ProfilesEditApp app = GetApp().GetApplication<ProfilesEditApp>(UIXApplicationStartTarget.Window);

            app.CreateNew(async r =>
            {
                if (!r.Canceled)
                {
                    await BuildProfilesListBoxAsync(r.PFID);
                    GameRunningProcess grp = ((GameRunningProcess)AppEnvironment.StartedBackgroundProcesses[typeof(GameRunningProcess).FullName]);

                    T1PROFI t1profi = new TXPROFI().Read(r.PFID);
                    grp.AddT1profi(t1profi);
                    grp.AddExecutables(t1profi.PFID, FnExecutables.GetAllActiveExecutablesFromDBObj(t1profi));
                }

            });
        }

        /// <summary>
        /// Context menu is opening
        /// </summary>
        /// <param name="target"></param>
        protected void EV_ListBoxProfiles_CtxtOpening(FrameworkElement target)
        {
            ListBoxItem listBoxItem = target as ListBoxItem;

            if (listBoxItem == null)
                return;

            if (listBoxItem.DataContext is not ProfilesListBoxItem profi)
                return;

            BuildContextMenu(listBoxItem, profi.ItemObject as T1PROFI);
        }

        /// <summary>
        /// Show the game in the library of its launcher
        /// </summary>
        protected void EV_ctxtOpenInLauncher()
        {
            T1PROFI t1profi = GetSelectedProfile();

            FnLauncher.OpenInLauncher(t1profi);
        }

        protected void EV_ctxtEdit()
        {
            T1PROFI t1profi = GetSelectedProfile();

            ProfilesEditApp app = GetApp().GetApplication<ProfilesEditApp>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
            app.Edit(t1profi, async r =>
            {
                if (!r.Canceled)
                {
                    GameRunningProcess grp = ((GameRunningProcess)AppEnvironment.StartedBackgroundProcesses[typeof(GameRunningProcess).FullName]);

                    T1PROFI t1profi = new TXPROFI().Read(r.PFID);
                    grp.AddT1profi(t1profi);
                    grp.AddExecutables(t1profi.PFID, FnExecutables.GetAllActiveExecutablesFromDBObj(t1profi));

                    await BuildProfilesListBoxAsync(r.PFID);
                }
            });
        }

        protected async Task EV_ctxtDelete()
        {
            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
            CFMBOXResult result = cfmbox.Show(GetErrorMessage("GLO0002", "this profile"), CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

            if (result == CFMBOXResult.Yes)
            {
                T1PROFI selectedT1profi = GetSelectedProfile();

                string coverPath = Path.Combine(AppConfig.Storage.ProfileCoversDirectoryPath ?? string.Empty, selectedT1profi.PPFN);
                File.Delete(coverPath);

                TFPROFI.DeleteT1PROFIAndLinkedData(selectedT1profi);

                GameRunningProcess grp = ((GameRunningProcess)AppEnvironment.StartedBackgroundProcesses[typeof(GameRunningProcess).FullName!]);
                grp.RemoveT1profi(selectedT1profi);
                grp.RemoveExecutables(selectedT1profi.PFID);

                await BuildProfilesListBoxAsync(0);
            }
        }

        protected void EV_ctxtOpenGameFolder()
        {
            T1PROFI t1profi = GetSelectedProfile();

            if (FnString.IsNullEmptyOrWhitespace(t1profi.EXGF))
            {
                CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
                cfmbox.Show(GetErrorMessage("PRO0006"), CFMBOXResult.Ok, CFMBOXIcon.Info);
                return;
            }

            if (!Directory.Exists(t1profi.EXGF))
            {
                CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
                cfmbox.Show(GetErrorMessage("GLO0014", "game folder", t1profi.EXGF), CFMBOXResult.Ok, CFMBOXIcon.Info);
                return;
            }

            FnOpSys.OpenExplorer(t1profi.EXGF);
        }

        protected async Task EV_ctxtCompleteProfile()
        {
            T1PROFI t1profi = GetSelectedProfile();
            T1PLTHR t1plthr = TFPLTHR.GetCurrentPlaythrough(t1profi.PFID);

            TFPLTHR.UpdatePlaythroughStatus(GetApp(), ref t1plthr, TFPLTHR.PlaythroughStatus.Completed);

            await BuildProfilesListBoxAsync(t1profi.PFID);
        }

        protected async Task EV_ctxtStartNewPlaythrough()
        {
            T1PROFI t1profi = GetSelectedProfile();

            PlaythroughEditApp app = GetApp().GetApplication<PlaythroughEditApp>();
            app.CreateNew(async r =>
            {
                if (r.HasChanged)
                {
                    await BuildProfilesListBoxAsync(t1profi.PFID);
                }
            }, t1profi);
        }

        protected async Task EV_ctxtCancelPlaythrough()
        {
            T1PROFI t1profi = GetSelectedProfile();
            T1PLTHR t1plthr = TFPLTHR.GetCurrentPlaythrough(t1profi.PFID);

            TFPLTHR.UpdatePlaythroughStatus(GetApp(), ref t1plthr, TFPLTHR.PlaythroughStatus.Canceled);

            await BuildProfilesListBoxAsync(t1profi.PFID);
        }

        protected async Task EV_ctxtArchiveProfile()
        {
            T1PROFI t1profi = GetSelectedProfile();

            t1profi.ARCH = true;

            new TXPROFI().Save(t1profi);

            await BuildProfilesListBoxAsync(0);
        }

        protected async Task EV_ctxtUnarchiveProfile()
        {
            T1PROFI t1profi = GetSelectedProfile();

            t1profi.ARCH = false;

            new TXPROFI().Save(t1profi);

            await BuildProfilesListBoxAsync(0);
        }

        protected void EV_ctxtShowPlaythroughs()
        {
            T1PROFI t1profi = GetSelectedProfile();
            PlaythroughsApp app = GetApp().GetApplication<PlaythroughsApp>();
            app.AppRunSelections = new PlaythroughAppRunSelections
            {
                Pfid = t1profi.PFID,
                Gana = t1profi.GANA
            };

            app.ShowWindow(async r =>
            {
                if (r.HasChanged)
                {
                    await BuildProfilesListBoxAsync(t1profi.PFID);
                }
            });
        }


        #endregion

        private T1PROFI GetSelectedProfile()
        {
            return _profilesSubGridViewModel?.SelectedT1Profi?.ItemObject as T1PROFI
                ?? throw new InvalidOperationException("No profile is currently selected.");
        }

        /// <summary>
        /// Filter cache (stores the filter selections)
        /// The filter cache is also used for persisted selections (e.g. after restarting the application)
        /// Only unchecked entries are stored, so an empty filter shows the whole library
        /// and tags created later are visible by default.
        /// </summary>
        public class FilterCache
        {
            private List<long> _excludedTagIds = new List<long>();
            private List<string> _excludedOptions = new List<string>();

            /// <summary>
            /// GRIDs of unchecked tags
            /// </summary>
            public List<long> ExcludedTagIds { get => _excludedTagIds; set => _excludedTagIds = value ?? new List<long>(); }

            /// <summary>
            /// Hides games without any tag
            /// </summary>
            public bool ExcludeUntagged { get; set; } = false;

            /// <summary>
            /// Unchecked fixed options (see ProfilesFilterOptions)
            /// </summary>
            public List<string> ExcludedOptions { get => _excludedOptions; set => _excludedOptions = value ?? new List<string>(); }

            public bool ShowArchived { get; set; } = false;
            public bool OnlyRecentlyPlayed { get; set; } = false;

            public FilterCache() { }
        }
    }
}
