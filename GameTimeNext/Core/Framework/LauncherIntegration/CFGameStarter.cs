using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.TableObjects;

namespace GameTimeNext.Core.Framework.LauncherIntegration
{
    public class CFGameStarter
    {
        public static void ActivateProfileSettings(CProfileSettings cProfileSettings)
        {
            // Wenn HDR im Profil aktiviert --> HDR aktivieren
            if (cProfileSettings.HDREnabled)
            {
                CFHDR.SetHdrForAllActiveDisplays(true);
            }
        }

        public static void DeactivateProfileSettings(CProfileSettings cProfileSettings)
        {
            // Wenn HDR im Profil aktiviert --> HDR aktivieren
            if (cProfileSettings.HDREnabled)
            {
                CFHDR.SetHdrForAllActiveDisplays(false);
            }
        }

        public static void DeactivateProfileSettings(long pfid)
        {
            if (pfid == 0)
                return;

            T1PROFI t1profi = new TXPROFI().Read(pfid);

            if (t1profi == null)
                return;

            CProfileSettings cProfileSettings = new CProfileSettings(t1profi.PRSE).Dezerialize();

            DeactivateProfileSettings(cProfileSettings);
        }
    }
}
