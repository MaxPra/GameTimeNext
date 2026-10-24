using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.Playthroughs;
using GameTimeNext.Core.Application.Profiles.BackgroundProcesses;
using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using GameTimeNext.Core.Application.Profiles.Types;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Application.TimeMonitoring;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.LauncherIntegration;
using GameTimeNext.Core.Framework.Logging;
using GameTimeNext.Core.Framework.UI;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.Utils;
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

            // Loadertexte setzen
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

        protected override void Check()
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

            GetView().ListBoxProfiles.ScrollIntoView(selectedProfi);

            if (!selectedTableObject.ACAC)
                return;

            if (FnString.IsNullEmptyOrWhitespace(selectedTableObject.ACCO))
                return;

            if (!AppEnvironment.GetAppConfig().AppSettings.AllowProfileSpecificStyleChanges)
                return;

            FnTheme.ApplyThemeColors(new CAccentColors(selectedTableObject.ACCO).Dezerialize().AccentColors);
        }
        #endregion

        #region Profiles List View
        private async Task BuildProfilesListBoxAsync(long pfid)
        {
            GetApp().Loader.Begin();

            await Task.Run(() =>
            {
                (List<T1PROFI> T1PROFIs, bool filterActive) = GetAllFilteredProfis();
                List<ProfilesListBoxItem> profileItems = BuildProfileListBoxItems(T1PROFIs);

                //Thread.Sleep(3000);

                View.Dispatcher.Invoke(() =>
                {
                    // Indicator on the filter button if the list is filtered
                    GetView().ElFilterActive.Visibility = filterActive ? Visibility.Visible : Visibility.Collapsed;

                    _profilesSubGridViewModel = new ProfilesSubGridViewModel();
                    _profilesSubGridViewModel.T1Profis = new System.Collections.ObjectModel.ObservableCollection<ProfilesListBoxItem>(profileItems);

                    if (profileItems.Count > 0)
                    {
                        _profilesSubGridViewModel.SelectedT1Profi =
                            profileItems.FirstOrDefault(p => (p.ItemObject as T1PROFI)?.PFID == pfid)
                            ?? profileItems.FirstOrDefault()!;
                    }


                    _dataWrapper!.TableObject = _profilesSubGridViewModel.SelectedT1Profi?.ItemObject as T1PROFI;

                    View.DataContext = _profilesSubGridViewModel;

                    if (_dataWrapper!.TableObject == null)
                    {
                        _dataWrapper!.TableObject = new TXPROFI().CreateNew();
                        GetApp().ProfilesDetailView.Visibility = Visibility.Hidden;
                        AppEnvironment.CurrentPfid = 0;
                    }
                    else
                    {
                        GetApp().ProfilesDetailView.Visibility = Visibility.Visible;
                        AppEnvironment.CurrentPfid = (_profilesSubGridViewModel.SelectedT1Profi?.ItemObject as T1PROFI)?.PFID ?? 0;
                    }


                    if (!FnString.IsNullEmptyOrWhitespace(_dataWrapper!.TableObject.ACCO) && AppEnvironment.GetAppConfig().AppSettings.AllowProfileSpecificStyleChanges)
                        FnTheme.ApplyThemeColors(new CAccentColors(_dataWrapper!.TableObject.ACCO).Dezerialize().AccentColors);

                    GetApp().Loader.Stop();

                }, DispatcherPriority.Normal);
            });
        }

        /// <summary>
        /// Reads all profiles and applies the library filter.
        /// Tags and fixed options are OR-combined within their group and AND-combined between groups.
        /// </summary>
        private (List<T1PROFI> Profiles, bool FilterActive) GetAllFilteredProfis()
        {
            List<T1PROFI> t1profisFiltered = new List<T1PROFI>();
            FilterCache filter = GetApp().FilterCache;

            // Only consider tags that still exist (deleted tags may still be stored in the filter)
            HashSet<long> existingTagIds = new TXGROUP().ReadAll()
                .Where(g => g.GTYP == GroupType.Tag)
                .Select(g => g.GRID)
                .ToHashSet();

            HashSet<long> excludedTagIds = filter.ExcludedTagIds.Where(existingTagIds.Contains).ToHashSet();
            HashSet<string> excludedOptions = filter.ExcludedOptions.ToHashSet();

            Dictionary<long, HashSet<long>> tagIdsByPfid = new TXGRPPO().ReadAll()
                .Where(g => existingTagIds.Contains(g.GRID))
                .GroupBy(g => g.PFID)
                .ToDictionary(g => g.Key, g => g.Select(x => x.GRID).ToHashSet());

            HashSet<long> completedPfids = TFPLTHR.GetPfidsWithCompletedPlaythrough();
            HashSet<long> currentPlaythroughPfids = TFPLTHR.GetPfidsWithCurrentPlaythrough();
            HashSet<long> pausedPlaythroughPfids = TFPLTHR.GetPfidsWithPausedPlaythrough();

            UIXQuery query = BuildProfilesQuery(filter);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    TXPROFI txprofi = new TXPROFI();
                    long pfid = UIXQuery.GetInt64(reader, K1PROFI.Name, K1PROFI.Fields.PFID);
                    T1PROFI t1profi = txprofi.Read(pfid);

                    tagIdsByPfid.TryGetValue(pfid, out HashSet<long>? tagIds);

                    string progress = GetProgress(t1profi, completedPfids.Contains(pfid), currentPlaythroughPfids.Contains(pfid), pausedPlaythroughPfids.Contains(pfid));

                    if (IsProfileVisible(t1profi, tagIds, progress, filter, excludedTagIds, excludedOptions))
                        t1profisFiltered.Add(t1profi);
                }
            }

            bool filterActive = excludedTagIds.Count > 0
                || filter.ExcludeUntagged
                || excludedOptions.Count > 0
                || filter.ShowArchived
                || filter.OnlyRecentlyPlayed;

            return (t1profisFiltered, filterActive);
        }

        /// <summary>
        /// Determines the progress option of a profile (exactly one per profile)
        /// </summary>
        private static string GetProgress(T1PROFI t1profi, bool hasCompletedPlaythrough, bool hasCurrentPlaythrough, bool hasPausedPlaythrough)
        {
            // Never played (the initial playthrough is open from the start, so this has to be checked first)
            if (t1profi.FIPL == DateTime.MinValue && !hasCompletedPlaythrough)
                return ProfilesFilterOptions.PROGRESS_UNPLAYED;

            // An open playthrough wins over completed ones (e.g. a second run)
            if (hasCurrentPlaythrough)
                return ProfilesFilterOptions.PROGRESS_IN_PROGRESS;

            // A paused playthrough can be resumed, so it wins over completed ones as well
            if (hasPausedPlaythrough)
                return ProfilesFilterOptions.PROGRESS_PAUSED;

            if (hasCompletedPlaythrough)
                return ProfilesFilterOptions.PROGRESS_COMPLETED;

            // Played, but only cancelled playthroughs left
            return ProfilesFilterOptions.PROGRESS_CANCELLED;
        }

        private static bool IsProfileVisible(T1PROFI t1profi, HashSet<long>? tagIds, string progress, FilterCache filter, HashSet<long> excludedTagIds, HashSet<string> excludedOptions)
        {
            // -- Tags (visible if at least one of its tags is checked)
            if (tagIds == null || tagIds.Count == 0)
            {
                if (filter.ExcludeUntagged)
                    return false;
            }
            else if (tagIds.All(excludedTagIds.Contains))
                return false;

            // -- Progress
            if (excludedOptions.Contains(progress))
                return false;

            // -- Availability (file system check only when needed)
            if (excludedOptions.Contains(ProfilesFilterOptions.AVAILABILITY_PLAYABLE) || excludedOptions.Contains(ProfilesFilterOptions.AVAILABILITY_NOT_PLAYABLE))
            {
                string availability = FnSystem.IsExeFoundInPath(t1profi.EXGF)
                    ? ProfilesFilterOptions.AVAILABILITY_PLAYABLE
                    : ProfilesFilterOptions.AVAILABILITY_NOT_PLAYABLE;

                if (excludedOptions.Contains(availability))
                    return false;
            }

            // -- Source (codetable lookup only when needed)
            if (excludedOptions.Contains(ProfilesFilterOptions.SOURCE_LOCAL) || excludedOptions.Contains(ProfilesFilterOptions.SOURCE_EXTERNAL))
            {
                string source = TFPROFI.IsExternalGame(t1profi)
                    ? ProfilesFilterOptions.SOURCE_EXTERNAL
                    : ProfilesFilterOptions.SOURCE_LOCAL;

                if (excludedOptions.Contains(source))
                    return false;
            }

            return true;
        }

        private UIXQuery BuildProfilesQuery(FilterCache filter)
        {
            UIXQuery query = new UIXQuery(K1PROFI.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.SetDistinct(true);

            // Felder
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PFID);

            // Where Restrictions
            AddWhereRestrictions(query, filter);

            // Order by LAPL
            query.AddOrderBy(K1PROFI.Name, K1PROFI.Fields.LAPL, OrderDirection.DESC);
            return query;
        }

        private void AddWhereRestrictions(UIXQuery query, FilterCache filter)
        {
            // -- Archived games are hidden unless explicitly requested
            if (!filter.ShowArchived)
                query.AddWhere(K1PROFI.Name, K1PROFI.Fields.ARCH, QueryCompareType.EQUALS, false);

            // -- Recently played
            if (filter.OnlyRecentlyPlayed)
            {
                DateTime limitDate = DateTime.Now.AddDays(-ProfilesFilterOptions.RECENTLY_PLAYED_DAYS);
                query.AddWhere(K1PROFI.Name, K1PROFI.Fields.LAPL, QueryCompareType.GREATER_THAN, limitDate);
            }

            GetView().Dispatcher.Invoke(() =>
            {
                // -- Profile name
                if (!FnString.IsNullEmptyOrWhitespace(GetView().tbSearchProfileName.Text))
                {
                    query.AddWhere(K1PROFI.Name, K1PROFI.Fields.GANA, QueryCompareType.LIKE, GetView().tbSearchProfileName.Text);
                }
            });
        }

        private void BuildContextMenu(ListBoxItem lbi, T1PROFI t1profi)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("ProfilesListBoxContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            // Prinzipiell immer Edit und Delete
            contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon("\uE70F"), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon("\uE74D"), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            // Game Folder
            ProfilesContextMenuBuilder.BuildContextShowGameFolder(contextBuilder, t1profi);

            // Playthrough
            ProfilesContextMenuBuilder.BuildContextPlayhtrougths(contextBuilder, t1profi);

            // Steam
            ProfilesContextMenuBuilder.BuildContextSteam(contextBuilder, t1profi);

            // Archivieren
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
            // Ermittlung der zugeordneten Gruppen
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
        /// Button Filter
        /// </summary>
        public void EV_BtnFilter()
        {
            if (!GetApp().ProfilesFilterView.IsShown)
            {
                GetApp().ProfilesFilterView.ShowView(true);

                // Geschlossen per Apply
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
        /// Kontextmenü öffnet sich
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
        /// Steam Bibliothek öffnen
        /// </summary>
        protected void EV_ctxtOpenSteamLibrary()
        {
            T1PROFI t1profi = GetSelectedProfile();

            FnSteam.OpenSteamLibrary(t1profi.SAID);
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
            CFMBOXResult result = cfmbox.Show("Question", "Are you sure you want to delete this profile?\nAll associated data will also be deleted.", CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

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
                cfmbox.Show("Information", "No game folder set for this profile.", CFMBOXResult.Ok, CFMBOXIcon.Info);
                return;
            }

            if (!Directory.Exists(t1profi.EXGF))
            {
                CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
                cfmbox.Show("Information", "The specified game folder does not exist.", CFMBOXResult.Ok, CFMBOXIcon.Info);
                return;
            }

            FnOpSys.OpenExplorer(t1profi.EXGF);
        }

        protected async Task EV_ctxtCompleteProfile()
        {
            T1PROFI t1profi = GetSelectedProfile();

            T1PLTHR t1plthr = TFPLTHR.GetCurrentPlaythrough(t1profi.PFID);

            // Playthrough als Abgeschlossen markieren
            t1plthr.PTCO = true;

            new TXPLTHR().Save(t1plthr);

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

            string text = "Do you really want to cancel your current playthrough?\nYou won't be able to undo this action!\n\nInformation: The gametime of this playthrough will be added to your overall gametime!";
            CFMBOXResult result = GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show("Question", text, CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

            if (result == CFMBOXResult.No)
                return;

            // Playthrough canceln
            t1plthr.PTCA = true;

            new TXPLTHR().Save(t1plthr);

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
        /// Filter Cache (Hier werden die Selektionen des Filters gespeichert)
        /// Bzw. wird der Filter Cache auch für gespeicherte Selektionen (z.b. bei Neustart der Anwendung) genutzt
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
