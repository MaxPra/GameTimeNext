using System.Globalization;

namespace GameTimeNext.Core.Application.BigPicture
{
    public static class CFBigPictureApp
    {
        /// <summary>
        /// Formats when a game was played last, readable from the couch: "Today", "Yesterday", "3 days ago", otherwise the date.
        /// Empty if it was never played.
        /// </summary>
        public static string FormatLastPlayed(DateTime lastPlayed)
        {
            if (lastPlayed == DateTime.MinValue)
                return string.Empty;

            int days = (DateTime.Today - lastPlayed.Date).Days;

            return days switch
            {
                <= 0 => "Played today",
                1 => "Played yesterday",
                < 7 => string.Format(CultureInfo.CurrentCulture, "Played {0} days ago", days),
                _ => "Last played " + lastPlayed.ToString("d", CultureInfo.CurrentCulture)
            };
        }
    }
}
