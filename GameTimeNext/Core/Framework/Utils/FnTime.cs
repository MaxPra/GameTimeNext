namespace GameTimeNext.Core.Framework.Utils
{
    public static class FnTime
    {
        /// <summary>
        /// Formats a duration in minutes as "##h ##m" (e.g. "05h 07m").
        /// Returns "n.A." for durations of zero or less.
        /// </summary>
        /// <param name="minutes"></param>
        /// <returns></returns>
        public static string FormatMinutesAsHoursAndMinutes(double minutes)
        {
            if (minutes <= 0)
                return "n.A.";

            int totalMinutes = (int)Math.Round(minutes, MidpointRounding.AwayFromZero);
            int hours = totalMinutes / 60;
            int remainingMinutes = totalMinutes % 60;

            return $"{hours:00}h {remainingMinutes:00}m";
        }
    }
}
