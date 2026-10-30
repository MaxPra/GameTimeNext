using System.Windows;

namespace GameTimeNext.Core.Application.Dashboard.ViewModels
{
    /// <summary>
    /// One day of the playtime bar chart (dashboard, playtime overview).
    /// </summary>
    public class DashboardDayBar
    {
        /// <summary>Short weekday below the bar.</summary>
        public string Label { get; init; } = string.Empty;

        /// <summary>Playtime of the day, shown above the bar of the peak day.</summary>
        public string ValueText { get; init; } = string.Empty;

        public string ToolTipText { get; init; } = string.Empty;

        /// <summary>Bar height relative to the peak day (star row), RestHeight is the remainder above it.</summary>
        public GridLength BarHeight { get; init; }

        public GridLength RestHeight { get; init; }

        public bool IsPeak { get; init; }

        public bool IsToday { get; init; }
    }
}
