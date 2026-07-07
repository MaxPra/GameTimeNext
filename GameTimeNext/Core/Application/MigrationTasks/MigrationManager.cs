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

            HandleMigrationVersion026b(versionOldRaw, currentVersion);

            HandleMigrationVersion032b(versionOldRaw, currentVersion);

            HandleMigrationVersion033b(versionOldRaw, currentVersion);

            HandleMigrationVersion100b(versionOldRaw, currentVersion);

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

        public static void HandleMigrationVersion026b(string versionOldRaw, AppVersion currentVersion)
        {
            // Ab Version 0.2.6
            if (currentVersion.NeedsMigrationFrom(versionOldRaw, "0.2.6"))
            {
                MigTask_026b_005.Execute();
            }
        }

        public static void HandleMigrationVersion032b(string versionOldRaw, AppVersion currentVersion)
        {
            // Ab Version 0.3.2
            if (currentVersion.NeedsMigrationFrom(versionOldRaw, "0.3.2"))
            {
                MigTask_032b_006.Execute();
            }
        }

        public static void HandleMigrationVersion033b(string versionOldRaw, AppVersion currentVersion)
        {
            // Ab Version 0.3.3
            if (currentVersion.NeedsMigrationFrom(versionOldRaw, "0.3.3"))
            {
                MigTask_033b_007.Execute();
            }
        }

        public static void HandleMigrationVersion100b(string versionOldRaw, AppVersion currentVersion)
        {
            // Ab Version 1.0.0
            if (currentVersion.NeedsMigrationFrom(versionOldRaw, "1.0.0"))
            {
                MigTask_100b_008.Execute();
            }
        }
    }
}
