using Arcora.Core.Application.TableObjects;
using Arcora.Core.Framework;

namespace Arcora.Core.Application.DataManagers
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
