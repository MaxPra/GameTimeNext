using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    public class ProfilesViewController : UIXViewControllerBase
    {
        private ProfilesSubViewDataWrapper? _dataWrapper;
        private ProfilesSubGridViewModel? _profilesSubGridViewModel;



        bool _playableFilter = false;

        public ProfilesViewController(UIXApplication app) : base(app)
        {
        }

        protected override void Init()
        {
            _dataWrapper = GetDataWrapper<ProfilesSubViewDataWrapper>();

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
            await BuildProfilesListBoxAsync(0);
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

            FnTheme.ApplyThemeColors(new CAccentColors(selectedProfi.ACCO).Dezerialize().AccentColors);
        }

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
                    }
                    else
                        GetApp().ProfilesDetailView.Visibility = Visibility.Visible;


                    if (!FnString.IsNullEmptyOrWhitespace(_dataWrapper!.TableObject.ACCO))
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
                    query.AddWhere(K1PROFI.Name, K1PROFI.Fields.COMP, QueryCompareType.EQUALS, true);

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

        private void FillProfileCover(List<T1PROFI> T1PROFIs)
        {
            foreach (T1PROFI prof in T1PROFIs)
            {
                prof.CoverImage = FnImage.LoadImageWithoutLock(prof.PPFN, 300, 450);
                prof.IsPlayable = FnSystem.IsExeFoundInPath(prof.EXGF);
            }
        }

        private ProfilesApp GetApp()
        {
            return (ProfilesApp)App;
        }

        private ProfilesView GetView()
        {
            return (ProfilesView)View;
        }


        protected async Task EV_tbSearchProfileName()
        {
            await BuildProfilesListBoxAsync(0);
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

                        await BuildProfilesListBoxAsync(0);
                    }
                });
            }

            else
                GetApp().ProfilesFilterView.CloseView();
        }

        protected void EV_BtnAddProfile()
        {
            ProfilesEditApp app = new ProfilesEditApp();
            app.CreateNew(async r =>
            {
                if (!r.Canceled)
                    await BuildProfilesListBoxAsync(r.PFID);
            });
        }

        protected void EV_ctxtEdit()
        {
            T1PROFI t1profi = _profilesSubGridViewModel!.SelectedT1Profi;

            ProfilesEditApp app = new ProfilesEditApp();
            app.Edit(t1profi, async r =>
            {
                if (!r.Canceled)
                    await BuildProfilesListBoxAsync(r.PFID);
            });
        }

        protected async Task EV_ctxtDelete()
        {
            CFMBOX cfmbox = new CFMBOX();
            CFMBOXResult result = cfmbox.Show("Question", "Are you sure you want to delete this profile?\nAll associated data will also be deleted.", CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

            if (result == CFMBOXResult.Yes)
            {
                TFPROFI.DeleteProfiAndLinkedData(_profilesSubGridViewModel!.SelectedT1Profi);

                await BuildProfilesListBoxAsync(0);
            }
        }

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
