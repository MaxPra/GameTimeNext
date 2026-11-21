using GameTimeNext.Core.Application.TableObjects;
using System.Data.SQLite;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXSCRSH : TXSCRSHBasic
    {
        public TXSCRSH()
        {
        }

        public TXSCRSH(SQLiteConnection connection) : base(connection)
        {
        }

        public override T1SCRSH Copy(T1SCRSH objFrom)
        {
            T1SCRSH obj = base.Copy(objFrom);
            obj.SSNA = $"{objFrom.SSNA} (Copy)";
            return obj;
        }
    }
}
