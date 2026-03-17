using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.Utils;

namespace GameTimeNext.Core.Framework.LauncherIntegration
{
    public class CFSteamGameStarter
    {
        /// <summary>
        /// Startet ein Steam-Spiel anhand der ID
        /// </summary>
        /// <param name="steamAppID"></param>
        public static async void StartSteamGame(string steamAppID, long pfid)
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
                CFMBOX cfmbox = new CFMBOX();
                cfmbox.Show("Info", "Steam could not be found!", CFMBOXResult.Ok, CFMBOXIcon.Info);
            }
        }
    }
}
