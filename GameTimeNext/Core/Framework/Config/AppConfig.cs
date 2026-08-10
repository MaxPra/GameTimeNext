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
        // OFDOI: Make self-creating
        // OFDOI: Replace all DirectorySeparatorChar
        private static readonly string FUCKING_WORK_FOR_NOW = Path.Combine(@"C:\Users\Oliver Fida\Desktop\TEMP\FWFN");

        public static class Root
        {
            public static string _publisherName = "MaxPra";
            public static string _applicationName = "GameTimeNext";
            public static string _databaseFileName = _applicationName + "Db.db";
            public static string _appConfigFileName = "appConfig.gtnconf";
            public static string _appLogFileName = $"ApplicationLog_{DateTime.Now:yyyy-MM-dd}.log";

            [JsonIgnore]
            public static string _rootApplicationDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(AppContext.BaseDirectory);
            }

            [JsonIgnore]
            public static string _rootWorkingDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Environment.CurrentDirectory);
            }

            [JsonIgnore]
            public static string _rootStorageDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _publisherName));
            }

            [JsonIgnore]
            public static string _rootLocalStorageDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), _publisherName, _applicationName));
            }
        }

        public static class Dev
        {
            [JsonIgnore]
            private static string _devDirectoryPath
            {
                get => ReturnEnsureDirectoryExists(Path.Combine(Root._rootLocalStorageDirectoryPath, "dev"));
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

        #region Internal
        #region Directories
        [JsonIgnore]
        private static string _normalStorageDirectoryName
        {
            get
            {
                string temp = Root._applicationName;

                // Modi
                if (AppEnvironment.StartArguments.ContainsKey("m") && !FnString.IsNullEmptyOrWhitespace(AppEnvironment.StartArguments["m"]))
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
            get => Path.Combine(Root._rootStorageDirectoryPath, _normalStorageDirectoryName);
        }

        [JsonIgnore]
        public static string StorageDirectoryPath
        {
            get => ReturnEnsureDirectoryExists(Path.Combine(Root._rootStorageDirectoryPath, _storageDirectoryName));
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
        private static string _logsDirectoryPath
        {
            get => ReturnEnsureDirectoryExists(Path.Combine(Root._rootWorkingDirectoryPath, "logs"));
        }

        [JsonIgnore]
        private static string _tempDirectoryPath
        {
            get => ReturnEnsureDirectoryExists(Path.Combine(Root._rootLocalStorageDirectoryPath, "temp"));
        }

        [JsonIgnore]
        public static string SteamGridDBCoversDirectoryPath
        {
            get => ReturnEnsureDirectoryExists(Path.Combine(_tempDirectoryPath, "steamGridDbCovers"));
        }

        [JsonIgnore]
        public static string TempCoversDirectoryPath
        {
            get => ReturnEnsureDirectoryExists(Path.Combine(_tempDirectoryPath, "tempCovers"));
        }

        [JsonIgnore]
        public static string ImportDirectoryPath
        {
            get => ReturnEnsureDirectoryExists(Path.Combine(_tempDirectoryPath, "import"));
        }

        [JsonIgnore]
        public static string TempBackupDirectoryPath
        {
            get => ReturnEnsureDirectoryExists(Path.Combine(_tempDirectoryPath, "backup"));
        }
        #endregion

        #region Files
        [JsonIgnore]
        public static string LogFilePath
        {
            get => ReturnEnsureFileExists(Path.Combine(_logsDirectoryPath, Root._appLogFileName));
        }

        [JsonIgnore]
        public static string AppConfigFilePath
        {
            get => ReturnEnsureFileExists(Path.Combine(StorageDirectoryPath, Root._appConfigFileName));
        }

        [JsonIgnore]
        public static string DatabaseFilePath
        {
            get => Path.Combine(StorageDirectoryPath, Root._databaseFileName);
        }
        #endregion
        #endregion

        #region External
        public FilterCache FilterCache { get; set; } = new FilterCache();
        public AppSettings AppSettings { get; set; } = new AppSettings();
        public UserSettings UserSettings { get; set; } = new UserSettings();

        public static string AppVersion { get; set; } = string.Empty;
        #endregion

        public AppConfig()
        {

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
