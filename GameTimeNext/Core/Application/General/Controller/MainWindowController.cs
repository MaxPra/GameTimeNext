using GameTimeNext.Core.Application.BigPicture;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.General.AppSearch;
using GameTimeNext.Core.Application.General.UserSettings;
using GameTimeNext.Core.Application.General.Views;
using GameTimeNext.Core.Application.GTXMigration;
using GameTimeNext.Core.Application.Home;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
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

namespace GameTimeNext.Core.Application.General.Controller
{
    internal class MainWindowController : UIXWindowControllerBase<MainApp, MainWindow>
    {
        private string _gtxPath = "C:\\GameTimeX";


        // Created when the Big Picture mode is opened the first time, then reused
        private BigPictureApp? _bigPictureApp = null;

        public MainWindowController(UIXApplication app) : base(app)
        {
        }

        protected override void Init()
        {
            GetApp().RootController = this;

            GetApp().CallDispatcher.Register(this, nameof(EXEV_SwitchToApplication));
            GetApp().CallDispatcher.Register(this, nameof(EXEV_ExitBigPicture));
        }

        protected override void BuildFirstImpl()
        {
            // Setting "Dynamic Background" (at runtime via FnTheme.ApplyWallpaperBackground)
            // Without dynamic background the main window is translucent (acrylic)
            bool isDynamicBackground = FnTheme.IsDynamicBackgroundEnabled(AppEnvironment.GetAppConfig().AppSettings.BackgroundSource);
            GetView().UseWallpaperBackground = isDynamicBackground;
            GetView().UseBackdropBlur = !isDynamicBackground;

            GetView().Title = AppConfig.Root.ApplicationName;
        }

        protected override async Task BuildFirstImplAsync()
        {
            // Set tab control for the app launcher
            AppEnvironment.AppLauncher.TabControl = GetView().MainTabControl;

            FnControls.SetVisible(GetView().pnlDevPathsHeader, FnSystem.IsDebug());
            FnControls.SetVisible(GetView().pnlUserModeHeader, FnSystem.IsDebug() && FnSystem.RunWithUserPermissions());

            BuildBetaTag();


            // Visible (still cloaked, see MainApp.Start) while the favorites are started: a hidden window doesn't render,
            // so the views of the tabs would not get loaded and opened
            FnControls.SetVisible(GetView(), true);

            FnLog.AddInfo(GetApp(), "Starting favorites...");
            AppEnvironment.AppLauncher.StartFavorites(GetApp());

            // First start after the installation: the welcome page is opened after the favorites and shown in front
            if (AppEnvironment.IsFirstStart)
                AppEnvironment.AppLauncher.LaunchApplication(typeof(HomeApp), GetApp());

            FnLog.AddInfo(GetApp(), "Starting background processes...");
            // Start background processes
            AppEnvironment.StartBackgroundProcesses(GetApp());

            // Wait until all pending render operations (priority 7) have been processed
            // before modal dialogs are shown. Background priority (4) is
            // lower than Render (7) → rendering runs first.
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);

            // Set WindowState
            _ = GetView().Dispatcher.BeginInvoke(() =>
            {
                GetView().WindowState = GetApp().StartMinimized
                    ? WindowState.Minimized
                    : WindowState.Maximized;

                // Start argument --bigpicture: open directly in the Big Picture mode (still cloaked, so no flicker)
                if (!GetApp().StartMinimized && AppEnvironment.StartArguments.ContainsKey("bigpicture"))
                    EnterBigPicture();

                // Only reveal the window once it has its final state (no flicker)
                GetApp().SetMainWindowCloaked(false);

                // The window was only shown hidden before, so Windows hasn't activated it: bring it to the front now, while
                // the splash (same process) still has the foreground, otherwise it only shows up in the taskbar. The splash
                // lives on its own thread (it is not the MainWindow of the application) and is closed afterwards.
                if (!GetApp().StartMinimized)
                    FnForeground.BringToFront(GetView());

                GetApp().SplashScreen?.Close();
            }, DispatcherPriority.ApplicationIdle);

            System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

            // Show errors that occurred before the MainWindow (app start)
            ShowErrorsFromErrorList();

            await CheckGTXMigration();


            FnLog.AddInfo(GetApp(), "*** Application started ***");
        }

        protected override void BuildImpl()
        {
        }

        public override bool HandleGlobalShortcut(KeyEventArgs e)
        {
            // The Big Picture mode covers the tabs: it gets the keyboard, the shortcuts of the tabs are off
            if (_bigPictureApp?.IsOpen == true)
                return _bigPictureApp.HandleKey(e);

            if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.M)
            {
                OpenAppMenu();
                return true;
            }

            return false;
        }

        /// <summary>
        /// "+" after the open applications in the main tab control (template part of the tab control style).
        /// </summary>
        protected void EV_BtnTabAdd()
        {
            OpenAppMenu();
        }

        /// <summary>
        /// Down arrow before the "+" in the main tab control (template part of the tab control style):
        /// opens the start order of the favorites.
        /// </summary>
        protected void EV_btnTabFavorites()
        {
            FavAppsStartOrderView view = GetApp().FavAppsStartOrderView!;

            // The popup closes itself on a click outside, without CloseView
            if (view.IsShown && view.Popup!.IsOpen)
            {
                view.CloseView();
                return;
            }

            view.Popup!.PlacementTarget = GetView().MainTabControl.Template.FindName("btnTabFavorites", GetView().MainTabControl) as UIElement;
            view.ShowView(true);
        }

        /// <summary>
        /// An open application was dragged to another position in the tab bar. The order of the open tabs is not saved.
        /// </summary>
        protected void EV_MainTabControl_DragAndDrop()
        {
        }

        /// <summary>
        /// Big Picture button of the title bar (template part of the window style).
        /// </summary>
        protected void EV_btnBigPicture()
        {
            EnterBigPicture();
        }

        /// <summary>
        /// Switches the window to full screen and shows the Big Picture mode over the tabs.
        /// </summary>
        private void EnterBigPicture()
        {
            if (_bigPictureApp?.IsOpen == true)
                return;

            // The running game detection needs the profiles application (GameRunningProcess)
            EnsureProfilesAppStarted();

            _bigPictureApp ??= GetApp().GetApplication<BigPictureApp>();

            GetView().IsFullScreen = true;
            _bigPictureApp.Start(GetApp(), GetView().cpBigPicture);

            // WPF renders covered elements too: the tabs below (e.g. animations of the profiles view) would keep costing
            // frames of the mode. Hidden keeps their layout and state.
            GetView().MainTabControl.Visibility = Visibility.Hidden;

            // No display timeout while the mode is open: monitors turning off and on change the displays under a
            // running (or paused) game, which can crash its renderer
            FnPowerRequest.KeepDisplayOn("Big Picture mode is open");

            FnLog.AddInfo(GetApp(), "Big Picture mode opened");
        }

        /// <summary>
        /// BigPictureViewController: back to the normal window with its tabs.
        /// </summary>
        protected void EXEV_ExitBigPicture()
        {
            if (_bigPictureApp?.IsOpen != true)
                return;

            _bigPictureApp.Close();
            GetView().IsFullScreen = false;

            FnPowerRequest.Release();

            GetView().MainTabControl.Visibility = Visibility.Visible;

            // The mode colored the window with the selected game: back to the profile selected in the profiles view
            FnTheme.ApplyProfileAccentColors(AppEnvironment.CurrentPfid == 0 ? null : GetApp().GetDataManager<TXPROFI>().Read(AppEnvironment.CurrentPfid));

            FnLog.AddInfo(GetApp(), "Big Picture mode closed");
        }

        private void EnsureProfilesAppStarted()
        {
            string className = typeof(ProfilesApp).FullName!;

            if (AppEnvironment.IsApplicationRunning(className))
                return;

            string tabName = "Profiles";

            AppEnvironment.AppLauncher.LaunchApplication(className, GetApp(), tabName);
        }

        private void OpenAppMenu()
        {
            AppSearchApp? appSearch = GetApp().GetApplication<AppSearchApp>(UIXApplicationStartTarget.Window);
            appSearch?.Search();
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

        protected override void CheckImpl()
        {
        }

        protected override void FillViewImpl()
        {
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
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
        /// Tab is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_TabChanged(TabControl source)
        {
            if (source.SelectedItem is TabItem ti)
            {
                if (ti.Tag.ToString() != null && AppEnvironment.StartedApplications.ContainsKey(ti.Tag.ToString()!))
                    AppEnvironment.StartedApplications[ti.Tag.ToString()!].OnFocus();
            }

        }

        private async Task CheckGTXMigration()
        {
            string[] gtnFiles = Directory.GetFiles(AppConfig.Storage.ProfileCoversDirectoryPath);
            string loaderTextStart = "Migrating GTX -> GTN ...";

            if (Directory.Exists(_gtxPath) && gtnFiles.Length == 0)
            {
                FnLog.AddInfo(GetApp(), "GameTimeX installation found. Prompting user for migration.");

                CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
                CFMBOXResult result = cfmbox.Show(GetErrorMessage("GTX0001", AppConfig.Root.ApplicationName), CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

                if (result == CFMBOXResult.Yes)
                {
                    FnLog.AddInfo(GetApp(), "User accepted migration. Starting migration process.");
                    GetApp().Loader.Begin(loaderTextStart);

                    bool hasError = false;

                    await Task.Run(() =>
                    {
                        try
                        {
                            GTXMigrationService gtxMigHelper = new GTXMigrationService(GetApp(), "C:\\GameTimeX", GetApp().Loader);
                            gtxMigHelper.MigrateToGTNXT();
                        }
                        catch (Exception ex)
                        {
                            hasError = true;
                            FnLog.AddError(GetApp(), "Error during GTX migration", ex);
                            GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("GLO0013", "migrating from GameTimeX", ex.Message), CFMBOXResult.Ok, CFMBOXIcon.Error);
                        }
                        finally
                        {
                            FnLog.AddInfo(GetApp(), $"GTX migration process completed {(hasError ? "with errors" : "successfully")}.");
                            GetApp().Loader.Stop();
                        }
                    });
                }
            }
        }

        private void BuildBetaTag()
        {
            if (AppEnvironment.AppVersion.IsBeta)
                GetView().Subtitle = "beta";
            else
                GetView().Subtitle = string.Empty;
        }

        private void ShowErrorsFromErrorList()
        {

            if (AppEnvironment.InformationList == null || AppEnvironment.InformationList.Count == 0)
                return;

            foreach (InformationListItem informationListItem in AppEnvironment.InformationList)
            {

                CFMBOXResult result;

                if (informationListItem.MBoxType == CFMBOXType.Default)
                    result = GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(informationListItem.GetText(GetApp()), informationListItem.Buttons, informationListItem.Icon, informationListItem.MBoxType);
                else
                    result = GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(informationListItem.Title, informationListItem.GetText(GetApp()), informationListItem.Buttons, informationListItem.Icon, informationListItem.MBoxType);

                if (result == CFMBOXResult.Yes)
                    informationListItem.YesAction?.Invoke();
            }

            AppEnvironment.InformationList.Clear();
        }

        protected void EV_ctxtHelp(FrameworkElement target)
        {
            if (target is TabItem tab)
                ShowTabHelp(tab);
        }

        protected void EV_BtnHelp()
        {
            ShowHelp(GetApp(), GetView().Title);
        }

        private void ShowTabHelp(TabItem tab)
        {
            if (!AppEnvironment.StartedApplications.TryGetValue(tab.Tag.ToString()!, out UIXApplication? application))
                return;

            ShowHelp(application, tab.Header.ToString()!);
        }

        private void ShowHelp(UIXApplication application, string title)
        {
            CFMainApp.ShowApplicationHelp(GetApp(), application.AppProperties.ApplicationName, title);
        }

        protected void EV_ctxtClose(FrameworkElement target)
        {
            TabItem tab = (TabItem)target;

            if (tab != null)
            {
                AppEnvironment.AppLauncher.CloseApplication(tab.Tag.ToString(), tab);
            }
        }

        protected void EV_ctxtCloseAll()
        {
            AppEnvironment.AppLauncher.CloseAll();
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

        protected void EV_ctxtUnsetAsPrimaryStart(FrameworkElement target)
        {
            TabItem tab = (TabItem)target;

            if (tab == null)
                return;

            FavoriteApplication favApp = FnUserSettings.GetFavoriteApplication(AppEnvironment.GetAppConfig().UserSettings.FavApps, tab.Tag.ToString());

            FnUserSettings.UnsetAsPrimaryStart(favApp);
        }

        protected void EXEV_SwitchToApplication(string applicationFullName)
        {
            if (!AppEnvironment.IsApplicationRunning(applicationFullName))
                return;

            GetView().MainTabControl.SelectedItem = GetView().MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.Tag?.ToString() == applicationFullName);
        }

        private void ReorderTabsByFavApps()
        {
            var favApps = AppEnvironment.GetAppConfig().UserSettings.FavApps;
            var tabControl = GetView().MainTabControl;

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

    }
}
