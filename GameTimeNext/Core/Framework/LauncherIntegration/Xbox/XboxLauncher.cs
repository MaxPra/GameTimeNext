using System.IO;
using System.Runtime.Versioning;
using UIX.ViewController.Engine.Utils;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace GameTimeNext.Core.Framework.LauncherIntegration.Xbox
{
    /// <summary>
    /// Xbox app (PC Game Pass, Microsoft Store). Game ID = AppUserModelId ("PackageFamilyName!AppId").
    /// Installed games are the store packages of the current user that contain a MicrosoftGame.config (GDK games).
    /// </summary>
    public class XboxLauncher : LauncherIntegrationBase
    {
        public override string LauncherType => "XBOX";

        public override bool SupportsImport => true;

        public override List<LauncherGame> GetInstalledGames()
        {
            List<LauncherGame> games = new List<LauncherGame>();

            // The package locations and app list entries need Windows 10 2004
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
                return games;

            foreach (Package package in new PackageManager().FindPackagesForUser(string.Empty))
            {
                try
                {
                    LauncherGame? game = CreateGame(package);

                    if (game != null)
                        games.Add(game);
                }
                catch (Exception ex)
                {
                    // Packages that can't be read (e.g. being updated) must not hide the other games
                    FnLog.AddWarning(nameof(XboxLauncher), $"Could not read package {package.Id.FullName}", ex);
                }
            }

            return games;
        }

        public override bool IsGameInstalled(string gameId)
        {
            string packageFamilyName = gameId.Split('!')[0];

            return new PackageManager().FindPackagesForUser(string.Empty, packageFamilyName).Any();
        }

        public override void StartGame(string gameId, string launchArgs)
        {
            StartShell("explorer.exe", $@"shell:AppsFolder\{gameId}");
        }

        [SupportedOSPlatform("windows10.0.19041")]
        private LauncherGame? CreateGame(Package package)
        {
            if (package.IsFramework || package.IsResourcePackage || package.IsBundle || package.SignatureKind != PackageSignatureKind.Store)
                return null;

            // Only games contain a MicrosoftGame.config
            if (!File.Exists(Path.Combine(package.EffectiveLocation.Path, "MicrosoftGame.config")))
                return null;

            string? appUserModelId = package.GetAppListEntries().FirstOrDefault()?.AppUserModelId;

            if (FnString.IsNullEmptyOrWhitespace(appUserModelId))
                return null;

            // Games installed into the "XboxGames" folder run from there (mutable location), not from WindowsApps
            string installPath = package.MutableLocation?.Path ?? package.EffectiveLocation.Path;

            return new LauncherGame(LauncherType, appUserModelId!, package.DisplayName, NormalizeFolder(installPath));
        }
    }
}
