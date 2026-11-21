using System.Globalization;
using System.IO;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.LauncherIntegration.Steam
{
    /// <summary>
    /// Steam integration. Game ID = Steam app ID.
    /// </summary>
    public class SteamLauncher : LauncherIntegrationBase
    {
        // Tools and redistributables that are no games
        private static readonly string[] NoiseTokens =
        {
            "steamworks", "redistributable", "proton", "steam linux runtime",
            "shader", "benchmark", "dedicated server"
        };

        public override string LauncherType => "STEAM";

        public override bool SupportsImport => true;

        public override bool SupportsLaunchArguments => true;

        public override bool SupportsOpenInLauncher => true;

        public override List<LauncherGame> GetInstalledGames()
        {
            var root = SteamLocator.GetSteamRoot();
            if (FnString.IsNullEmptyOrWhitespace(root))
                return new List<LauncherGame>();

            List<SteamManifest> manifests = SteamManifestReader.ScanAllGames(SteamLocator.GetLibraryPaths(root!));

            return manifests
                .Where(g => !NoiseTokens.Any(t => g.Name?.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0))
                // Only fully installed games (StateFlags == 4)
                .Where(g => g.StateFlags == 4)
                .Where(g => SteamManifestReader.ResolveInstallPath(g) != null)
                // One entry per AppID; prefer one with an install dir
                .GroupBy(g => g.AppId)
                .Select(grp => grp.OrderByDescending(x => !FnString.IsNullEmptyOrWhitespace(x.InstallDir))
                                  .ThenBy(x => x.LibraryPath)
                                  .First())
                .Select(g => new LauncherGame(LauncherType, g.AppId.ToString(CultureInfo.InvariantCulture), g.Name, SteamManifestReader.ResolveInstallPath(g)!))
                .ToList();
        }

        public override bool IsGameInstalled(string gameId)
        {
            return SteamLocator.IsGameInstalledByAppId(gameId);
        }

        public override void StartGame(string gameId, string launchArgs)
        {
            var steamExe = Path.Combine(SteamLocator.GetSteamRoot()!, "steam.exe");
            StartShell(steamExe, $"-applaunch {gameId} {launchArgs}".TrimEnd());
        }

        public override void OpenInLauncher(string gameId)
        {
            StartShell($"steam://nav/games/details/{gameId}");
        }
    }
}
