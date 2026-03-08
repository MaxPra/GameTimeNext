namespace GameTimeNext.Core.Application.Profiles
{
    public class CFProfilesApp
    {
        public static string FormatGameTime(double gameTime)
        {
            if (gameTime == 0)
            {
                return "n.A.";
            }

            double gameTimeHours = gameTime / 60.0;
            double roundedHours = Math.Round(gameTimeHours, 1);

            return roundedHours.ToString("0.#") + "h";
        }

        public static string FormatFirstLastDate(DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
            {
                return "n.A.";
            }

            return dateTime.ToString("dd.MM.yyyy HH:mm");
        }
    }
}
