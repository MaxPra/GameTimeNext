using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXSESSI : TXSESSIBasic
    {
        public override void Save(T1SESSI obj)
        {
            if (AppEnvironment.GetAppConfig().AppSettings.EnableSessionCleanup && obj.PLTI < (AppEnvironment.GetAppConfig().AppSettings.SessionCleanupSeconds / 60)) return;

            base.Save(obj);
        }
    }
}
