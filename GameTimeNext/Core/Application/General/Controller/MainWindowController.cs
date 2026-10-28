using GameTimeNext.Core.Application.BigPicture;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.General.AppSearch;
using GameTimeNext.Core.Application.General.FavAppsReorder;
using GameTimeNext.Core.Application.General.UserSettings;
using GameTimeNext.Core.Application.General.ViewModels;
using GameTimeNext.Core.Application.GTXMigration;
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
    internal class MainWindowController : UIXWindowControllerBase
    {
        private string _gtxPath = "C:\\GameTimeX";

        private MainWindowViewModel? _mainWindowViewModel = null;

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
            GetWindow().UseWallpaperBackground = isDynamicBackground;
            GetWindow().UseBackdropBlur = !isDynamicBackground;
        }

        protected override async Task BuildFirstImplAsync()
        {
            // Set tab control for the app launcher
            AppEnvironment.AppLauncher.TabControl = GetWindow().MainTabControl;

            FnControls.SetVisible(GetWindow().pnlDevPathsHeader, FnSystem.IsDebug());
            FnControls.SetVisible(GetWindow().pnlUserModeHeader, FnSystem.IsDebug() && FnSystem.RunWithUserPermissions());

            BuildBetaTag();


            FnLog.AddInfo("MainApp", "Starting favorites...");
            AppEnvironment.AppLauncher.StartFavorites(GetApp());

            FnLog.AddInfo("MainApp", "Starting background processes...");
            // Start background processes
            AppEnvironment.StartBackgroundProcesses(GetApp());

            // Wait until all pending render operations (priority 7) have been processed
            // before modal dialogs are shown. Background priority (4) is
            // lower than Render (7) → rendering runs first.
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);

            if (GetApp().SplashScreen is not null)
            {
                GetApp().SplashScreen?.Close();

                if (ReferenceEquals(System.Windows.Application.Current.MainWindow, GetApp().SplashScreen))
                    System.Windows.Application.Current.MainWindow = null;
            }

            FnControls.SetVisible(GetWindow(), true);

            // Set WindowState
            _ = GetWindow().Dispatcher.BeginInvoke(() =>
            {
                GetWindow().WindowState = GetApp().StartMinimized
                    ? WindowState.Minimized
                    : WindowState.Maximized;

                // Start argument --bigpicture: open directly in the Big Picture mode (still cloaked, so no flicker)
                if (!GetApp().StartMinimized && AppEnvironment.StartArguments.ContainsKey("bigpicture"))
                    EnterBigPicture();

                // Only reveal the window once it has its final state (no flicker)
                GetApp().SetMainWindowCloaked(false);
            }, DispatcherPriority.ApplicationIdle);

            System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

            // Show errors that occurred before the MainWindow (app start)
            ShowErrorsFromErrorList();

            await CheckGTXMigration();


            FnLog.AddInfo("MainApp", "*** Application started ***");
        }

        protected override void BuildImpl()
        {
            FnControls.SetVisible(GetWindow().pnlEmptyTabMessage, GetWindow().MainTabControl.Items.Count == 0);
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

            GetWindow().IsFullScreen = true;
            _bigPictureApp.Start(GetApp(), GetWindow().cpBigPicture);

            // WPF renders covered elements too: the tabs below (e.g. animations of the profiles view) would keep costing
            // frames of the mode. Hidden keeps their layout and state.
            GetWindow().MainTabControl.Visibility = Visibility.Hidden;
            FnControls.SetVisible(GetWindow().pnlEmptyTabMessage, false);

            FnLog.AddInfo("MainApp", "Big Picture mode opened");
        }

        /// <summary>
        /// BigPictureViewController: back to the normal window with its tabs.
        /// </summary>
        protected void EXEV_ExitBigPicture()
        {
            if (_bigPictureApp?.IsOpen != true)
                return;

            _bigPictureApp.Close();
            GetWindow().IsFullScreen = false;

            GetWindow().MainTabControl.Visibility = Visibility.Visible;
            FnControls.SetVisible(GetWindow().pnlEmptyTabMessage, GetWindow().MainTabControl.Items.Count == 0);

            // The mode colored the window with the selected game: back to the profile selected in the profiles view
            FnTheme.ApplyProfileAccentColors(AppEnvironment.CurrentPfid == 0 ? null : new TXPROFI().Read(AppEnvironment.CurrentPfid));

            FnLog.AddInfo("MainApp", "Big Picture mode closed");
        }

        private void EnsureProfilesAppStarted()
        {
            string className = typeof(ProfilesApp).FullName!;

            if (AppEnvironment.IsApplicationRunning(className))
                return;

            string tabName = AppEnvironment.AvailableApplications.FirstOrDefault(a => a.ClassName == className)?.Name ?? "Profiles";

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
                    {
                        FnControls.SetVisible(GetWindow().pnlEmptyTabMessage, GetWindow().MainTabControl.Items.Count == 0);
                        Event_TabChanged((TabControl)source);
                    }
                    break;
            }
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
                FnLog.AddInfo("MainApp", "GameTimeX installation found. Prompting user for migration.");

                CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
                CFMBOXResult result = cfmbox.Show(GetErrorMessage("GTX0001", AppConfig.Root.ApplicationName), CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

                if (result == CFMBOXResult.Yes)
                {
                    FnLog.AddInfo("MainApp", "User accepted migration. Starting migration process.");
                    GetApp().Loader.Begin(loaderTextStart);

                    bool hasError = false;

                    await Task.Run(() =>
                    {
                        try
                        {
                            GTXMigrationService gtxMigHelper = new GTXMigrationService("C:\\GameTimeX", GetApp().Loader);
                            gtxMigHelper.MigrateToGTNXT();
                        }
                        catch (Exception ex)
                        {
                            hasError = true;
                            FnLog.AddError("MainApp", "Error during GTX migration", ex);
                            GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("GLO0013", "migrating from GameTimeX", ex.Message), CFMBOXResult.Ok, CFMBOXIcon.Error);
                        }
                        finally
                        {
                            FnLog.AddInfo("MainApp", $"GTX migration process completed {(hasError ? "with errors" : "successfully")}.");
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
            ShowHelp(GetApp(), GetWindow().Title);
        }

        private void ShowTabHelp(TabItem tab)
        {
            if (!AppEnvironment.StartedApplications.TryGetValue(tab.Tag.ToString()!, out UIXApplication? application))
                return;

            ShowHelp(application, tab.Header.ToString()!);
        }

        private void ShowHelp(UIXApplication application, string title)
        {
            // Read again instead of the properties loaded at the start of the app, so a changed help text shows up immediately
            UIXAppProperties properties = UIXApplication.AppPropertiesProvider?.Read(application.AppProperties.ApplicationName) ?? application.AppProperties;

            string helpText = FnString.IsNullEmptyOrWhitespace(properties.ApplicationHelpText)
                ? "No help is available for this application yet."
                : properties.ApplicationHelpText;

            GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window)
                .Show($"Help - {title}", helpText, CFMBOXResult.Ok, CFMBOXIcon.Info, CFMBOXType.MarkdownLarge);
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

        protected void EV_ctxtUnsetAsPrimaryStart(FrameworkElement target)
        {
            TabItem tab = (TabItem)target;

            if (tab == null)
                return;

            FavoriteApplication favApp = FnUserSettings.GetFavoriteApplication(AppEnvironment.GetAppConfig().UserSettings.FavApps, tab.Tag.ToString());

            FnUserSettings.UnsetAsPrimaryStart(favApp);
        }

        protected void EV_ctxtEditOrder()
        {
            FavAppsReorderApp? favAppsReorderApp = GetApp().GetApplication<FavAppsReorderApp>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);

            favAppsReorderApp.AppResult = (result =>
            {
                if (result.HasChanged)
                {
                    ReorderTabsByFavApps();
                }
            });

            favAppsReorderApp.Reorder();
        }

        protected void EXEV_SwitchToApplication(string applicationFullName)
        {
            if (!AppEnvironment.IsApplicationRunning(applicationFullName))
                return;

            GetWindow().MainTabControl.SelectedItem = GetWindow().MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.Tag?.ToString() == applicationFullName);
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
