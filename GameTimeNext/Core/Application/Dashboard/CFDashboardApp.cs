namespace GameTimeNext.Core.Application.Dashboard
{
    public class CFDashboardApp
    {
        public static string FormatTime(double minutes)
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
