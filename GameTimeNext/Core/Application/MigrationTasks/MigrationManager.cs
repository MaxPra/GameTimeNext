using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Versioning;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.MigrationTasks
{
    public class MigrationManager
    {
        public static void MigrateIfNeeded()
        {
            string versionOldRaw = AppEnvironment.GetAppConfig().AppVersion;
            AppVersion currentVersion = AppEnvironment.AppVersion;

            if (FnString.IsNullEmptyOrWhitespace(versionOldRaw))
                return;

            HandleMigrationVersion017b(versionOldRaw, currentVersion);

            HandleMigrationVersion019b(versionOldRaw, currentVersion);

            HandleMigrationVersion020b(versionOldRaw, currentVersion);

            HandleMigrationVersion021b(versionOldRaw, currentVersion);

        }

        public static void HandleMigrationVersion017b(string versionOldRaw, AppVersion currentVersion)
        {
            // Ab Version 0.1.7
            if (currentVersion.NeedsMigrationFrom(versionOldRaw, "0.1.7"))
            {
                MigTask_017b_001.Execute();
            }
        }

        public static void HandleMigrationVersion019b(string versionOldRaw, AppVersion currentVersion)
        {
            // Ab Version 0.1.9
            if (currentVersion.NeedsMigrationFrom(versionOldRaw, "0.1.9"))
            {
                MigTask_019b_002.Execute();
            }
        }

        public static void HandleMigrationVersion020b(string versionOldRaw, AppVersion currentVersion)
        {
            // Ab Version 0.2.0
            if (currentVersion.NeedsMigrationFrom(versionOldRaw, "0.2.0"))
            {
                MigTask_020b_003.Execute();
            }
        }

        public static void HandleMigrationVersion021b(string versionOldRaw, AppVersion currentVersion)
        {
            // Ab Version 0.2.1
            if (currentVersion.NeedsMigrationFrom(versionOldRaw, "0.2.1"))
            {
                MigTask_021b_004.Execute();
            }
        }
    }
}
