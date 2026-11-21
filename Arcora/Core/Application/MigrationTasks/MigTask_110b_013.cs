using Arcora.Core.Application.General.UserSettings;
using Arcora.Core.Framework;
using Arcora.Core.Framework.Config;
using System.Globalization;
using UIX.ViewController.Engine.Utils;

namespace Arcora.Core.Application.MigrationTasks
{
    /// <summary>
    /// The namespaces were renamed from GameTimeNext.* to Arcora.*: the favorites in the config store the full type name
    /// of their application, which is rewritten to the new namespace.
    /// Runs on every start (only touches entries that still have the old name), because the version check ignores the
    /// beta tag (configs already on 1.1.0-beta would be skipped) and an imported backup brings its old config back.
    /// </summary>
    internal class MigTask_110b_013 : MigTaskBase
    {
        private const string FormerNamespacePrefix = "GameTimeNext.";

        public MigTask_110b_013() : base("1.1.0", executeAlways: true)
        {

        }

        protected override void ExecuteImpl()
        {
            string newNamespacePrefix = AppConfig.Root.InternalApplicationName + ".";
            int migratedEntries = 0;

            foreach (FavoriteApplication favoriteApplication in AppEnvironment.GetAppConfig().UserSettings.FavApps)
            {
                if (favoriteApplication.FullName?.StartsWith(FormerNamespacePrefix, StringComparison.Ordinal) != true)
                    continue;

                favoriteApplication.FullName = newNamespacePrefix + favoriteApplication.FullName.Substring(FormerNamespacePrefix.Length);
                migratedEntries++;
            }

            if (migratedEntries == 0)
                return;

            AppEnvironment.SaveAppConfig();
            FnLog.AddInfo("MainApp", $"Namespace of {migratedEntries.ToString(CultureInfo.InvariantCulture)} favorite application(s) changed from GameTimeNext to {AppConfig.Root.InternalApplicationName}");
        }
    }
}
