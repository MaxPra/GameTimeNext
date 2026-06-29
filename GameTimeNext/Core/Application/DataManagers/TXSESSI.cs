using GameTimeNext.Core.Application.TableObjects;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXSESSI : TXSESSIBasic
    {
        public override void Save(T1SESSI obj)
        {
            if (obj.PLTI < 1) return;

            base.Save(obj);
        }
    }
}
