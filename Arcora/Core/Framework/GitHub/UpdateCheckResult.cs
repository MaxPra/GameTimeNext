namespace GameTimeNext.Core.Framework.GitHub
{
    public sealed class UpdateCheckResult
    {
        public bool UpdateAvailable { get; set; }
        public Version? CurrentVersion { get; set; }
        public Version? LatestVersion { get; set; }
        public string? ReleaseUrl { get; set; }
    }
}
