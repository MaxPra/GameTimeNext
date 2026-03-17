using GameTimeNext.Core.Application.General.UserSettings;
using GameTimeNext.Core.Application.General.ViewModels;
using GameTimeNext.Core.Application.GTXMigration;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;
using UIX.ViewController.Engine.Runnables;
using AppEnvironment = GameTimeNext.Core.Framework.AppEnvironment;

namespace GameTimeNext.Core.Application.General.Controller
{
    internal class MainWindowController : UIXWindowControllerBase
    {
        private string _gtxPath = "C:\\GameTimeX";

        private MainWindowViewModel? _mainWindowViewModel = null;

        public MainWindowController(UIXApplication app) : base(app)
        {
        }

        protected override void Init()
        {

        }

        protected override void BuildFirst()
        {

        }

        protected override async Task BuildFirstAsync()
        {
            // Für Applauncher Tab Control setzen
            AppEnvironment.AppLauncher.TabControl = GetWindow().MainTabControl;

            // DEV-Batch & Metadata-Tab
            if (!Debugger.IsAttached)
            {
                GetWindow().BdDevModeBatch.Visibility = Visibility.Hidden;
            }

            BuildBetaTag();

            await CheckGTXMigration();

            await BuildApplicationSearch();

            AppEnvironment.AppLauncher.StartFavorites(GetApp());

            // Hintergrundprozesse starten
            AppEnvironment.StartBackgroundProcesses(GetApp());
        }

        protected override void Build()
        {

        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
            switch (eventName)
            {
                case UIXEventNames.Selector.SelectionChanged:

                    if (source is TabControl)
                        Event_TabChanged((TabControl)source);
                    break;
            }
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

        protected override void DataWrapperSelectionChangedImpl(Selector source) { }

        protected override void Event_Closing()
        {
        }

        protected override void Event_Minimize()
        {
        }

        protected override void Event_Maximize()
        {
        }

        /// <summary>
        /// Wird Tab geändert
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_TabChanged(TabControl source)
        {
            if (source.SelectedItem is TabItem ti && ti.Name == "Tab_Profiles")
            {

                //T1GROUP t1group = new TXGROUP().CreateNew();
                //t1group.GRID = 15;
                //t1group.IsSelected = true;

                //GetApp().ProfilesApp.FilterCache.SelectedTags.Clear();
                //GetApp().ProfilesApp.FilterCache.SelectedTags.Add(t1group);

                GetApp().ProfilesApp.ProfilesView.ViewController.Show(true);
            }

        }

        private async Task CheckGTXMigration()
        {
            string[] gtnFiles = Directory.GetFiles(AppEnvironment.GetAppConfig().CoverFolderPath);
            string loaderTextStart = "Migrating GTX -> GTN ...";

            if (Directory.Exists(_gtxPath) && gtnFiles.Length == 0)
            {
                CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>();
                string msg = "A GameTimeX-Installation was found.\nDo you want to migrate your profiles to GameTimeNXT?";
                CFMBOXResult result = cfmbox.Show("Start GameTimeX migration?", msg, CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

                if (result == CFMBOXResult.Yes)
                {
                    GetApp().Loader.Begin(loaderTextStart);

                    await Task.Run(() =>
                    {
                        try
                        {
                            GTXMigrationService gtxMigHelper = new GTXMigrationService("C:\\GameTimeX", GetApp().Loader);
                            gtxMigHelper.MigrateToGTNXT();
                        }
                        catch (Exception ex)
                        {
                        }
                        finally
                        {
                            GetApp().Loader.Stop();
                        }
                    });
                }
            }
        }

        private async Task BuildApplicationSearch()
        {
            _mainWindowViewModel = new MainWindowViewModel();
            _mainWindowViewModel.AvailableApplications =
                new System.Collections.ObjectModel.ObservableCollection<SearchableApplication>(AppEnvironment.AvailableApplications);

            _mainWindowViewModel.PropertyChanged += MainWindowViewModel_PropertyChanged;

            GetWindow().DataContext = _mainWindowViewModel;
        }

        private void MainWindowViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.SelectedApplication))
            {
                SearchableApplication selected = _mainWindowViewModel?.SelectedApplication;

                if (selected == null)
                    return;

                if (!AppEnvironment.StartedApplications.ContainsKey(selected.ClassName))
                    AppEnvironment.AppLauncher.LaunchApplication(selected.ClassName, GetApp(), selected.Name);

                _mainWindowViewModel.SelectedApplication = null;
            }
        }

        private void BuildBetaTag()
        {
            if (AppEnvironment.AppVersion.IsBeta)
                GetWindow().Subtitle = "BETA";
            else
                GetWindow().Subtitle = string.Empty;
        }

        protected void EV_ctxtClose(FrameworkElement target)
        {
            TabItem tab = (TabItem)target;

            if (tab != null)
            {
                AppEnvironment.AppLauncher.CloseApplication(tab.Tag.ToString(), tab);
            }
        }

        protected void EV_ctxtFav(FrameworkElement target)
        {
            TabItem tab = (TabItem)target;

            if (tab != null)
            {
                FavoriteApplication favoriteApplication = new FavoriteApplication();
                favoriteApplication.FullName = tab.Tag.ToString();
                favoriteApplication.AppName = tab.Header.ToString();
                favoriteApplication.PrimaryStart = false;

                FnUserSettings.AddFavoriteApplication(AppEnvironment.GetAppConfig().UserSettings.FavApps, favoriteApplication);
            }
        }

        protected void EV_ctxtDeleteFav(FrameworkElement target)
        {
            TabItem tab = (TabItem)target;

            if (tab == null)
                return;

            FavoriteApplication favApp = FnUserSettings.GetFavoriteApplication(AppEnvironment.GetAppConfig().UserSettings.FavApps, tab.Tag.ToString());

            FnUserSettings.RemoveFavoriteApplication(AppEnvironment.GetAppConfig().UserSettings.FavApps, favApp);

        }

        protected void EV_ctxtSetAsPrimaryStart(FrameworkElement target)
        {
            TabItem tab = (TabItem)target;

            if (tab == null)
                return;

            FavoriteApplication favApp = FnUserSettings.GetFavoriteApplication(AppEnvironment.GetAppConfig().UserSettings.FavApps, tab.Tag.ToString());

            FnUserSettings.SetAsPrimaryStart(favApp);
        }

        protected void EV_tabApplication_CtxtOpening(FrameworkElement target)
        {
            if (target is not TabItem tab)
                return;

            FavoriteApplication favApp = FnUserSettings.GetFavoriteApplication(
                AppEnvironment.GetAppConfig().UserSettings.FavApps,
                tab.Tag?.ToString());

            AppEnvironment.AppLauncher.BuildContextMenu(tab, favApp);
        }

        private MainWindow GetWindow()
        {
            return (MainWindow)View;
        }

        private MainApp GetApp()
        {
            return (MainApp)App;
        }
    }
}
