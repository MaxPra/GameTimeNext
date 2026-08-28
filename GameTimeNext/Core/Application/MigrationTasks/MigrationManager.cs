using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.UI.Dialogs;

namespace GameTimeNext.Core.Application.MigrationTasks
{
    public class MigrationManager
    {
        public static void Migrate()
        {
            List<MigTaskBase> migrations = new List<MigTaskBase>()
            {
                new MigTask_026b_005(),
                new MigTask_033b_007(),
                new MigTask_100b_008(),
            };

            migrations.ForEach(mT =>mT.Execute());
        }

        public static void RestartGTN()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                CFMBOX cfmbox = new CFMBOX();
                cfmbox.Show($"{AppConfig.Root.ApplicationName} will now be restarted in order to activate the new version!", CFMBOXResult.Ok, CFMBOXIcon.Info);

                AppEnvironment.RestartGTNApplication();
            });
        }
    }
}
