using UIX.ViewController.Engine.Runnables;
using Arcora.Core.Application.TableObjects;

namespace Arcora.Core.Application.DataManagers
{
    public class TFSCRSH
    {
        public static void DeleteScreensetupAndMonitors(UIXApplication app, T1SCRSH t1scrsh)
        {
            // Monitors
            app.GetDataManager<TXSCRSP>().DeleteAllWhereSSID(t1scrsh.SSID);

            // Screensetup
            app.GetDataManager<TXSCRSH>().Delete(t1scrsh.SSID);
        }
    }
}
