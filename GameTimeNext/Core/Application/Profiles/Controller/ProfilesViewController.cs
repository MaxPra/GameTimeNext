using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
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

        protected override Task BuildFirstAsync()
        {
            GetApp().Loader.Begin();

            return Task.Run(() =>
            {
                TBLM_PROFI tblm_profi = new TBLM_PROFI();
                List<TBL_PROFI> tbl_profis = tblm_profi.ReadAll();

                FillProfileCover(tbl_profis);

                Thread.Sleep(5000);

                View.Dispatcher.Invoke(() =>
                {
                    _profilesSubGridViewModel = new ProfilesSubGridViewModel();
                    _profilesSubGridViewModel.TblProfis = new System.Collections.ObjectModel.ObservableCollection<TBL_PROFI>(tbl_profis);

                    if (tbl_profis.Count > 0 && AppEnvironment.GetCurrentProfile() != null)
                    {
                        _profilesSubGridViewModel.SelectedTblProfi =
                            tbl_profis.FirstOrDefault(p => p.PFID == AppEnvironment.GetCurrentProfile()!.PFID)
                            ?? tbl_profis.FirstOrDefault()!;
                    }

                    dataWrapper!.TableObject = _profilesSubGridViewModel.SelectedTblProfi;

                    if (dataWrapper!.TableObject == null)
                        dataWrapper!.TableObject = new TBLM_PROFI().CreateNew();

                    View.DataContext = _profilesSubGridViewModel;

                    if (!FnString.IsNullEmptyOrWhitespace(dataWrapper!.TableObject.ACCO))
                        FnTheme.ApplyThemeColors(new CAccentColors(dataWrapper!.TableObject.ACCO).Dezerialize().AccentColors);

                    GetApp().Loader.Stop();

                }, DispatcherPriority.Normal);
            });
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
            TBL_PROFI selectedProfi = source.SelectedItem as TBL_PROFI;

            if (selectedProfi == null)
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
            List<TBL_GROUP> groups = new List<TBL_GROUP>();
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
            app.CreateNew(GetApp());
        }

        private void FillProfileCover(List<TBL_PROFI> tbl_profis)
        {
            foreach (TBL_PROFI prof in tbl_profis)
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
