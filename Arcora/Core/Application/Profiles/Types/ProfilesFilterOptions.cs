namespace GameTimeNext.Core.Application.Profiles.Types
{
    /// <summary>
    /// Fixed filter options of the library filter.
    /// Options of one group are mutually exclusive, so every profile falls into exactly one option per group.
    /// Within a group the options are OR-combined, between groups they are AND-combined.
    /// </summary>
    public class ProfilesFilterOptions
    {
        // Progress
        public const string PROGRESS_UNPLAYED = "GTN.FILTER.PROGRESS.UNPLAYED";
        public const string PROGRESS_IN_PROGRESS = "GTN.FILTER.PROGRESS.IN_PROGRESS";
        public const string PROGRESS_COMPLETED = "GTN.FILTER.PROGRESS.COMPLETED";
        public const string PROGRESS_PAUSED = "GTN.FILTER.PROGRESS.PAUSED";
        public const string PROGRESS_CANCELLED = "GTN.FILTER.PROGRESS.CANCELLED";

        // Availability
        public const string AVAILABILITY_PLAYABLE = "GTN.FILTER.AVAILABILITY.PLAYABLE";
        public const string AVAILABILITY_NOT_PLAYABLE = "GTN.FILTER.AVAILABILITY.NOT_PLAYABLE";

        // Source
        public const string SOURCE_LOCAL = "GTN.FILTER.SOURCE.LOCAL";
        public const string SOURCE_EXTERNAL = "GTN.FILTER.SOURCE.EXTERNAL";

        /// <summary>
        /// Number of days a game counts as "recently played"
        /// </summary>
        public const int RECENTLY_PLAYED_DAYS = 14;

        public static readonly IReadOnlyList<(string Key, string Name)> Progress =
        [
            (PROGRESS_UNPLAYED, "Unplayed"),
            (PROGRESS_IN_PROGRESS, "In Progress"),
            (PROGRESS_PAUSED, "Paused"),
            (PROGRESS_COMPLETED, "Completed"),
            (PROGRESS_CANCELLED, "Cancelled"),
        ];

        public static readonly IReadOnlyList<(string Key, string Name)> Availability =
        [
            (AVAILABILITY_PLAYABLE, "Playable"),
            (AVAILABILITY_NOT_PLAYABLE, "Not playable"),
        ];

        public static readonly IReadOnlyList<(string Key, string Name)> Source =
        [
            (SOURCE_LOCAL, "Local"),
            (SOURCE_EXTERNAL, "External"),
        ];
    }
}
