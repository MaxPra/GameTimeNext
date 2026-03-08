using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.TableObjects;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    public class ProfilesViewController : UIXViewControllerBase
    {
        private ProfilesSubViewDataWrapper? dataWrapper;

        ProfilesSubGridViewModel? _profilesSubGridViewModel;

        public ProfilesViewController(UIXApplication app) : base(app)
        {
        }

        protected override void Init()
        {
            dataWrapper = GetDataWrapper<ProfilesSubViewDataWrapper>();

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

        /// <summary>
        /// Button Filter
        /// </summary>
        public void EV_BtnFilter()
        {
            List<T1GROUP> groups = new List<T1GROUP>();
            bool applied = false;

            if (!GetApp().ProfilesFilterView.IsShown)
            {
                GetApp().ProfilesFilterView.ShowView(false);

                // Geschlossen per Apply
                GetApp().ProfilesFilterView.ViewController.SetResultCallback<ProfilesFilterViewController.ProfileFilterViewReturn>(r =>
                {
                    groups = r.TblGroups;
                    applied = r.Applied;

                    if (applied)
                        MessageBox.Show("Applied!!!");
                    else
                        MessageBox.Show("Canceled :(");
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

        private async Task BuildProfilesListBoxAsync(long pfid)
        {
            GetApp().Loader.Begin();

            await Task.Run(() =>
            {
                TXPROFI TXPROFI = new TXPROFI();
                List<T1PROFI> T1PROFIs = TXPROFI.ReadAll();

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

                    dataWrapper!.TableObject = _profilesSubGridViewModel.SelectedT1Profi;

                    if (dataWrapper!.TableObject == null)
                        dataWrapper!.TableObject = new TXPROFI().CreateNew();

                    View.DataContext = _profilesSubGridViewModel;

                    if (!FnString.IsNullEmptyOrWhitespace(dataWrapper!.TableObject.ACCO))
                        FnTheme.ApplyThemeColors(new CAccentColors(dataWrapper!.TableObject.ACCO).Dezerialize().AccentColors);

                    GetApp().Loader.Stop();

                }, DispatcherPriority.Normal);
            });
        }

        private void FillProfileCover(List<T1PROFI> T1PROFIs)
        {
            foreach (T1PROFI prof in T1PROFIs)
            {
                prof.CoverImage = FnImage.LoadImageWithoutLock(prof.PPFN, 300, 450);
            }
        }

        private ProfilesApp GetApp()
        {
            return (ProfilesApp)App;
        }
    }
}
