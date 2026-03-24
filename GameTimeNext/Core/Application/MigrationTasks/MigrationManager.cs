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

            // Ab Version 0.1.7
            if (currentVersion.NeedsMigrationFrom(versionOldRaw, "0.1.7"))
            {
                MigTask_001.Execute();
            }
        }
    }
}
