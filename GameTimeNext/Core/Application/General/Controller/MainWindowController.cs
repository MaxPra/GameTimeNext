using GameTimeNext.Core.Application.General.AppSearch;
using GameTimeNext.Core.Application.General.FavAppsReorder;
using GameTimeNext.Core.Application.General.UserSettings;
using GameTimeNext.Core.Application.General.ViewModels;
using GameTimeNext.Core.Application.GTXMigration;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.Utils;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
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
            GetApp().RootController = this;
        }

        protected override void BuildFirst()
        {

        }

        protected override async Task BuildFirstAsync()
        {
            // Für Applauncher Tab Control setzen
            AppEnvironment.AppLauncher.TabControl = GetWindow().MainTabControl;

            FnControls.SetVisible(GetWindow().pnlDevPathsHeader, FnSystem.IsDebug());

            BuildBetaTag();

            await CheckGTXMigration();

            AppEnvironment.AppLauncher.StartFavorites(GetApp());

            // Hintergrundprozesse starten
            AppEnvironment.StartBackgroundProcesses(GetApp());

            // Warten bis alle ausstehenden Render-Operationen (Prio 7) abgearbeitet
            // sind, bevor modale Dialoge gezeigt werden. Background-Prio (4) ist
            // niedriger als Render (7) → Render läuft zuerst durch.
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);

            // Fehler anzeigen, die vor MainWindow passiert sind (Appstart)
            ShowErrorsFromErrorList();
        }

        protected override void Build()
        {
            FnControls.SetVisible(GetWindow().txtEmptyTabMessage, GetWindow().MainTabControl.Items.Count == 0);
        }

        public override bool HandleGlobalShortcut(KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.M)
            {
                AppSearchApp? appSearch = GetApp().GetApplication<AppSearchApp>();
                appSearch?.AppSearchViewController?.Show(true);
                return true;
            }

            return false;
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

        private void BuildBetaTag()
        {
            if (AppEnvironment.AppVersion.IsBeta)
                GetWindow().Subtitle = "BETA";
            else
                GetWindow().Subtitle = string.Empty;
        }

        private void ShowErrorsFromErrorList()
        {

            if (AppEnvironment.InformationList == null || AppEnvironment.InformationList.Count == 0)
                return;

            foreach (InformationListItem informationListItem in AppEnvironment.InformationList)
            {

                CFMBOXResult result;

                if (informationListItem.MBoxType == CFMBOXType.Default)
                    result = GetApp().GetApplication<CFMBOX>().Show(informationListItem.Text, informationListItem.Buttons, informationListItem.Icon, informationListItem.MBoxType);
                else
                    result = GetApp().GetApplication<CFMBOX>().Show(informationListItem.Title, informationListItem.Text, informationListItem.Buttons, informationListItem.Icon, informationListItem.MBoxType);

                if (result == CFMBOXResult.Yes)
                    informationListItem.YesAction?.Invoke();
            }

            AppEnvironment.InformationList.Clear();
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

        protected void EV_ctxtEditOrder()
        {
            FavAppsReorderApp? favAppsReorderApp = GetApp().GetApplication<FavAppsReorderApp>();

            favAppsReorderApp.AppResult = (result =>
            {
                if (result.HasChanged)
                {
                    ReorderTabsByFavApps();
                }
            });

            favAppsReorderApp.Reorder();
        }

        private void ReorderTabsByFavApps()
        {
            var favApps = AppEnvironment.GetAppConfig().UserSettings.FavApps;
            var tabControl = GetWindow().MainTabControl;

            if (favApps == null || favApps.Count == 0 || tabControl.Items.Count == 0)
                return;

            var sortedFavApps = favApps.OrderBy(f => f.Order).ToList();

            var tabs = tabControl.Items.Cast<TabItem>().ToList();

            for (int i = 0; i < sortedFavApps.Count; i++)
            {
                var favApp = sortedFavApps[i];
                var matchingTab = tabs.FirstOrDefault(t => t.Tag?.ToString() == favApp.FullName);

                if (matchingTab != null)
                {
                    int currentIndex = tabControl.Items.IndexOf(matchingTab);
                    if (currentIndex != i && currentIndex >= 0)
                    {
                        tabControl.Items.RemoveAt(currentIndex);

                        int insertIndex = Math.Min(i, tabControl.Items.Count);
                        tabControl.Items.Insert(insertIndex, matchingTab);
                    }
                }
            }
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
