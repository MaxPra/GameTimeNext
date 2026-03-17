namespace GameTimeNext.Core.Framework.LauncherIntegration
{
    public class SteamGameStarter
    {

        public string SteamAppID { get; set; }
        public List<string> SteamGameArgs;

        public SteamGameStarter(List<string> steamGameArgs, string steamAppID)
        {
            SteamGameArgs = steamGameArgs;
            SteamAppID = steamAppID;
        }

        public void StartGame()
        {
            var steamRoot = SteamLocatorService.GetSteamRoot();
            var steamExe = System.IO.Path.Combine(steamRoot, "steam.exe");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = steamExe,
                Arguments = $"-applaunch {SteamAppID} {BuildSteamArgs()}",
                UseShellExecute = true
            });
        }

        private string BuildSteamArgs()
        {
            string steamArgs = "";

            if (SteamGameArgs == null || SteamGameArgs.Count == 0)
            {
                return "";
            }


            for (int i = 0; i < SteamGameArgs.Count; i++)
            {
                steamArgs += SteamGameArgs[i];

                if (i + 1 < SteamGameArgs.Count)
                {
                    steamArgs += " ";
                }
            }

            return steamArgs;

        }
    }
}
