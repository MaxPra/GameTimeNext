using GameTimeNext.Core.Application.Codetables;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.Profiles.Types;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.Igdb;
using GameTimeNext.Core.Framework.LauncherIntegration;
using GameTimeNext.Core.Framework.LauncherIntegration.Link;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.Utils;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static GameTimeNext.Core.Application.Profiles.Controller.ProfilesManualEstTimesEditViewController;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    public class ProfilesEditViewController : UIXViewControllerBase<ProfilesEditApp, ProfilesEditView>
    {

        private ProfilesEditViewModel? _profilesEditViewModel;

        private string _coverAppDataPath = string.Empty;
        private string _coverAppFolderFileName = string.Empty;
        private string _selectedSteamGridDBImagePath = string.Empty;
        private string _oldProfileCoverFileName = string.Empty;
        private BitmapImage _croppedProfileCover = new BitmapImage();

        private bool _isEstTimeFilledManual = false;

        private bool _cmbTargetPlayTypeFilledCompletely = false;

        private bool _successfullyFilledEstTimes = false;
        private bool _hasProfileCoverChanged = false;

        public string CoverAppDataPath
        {
            get { return _coverAppDataPath; }
            set { _coverAppDataPath = value; }
        }

        public string CoverAppFolderFileName
        {
            get { return _coverAppFolderFileName; }
            set { _coverAppFolderFileName = value; }
        }

        public ProfilesEditViewController(UIXApplication app) : base(app)
        {
        }

        public class ProfilesEditViewReturn : UIXViewReturn
        {
            public long PFID { get; set; } = 0;
        }

        protected override void Init()
        {
            // Set loader texts
            GetApp().Loader.SetRandomTexts(
                                            "Loading the game profile...",
                                            "Polishing the cover...",
                                            "Looking up the game details...",
                                            "Checking the launcher...",
                                            "Asking IGDB about the game...",
                                            "Calculating how long it takes to beat...",
                                            "Gathering your playthroughs..."
                                            );


            _profilesEditViewModel = new ProfilesEditViewModel();
            ViewReturn = new ProfilesEditViewReturn();

            _hasProfileCoverChanged = false;
            _isEstTimeFilledManual = GetApp().T1Profi.ETML;
            _oldProfileCoverFileName = GetApp().T1Profi.PPFN;

            GetApp().ManualEstTimesCache.estTimeMain = GetApp().T1Profi.ETMA / 60;
            GetApp().ManualEstTimesCache.estTimeMainExtra = GetApp().T1Profi.ETME / 60;
            GetApp().ManualEstTimesCache.estTimeCompletionist = GetApp().T1Profi.ETCO / 60;

            AddIdentifier("T1PROFI", GetApp().T1Profi);
            AddSource("T1CTABD", new TXCTABD());
        }

        protected override void BuildFirstImpl()
        {
            using (SuppressRunEventPipeline())
            {
                BuildTagGrid(GetApp().T1Profi.PFID);

                // Enabled state of the IGDB estimated time ComboBox
                //bool isIGDBLinkedCorrectly = !FnString.IsNullEmptyOrWhitespace(AppEnvironment.GetAppConfig().AppSettings.TwitchIGDBClientID) &&
                //    !FnString.IsNullEmptyOrWhitespace(AppEnvironment.GetAppConfig().AppSettings.TwitchIGDBClientSecret);

                //FnControls.SetEnabled(GetView().cmbEstimatedTimeType, isIGDBLinkedCorrectly);
            }

        }

        protected override async Task BuildFirstImplAsync()
        {
            // New profiles start with the placeholder cover, replaced by choosing another one
            if (GetView().ViewIndicator.Contains("CN") && FnString.IsNullEmptyOrWhitespace(GetApp().T1Profi.PPFN))
            {
                (string path, string fileName) tempCover = await FnSystem.SaveCroppedImageTempPath(CFProfilesApp.LoadPlaceholderCover(), GetApp().Loader);

                await FillViewCoverChanged(tempCover.path);
            }
        }

        protected override void BuildImpl()
        {
            ControlBlackoutCheckboxProtection();

            BuildLauncherSection();
            BuildAccentColorSection();
        }

        protected override void CheckImpl()
        {
            // Profile Name
            if (FnString.IsNullEmptyOrWhitespace(GetView().txbProfileName.Text))
                AddViewError(GetView().txbProfileName, GetErrorMessage("GLO0003", "Profile name"));

            // Cover (set by the controller, no field: the error is shown on the button choosing one)
            if (FnString.IsNullEmptyOrWhitespace(GetApp().T1Profi.PPFN))
                AddViewError(GetView().btnBrowseLocalImage, GetErrorMessage("GLO0003", "Profile cover"));

            // Accent Colors
            if (GetView().cbUseProfileAccentColors.IsChecked == true
                && (GetView().tglAccent1.IsChecked == false && GetView().tglAccent2.IsChecked == false && GetView().tglAccent3.IsChecked == false))
                AddViewError(GetView().cbUseProfileAccentColors, GetErrorMessage("GLO0003", "An accent color"));

            if (GetView().cbUseProfileAccentColors.IsChecked == false
                && (GetView().tglAccent1.IsChecked == true || GetView().tglAccent2.IsChecked == true || GetView().tglAccent3.IsChecked == true))
                AddViewError(GetView().cbUseProfileAccentColors, GetErrorMessage("PRO0001"));

            if (FnString.IsNullEmptyOrWhitespace(GetView().cmbPlatform.SelectedValue.ToString()!))
                AddViewError(GetView().cmbPlatform, GetErrorMessage("GLO0003", "Platform"));
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected override void Event_Closing()
        {
            try { Directory.Delete(AppConfig.Temp.SteamGridDBCoversDirectoryPath, true); } catch { }
            try { Directory.Delete(AppConfig.Temp.ProfileCoversDirectoryPath, true); } catch { }


            Exit(false);
        }

        protected override void Event_Maximize()
        {
        }

        protected override void Event_Minimize()
        {
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
        {
            CFMBOX.AskUnsavedChanges(GetApp(), args);
        }

        protected override void FillDBOImpl()
        {
        }

        protected override async Task FillDBOImplAsync()
        {
            // Accent colors
            SaveAccentColors();
            // Save expected playtime (manual)
            FillDBOManualEstTimes();

            if (AppEnvironment.GetAppConfig().AppSettings.EnableTwitchIGDB)
            {

                // Save expected playtimes
                _successfullyFilledEstTimes = await FillDBOEstimatedTimesIGDBAsync();
            }

            // Profile settings
            FillDBOProfileSettings();

            // Copy image. Compared with the cover the profile was opened with, not with the change tracking:
            // its baseline is taken after BuildFirst, so the preset placeholder cover would count as unchanged
            if (GetApp().T1Profi.PPFN != _oldProfileCoverFileName)
                _hasProfileCoverChanged = true;
        }


        protected override void FillViewImpl()
        {
            using (SuppressRunEventPipeline())
            {
                FillComboboxPlayTypes();

                FillViewProfileSettings();

                FillViewAccentColors();
            }

            FillViewCoverPreview();
        }

        /// <summary>
        /// Shows the chosen cover (not saved yet) or the profile's cover.
        /// </summary>
        private void FillViewCoverPreview()
        {
            GetView().imgCover.Source = FnString.IsNullEmptyOrWhitespace(_coverAppDataPath)
                ? CFProfilesApp.LoadProfileCover(GetApp().T1Profi.PPFN)
                : FnImage.LoadImageWithoutLock(_coverAppDataPath, 300, 450) ?? CFProfilesApp.LoadPlaceholderCover(300, 450);
        }

        private void FillComboboxPlayTypes()
        {
            _cmbTargetPlayTypeFilledCompletely = false;

            UIXManualCodetable codetable = new UIXManualCodetable();
            codetable.AddEntry(EstimatedTimeTypes.EST_TIME_NONE, "(None)");
            codetable.AddEntry(EstimatedTimeTypes.EST_TIME_MAIN, "Main Story");
            codetable.AddEntry(EstimatedTimeTypes.EST_TIME_MAIN_EXTRA, "Main Story + Extra");
            codetable.AddEntry(EstimatedTimeTypes.EST_TIME_COMPLETIONIST, "Completionist");
            codetable.ApplyTo(GetView().cmbEstimatedTimeType);

            _cmbTargetPlayTypeFilledCompletely = true;

            if (!FnString.IsNullEmptyOrWhitespace(GetApp().T1Profi.ETTY))
            {
                foreach (ComboBoxItem item in GetView().cmbEstimatedTimeType.Items)
                {
                    if (item.Tag?.ToString() == GetApp().T1Profi.ETTY)
                    {
                        GetView().cmbEstimatedTimeType.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        protected override void SaveDBOImpl()
        {
            // Tags
            FillDBOTags();

            if (GetView().ViewIndicator.Contains("CN"))
            {
                TFPLTHR.CreateNewPlaythrough(GetApp().T1Profi.PFID);
            }
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override async Task TriggeredEventAsync(FrameworkElement source, string eventName)
        {
            if (source is ToggleButton && source.Name.StartsWith("tglAccent") && eventName == UIXEventNames.ToggleButton.Checked)
            {
                ToggleAccentChanged(source);
            }
        }

        private void FillViewLinkedGame(LauncherGame game)
        {
            // Profile name
            if (GetView().ViewIndicator.Contains("CN"))
                GetView().txbProfileName.Text = game.Name;

            // Game folder (the executables were reset for it)
            if (!FnString.IsNullEmptyOrWhitespace(game.InstallPath))
                GetView().txbGameFolder.Text = game.InstallPath;
        }

        private void FillViewProfileSettings()
        {
            // Load profile settings
            CProfileSettings cProfileSettings = new CProfileSettings(GetApp().T1Profi.PRSE).Dezerialize();

            GetView().cbEnableHdrOnStart.IsChecked = cProfileSettings.HDREnabled == true;
            GetView().cbPauseInBigPicture.IsChecked = cProfileSettings.PauseInBigPicture;

            using (SuppressRunEventPipeline())
            {
                GetView().cbOverrideGlobalBlackout.IsChecked = cProfileSettings.OverrideGlobalBlackout == true;
                GetView().cbBlackoutSideMonitors.IsChecked = cProfileSettings.BlackoutSideMonitors == true;
            }

            ControlBlackoutCheckboxProtection();

            GetView().txbLaunchArgs.Text = cProfileSettings.LaunchArgs;
        }

        private void FillViewAccentColors()
        {
            if (FnString.IsNullEmptyOrWhitespace(_selectedSteamGridDBImagePath) && GetView().ViewIndicator.Contains("ED"))
            {
                // Read from T1PROFI
                CAccentColorsInit cAccentColorsInit = new CAccentColorsInit(GetApp().T1Profi.ACIN);
                cAccentColorsInit = cAccentColorsInit.Dezerialize();

                CAccentColors cAccentColors = new CAccentColors(GetApp().T1Profi.ACCO);
                cAccentColors = cAccentColors.Dezerialize();

                Color color1 = (Color)ColorConverter.ConvertFromString(cAccentColorsInit.AccentColors.ElementAt(0).Key);
                Color color2 = (Color)ColorConverter.ConvertFromString(cAccentColorsInit.AccentColors.ElementAt(1).Key);
                Color color3 = (Color)ColorConverter.ConvertFromString(cAccentColorsInit.AccentColors.ElementAt(2).Key);

                GetView().tglAccent1.Background = new SolidColorBrush(color1);
                GetView().tglAccent1.Tag = color1;
                GetView().tglAccent1.IsChecked = cAccentColorsInit.AccentColors.ElementAt(0).Value;

                GetView().tglAccent2.Background = new SolidColorBrush(color2);
                GetView().tglAccent2.Tag = color2;
                GetView().tglAccent2.IsChecked = cAccentColorsInit.AccentColors.ElementAt(1).Value;

                GetView().tglAccent3.Background = new SolidColorBrush(color3);
                GetView().tglAccent3.Tag = color3;
                GetView().tglAccent3.IsChecked = cAccentColorsInit.AccentColors.ElementAt(2).Value;
            }
        }

        private async Task FillViewCoverChanged(string path)
        {

            _coverAppFolderFileName = CFProfilesEditApp.GetGUIDCoverName("jpg");

            GetView().Dispatcher.Invoke(() =>
            {
                GetApp().T1Profi.PPFN = _coverAppFolderFileName;
            });

            GetApp().Loader.Begin();

            List<Color> accentColors = new List<Color>();

            await Task.Run(async () =>
            {
                try
                {
                    accentColors = FnImage.GetTopAccentColors(path);
                }
                catch (Exception ex)
                {
                    // Later: logging etc. here
                }
                finally
                {
                    GetApp().Loader.Stop();
                }
            });

            if (accentColors.Count == 3)
            {

                GetView().Dispatcher.Invoke(() =>
                {
                    // Fill accent colors
                    Color color1 = Color.FromArgb(255, accentColors[0].R, accentColors[0].G, accentColors[0].B);
                    Color color2 = Color.FromArgb(255, accentColors[1].R, accentColors[1].G, accentColors[1].B);
                    Color color3 = Color.FromArgb(255, accentColors[2].R, accentColors[2].G, accentColors[2].B);

                    GetView().tglAccent1.Background = new SolidColorBrush(color1);
                    GetView().tglAccent1.Tag = color1;

                    GetView().tglAccent2.Background = new SolidColorBrush(color2);
                    GetView().tglAccent2.Tag = color2;

                    GetView().tglAccent3.Background = new SolidColorBrush(color3);
                    GetView().tglAccent3.Tag = color3;
                });
            }

            // Remember path for copying later
            _coverAppDataPath = path;

            GetView().Dispatcher.Invoke(FillViewCoverPreview);
        }

        private void FillDBOManualEstTimes()
        {
            ComboBoxItem combItem = GetView().cmbEstimatedTimeType.SelectedItem as ComboBoxItem;

            GetApp().T1Profi.ETML = _isEstTimeFilledManual;
            GetApp().T1Profi.ETMA = GetApp().ManualEstTimesCache.estTimeMain * 60;
            GetApp().T1Profi.ETME = GetApp().ManualEstTimesCache.estTimeMainExtra * 60;
            GetApp().T1Profi.ETCO = GetApp().ManualEstTimesCache.estTimeCompletionist * 60;

            GetApp().T1Profi.ETTY = combItem.Tag.ToString();
        }

        private void FillDBOTags()
        {
            // Read selected groups
            List<ProfilesGroupListBoxItem> selectedGroups = _profilesEditViewModel!.T1GROUPs.Where(t => t.COISSEL).ToList();

            // Delete all existing entries for this profile
            TXGRPPO tblmGrppo = new TXGRPPO();
            tblmGrppo.DeleteAllWherePFID(GetApp().T1Profi.PFID);

            foreach (ProfilesGroupListBoxItem grp in selectedGroups)
            {
                T1GROUP t1group = (T1GROUP)grp.ItemObject!;

                T1GRPPO tblGrppo = tblmGrppo.CreateNew();
                tblGrppo.GRID = t1group!.GRID;
                tblGrppo.PFID = GetApp().T1Profi.PFID;
                tblmGrppo.Save(tblGrppo);
            }
        }

        private void FillDBOProfileSettings()
        {
            CProfileSettings cProfileSettings = new CProfileSettings();

            // Not edited here: a new game is added shrink-wrapped if set, an existing one keeps its state
            cProfileSettings.Sealed = GetView().ViewIndicator.Contains("CN")
                ? AppEnvironment.GetAppConfig().AppSettings.SealNewGames
                : new CProfileSettings(GetApp().T1Profi.PRSE).Dezerialize().Sealed;

            cProfileSettings.HDREnabled = GetView().cbEnableHdrOnStart.IsChecked == true;
            cProfileSettings.LaunchArgs = GetView().txbLaunchArgs.Text.Trim();
            cProfileSettings.PauseInBigPicture = GetView().cbPauseInBigPicture.IsChecked == true;

            cProfileSettings.OverrideGlobalBlackout = GetView().cbOverrideGlobalBlackout.IsChecked == true;
            cProfileSettings.BlackoutSideMonitors =
                cProfileSettings.OverrideGlobalBlackout &&
                GetView().cbBlackoutSideMonitors.IsChecked == true;

            GetApp().T1Profi.PRSE = cProfileSettings.Serialize();
        }

        private void FillDBOExecutables(List<Executable> selectedExecutables)
        {
            CExecutables cExecutables = new CExecutables();

            foreach (Executable executable in selectedExecutables)
            {
                cExecutables.KeyValuePairs.Add(executable.Name, executable.IsSelected);
            }

            GetApp().T1Profi.EXEC = cExecutables.Serialize();
        }

        private async Task<bool> FillDBOEstimatedTimesIGDBAsync()
        {
            // The loader belongs to the profile library (host application): stopped in finally on every way out,
            // otherwise an IGDB error would leave the library loader visible for good
            bool loaderStarted = false;

            try
            {
                if (!_cmbTargetPlayTypeFilledCompletely || _isEstTimeFilledManual)
                    return false;

                ComboBoxItem combItem = GetView().cmbEstimatedTimeType.SelectedItem as ComboBoxItem;

                long steamAppId = FnLauncher.GetSteamAppId(GetApp().T1Profi);

                if (steamAppId == 0 && FnString.IsNullEmptyOrWhitespace(GetView().txbProfileName.Text) || combItem.Tag.ToString() == EstimatedTimeTypes.EST_TIME_NONE)
                {
                    // -- Filling
                    GetApp().T1Profi.ETMA = 0;
                    GetApp().T1Profi.ETME = 0;
                    GetApp().T1Profi.ETCO = 0;

                    GetApp().T1Profi.ETTY = EstimatedTimeTypes.EST_TIME_NONE;
                    return true;
                }

                GetApp().Loader.Begin();
                loaderStarted = true;

                string clientId = AppEnvironment.GetAppConfig().AppSettings.TwitchIGDBClientID;

                IgdbService igdbservice = new IgdbService(GetApp(), clientId);

                int? gameId = null;

                if (steamAppId != 0)
                {
                    int steamSourceId = igdbservice.GetSteamSourceId();
                    gameId = await igdbservice.FindGameIdBySteamAppIdAsync(steamSourceId, steamAppId.ToString(CultureInfo.InvariantCulture));
                }

                if (gameId == null)
                {
                    var game = await igdbservice.FindGameByNameAsync(GetApp().T1Profi.GANA);

                    gameId = game?.Id;
                }

                if (gameId == null)
                {
                    GetView().Dispatcher.Invoke(() =>
                    {
                        GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("PRO0002"), CFMBOXResult.Ok, CFMBOXIcon.Warning);
                    });

                    ResetPlaytypes();

                    return false;
                }

                var timeToBeat = await igdbservice.GetGameTimeToBeatAsync(gameId!.Value);

                if (timeToBeat == null)
                {
                    GetView().Dispatcher.Invoke(() =>
                    {
                        GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("GLO0016", "the time to beat"), CFMBOXResult.Ok, CFMBOXIcon.Warning);
                    });

                    ResetPlaytypes();

                    return false;
                }

                if (timeToBeat.Hastily == null || timeToBeat.Normally == null || timeToBeat.Completely == null)
                {
                    GetView().Dispatcher.Invoke(() =>
                    {
                        GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("GLO0016", "the time to beat"), CFMBOXResult.Ok, CFMBOXIcon.Warning);
                    });

                    ResetPlaytypes();

                    return false;
                }

                // -- Filling
                GetApp().T1Profi.ETMA = ((double)timeToBeat.Hastily!) / 60;
                GetApp().T1Profi.ETME = ((double)timeToBeat.Normally!) / 60;
                GetApp().T1Profi.ETCO = ((double)timeToBeat.Completely!) / 60;

                GetApp().T1Profi.ETTY = combItem.Tag.ToString();

                return true;
            }
            catch (Exception ex)
            {
                // ignored
                return false;
            }
            finally
            {
                if (loaderStarted)
                    GetApp().Loader.Stop();
            }
        }

        private void FillComboboxPlatformsManual()
        {
            UIXManualCodetable uIXManualCodetable = new UIXManualCodetable();

            TXCTABD txctabd = new TXCTABD();
            List<T1CTABD> entrys = txctabd.GetEntries("pF");

            foreach (T1CTABD entry in entrys)
            {
                uIXManualCodetable.AddEntry(entry.TXNUM, entry.DESCR);
            }

            uIXManualCodetable.ApplyTo(GetView().cmbPlatform);
        }

        private void BuildLauncherSection()
        {
            T1PROFI t1profi = GetApp().T1Profi;
            bool isLinked = FnLauncher.IsLinked(t1profi);

            FnControls.SetVisible(GetView().pnlLauncherLinkState, isLinked);
            FnControls.SetVisible(GetView().pnlLauncherSettings, isLinked);
            FnControls.SetVisible(GetView().grdLaunchArgs, FnLauncher.GetLauncher(t1profi)?.SupportsLaunchArguments == true);

            if (isLinked)
            {
                GetView().txtLauncherLinkStatus.Text = $"Linked with {FnLauncher.GetLauncherName(t1profi.LNTY)}";
                GetView().txtLauncherGameId.Text = t1profi.LNID;
            }
        }

        private void BuildAccentColorSection()
        {
            // Visibility of the accent colors
            if (!FnString.IsNullEmptyOrWhitespace(GetApp().T1Profi.PPFN))
            {
                GetView().grdAccentColorSettings.Visibility = Visibility.Visible;
            }
            else
            {
                GetView().grdAccentColorSettings.Visibility = Visibility.Collapsed;
            }
        }

        private void BuildTagGrid(long pfid)
        {
            List<ProfilesGroupListBoxItem> t1groups = new List<ProfilesGroupListBoxItem>();

            UIXQuery query = BuildTagGridQuery(pfid);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {

                    long grid = UIXQuery.GetInt64(reader, K1GROUP.Name, K1GROUP.Fields.GRID);
                    long gpid = UIXQuery.GetInt64(reader, K1GRPPO.Name, K1GRPPO.Fields.GPID);

                    T1GROUP t1group = new TXGROUP().Read(grid);
                    t1groups.Add(new ProfilesGroupListBoxItem
                    {
                        ItemObject = t1group,
                        COGRPID = gpid,
                        COISSEL = gpid > 0
                    });
                }
            }

            // Filter
            t1groups = t1groups.Where(s => (s.ItemObject as T1GROUP)?.GTYP == GroupType.Tag).ToList();

            // Fill view model
            _profilesEditViewModel = new ProfilesEditViewModel();
            _profilesEditViewModel.T1GROUPs = new System.Collections.ObjectModel.ObservableCollection<ProfilesGroupListBoxItem>(t1groups);


            if (t1groups != null && t1groups.Count > 0)
                _profilesEditViewModel.SelectedTBLGROUP = t1groups.FirstOrDefault(p => p.COISSEL);

            View.DataContext = _profilesEditViewModel;
        }

        private UIXQuery BuildTagGridQuery(long pfid)
        {
            UIXQuery query = new UIXQuery(K1GROUP.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            // Fields
            query.AddField(K1GROUP.Name, K1GROUP.Fields.GRID);

            if (pfid > 0)
            {
                query.AddField(K1GRPPO.Name, K1GRPPO.Fields.GPID);

                // Left Join T1GRPPO
                UIXQueryTable t1grppo = query.AddJoinTable(K1GRPPO.Name, JoinType.LEFT);
                t1grppo.AddJoinCondition(K1GROUP.Name, K1GROUP.Fields.GRID, QueryCompareType.EQUALS, K1GRPPO.Name, K1GRPPO.Fields.GRID);

                if (pfid > 0)
                    t1grppo.AddJoinCondition(K1GRPPO.Name, K1GRPPO.Fields.PFID, QueryCompareType.EQUALS, pfid);
            }

            // Where
            query.AddWhere(K1GROUP.Name, K1GROUP.Fields.GTYP, QueryCompareType.EQUALS, GroupType.Tag);

            return query;
        }

        private void SaveAccentColors()
        {
            if (GetView().cbUseProfileAccentColors.IsChecked == false)
                return;

            // -- Accent colors (corresponding to the selected one)
            string[] accentColorsCalculated = FnTheme.CalculateAccentStateColors(CFProfilesEditApp.GetSelectedToggleButton(GetView()).Tag.ToString());

            Dictionary<string, string> accentColorsDic = new Dictionary<string, string>();

            accentColorsDic.Add("accent", accentColorsCalculated[0]);
            accentColorsDic.Add("hover", accentColorsCalculated[1]);
            accentColorsDic.Add("pressed", accentColorsCalculated[2]);

            CAccentColors cAccentColors = new CAccentColors();
            cAccentColors.AccentColors = accentColorsDic;

            GetApp().T1Profi.ACCO = cAccentColors.Serialize();

            // -- Save automatically determined accent colors
            string[] accentColorsInit = new string[3];

            accentColorsInit[0] = GetView().tglAccent1.Tag.ToString()!;
            accentColorsInit[1] = GetView().tglAccent2.Tag.ToString()!;
            accentColorsInit[2] = GetView().tglAccent3.Tag.ToString()!;

            Dictionary<string, bool> dicAccentColorsInit = new Dictionary<string, bool>();
            dicAccentColorsInit.Add(accentColorsInit[0], GetView().tglAccent1.IsChecked == true);
            dicAccentColorsInit.Add(accentColorsInit[1], GetView().tglAccent2.IsChecked == true);
            dicAccentColorsInit.Add(accentColorsInit[2], GetView().tglAccent3.IsChecked == true);

            CAccentColorsInit cAccentColorsInit = new CAccentColorsInit();
            cAccentColorsInit.AccentColors = dicAccentColorsInit;

            GetApp().T1Profi.ACIN = cAccentColorsInit.Serialize();
        }

        private void ToggleAccentChanged(FrameworkElement toggleChecked)
        {
            if (toggleChecked == GetView().tglAccent1)
            {
                GetView().tglAccent2.IsChecked = false;
                GetView().tglAccent3.IsChecked = false;
            }
            else if (toggleChecked == GetView().tglAccent2)
            {
                GetView().tglAccent1.IsChecked = false;
                GetView().tglAccent3.IsChecked = false;
            }
            else if (toggleChecked == GetView().tglAccent3)
            {
                GetView().tglAccent1.IsChecked = false;
                GetView().tglAccent2.IsChecked = false;
            }
        }

        private void ControlBlackoutCheckboxProtection()
        {
            using (SuppressRunEventPipeline())
            {
                bool isOverrideEnabled = GetView().cbOverrideGlobalBlackout.IsChecked == true;

                FnControls.SetEnabled(GetView().cbBlackoutSideMonitors, isOverrideEnabled);
                if (!isOverrideEnabled)
                    GetView().cbBlackoutSideMonitors.IsChecked = false;
            }
        }

        private void ResetPlaytypes()
        {
            GetView().cmbEstimatedTimeType.SelectedIndex = 0;

            FillDBO();
        }

        private void ShowManualEstTimesEditView()
        {
            GetApp().ManualEstTimesCache.ProfileName = GetView().txbProfileName.Text;

            GetApp().ProfilesManualEstTimesEditView = new ProfilesManualEstTimesEditView();
            GetApp().ProfilesManualEstTimesEditView.ViewController = new ProfilesManualEstTimesEditViewController(GetApp());

            GetApp().ProfilesManualEstTimesEditView.ViewController.SetResultCallback<ProfilesManualEstTimesEditViewReturn>(r =>
            {
                if (!r.Canceled)
                {
                    _isEstTimeFilledManual = true;

                    GetApp().ManualEstTimesCache.estTimeMain = r.estTimeMain;
                    GetApp().ManualEstTimesCache.estTimeMainExtra = r.estTimeMainExtra;
                    GetApp().ManualEstTimesCache.estTimeCompletionist = r.estTimeCompletionist;
                }
            });

            GetApp().ProfilesManualEstTimesEditView.ViewController.Show();
        }

        private void LinkGame(LauncherGame game)
        {
            FnLauncher.LinkGame(GetApp().T1Profi, game);

            // Select executable
            if (!FnString.IsNullEmptyOrWhitespace(game.InstallPath))
                SearchExecutables(game.InstallPath);

            using (SuppressRunEventPipeline())
                FillViewLinkedGame(game);

            Build();
        }

        private void SearchExecutables(string gameFolderPath)
        {
            ProfilesExecutablesEditApp profilesExecutablesEditApp = GetApp().GetApplication<ProfilesExecutablesEditApp>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
            profilesExecutablesEditApp.Search(gameFolderPath, r =>
            {
                if (!r.Canceled)
                {
                    FillDBOExecutables(r.SelectedExecutables);
                }

            });
        }

        protected void EV_btnLauncherImport()
        {
            ProfilesLauncherImportApp app = GetApp().GetApplication<ProfilesLauncherImportApp>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
            app.Search(r =>
            {
                if (!r.Canceled)
                    LinkGame(r.Game!);
            });
        }

        protected void EV_btnLinkFile()
        {
            string filePath = FnSystemDialogs.ShowFileDialog("Choose game or shortcut", "Programs and shortcuts (*.exe;*.lnk;*.url)|*.exe;*.lnk;*.url", false);

            if (!FnString.IsNullEmptyOrWhitespace(filePath))
                LinkGame(LinkLauncher.CreateGame(filePath));
        }

        protected void EV_btnBrowseGameFolder()
        {
            //GetApp().GetApplication<CFMBOX>().Show("Coming soon!", "This feature isn't available in the current BETA-build but will likely be added in the future!", CFMBOXResult.Ok);

            string gameFolderPath = FnSystemDialogs.ShowFolderDialog("Choose game folder", false);

            if (!FnString.IsNullEmptyOrWhitespace(gameFolderPath))
            {
                // Select executable
                SearchExecutables(gameFolderPath);

                GetView().txbGameFolder.Text = gameFolderPath;
            }
        }

        protected async Task EV_btnSteamGridDb()
        {
            // Check SteamGridDB API key
            if (FnString.IsNullEmptyOrWhitespace(AppEnvironment.GetAppConfig().AppSettings.SteamGridDbKey))
            {
                CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
                cfmbox.Show(GetErrorMessage("GLO0015", "SteamGridDB"), CFMBOXResult.Ok, CFMBOXIcon.Info);
                return;
            }

            // Check Steam profile link or game name
            long steamAppId = FnLauncher.GetSteamAppId(GetApp().T1Profi);

            if (steamAppId == 0 && FnString.IsNullEmptyOrWhitespace(GetView().txbProfileName.Text))
            {
                CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
                cfmbox.Show(GetErrorMessage("PRO0003"), CFMBOXResult.Ok, CFMBOXIcon.Info);
                return;
            }

            ProfilesSteamGridDBApp app = GetApp().GetApplication<ProfilesSteamGridDBApp>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
            app.Search(GetApp(), steamAppId, GetView().txbProfileName.Text, r =>
            {
                if (!r.Canceled)
                    Task.Run(() => ApplyCoverAsync(r.SelectedImagePath));
            });
        }
        protected void EV_btnUnlinkLauncher()
        {
            FnLauncher.Unlink(GetApp().T1Profi);
        }

        protected async Task EV_btnBrowseLocalImage()
        {
            string filePath = FnSystemDialogs.ShowFileDialog("Choose local image", "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp", false);

            if (!FnString.IsNullEmptyOrWhitespace(filePath))
            {
                BitmapImage chosenImage = FnImage.LoadImageWithoutLock(filePath);

                ProfilesCropImageApp app = GetApp().GetApplication<ProfilesCropImageApp>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
                app.Crop(chosenImage, r =>
                {
                    if (!r.Canceled)
                        Task.Run(() => ApplyCoverImageAsync(r.CroppedImage!));
                });
            }
        }

        protected async Task EV_btnUsePlaceholderCover()
        {
            BitmapImage placeholderCover = CFProfilesApp.LoadPlaceholderCover();

            await Task.Run(() => ApplyCoverImageAsync(placeholderCover));
        }

        /// <summary>
        /// Saves the image as cover to the temp cover folder and applies it (see ApplyCoverAsync).
        /// </summary>
        private async Task ApplyCoverImageAsync(BitmapImage image)
        {
            (string path, string fileName) tempCover = await FnSystem.SaveCroppedImageTempPath(image, GetApp().Loader);

            await ApplyCoverAsync(tempCover.path);
        }

        /// <summary>
        /// Sets the cover file as new profile cover (file name, accent colors; copied on save) and refreshes the view.
        /// </summary>
        private async Task ApplyCoverAsync(string path)
        {
            using (SuppressRunEventPipeline())
                await FillViewCoverChanged(path);

            GetView().Dispatcher.Invoke(() =>
            { RunEventPipelineSync(View, string.Empty); });
        }

        protected void EV_btnManualEstTimes()
        {

            if (GetApp().ManualEstTimesCache.estTimeMain == 0 && GetApp().ManualEstTimesCache.estTimeMainExtra == 0 && GetApp().ManualEstTimesCache.estTimeCompletionist == 0)
                CFProfilesEditApp.OpenHowLongToBeatForGame(GetView().txbProfileName.Text);

            ShowManualEstTimesEditView();
        }

        protected void EV_btnAddPlatformWizard()
        {
            CodetablesEntryEditApp entryeditAppImages = GetApp().GetApplication<CodetablesEntryEditApp>("Create new image with wizard", UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
            entryeditAppImages.RunParameters.OverrideSaveButtonText = "Next";
            entryeditAppImages.RunParameters.ShowSkipButton = true;

            entryeditAppImages.CreateNew(r =>
            {
                if (r.Canceled)
                    return;

                CodetablesEntryEditApp entryEditAppPlatforms = GetApp().GetApplication<CodetablesEntryEditApp>("Create new platform with wizard", UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
                entryEditAppPlatforms.RunParameters.ShowSkipButton = true;
                entryEditAppPlatforms.CreateNew(r =>
                {
                    if (r.Canceled || r.Skipped)
                        return;

                    using (SuppressRunEventPipeline())
                    {
                        FillComboboxPlatformsManual();

                        if (GetView().cmbPlatform.Items.Count > 0)
                            GetView().cmbPlatform.SelectedValue = r.Platform;
                    }
                }, "pF");

            }, "iM");
        }

        protected void EV_btnSave()
        {
            GetViewReturn<ProfilesEditViewReturn>().Canceled = false;
            GetViewReturn<ProfilesEditViewReturn>().PFID = GetApp().T1Profi.PFID;

            if (_hasProfileCoverChanged)
            {
                CFProfilesEditApp.CopyProfileCoverToAppCoverFolder(_coverAppDataPath, _coverAppFolderFileName);

                try
                {
                    // Delete old image
                    File.Delete(Path.Combine(AppConfig.Storage.ProfileCoversDirectoryPath, _oldProfileCoverFileName));
                }
                catch (Exception)
                {
                }
            }

            Exit(true);
        }

        protected void EV_btnCancel()
        {
            Exit(true);
        }
    }
}
