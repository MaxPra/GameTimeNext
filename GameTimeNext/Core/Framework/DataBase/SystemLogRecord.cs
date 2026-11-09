namespace GameTimeNext.Core.Framework.DataBase
{
    /// <summary>
    /// Row of T1SYLOG in the log database, read by LogDataBaseManager.ReadT1sylog (the log database has no table objects).
    /// </summary>
    public sealed class SystemLogRecord
    {
        public long LGID { get; init; }

        public DateTime LGTS { get; init; }

        public string LGTY { get; init; } = string.Empty;

        public string LGSR { get; init; } = string.Empty;

        public string LGMS { get; init; } = string.Empty;

        public string LGEX { get; init; } = string.Empty;

        public string LGCL { get; init; } = string.Empty;

        public string LGST { get; init; } = string.Empty;
    }
}
