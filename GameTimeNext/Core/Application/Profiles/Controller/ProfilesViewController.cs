using GameTimeNext.Core.Application.BigPicture;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.Playthroughs;
using GameTimeNext.Core.Application.Profiles.BackgroundProcesses;
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using GameTimeNext.Core.Application.Profiles.Selectors;
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
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static UIX.ViewController.Engine.FrameworkElements.UIXContextMenuFactory;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    public class ProfilesViewController : UIXViewControllerBase<ProfilesApp, ProfilesView>
    {
        private ProfilesSubViewDataWrapper? _dataWrapper;

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

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
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
        /// <summary>
        /// Rebuilds the list (ProfilesView_ProfilesSelectorAdapter) and selects the profile with the given PFID, or
        /// the first one if it is not in the list. Without profiles the detail view is hidden.
        /// </summary>
        public async Task BuildProfilesListBoxAsync(long pfid)
        {
            UIXListBox listBoxProfiles = GetView().ListBoxProfiles;

            if (!await listBoxProfiles.LoadDataAsync())
                return;

            // Indicator on the filter button if the list is filtered
            GetView().ElFilterActive.Visibility = listBoxProfiles.GetSelectorAdapter<ProfilesView_ProfilesSelectorAdapter>().FilterActive
                ? Visibility.Visible
                : Visibility.Collapsed;

            List<ProfilesListBoxItem> profileItems = listBoxProfiles.Items.OfType<ProfilesListBoxItem>().ToList();

            ProfilesListBoxItem? selectedItem = profileItems.FirstOrDefault(p => (p.ItemObject as T1PROFI)?.PFID == pfid)
                                                ?? profileItems.FirstOrDefault();

            _dataWrapper!.Set(selectedItem?.ItemObject as T1PROFI);

            if (!_dataWrapper!.HasData)
            {
                _dataWrapper!.Set(new TXPROFI().CreateNew());
                GetApp().ProfilesDetailView.Visibility = Visibility.Hidden;
                AppEnvironment.CurrentPfid = 0;
                FnTheme.ApplyProfileBackground(null);
                FnTheme.ApplyProfileAccentColors(null);
            }
            else
            {
                GetApp().ProfilesDetailView.Visibility = Visibility.Visible;
                AppEnvironment.CurrentPfid = (selectedItem?.ItemObject as T1PROFI)?.PFID ?? 0;
                FnTheme.ApplyProfileBackground(_dataWrapper!.Get());
                FnTheme.ApplyProfileAccentColors(_dataWrapper!.Get());
            }

            if (selectedItem != null)
                listBoxProfiles.SelectedItem = selectedItem;
        }

        private ProfilesListBoxItem? GetSelectedItem()
        {
            return GetView().ListBoxProfiles.SelectedItem as ProfilesListBoxItem;
        }
        #endregion

        /// <summary>
        /// Unwraps the selected (sealed) game: saves it and plays the unwrap animation on its tile. The list is not
        /// rebuilt, that would end the animation.
        /// </summary>
        public void UnwrapSelectedProfile()
        {
            ProfilesListBoxItem? item = GetSelectedItem();

            if (item?.ItemObject is not T1PROFI t1profi)
                return;

            TFPROFI.Unseal(t1profi);

            item.COISSEA = false;

            ProfilesDetailSubViewController profileDetailViewController = (ProfilesDetailSubViewController)GetApp().ProfilesDetailView.ViewController;
            profileDetailViewController.UpdateUIUnwrapped();
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

        protected async Task EXEV_SwitchProfile(long pfid)
        {
            UIXListBox listBoxProfiles = GetView().ListBoxProfiles;

            if (listBoxProfiles.ItemsSource == null)
                return;

            ProfilesListBoxItem? FindCurrent() => listBoxProfiles.Items.OfType<ProfilesListBoxItem>().FirstOrDefault(p => (p.ItemObject as T1PROFI)?.PFID == pfid);

            if (FindCurrent() == null)
                await AdjustFiltersToStartedGameProfile(pfid);

            listBoxProfiles.SelectedItem = FindCurrent() ?? throw new InvalidOperationException("The started profile is not in the list.");

            // The Big Picture mode shows the game itself; a toast would pop up in front of the game started from it
            if (CFBigPictureApp.IsOpen)
                return;

            ToastMessage tm = new ToastMessage("Switched profile...", (GetSelectedItem()?.ItemObject as T1PROFI)?.GANA ?? string.Empty);
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

        #endregion

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
