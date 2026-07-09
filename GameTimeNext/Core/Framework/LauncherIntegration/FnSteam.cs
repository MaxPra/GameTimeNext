using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.Utils;
using System.Diagnostics;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Framework.LauncherIntegration
{
    public class FnSteam
    {
        /// <summary>
        /// Startet ein Steam-Spiel anhand der ID
        /// </summary>
        /// <param name="steamAppID"></param>
        public static async void StartSteamGame(string steamAppID, long pfid, UIXApplication app)
        {
            if (SteamLocatorService.IsGameInstalledByAppId(steamAppID))
            {

                T1PROFI t1profi = new TXPROFI().Read(pfid);

                if (t1profi == null)
                    return;

                // Profileinstellungen laden
                CProfileSettings cProfileSettings = new CProfileSettings(t1profi.PRSE).Dezerialize();

                // Zuvor Profileinstellungen aktivieren
                CFGameStarter.ActivateProfileSettings(cProfileSettings);

                await Task.Delay(5000);

                SteamGameStarter steamGameStarter = new SteamGameStarter(FnConvert.ToList(cProfileSettings.SteamGameArgs), steamAppID);
                steamGameStarter.StartGame();

            }
            else
            {
                CFMBOX cfmbox = app.GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
                cfmbox.Show("Info", "Steam could not be found!", CFMBOXResult.Ok, CFMBOXIcon.Info);
            }
        }

        /// <summary>
        /// Opens the steam library page and selects the game with the given appID.
        /// </summary>
        /// <param name="steamAppID"></param>
        public static void OpenSteamLibrary(long steamAppID)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"steam://nav/games/details/{steamAppID}",
                UseShellExecute = true
            });
        }
    }
}
