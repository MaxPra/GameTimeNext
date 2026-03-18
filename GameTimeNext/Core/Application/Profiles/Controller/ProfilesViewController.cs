using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.Profiles.BackgroundProcesses;
using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Application.TimeMonitoring;
using GameTimeNext.Core.Framework;
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
        private bool _playableFilter = false;

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
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirst()
        {
        }

        protected override async Task BuildFirstAsync()
        {
            await BuildProfilesListBoxAsync(AppEnvironment.CurrentPfid);
        }

        protected override void Build()
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
            T1PROFI selectedProfi = source.SelectedItem as T1PROFI;

            if (selectedProfi == null)
                return;

            if (!selectedProfi.ACAC)
                return;

            if (FnString.IsNullEmptyOrWhitespace(selectedProfi.ACCO))
                return;

            if (!AppEnvironment.GetAppConfig().AppSettings.AllowProfileSpecificStyleChanges)
                return;

            FnTheme.ApplyThemeColors(new CAccentColors(selectedProfi.ACCO).Dezerialize().AccentColors);

            AppEnvironment.CurrentPfid = selectedProfi.PFID;

            GetView().ListBoxProfiles.ScrollIntoView(selectedProfi);
        }
        #endregion

        #region Profiles List View
        private async Task BuildProfilesListBoxAsync(long pfid)
        {
            GetApp().Loader.Begin();

            await Task.Run(() =>
            {
                TXPROFI TXPROFI = new TXPROFI();
                List<T1PROFI> T1PROFIs = GetAllFilteredProfis();

                FillProfileCover(T1PROFIs);

                //Thread.Sleep(3000);

                View.Dispatcher.Invoke(() =>
                {
                    _profilesSubGridViewModel = new ProfilesSubGridViewModel();
                    _profilesSubGridViewModel.T1Profis = new System.Collections.ObjectModel.ObservableCollection<T1PROFI>(T1PROFIs);

                    if (T1PROFIs.Count > 0)
                    {
                        _profilesSubGridViewModel.SelectedT1Profi =
                            T1PROFIs.FirstOrDefault(p => p.PFID == pfid)
                            ?? T1PROFIs.FirstOrDefault()!;
                    }


                    _dataWrapper!.TableObject = _profilesSubGridViewModel.SelectedT1Profi;

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
                        AppEnvironment.CurrentPfid = _profilesSubGridViewModel.SelectedT1Profi.PFID;
                    }


                    if (!FnString.IsNullEmptyOrWhitespace(_dataWrapper!.TableObject.ACCO) && AppEnvironment.GetAppConfig().AppSettings.AllowProfileSpecificStyleChanges)
                        FnTheme.ApplyThemeColors(new CAccentColors(_dataWrapper!.TableObject.ACCO).Dezerialize().AccentColors);

                    GetApp().Loader.Stop();

                }, DispatcherPriority.Normal);
            });
        }

        private List<T1PROFI> GetAllFilteredProfis()
        {
            List<T1PROFI> t1profisFiltered = new List<T1PROFI>();

            UIXQuery query = BuildProfilesQuery();

            string sql = query.PreviewQuery();

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    TXPROFI txprofi = new TXPROFI();

                    long pfid = UIXQuery.GetInt64(reader, K1PROFI.Fields.PFID);

                    T1PROFI t1profi = txprofi.Read(UIXQuery.GetInt64(reader, K1PROFI.Name, K1PROFI.Fields.PFID));

                    // Hier wird nach "playable" gefiltert
                    if ((_playableFilter && FnSystem.IsExeFoundInPath(t1profi.EXGF)) || !_playableFilter)
                        t1profisFiltered.Add(t1profi);
                }
            }

            return t1profisFiltered;
        }

        private UIXQuery BuildProfilesQuery()
        {
            UIXQuery query = new UIXQuery(K1PROFI.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.SetDistinct(true);

            // Felder
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PFID);

            // Where Restrictions
            AddWhereRestrictions(query);

            // Order by LAPL
            query.AddOrderBy(K1PROFI.Name, K1PROFI.Fields.LAPL, OrderDirection.DESC);
            return query;
        }

        private void AddWhereRestrictions(UIXQuery query)
        {

            ProfilesFilterViewController filterController = GetApp().ProfilesFilterView.ViewController as ProfilesFilterViewController;

            // Where Restriction
            List<T1GROUP> selectedTags = new List<T1GROUP>();
            List<T1GROUP> selectedStates = new List<T1GROUP>();

            //if (filterController.ProfilesFilterViewModel != null)
            //{
            //    selectedTags = filterController.ProfilesFilterViewModel.T1GROUPs.Where(t => t.IsSelected == true).ToList();
            //    selectedStates = filterController.ProfilesFilterViewModel.States.Where(s => s.IsSelected == true).ToList();
            //}

            // Selektion aus Filter Cache lesen
            selectedTags = GetApp().FilterCache.SelectedTags;
            selectedStates = GetApp().FilterCache.SelectedStates;

            // -- Tags
            if (selectedTags != null && selectedTags.Count > 0)
            {
                List<long> selectedTagKeys = BuildTagListForQuery(selectedTags);

                UIXQueryTable t1grppo_table = query.AddJoinTable(K1GRPPO.Name, JoinType.INNER);
                t1grppo_table.AddJoinCondition(K1PROFI.Name, K1PROFI.Fields.PFID, QueryCompareType.EQUALS, K1GRPPO.Name, K1GRPPO.Fields.PFID);

                // Alle Gruppen aus der GRPPO wo einer der selektierten Keys enthalten ist
                query.AddWhereIn(K1GRPPO.Name, K1GRPPO.Fields.GRID, selectedTagKeys);
            }

            // -- States
            if (selectedStates != null && selectedStates.Count > 0)
            {

                bool containsCompleted = false;
                bool containsUnplayed = false;
                bool containsCurrentlyPlaying = false;
                bool containsPlayable = false;

                foreach (T1GROUP group in selectedStates)
                {
                    if (group.GRNA == "Completed")
                        containsCompleted = true;
                    else if (group.GRNA == "Unplayed")
                        containsUnplayed = true;
                    else if (group.GRNA == "Currently Playing")
                        containsCurrentlyPlaying = true;
                    else if (group.GRNA == "Playable")
                        containsPlayable = true;
                }

                if (containsUnplayed)
                {
                    query.AddWhere(K1PROFI.Name, K1PROFI.Fields.FIPL, QueryCompareType.EQUALS, DateTime.MinValue);
                }

                if (containsCurrentlyPlaying)
                {
                    DateTime limitDate = DateTime.Now.AddDays(-14);
                    query.AddWhere(K1PROFI.Name, K1PROFI.Fields.LAPL, QueryCompareType.GREATER_THAN, limitDate);
                }

                if (containsCompleted)
                {
                    // query.AddWhere(K1PROFI.Name, K1PROFI.Fields.COMP, QueryCompareType.EQUALS, true);
                    UIXQueryTable t1plthr_table = query.AddJoinTable(K1PLTHR.Name, JoinType.INNER);
                    t1plthr_table.AddJoinCondition(K1PROFI.Name, K1PROFI.Fields.PFID, QueryCompareType.EQUALS, K1PLTHR.Name, K1PLTHR.Fields.PFID);
                    query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTCO, QueryCompareType.EQUALS, true);
                }


                // Hier im Objekt speichern, da außerhalb der Methode darauf zugegriffen werden muss (ausnahme)
                _playableFilter = containsPlayable;
            }
            else
                _playableFilter = false;

            GetView().Dispatcher.Invoke(() =>
            {
                // -- Profile name
                if (!FnString.IsNullEmptyOrWhitespace(GetView().tbSearchProfileName.Text))
                {
                    query.AddWhere(K1PROFI.Name, K1PROFI.Fields.GANA, QueryCompareType.LIKE, GetView().tbSearchProfileName.Text);
                }
            });
        }

        private List<long> BuildTagListForQuery(List<T1GROUP> selectedTags)
        {

            List<long> keyList = new List<long>();

            foreach (T1GROUP tag in selectedTags)
            {
                keyList.Add(tag.GRID);
            }

            return keyList;
        }

        private void BuildContextMenu(ListBoxItem lbi, T1PROFI t1profi)
        {
            Style contextMenuItemStyle = (Style)System.Windows.Application.Current.FindResource("ModernContextMenuItemStyle");
            Style contextMenuStyle = (Style)System.Windows.Application.Current.FindResource("ModernContextMenuStyle");

            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("ProfilesListBoxContextMenu");
            contextBuilder.SetStyle(contextMenuStyle);

            // Prinzipiell immer Edit und Delete
            contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon("\uE70F"), itemStyle: contextMenuItemStyle);
            contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon("\uE74D"), itemStyle: contextMenuItemStyle);

            // Wenn Profil noch nicht als durchgespielt markiert
            T1PLTHR currentPlaythrough = TFPLTHR.GetCurrentPlaythrough(t1profi.PFID);

            if (currentPlaythrough != null && !TFPLTHR.GetCurrentPlaythrough(t1profi.PFID).PTCO)
            {
                contextBuilder.AddItem("ctxtCompleteProfile", "Current playthrough completed", icon: UIXContextMenuFactory.CreateMdlIcon("\uE930"), itemStyle: contextMenuItemStyle);
            }
            else
            {
                contextBuilder.AddItem("ctxtStartNewPlaythrough", "Start new playthrough", icon: UIXContextMenuFactory.CreateMdlIcon("\uE72C"), itemStyle: contextMenuItemStyle);
            }

            if (contextBuilder.HasItems())
                lbi.ContextMenu = contextBuilder.Build();
            else
                lbi.ContextMenu = null;
        }
        #endregion

        private void FillProfileCover(List<T1PROFI> T1PROFIs)
        {
            foreach (T1PROFI prof in T1PROFIs)
            {
                prof.CoverImage = FnImage.LoadImageWithoutLock(prof.PPFN, 300, 450);
                prof.IsPlayable = FnSystem.IsExeFoundInPath(prof.EXGF);
            }
        }

        private async Task AdjustFiltersToStartedGameProfile(long pfid)
        {
            // Ermittlung der zugeordneten Gruppen
            T1PROFI t1profi = new TXPROFI().Read(pfid);

            List<T1GROUP> tags = TFPROFI.GetAllLinkedTags(t1profi);

            // Alle Selektionen bereinigen
            if (tags.Count == 0)
                GetApp().FilterCache.SelectedTags = new List<T1GROUP>();
            else
                GetApp().FilterCache.SelectedTags = tags;

            // Alle States bereinigen
            GetApp().FilterCache.SelectedStates = new List<T1GROUP>();

            foreach (var tag in tags)
            {
                tag.IsSelected = true;
            }

            await BuildProfilesListBoxAsync(pfid);
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

            ProfilesDetailSubViewController profileDetailViewController = (ProfilesDetailSubViewController)GetApp().ProfilesDetailView.ViewController;
            profileDetailViewController.UpdateUIMonitoringStarted();
        }

        protected async Task EXEV_GameTimeMonitoringStopped()
        {
            CFGameTimeMonitoring.UpdateTableObject();

            GetView().MonitoringOverlay.Visibility = Visibility.Hidden;

            await BuildProfilesListBoxAsync(AppEnvironment.CurrentPfid);

            ProfilesDetailSubViewController profileDetailViewController = (ProfilesDetailSubViewController)GetApp().ProfilesDetailView.ViewController;
            profileDetailViewController.UpdateUIMonitoringStopped();
        }

        protected async Task EXEV_SwitchProfile()
        {
            if (_profilesSubGridViewModel == null)
                return;

            List<T1PROFI> currentProfis = _profilesSubGridViewModel.T1Profis.Where(p => p.PFID == AppEnvironment.CurrentPfid).ToList();

            if (currentProfis == null || currentProfis.Count == 0)
                await AdjustFiltersToStartedGameProfile(AppEnvironment.CurrentPfid);

            _profilesSubGridViewModel.SelectedT1Profi = _profilesSubGridViewModel.T1Profis.Where(p => p.PFID == AppEnvironment.CurrentPfid).ToList()[0];

            ToastMessage tm = new ToastMessage("Switched profile...", _profilesSubGridViewModel!.SelectedT1Profi.GANA);
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
            List<T1GROUP> tags = new List<T1GROUP>();
            List<T1GROUP> states = new List<T1GROUP>();
            bool applied = false;

            if (!GetApp().ProfilesFilterView.IsShown)
            {
                GetApp().ProfilesFilterView.ShowView(true);

                // Geschlossen per Apply
                GetApp().ProfilesFilterView.ViewController.SetResultCallback<ProfilesFilterViewController.ProfileFilterViewReturn>(async r =>
                {
                    tags = r.TblGroups;
                    states = r.States;
                    applied = r.Applied;

                    if (applied)
                    {
                        // Filter Cache aktualisieren
                        GetApp().FilterCache.SelectedTags = tags;
                        GetApp().FilterCache.SelectedStates = states;

                        AppEnvironment.GetAppConfig().FilterCache = GetApp().FilterCache;
                        AppEnvironment.SaveAppConfig();

                        await BuildProfilesListBoxAsync(AppEnvironment.CurrentPfid);
                    }
                });
            }

            else
                GetApp().ProfilesFilterView.CloseView();
        }

        protected void EV_BtnAddProfile()
        {
            ProfilesEditApp app = GetApp().GetApplication<ProfilesEditApp>();
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

            if (listBoxItem.DataContext is not T1PROFI profi)
                return;

            BuildContextMenu(listBoxItem, profi);
        }

        protected void EV_ctxtEdit()
        {
            T1PROFI t1profi = _profilesSubGridViewModel!.SelectedT1Profi;

            ProfilesEditApp app = GetApp().GetApplication<ProfilesEditApp>();
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
            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>();
            CFMBOXResult result = cfmbox.Show("Question", "Are you sure you want to delete this profile?\nAll associated data will also be deleted.", CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

            if (result == CFMBOXResult.Yes)
            {
                T1PROFI selectedT1profi = _profilesSubGridViewModel!.SelectedT1Profi;

                File.Delete(selectedT1profi.PPFN);

                TFPROFI.DeleteT1PROFIAndLinkedData(selectedT1profi);

                GameRunningProcess grp = ((GameRunningProcess)AppEnvironment.StartedBackgroundProcesses[typeof(GameRunningProcess).FullName!]);
                grp.RemoveT1profi(selectedT1profi);
                grp.RemoveExecutables(selectedT1profi.PFID);

                await BuildProfilesListBoxAsync(0);
            }
        }

        protected void EV_ctxtCompleteProfile()
        {
            T1PROFI t1profi = _profilesSubGridViewModel!.SelectedT1Profi;

            T1PLTHR t1plthr = TFPLTHR.GetCurrentPlaythrough(t1profi.PFID);

            // Playthrough als Abgeschlossen markieren
            t1plthr.PTCO = true;

            new TXPLTHR().Save(t1plthr);

            _dataWrapper.TargetController.Open(true);
        }

        protected void EV_ctxtStartNewPlaythrough()
        {
            T1PROFI t1profi = _profilesSubGridViewModel!.SelectedT1Profi;

            ProfilesPlaythroughEditApp app = GetApp().GetApplication<ProfilesPlaythroughEditApp>();
            app.CreateNew(t1profi, r =>
            {
                if (!r.Canceled)
                {
                    // Nochmaliges öffnen triggern
                    _dataWrapper!.TargetController.Open(true);
                }
            });
        }

        #endregion

        /// <summary>
        /// Filter Cache (Hier werden die Selektionen des Filters gespeichert)
        /// Bzw. wird der Filter Cache auch für gespeicherte Selektionen (z.b. bei Neustart der Anwendung) genutzt
        /// </summary>
        public class FilterCache
        {
            public List<T1GROUP> SelectedTags { get; set; }
            public List<T1GROUP> SelectedStates { get; set; }

            public FilterCache() { }
        }
    }
}
