using UIX.ViewController.Engine.Runnables;
using GameTimeNext.Core.Application.TableObjects;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TFCTABH
    {
        public static void DeleteCodetableAndEntries(UIXApplication app, T1CTABH t1ctabh)
        {
            // Entries
            app.GetDataManager<TXCTABD>().DeleteAllEntries(t1ctabh.TXTYP);

            // Codetable
            app.GetDataManager<TXCTABH>().Delete(t1ctabh.TXTYP);
        }
    }
}
