using UIX.ViewController.Engine.Runnables;
using Arcora.Core.Application.TableObjects;

namespace Arcora.Core.Application.DataManagers
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
