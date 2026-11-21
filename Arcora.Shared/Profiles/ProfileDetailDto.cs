namespace GameTimeNext.Shared.Profiles
{
    public class ProfileDetailDto : ProfileDto
    {
        public double TotalMinutes { get; set; }
        public double TodayMinutes { get; set; }
        public DateTime? LastPlayed { get; set; }
        public string? CurrentPlaythrough { get; set; }
        public double? CurrentPlaythroughMinutes { get; set; }
    }
}
