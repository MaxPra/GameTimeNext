using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.LauncherIntegration;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    public class ProfilesEditViewController : UIXWindowControllerBase
    {

        private ProfilesEditViewModel? _profilesEditViewModel;

        private string _coverAppDataPath = string.Empty;
        private string _coverAppFolderFileName = string.Empty;

        public ProfilesEditViewController(UIXApplication app) : base(app)
        {
        }

        protected override void Init()
        {

            _profilesEditViewModel = new ProfilesEditViewModel();

            AddIdentifier("TBL_PROFI", GetApp().TblProfi);
        }

        protected override void Build()
        {
            // Sichtbarkeitssteuerung Steam Linked Sektion
            if (GetApp().TblProfi.SAID == 0)
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

        protected override void BuildFirst()
        {
            BuildTagGrid();
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
        }

        protected override void Event_Maximize()
        {
        }

        protected override void Event_Minimize()
        {
        }

        protected override void FillDBOImpl()
        {
            // Akzentfarben
            HandleAccentColors();
        }

        protected override void FillViewImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
            // Bild kopieren
            CFProfilesEditApp.CopyProfileCoverToAppCoverFolder(_coverAppDataPath, _coverAppFolderFileName);
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
            if (source is ToggleButton && source.Name.StartsWith("tglAccent") && eventName == UIXEventNames.ToggleButton.Checked)
            {
                ToggleAccentChanged(source);
            }
        }

        protected void EV_BtnSteamImport()
        {
            ProfilesSteamImportApp app = new ProfilesSteamImportApp();
            app.Search(r =>
            {
                if (!r.Canceled)
                {
                    FillDBOSteamImport(r.SteamGame!);
                    FillViewSteamImport(r.SteamGame!);
                    Build();
                }
            });
        }

        protected async Task EV_btnSteamGridDb()
        {
            // SteamGridDb API Key prüfen
            if (FnString.IsNullEmptyOrWhitespace(AppEnvironment.GetAppConfig().SteamGridDbAPIKey))
            {
                CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>();
                cfmbox.Show("No API Key found!", "No API key was found.\nPlease make sure that the API key is stored in the settings.", CFMBOXResult.Ok, CFMBOXIcon.Info);
                return;
            }

            // Steamprofilverknüpfung prüfen
            if (GetApp().TblProfi.SAID == 0)
            {
                CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>();
                cfmbox.Show("No Steam profile has been linked!", "Please note that the SteamGridDB cover selection is only available if a Steam profile is linked to a GameTimeNext profile.", CFMBOXResult.Ok, CFMBOXIcon.Info);
                return;
            }

            ProfilesSteamGridDBApp app = GetApp().GetApplication<ProfilesSteamGridDBApp>();
            app.Search(GetApp(), GetApp().TblProfi.SAID, r =>
            {
                if (!r.Canceled)
                {
                    Task.Run(async () =>
                    {
                        await FillViewSteamGridDBCoverChanged(r.SelectedImagePath);
                    });
                }
            });
        }

        protected void EV_btnUnlinkSteamProfile()
        {
            GetApp().TblProfi.SAID = 0;
        }

        private void FillViewSteamImport(SteamGame selectedGame)
        {
            // Textblock von Steam Link Sektion
            GetWnd().txtSteamLinkGame.Text = selectedGame.Name;

            // Profilname
            GetWnd().txbProfileName.Text = selectedGame.Name;

            // Spielordner
            GetWnd().txbGameFolder.Text = SteamManifestHelper.ResolveInstallPath(selectedGame);
        }

        private void CheckSteamRelatedFields()
        {
            if (GetApp().TblProfi.SAID == 0)
                return;
        }

        private void HandleAccentColors()
        {
            if (GetWnd().cbUseProfileAccentColors.IsChecked == false)
                return;

            // Akzent farben
            string[] accentColorsCalculated = FnTheme.CalculateAccentStateColors(CFProfilesEditApp.GetSelectedToggleButton(GetWnd()).Tag.ToString());

            Dictionary<string, string> accentColorsDic = new Dictionary<string, string>();

            accentColorsDic.Add("accent", accentColorsCalculated[0]);
            accentColorsDic.Add("hover", accentColorsCalculated[1]);
            accentColorsDic.Add("pressed", accentColorsCalculated[2]);

            CAccentColors cAccentColors = new CAccentColors();
            cAccentColors.AccentColors = accentColorsDic;

            GetApp().TblProfi.ACCO = cAccentColors.Serialize();
        }

        private async Task FillViewSteamGridDBCoverChanged(string path)
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
            GetApp().TblProfi.SAID = selectedGame.AppId;
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

        private void BuildTagGrid()
        {
            TBLM_GROUP tblm_group = new TBLM_GROUP();

            List<TBL_GROUP> states = new List<TBL_GROUP>();
            List<TBL_GROUP> tbl_groups = tblm_group.ReadAll();

            // Filtern
            tbl_groups = tbl_groups.Where(s => s.GTYP == GroupType.Tag).ToList();

            // Viewmodel befüllen
            _profilesEditViewModel = new ProfilesEditViewModel();
            _profilesEditViewModel.Tbl_Groups = new System.Collections.ObjectModel.ObservableCollection<TBL_GROUP>(tbl_groups);


            if (tbl_groups != null && tbl_groups.Count > 0)
                _profilesEditViewModel.SelectedTBLGROUP = tbl_groups.FirstOrDefault(p => p.IsSelected == true);

            View.DataContext = _profilesEditViewModel;
        }

        private ProfilesEditApp GetApp()
        {
            return (ProfilesEditApp)App;
        }

        private ProfilesEditView GetWnd()
        {
            return (ProfilesEditView)View;
        }
    }
}
