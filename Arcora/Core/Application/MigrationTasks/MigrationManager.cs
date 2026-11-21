using Arcora.Core.Framework;
using Arcora.Core.Framework.Config;
using Arcora.Core.Framework.UI.Dialogs;

namespace Arcora.Core.Application.MigrationTasks
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
                new MigTask_100b_009(),
                new MigTask_100b_010(),
                new MigTask_100b_011(),
                new MigTask_100b_012(),
                new MigTask_110b_013(),
            };

            migrations.ForEach(mT =>mT.Execute());

            AppEnvironment.AppVersion.SetAppVersionInConfig();
        }

        public static void RestartArcora()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                CFMBOX cfmbox = new CFMBOX();
                cfmbox.Show(cfmbox.GetErrorMessage("GLO0018", AppConfig.Root.ApplicationName), CFMBOXResult.Ok, CFMBOXIcon.Info);

                AppEnvironment.RestartArcoraApplication();
            });
        }
    }
}
