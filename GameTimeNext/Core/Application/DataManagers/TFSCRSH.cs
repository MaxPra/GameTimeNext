using GameTimeNext.Core.Application.TableObjects;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TFSCRSH
    {
        public static void DeleteScreensetupAndMonitors(T1SCRSH t1scrsh)
        {
            // Monitors
            new TXSCRSP().DeleteAllWhereSSID(t1scrsh.SSID);

            // Screensetup
            new TXSCRSH().Delete(t1scrsh.SSID);
        }
    }
}
