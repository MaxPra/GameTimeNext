using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.Profiles.Types;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Igdb;
using GameTimeNext.Core.Framework.LauncherIntegration;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.Utils;
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
    public class ProfilesEditViewController : UIXWindowControllerBase
    {

        private ProfilesEditViewModel? _profilesEditViewModel;

        private string _coverAppDataPath = string.Empty;
        private string _coverAppFolderFileName = string.Empty;
        private string _selectedSteamGridDBImagePath = string.Empty;
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

            _profilesEditViewModel = new ProfilesEditViewModel();
            ViewReturn = new ProfilesEditViewReturn();

            _hasProfileCoverChanged = false;
            _isEstTimeFilledManual = GetApp().T1Profi.ETML;

            GetApp().ManualEstTimesCache.estTimeMain = GetApp().T1Profi.ETMA / 60;
            GetApp().ManualEstTimesCache.estTimeMainExtra = GetApp().T1Profi.ETME / 60;
            GetApp().ManualEstTimesCache.estTimeCompletionist = GetApp().T1Profi.ETCO / 60;

            AddIdentifier("T1PROFI", GetApp().T1Profi);
        }

        protected override void BuildFirst()
        {
            BuildTagGrid(GetApp().T1Profi.PFID);

            // Enabled Steuerung Combobox IGDB Estimated Time
            bool isIGDBLinkedCorrectly = !FnString.IsNullEmptyOrWhitespace(AppEnvironment.GetAppConfig().AppSettings.TwitchIGDBClientID) &&
                !FnString.IsNullEmptyOrWhitespace(AppEnvironment.GetAppConfig().AppSettings.TwitchIGDBClientSecret);

            FnControls.SetEnabled(GetWnd().cmbEstimatedTimeType, isIGDBLinkedCorrectly);
        }

        protected override void Build()
        {
            BuildSteamRelatedSettings();
            BuildSteamLinkSection();
            BuildAccentColorSection();
        }

        protected override void Check()
        {
            // Profile Name
            if (FnString.IsNullEmptyOrWhitespace(GetWnd().txbProfileName.Text))
                AddViewError(GetWnd().txbProfileName, "Invalid input: Profile name");

            // Cover
            if (FnString.IsNullEmptyOrWhitespace(GetWnd().txbImagePath.Text))
                AddViewError(GetWnd().txbImagePath, "Invalid input: Profile cover");

            // Accent Colors
            if (GetWnd().cbUseProfileAccentColors.IsChecked == true
                && (GetWnd().tglAccent1.IsChecked == false && GetWnd().tglAccent2.IsChecked == false && GetWnd().tglAccent3.IsChecked == false))
                AddViewError(GetWnd().cbUseProfileAccentColors, "Invalid input: Accent colors are enabled, but no color was specified.");

            if (GetWnd().cbUseProfileAccentColors.IsChecked == false
                && (GetWnd().tglAccent1.IsChecked == true || GetWnd().tglAccent2.IsChecked == true || GetWnd().tglAccent3.IsChecked == true))
                AddViewError(GetWnd().cbUseProfileAccentColors, "Invalid input: Accent colors are disabled, but a color was specified.");
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected override void Event_Closing()
        {
            try { Directory.Delete(AppEnvironment.GetAppConfig().AppDataLocalPathSteamGridDBCovers, true); } catch { }
            try { Directory.Delete(AppEnvironment.GetAppConfig().AppDataLocalPathTempCovers, true); } catch { }


            Exit(false);
        }

        protected override void Event_Maximize()
        {
        }

        protected override void Event_Minimize()
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override async Task FillDBOImplAsync()
        {
            // Akzentfarben
            SaveAccentColors();

            // Erwartete Spielzeit speichern (Manuell)
            FillDBOManualEstTimes();

            // Erwartete Spielzeiten speichern
            _successfullyFilledEstTimes = await FillDBOEstimatedTimesIGDBAsync();

            // Profileinstellungen
            FillDBOProfileSettings();

            // Bild kopieren
            if (GetApp().T1Profi.HasFieldDataChanged(K1PROFI.Fields.PPFN))
                _hasProfileCoverChanged = true;
        }


        protected override void FillViewImpl()
        {
            if (GetWnd().ViewIndicator.Contains("ED"))
                FillViewSteamImport(null!);

            FillComboboxPlayTypes();

            FillViewProfileSettings();

            FillViewAccentColors();
        }

        private void FillComboboxPlayTypes()
        {
            _cmbTargetPlayTypeFilledCompletely = false;

            GetWnd().cmbEstimatedTimeType.Items.Clear();

            GetWnd().cmbEstimatedTimeType.Items.Add(new ComboBoxItem { Content = "(None)", Tag = EstimatedTimeTypes.EST_TIME_NONE });
            GetWnd().cmbEstimatedTimeType.Items.Add(new ComboBoxItem { Content = "Main Story", Tag = EstimatedTimeTypes.EST_TIME_MAIN });
            GetWnd().cmbEstimatedTimeType.Items.Add(new ComboBoxItem { Content = "Main Story + Extra", Tag = EstimatedTimeTypes.EST_TIME_MAIN_EXTRA });
            GetWnd().cmbEstimatedTimeType.Items.Add(new ComboBoxItem { Content = "Completionist", Tag = EstimatedTimeTypes.EST_TIME_COMPLETIONIST });

            _cmbTargetPlayTypeFilledCompletely = true;

            if (!FnString.IsNullEmptyOrWhitespace(GetApp().T1Profi.ETTY))
            {
                foreach (ComboBoxItem item in GetWnd().cmbEstimatedTimeType.Items)
                {
                    if (item.Tag?.ToString() == GetApp().T1Profi.ETTY)
                    {
                        GetWnd().cmbEstimatedTimeType.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        protected override void SaveDBOImpl()
        {
            // Tags
            FillDBOTags();

            if (GetWnd().ViewIndicator.Contains("CN"))
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

        private void FillViewSteamImport(SteamGame selectedGame)
        {

            if (GetApp().T1Profi.SAID == 0)
                return;

            // Profilname
            if (GetWnd().ViewIndicator.Contains("CN"))
            {
                GetWnd().txbProfileName.Text = selectedGame.Name;

                // Spielordner
                GetWnd().txbGameFolder.Text = SteamManifestHelper.ResolveInstallPath(selectedGame);
            }
            else
            {
                GetWnd().txbProfileName.Text = GetApp().T1Profi.GANA;

                // Spielordner
                GetWnd().txbGameFolder.Text = GetApp().T1Profi.EXGF;
            }
        }

        private void FillViewProfileSettings()
        {
            // Profileinstellungen laden
            CProfileSettings cProfileSettings = new CProfileSettings(GetApp().T1Profi.PRSE).Dezerialize();

            GetWnd().cbEnableHdrOnStart.IsChecked = cProfileSettings.HDREnabled == true;

            GetWnd().txbSteamArgs.Text = cProfileSettings.SteamGameArgs.Replace(";", " ");
        }

        private void FillViewAccentColors()
        {
            if (FnString.IsNullEmptyOrWhitespace(_selectedSteamGridDBImagePath) && GetWnd().ViewIndicator.Contains("ED"))
            {
                // Aus T1PROFI lesen
                CAccentColorsInit cAccentColorsInit = new CAccentColorsInit(GetApp().T1Profi.ACIN);
                cAccentColorsInit = cAccentColorsInit.Dezerialize();

                CAccentColors cAccentColors = new CAccentColors(GetApp().T1Profi.ACCO);
                cAccentColors = cAccentColors.Dezerialize();

                Color color1 = (Color)ColorConverter.ConvertFromString(cAccentColorsInit.AccentColors.ElementAt(0).Key);
                Color color2 = (Color)ColorConverter.ConvertFromString(cAccentColorsInit.AccentColors.ElementAt(1).Key);
                Color color3 = (Color)ColorConverter.ConvertFromString(cAccentColorsInit.AccentColors.ElementAt(2).Key);

                GetWnd().tglAccent1.Background = new SolidColorBrush(color1);
                GetWnd().tglAccent1.Tag = color1;
                GetWnd().tglAccent1.IsChecked = cAccentColorsInit.AccentColors.ElementAt(0).Value;

                GetWnd().tglAccent2.Background = new SolidColorBrush(color2);
                GetWnd().tglAccent2.Tag = color2;
                GetWnd().tglAccent2.IsChecked = cAccentColorsInit.AccentColors.ElementAt(1).Value;

                GetWnd().tglAccent3.Background = new SolidColorBrush(color3);
                GetWnd().tglAccent3.Tag = color3;
                GetWnd().tglAccent3.IsChecked = cAccentColorsInit.AccentColors.ElementAt(2).Value;
            }
        }

        private async Task FillViewCoverChanged(string path)
        {

            _coverAppFolderFileName = CFProfilesEditApp.GetGUIDCoverName("jpg");

            GetWnd().Dispatcher.Invoke(() =>
            {
                GetWnd().txbImagePath.Text = AppEnvironment.GetAppConfig().CoverFolderPath + System.IO.Path.DirectorySeparatorChar + _coverAppFolderFileName;
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
                    // Später hier Logausgabe und so weiter
                }
                finally
                {
                    GetApp().Loader.Stop();
                }
            });

            if (accentColors.Count == 3)
            {

                GetWnd().Dispatcher.Invoke(() =>
                {
                    // Accent Farben befüllen
                    Color color1 = Color.FromArgb(255, accentColors[0].R, accentColors[0].G, accentColors[0].B);
                    Color color2 = Color.FromArgb(255, accentColors[1].R, accentColors[1].G, accentColors[1].B);
                    Color color3 = Color.FromArgb(255, accentColors[2].R, accentColors[2].G, accentColors[2].B);

                    GetWnd().tglAccent1.Background = new SolidColorBrush(color1);
                    GetWnd().tglAccent1.Tag = color1;

                    GetWnd().tglAccent2.Background = new SolidColorBrush(color2);
                    GetWnd().tglAccent2.Tag = color2;

                    GetWnd().tglAccent3.Background = new SolidColorBrush(color3);
                    GetWnd().tglAccent3.Tag = color3;
                });
            }

            // Pfad für späteres Kopieren merken
            _coverAppDataPath = path;
        }

        private void FillDBOSteamImport(SteamGame selectedGame)
        {
            // Steam App Id setzen --> Ausnahme direkt am Table Object
            GetApp().T1Profi.SAID = selectedGame.AppId;

            GetApp().T1Profi.EXEC = string.Empty;
        }

        private void FillDBOManualEstTimes()
        {
            ComboBoxItem combItem = GetWnd().cmbEstimatedTimeType.SelectedItem as ComboBoxItem;

            GetApp().T1Profi.ETML = _isEstTimeFilledManual;
            GetApp().T1Profi.ETMA = GetApp().ManualEstTimesCache.estTimeMain * 60;
            GetApp().T1Profi.ETME = GetApp().ManualEstTimesCache.estTimeMainExtra * 60;
            GetApp().T1Profi.ETCO = GetApp().ManualEstTimesCache.estTimeCompletionist * 60;

            GetApp().T1Profi.ETTY = combItem.Tag.ToString();
        }

        private void FillDBOTags()
        {
            // selektierte Gruppen auslesen
            List<T1GROUP> selectedGroups = _profilesEditViewModel!.T1GROUPs.Where(t => t.IsSelected == true).ToList();

            // Alle bisherigen für dieses Profil löschen
            TXGRPPO tblmGrppo = new TXGRPPO();
            tblmGrppo.DeleteAllWherePFID(GetApp().T1Profi.PFID);

            foreach (T1GROUP grp in selectedGroups)
            {
                T1GRPPO tblGrppo = tblmGrppo.CreateNew();
                tblGrppo.GRID = grp.GRID;
                tblGrppo.PFID = GetApp().T1Profi.PFID;
                tblmGrppo.Save(tblGrppo);
            }
        }

        private void FillDBOProfileSettings()
        {
            CProfileSettings cProfileSettings = new CProfileSettings();

            cProfileSettings.HDREnabled = GetWnd().cbEnableHdrOnStart.IsChecked == true;
            cProfileSettings.SteamGameArgs = GetWnd().txbSteamArgs.Text.Replace(" ", ";");

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

            if (!_cmbTargetPlayTypeFilledCompletely || _isEstTimeFilledManual)
                return false;

            ComboBoxItem combItem = GetWnd().cmbEstimatedTimeType.SelectedItem as ComboBoxItem;

            if (GetApp().T1Profi.SAID == 0 && FnString.IsNullEmptyOrWhitespace(GetWnd().txbProfileName.Text) || combItem.Tag.ToString() == EstimatedTimeTypes.EST_TIME_NONE)
            {
                // -- Befüllung
                GetApp().T1Profi.ETMA = 0;
                GetApp().T1Profi.ETME = 0;
                GetApp().T1Profi.ETCO = 0;

                GetApp().T1Profi.ETTY = EstimatedTimeTypes.EST_TIME_NONE;
                return true;
            }

            GetApp().Loader.Begin();

            string clientId = AppEnvironment.GetAppConfig().AppSettings.TwitchIGDBClientID;

            IgdbService igdbservice = new IgdbService(clientId, AppEnvironment.TwitchAuthenticationToken);

            int? gameId = null;

            if (GetApp().T1Profi.SAID != 0)
            {
                int steamSourceId = igdbservice.GetSteamSourceId();
                gameId = await igdbservice.FindGameIdBySteamAppIdAsync(steamSourceId, GetApp().T1Profi.SAID.ToString());
            }

            if (gameId == null)
            {
                var game = await igdbservice.FindGameByNameAsync(GetApp().T1Profi.GANA);

                gameId = game?.Id;
            }

            if (gameId == null)
            {
                GetWnd().Dispatcher.Invoke(() =>
                {
                    GetApp().GetApplication<CFMBOX>().Show("Warning", "Could not find game on IGDB.", CFMBOXResult.Ok, CFMBOXIcon.Warning);
                });

                ResetPlaytypes();

                GetApp().Loader.Stop();

                return false;
            }

            var timeToBeat = await igdbservice.GetGameTimeToBeatAsync(gameId!.Value);

            if (timeToBeat == null)
            {
                GetWnd().Dispatcher.Invoke(() =>
                {
                    GetApp().GetApplication<CFMBOX>().Show("Warning", "Could not query time to beat.", CFMBOXResult.Ok, CFMBOXIcon.Warning);
                });

                ResetPlaytypes();

                GetApp().Loader.Stop();
                return false;
            }

            if (timeToBeat.Hastily == null || timeToBeat.Normally == null || timeToBeat.Completely == null)
            {
                GetWnd().Dispatcher.Invoke(() =>
                {
                    GetApp().GetApplication<CFMBOX>().Show("Warning", "Could not query time to beat.", CFMBOXResult.Ok, CFMBOXIcon.Warning);
                });

                ResetPlaytypes();

                GetApp().Loader.Stop();
                return false;
            }

            // -- Befüllung
            GetApp().T1Profi.ETMA = ((double)timeToBeat.Hastily!) / 60;
            GetApp().T1Profi.ETME = ((double)timeToBeat.Normally!) / 60;
            GetApp().T1Profi.ETCO = ((double)timeToBeat.Completely!) / 60;

            GetApp().T1Profi.ETTY = combItem.Tag.ToString();

            GetApp().Loader.Stop();

            return true;

        }

        private void BuildSteamLinkSection()
        {
            // Sichtbarkeitssteuerung Steam Linked Sektion
            if (GetApp().T1Profi.SAID == 0)
            {
                GetWnd().pnlSteamLinkState.Visibility = Visibility.Collapsed;
                GetWnd().pnlSteamSettings.Visibility = Visibility.Collapsed;
            }
            else
            {

                GetWnd().pnlSteamLinkState.Visibility = Visibility.Visible;
                GetWnd().pnlSteamSettings.Visibility = Visibility.Visible;
            }
        }

        private void BuildAccentColorSection()
        {
            // Sichtbarkeitssteuerung AccentColors
            if (!FnString.IsNullEmptyOrWhitespace(GetWnd().txbImagePath.Text))
            {
                GetWnd().grdAccentColorSettings.Visibility = Visibility.Visible;
            }
            else
            {
                GetWnd().grdAccentColorSettings.Visibility = Visibility.Collapsed;
            }
        }

        private void BuildSteamRelatedSettings()
        {
            if (GetApp().T1Profi.SAID == 0)
            {
                GetWnd().pnlSteamSettings.Visibility = Visibility.Collapsed;
            }
            else
            {
                GetWnd().pnlSteamSettings.Visibility = Visibility.Visible;
            }
        }

        private void BuildTagGrid(long pfid)
        {
            TXGROUP TXGROUP = new TXGROUP();

            List<T1GROUP> t1groups = new List<T1GROUP>();

            UIXQuery query = BuildTagGridQuery(pfid);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {

                    long grid = UIXQuery.GetInt64(reader, K1GROUP.Name, K1GROUP.Fields.GRID);
                    long gpid = UIXQuery.GetInt64(reader, K1GRPPO.Name, K1GRPPO.Fields.GPID);

                    T1GROUP t1group = new TXGROUP().Read(grid);

                    if (gpid > 0)
                        t1group.IsSelected = true;

                    t1groups.Add(t1group);
                }
            }

            // Filtern
            t1groups = t1groups.Where(s => s.GTYP == GroupType.Tag).ToList();

            // Viewmodel befüllen
            _profilesEditViewModel = new ProfilesEditViewModel();
            _profilesEditViewModel.T1GROUPs = new System.Collections.ObjectModel.ObservableCollection<T1GROUP>(t1groups);


            if (t1groups != null && t1groups.Count > 0)
                _profilesEditViewModel.SelectedTBLGROUP = t1groups.FirstOrDefault(p => p.IsSelected == true);

            View.DataContext = _profilesEditViewModel;
        }

        private UIXQuery BuildTagGridQuery(long pfid)
        {
            UIXQuery query = new UIXQuery(K1GROUP.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            // Felder
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
            if (GetWnd().cbUseProfileAccentColors.IsChecked == false)
                return;

            // -- Akzent farben (korrespondierend aus der gewählten)
            string[] accentColorsCalculated = FnTheme.CalculateAccentStateColors(CFProfilesEditApp.GetSelectedToggleButton(GetWnd()).Tag.ToString());

            Dictionary<string, string> accentColorsDic = new Dictionary<string, string>();

            accentColorsDic.Add("accent", accentColorsCalculated[0]);
            accentColorsDic.Add("hover", accentColorsCalculated[1]);
            accentColorsDic.Add("pressed", accentColorsCalculated[2]);

            CAccentColors cAccentColors = new CAccentColors();
            cAccentColors.AccentColors = accentColorsDic;

            GetApp().T1Profi.ACCO = cAccentColors.Serialize();

            // -- Automatisch ermittelte Akzentfarben abspeichern
            string[] accentColorsInit = new string[3];

            accentColorsInit[0] = GetWnd().tglAccent1.Tag.ToString()!;
            accentColorsInit[1] = GetWnd().tglAccent2.Tag.ToString()!;
            accentColorsInit[2] = GetWnd().tglAccent3.Tag.ToString()!;

            Dictionary<string, bool> dicAccentColorsInit = new Dictionary<string, bool>();
            dicAccentColorsInit.Add(accentColorsInit[0], GetWnd().tglAccent1.IsChecked == true);
            dicAccentColorsInit.Add(accentColorsInit[1], GetWnd().tglAccent2.IsChecked == true);
            dicAccentColorsInit.Add(accentColorsInit[2], GetWnd().tglAccent3.IsChecked == true);

            CAccentColorsInit cAccentColorsInit = new CAccentColorsInit();
            cAccentColorsInit.AccentColors = dicAccentColorsInit;

            GetApp().T1Profi.ACIN = cAccentColorsInit.Serialize();
        }

        private void ToggleAccentChanged(FrameworkElement toggleChecked)
        {
            if (toggleChecked == GetWnd().tglAccent1)
            {
                GetWnd().tglAccent2.IsChecked = false;
                GetWnd().tglAccent3.IsChecked = false;
            }
            else if (toggleChecked == GetWnd().tglAccent2)
            {
                GetWnd().tglAccent1.IsChecked = false;
                GetWnd().tglAccent3.IsChecked = false;
            }
            else if (toggleChecked == GetWnd().tglAccent3)
            {
                GetWnd().tglAccent1.IsChecked = false;
                GetWnd().tglAccent2.IsChecked = false;
            }
        }

        private void ResetPlaytypes()
        {
            GetWnd().cmbEstimatedTimeType.SelectedIndex = 0;

            FillDBO();
        }

        private void ShowManualEstTimesEditView()
        {
            GetApp().ManualEstTimesCache.ProfileName = GetWnd().txbProfileName.Text;

            GetApp().ProfilesManualEstTimesEditView = new ProfilesManualEstTimesEditView();
            GetApp().ProfilesManualEstTimesEditView.WndController = new ProfilesManualEstTimesEditViewController(GetApp());

            GetApp().ProfilesManualEstTimesEditView.WndController.SetResultCallback<ProfilesManualEstTimesEditViewReturn>(r =>
            {
                if (!r.Canceled)
                {
                    _isEstTimeFilledManual = true;

                    GetApp().ManualEstTimesCache.estTimeMain = r.estTimeMain;
                    GetApp().ManualEstTimesCache.estTimeMainExtra = r.estTimeMainExtra;
                    GetApp().ManualEstTimesCache.estTimeCompletionist = r.estTimeCompletionist;
                }
            });

            GetApp().ProfilesManualEstTimesEditView.WndController.Show(true);
        }

        public ProfilesEditApp GetApp()
        {
            return (ProfilesEditApp)App;
        }

        public ProfilesEditView GetWnd()
        {
            return (ProfilesEditView)View;
        }

        protected void EV_BtnSteamImport()
        {
            ProfilesSteamImportApp app = GetApp().GetApplication<ProfilesSteamImportApp>();
            app.Search(r =>
            {
                if (!r.Canceled)
                {

                    FillDBOSteamImport(r.SteamGame!);

                    // Exe auswählen
                    ProfilesExecutablesEditApp profilesExecutablesEditApp = GetApp().GetApplication<ProfilesExecutablesEditApp>();
                    profilesExecutablesEditApp.Search(SteamManifestHelper.ResolveInstallPath(r.SteamGame!), r =>
                    {
                        if (!r.Canceled)
                        {
                            FillDBOExecutables(r.SelectedExecutables);
                        }

                    });

                    FillViewSteamImport(r.SteamGame!);
                    Build();
                }
            });
        }

        protected void EV_btnBrowseGameFolder()
        {
            //GetApp().GetApplication<CFMBOX>().Show("Coming soon!", "This feature isn't available in the current BETA-build but will likely be added in the future!", CFMBOXResult.Ok);

            string gameFolderPath = FnSystemDialogs.ShowFolderDialog("Choose game folder", false);

            if (!FnString.IsNullEmptyOrWhitespace(gameFolderPath))
            {
                // Exe auswählen
                ProfilesExecutablesEditApp profilesExecutablesEditApp = GetApp().GetApplication<ProfilesExecutablesEditApp>();
                profilesExecutablesEditApp.Search(gameFolderPath, r =>
                {
                    if (!r.Canceled)
                    {
                        FillDBOExecutables(r.SelectedExecutables);
                    }

                });

                GetWnd().txbGameFolder.Text = gameFolderPath;
            }
        }

        protected async Task EV_btnSteamGridDb()
        {
            // SteamGridDb API Key prüfen
            if (FnString.IsNullEmptyOrWhitespace(AppEnvironment.GetAppConfig().AppSettings.SteamGridDbKey))
            {
                CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>();
                cfmbox.Show("No API Key found!", "No API key was found.\nPlease make sure that the API key is stored in the settings.", CFMBOXResult.Ok, CFMBOXIcon.Info);
                return;
            }

            // Steamprofilverknüpfung oder Spielnamen prüfen
            if (GetApp().T1Profi.SAID == 0 && FnString.IsNullEmptyOrWhitespace(GetWnd().txbProfileName.Text))
            {
                CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>();
                cfmbox.Show("Attention!", "Please note that the SteamGridDB search is only available if a profile name is provided or the profile is linked to a Steam game.", CFMBOXResult.Ok, CFMBOXIcon.Info);
                return;
            }

            ProfilesSteamGridDBApp app = GetApp().GetApplication<ProfilesSteamGridDBApp>();
            app.Search(GetApp(), GetApp().T1Profi.SAID, GetWnd().txbProfileName.Text, r =>
            {
                if (!r.Canceled)
                {
                    Task.Run(async () =>
                    {
                        await FillViewCoverChanged(r.SelectedImagePath);
                    });


                    RunEventPipelineSync(View, string.Empty);

                }
            });
        }
        protected void EV_btnUnlinkSteamProfile()
        {
            GetApp().T1Profi.SAID = 0;
        }

        protected async Task EV_btnBrowseLocalImage()
        {
            string filePath = FnSystemDialogs.ShowFileDialog("Choose local image", "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp", false);

            if (!FnString.IsNullEmptyOrWhitespace(filePath))
            {
                BitmapImage chosenImage = FnImage.LoadImageWithoutLock(filePath);

                ProfilesCropImageApp app = GetApp().GetApplication<ProfilesCropImageApp>();
                app.Crop(chosenImage, r =>
                {
                    if (!r.Canceled)
                    {
                        (string path, string fileName) tuple = (string.Empty, string.Empty);

                        Task.Run(async () =>
                        {
                            tuple = await FnSystem.SaveCroppedImageTempPath(r.CroppedImage!, GetApp().Loader);

                            await FillViewCoverChanged(tuple.path);
                        });

                        RunEventPipelineSync(View, string.Empty);
                    }
                });
            }
        }

        protected void EV_btnManualEstTimes()
        {

            if (GetApp().ManualEstTimesCache.estTimeMain == 0 && GetApp().ManualEstTimesCache.estTimeMainExtra == 0 && GetApp().ManualEstTimesCache.estTimeCompletionist == 0)
                CFProfilesEditApp.OpenHowLongToBeatForGame(GetWnd().txbProfileName.Text);

            ShowManualEstTimesEditView();
        }

        protected void EV_btnSave()
        {
            GetViewReturn<ProfilesEditViewReturn>().Canceled = false;
            GetViewReturn<ProfilesEditViewReturn>().PFID = GetApp().T1Profi.PFID;

            if (_hasProfileCoverChanged)
                CFProfilesEditApp.CopyProfileCoverToAppCoverFolder(_coverAppDataPath, _coverAppFolderFileName);

            Exit(true);
        }

        protected void EV_btnCancel()
        {
            Exit(true);
        }
    }
}