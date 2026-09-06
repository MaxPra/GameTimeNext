using GameTimeNext.Core.Framework.Utils;
using System.IO;
using System.Security.Policy;
using System.Text.Json.Serialization;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.Config
{
    internal partial class AppConfig
    {
        public static class Root
        {
            public static string PublisherName = "MaxPra";
            public static string ApplicationName = "GameTimeNext";
            public static string DatabaseFileName = ApplicationName + "Db.db";
            public static string AppConfigFileName = "appConfig.gtnconf";
            public static string AppLogFileName = $"ApplicationLog_{DateTime.Now:yyyy-MM-dd}.log";

            [JsonIgnore]
            public static string ApplicationDirectoryPath
            {
                get => AppContext.BaseDirectory;
            }

            [JsonIgnore]
            public static string StorageDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), PublisherName));
            }

            [JsonIgnore]
            public static string LocalStorageDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), PublisherName, ApplicationName));
            }
        }

        public static class Dev
        {
            [JsonIgnore]
            public static string DevDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(Root.LocalStorageDirectoryPath, "dev"));
            }

            [JsonIgnore]
            public static string GenClassDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(DevDirectoryPath, "genClass"));
            }

            [JsonIgnore]
            public static string BackupDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(DevDirectoryPath, "backup"));
            }

            [JsonIgnore]
            public static string DevSyncDirectoryPath
            {
                get
                {
                    string startingDirectoryPath = Root.ApplicationDirectoryPath;

                    DirectoryInfo? current = new DirectoryInfo(startingDirectoryPath);
                    while (current is not null)
                    {
                        string slnxFilePath = Path.Combine(current.FullName, $"{Root.ApplicationName}.slnx");
                        if (File.Exists(slnxFilePath))
                            return Path.Combine(current.FullName, "devsync");

                        current = current.Parent;
                    }

                    return Path.Combine(startingDirectoryPath, "devsync");
                }
            }

            [JsonIgnore]
            public static string DevSyncDefaultImagesSymbolsDirectoryPath
            {
                get => GetImagesAndSymbolsDirectoryPath(DevSyncDirectoryPath);
            }

            public static string GetImagesAndSymbolsDirectoryPath(string parentDirectoryPath)
            {
                string temp = ReturnEnsureDirectoryExists(Path.Combine(parentDirectoryPath, "imagesAndSymbols"), requireParentExists: true);
                return ReturnEnsureDirectoryExists(Path.Combine(temp, "default"), requireParentExists: true);
            }
        }

        public static class Storage
        {
            [JsonIgnore]
            private static string _normalStorageDirectoryName
            {
                get
                {
                    string temp = Root.ApplicationName;

                    // Modi
                    if (AppEnvironment.StartArguments is not null && AppEnvironment.StartArguments.ContainsKey("m") && !FnString.IsNullEmptyOrWhitespace(AppEnvironment.StartArguments["m"]))
                        temp += "_m" + AppEnvironment.StartArguments["m"];

                    return temp;
                }
            }

            [JsonIgnore]
            private static string _storageDirectoryName
            {
                get
                {
                    string temp = _normalStorageDirectoryName;

                    if (FnSystem.IsDebug())
                        temp += "_dev";

                    return temp;
                }
            }

            [JsonIgnore]
            public static string NormalStorageDirectoryPath
            {
                get => Path.Combine(Root.StorageDirectoryPath, _normalStorageDirectoryName);
            }

            [JsonIgnore]
            public static string StorageDirectoryPath
            {
                get => Path.Combine(Root.StorageDirectoryPath, _storageDirectoryName);
            }

            [JsonIgnore]
            private static string _dataDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(StorageDirectoryPath, "data"), requireParentExists: true);
            }

            [JsonIgnore]
            public static string ProfileCoversDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(_dataDirectoryPath, "profileCovers"), requireParentExists: true);
            }

            [JsonIgnore]
            public static string ImagesSymbolsDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(_dataDirectoryPath, "imagesAndSymbols"), requireParentExists: true);
            }

            [JsonIgnore]
            public static string DefaultImagesSymbolsDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(ImagesSymbolsDirectoryPath, "default"), requireParentExists: true);
            }

            [JsonIgnore]
            public static string UserImagesSymbolsDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(ImagesSymbolsDirectoryPath, "user"), requireParentExists: true);
            }

            [JsonIgnore]
            public static string AppConfigFilePath
            {
                get => Path.Combine(StorageDirectoryPath, Root.AppConfigFileName);
            }

            [JsonIgnore]
            public static string DatabaseFilePath
            {
                get => Path.Combine(StorageDirectoryPath, Root.DatabaseFileName);
            }
        }

        public static class Temp
        {
            [JsonIgnore]
            public static string SteamGridDBCoversDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(_tempDirectoryPath, "steamGridDbCovers"));
            }

            [JsonIgnore]
            public static string ProfileCoversDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(_tempDirectoryPath, "tempCovers"));
            }

            [JsonIgnore]
            public static string ImportDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(_tempDirectoryPath, "import"));
            }

            [JsonIgnore]
            public static string BackupDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(_tempDirectoryPath, "backup"));
            }
        }

        [JsonIgnore]
        public static string LogsDirectoryPath
        {
            get
            {
                string temp = Root.LocalStorageDirectoryPath;

                if (FnSystem.IsDebug())
                    temp = Dev.DevDirectoryPath;

                return ReturnEnsureDirectoryExists(Path.Combine(temp, "logs"));
            }
        }

        [JsonIgnore]
        private static string _tempDirectoryPath
        {
            get => ReturnEnsureDirectoryExists(Path.Combine(Root.LocalStorageDirectoryPath, "temp"));
        }

        [JsonIgnore]
        public static string LogFilePath
        {
            get => ReturnEnsureFileExists(Path.Combine(LogsDirectoryPath, Root.AppLogFileName));
        }

        private static void EnsurePathsExist()
        {
            string temp = string.Empty;

            // Root
            temp = Root.ApplicationDirectoryPath;
            temp = Root.StorageDirectoryPath;
            temp = Root.LocalStorageDirectoryPath;

            // Dev
            if (FnSystem.IsDebug())
            {
                temp = Dev.GenClassDirectoryPath;
                temp = Dev.BackupDirectoryPath;
            }

            // Storage
            temp = Storage.NormalStorageDirectoryPath;
            temp = Storage.StorageDirectoryPath;
            temp = Storage.ProfileCoversDirectoryPath;
            temp = Storage.ImagesSymbolsDirectoryPath;
            temp = Storage.DefaultImagesSymbolsDirectoryPath;
            temp = Storage.UserImagesSymbolsDirectoryPath;
            temp = Storage.AppConfigFilePath;
            temp = Storage.DatabaseFilePath;

            // Temp
            temp = Temp.SteamGridDBCoversDirectoryPath;
            temp = Temp.ProfileCoversDirectoryPath;
            temp = Temp.ImportDirectoryPath;
            temp = Temp.BackupDirectoryPath;

            // General
            temp = LogFilePath;
        }

        private static string ReturnEnsureDirectoryExists(string path, bool requireParentExists = false)
        {
            if (!Directory.Exists(path))
            {
                DirectoryInfo? parentDirectoryInfo = new DirectoryInfo(path).Parent;
                if (!requireParentExists || (parentDirectoryInfo is not null && parentDirectoryInfo.Exists))
                    Directory.CreateDirectory(path);
            }

            return path;
        }

        private static string ReturnEnsureFileExists(string path)
        {
            if (!File.Exists(path))
            {
                FileStream fs = File.Create(path);
                fs.Close();
            }

            return path;
        }
    }
}
