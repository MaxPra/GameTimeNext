using GameTimeNext.Core.Application.General.UserSettings;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Utils;
using System.IO;
using System.Text.Json.Serialization;
using UIX.ViewController.Engine.Utils;
using static GameTimeNext.Core.Application.Profiles.Controller.ProfilesViewController;

namespace GameTimeNext.Core.Framework.Config
{
    internal class AppConfig
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
            public static string WorkingDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Environment.CurrentDirectory);
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
            private static string _devDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(Root.LocalStorageDirectoryPath, "dev"));
            }

            [JsonIgnore]
            public static string GenClassDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(_devDirectoryPath, "genClass"));
            }

            [JsonIgnore]
            public static string BackupDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(_devDirectoryPath, "backup"));
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

                    return ReturnEnsureDirectoryExists(temp);
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
                get => ReturnEnsureDirectoryExists(Path.Combine(Root.StorageDirectoryPath, _storageDirectoryName));
            }

            [JsonIgnore]
            private static string _dataDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(StorageDirectoryPath, "data"));
            }

            [JsonIgnore]
            public static string ProfileCoversDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(_dataDirectoryPath, "profileCovers"));
            }

            [JsonIgnore]
            public static string ImagesSymbolsDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(_dataDirectoryPath, "imagesAndSymbols"));
            }

            [JsonIgnore]
            public static string DefaultImagesSymbolsDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(ImagesSymbolsDirectoryPath, "default"));
            }

            [JsonIgnore]
            public static string UserImagesSymbolsDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(ImagesSymbolsDirectoryPath, "user"));
            }

            [JsonIgnore]
            public static string AppConfigFilePath
            {
                get => ReturnEnsureFileExists(Path.Combine(StorageDirectoryPath, Root.AppConfigFileName));
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
        private static string _logsDirectoryPath
        {
            get => ReturnEnsureDirectoryExists(Path.Combine(Root.WorkingDirectoryPath, "logs"));
        }

        [JsonIgnore]
        private static string _tempDirectoryPath
        {
            get => ReturnEnsureDirectoryExists(Path.Combine(Root.LocalStorageDirectoryPath, "temp"));
        }

        [JsonIgnore]
        public static string LogFilePath
        {
            get => ReturnEnsureFileExists(Path.Combine(_logsDirectoryPath, Root.AppLogFileName));
        }

        #region External
        public FilterCache FilterCache { get; set; } = new FilterCache();
        public AppSettings AppSettings { get; set; } = new AppSettings();
        public UserSettings UserSettings { get; set; } = new UserSettings();

        public static string AppVersion { get; set; } = string.Empty;
        #endregion

        public AppConfig()
        {
            EnsurePathsExist();
        }

        private static void EnsurePathsExist()
        {
            string temp = string.Empty;

            // Root
            temp = Root.ApplicationDirectoryPath;
            temp = Root.WorkingDirectoryPath;
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

        private static string ReturnEnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

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
