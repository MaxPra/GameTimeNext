using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.UI;
using GameTimeNext.Core.Framework.UI.Dialogs;

namespace GameTimeNext.Core.Application.Profiles
{
    public class CFProfilesApp
    {
        public static string FormatGameTimeHours(double gameTime)
        {

            string returnedFormat = "";

            if (gameTime == 0)
            {
                return "n.A.";
            }

            double gameTimeHours = gameTime / 60.0;
            double roundedHours = Math.Round(gameTimeHours, 1);

            returnedFormat = roundedHours.ToString("0.#") + " h";

            if (roundedHours < 1)
                returnedFormat = "< 1 h";

            return returnedFormat;
        }

        public static string FormatGameTimeMinutes(double gameTime)
        {
            string returnedFormat = "";

            if (gameTime == 0)
            {
                return "n.A.";
            }

            double gameTimeMinutes = gameTime;
            double roundedMinutes = Math.Round(gameTimeMinutes, 1);

            returnedFormat = roundedMinutes.ToString("0") + " min";

            return returnedFormat;
        }

        public static string FormatFirstLastDate(DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
            {
                return "n.A.";
            }

            return dateTime.ToString("dd.MM.yyyy HH:mm");
        }

        public static bool AskForNewPlaythroughCreationIfNotActive(long pfid)
        {
            bool resultNewPlaythrough = false;

            if (TFPLTHR.GetCurrentPlaythrough(pfid) != null)
                return true;



            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {

                ToastMessage tm = new ToastMessage("Attention!", "GameTimeNext needs your attention!");
                tm.Show();

                CFMBOX cfmbox = new CFMBOX();
                CFMBOXResult result = cfmbox.Show("Question", "No active playthrough is set for this profile.\nCreate a new playthrough?", CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

                if (result == CFMBOXResult.Yes)
                {
                    long nextNumber = TFPLTHR.GetCurrentPlaythroughCount(pfid, PlaythroughType.NEW_PLAYTHROUGH) + 1;
                    TFPLTHR.CreateNewPlaythrough(pfid, "Playthrough #" + nextNumber, PlaythroughType.NEW_PLAYTHROUGH);

                    resultNewPlaythrough = true;
                }
            });

            return resultNewPlaythrough;
        }
    }
}
