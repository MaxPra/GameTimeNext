using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXGRPPO : TXGRPPOBasic
    {
        public void DeleteAllWherePFID(long pfid)
        {
            if (pfid == 0)
                return;

            string sql = $"DELETE FROM T1GRPPO WHERE PFID = '{pfid}';";
            UIXQuery.ExecuteCustom(sql, _connection);
        }
    }
}
