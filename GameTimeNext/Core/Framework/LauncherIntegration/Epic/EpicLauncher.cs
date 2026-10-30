using System.IO;
using System.Text.Json;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.LauncherIntegration.Epic
{
    /// <summary>
    /// Epic Games Launcher. Game ID = "CatalogNamespace:CatalogItemId:AppName" (the ID of the launch URI).
    /// Installed games are read from the launcher's manifests (*.item, JSON).
    /// </summary>
    public class EpicLauncher : LauncherIntegrationBase
    {
        private record EpicManifest(string AppName, string GameId, string Name, string InstallPath);

        public override string LauncherType => "EPIC";

        public override bool SupportsImport => true;

        public override List<LauncherGame> GetInstalledGames()
        {
            return ReadManifests()
                .Select(m => new LauncherGame(LauncherType, m.GameId, m.Name, m.InstallPath))
                .ToList();
        }

        public override bool IsGameInstalled(string gameId)
        {
            string appName = gameId.Split(':').Last();

            return ReadManifests().Any(m => m.AppName == appName);
        }

        public override void StartGame(string gameId, string launchArgs)
        {
            StartShell($"com.epicgames.launcher://apps/{Uri.EscapeDataString(gameId)}?action=launch&silent=true");
        }

        private static List<EpicManifest> ReadManifests()
        {
            List<EpicManifest> manifests = new List<EpicManifest>();

            string manifestsFolder = GetManifestsFolder();

            if (!Directory.Exists(manifestsFolder))
                return manifests;

            foreach (string file in Directory.EnumerateFiles(manifestsFolder, "*.item"))
            {
                try
                {
                    EpicManifest? manifest = ReadManifest(file);

                    if (manifest != null)
                        manifests.Add(manifest);
                }
                catch (Exception ex)
                {
                    // A broken manifest must not hide the other games
                    FnLog.AddWarning(nameof(EpicLauncher), $"Could not read manifest {Path.GetFileName(file)}", ex);
                }
            }

            return manifests;
        }

        private static EpicManifest? ReadManifest(string file)
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(file));
            JsonElement root = document.RootElement;

            string appName = GetString(root, "AppName");
            string mainGameAppName = GetString(root, "MainGameAppName");
            string installPath = NormalizeFolder(GetString(root, "InstallLocation"));

            // Not completely installed
            if (root.TryGetProperty("bIsIncompleteInstall", out JsonElement incomplete) && incomplete.ValueKind == JsonValueKind.True)
                return null;

            // DLC of another game
            if (!FnString.IsNullEmptyOrWhitespace(mainGameAppName) && mainGameAppName != appName)
                return null;

            // Engine plugins, tools, ...
            if (root.TryGetProperty("AppCategories", out JsonElement categories) && categories.ValueKind == JsonValueKind.Array
                && !categories.EnumerateArray().Any(c => c.GetString() == "games"))
                return null;

            if (FnString.IsNullEmptyOrWhitespace(appName) || !Directory.Exists(installPath))
                return null;

            string gameId = $"{GetString(root, "CatalogNamespace")}:{GetString(root, "CatalogItemId")}:{appName}";

            return new EpicManifest(appName, gameId, GetString(root, "DisplayName"), installPath);
        }

        private static string GetManifestsFolder()
        {
            // e.g. "C:\ProgramData\Epic\EpicGamesLauncher\Data\"
            string dataPath = GetLocalMachineValue(@"SOFTWARE\WOW6432Node\Epic Games\EpicGamesLauncher", "AppDataPath");

            if (FnString.IsNullEmptyOrWhitespace(dataPath))
                dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data");

            return Path.Combine(dataPath, "Manifests");
        }

        private static string GetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }
    }
}
