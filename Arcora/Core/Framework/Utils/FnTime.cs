namespace GameTimeNext.Core.Framework.Utils
{
    public static class FnTime
    {
        /// <summary>
        /// Placeholder for values that do not exist (e.g. never played, no estimate set).
        /// </summary>
        public const string NoValue = "-";

        /// <summary>
        /// Formats a duration in minutes as "##h ##m" (e.g. "05h 07m").
        /// A duration of zero or less is shown as "00h 00m", unless <paramref name="zeroText"/> is given.
        /// Pass <see cref="NoValue"/> as <paramref name="zeroText"/> where zero means "no data" rather than "no time".
        /// </summary>
        /// <param name="minutes"></param>
        /// <param name="zeroText"></param>
        /// <returns></returns>
        public static string FormatMinutesAsHoursAndMinutes(double minutes, string? zeroText = null)
        {
            int totalMinutes = (int)Math.Round(Math.Max(minutes, 0), MidpointRounding.AwayFromZero);

            if (totalMinutes == 0 && zeroText != null)
                return zeroText;

            int hours = totalMinutes / 60;
            int remainingMinutes = totalMinutes % 60;

            return $"{hours:00}h {remainingMinutes:00}m";
        }
    }
}
