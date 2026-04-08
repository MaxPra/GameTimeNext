using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.MigrationTasks;
using GameTimeNext.Core.Application.Profiles.BackgroundProcesses;
using GameTimeNext.Core.Application.Settings;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.DataBase;
using GameTimeNext.Core.Framework.Files;
using GameTimeNext.Core.Framework.Igdb;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.Utils;
using GameTimeNext.Core.Framework.Versioning;
using Microsoft.VisualBasic.FileIO;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework
{
    internal class AppEnvironment
    {

        private static AppConfig _appConfig = new AppConfig();
        private static DataBaseManager _databaseManager = new DataBaseManager();
        private static T1PROFI? _t1Profi = new T1PROFI();
        private static ContentControl _loader = new ContentControl();
        private static Dictionary<string, UIXApplication> _startedApplications = new Dictionary<string, UIXApplication>();
        private static Dictionary<string, UIXBackgroundProcess> _startedBackgroundProcesses = new Dictionary<string, UIXBackgroundProcess>();

        // [------------------------------------------------]
        // [------------------ PUBLIC ----------------------]
        // [------------------------------------------------]

        public static Dictionary<string, UIXApplication> StartedApplications { get => _startedApplications; set => _startedApplications = value; }
        public static List<SearchableApplication> AvailableApplications { get; set; } = new List<SearchableApplication>();
        public static Dictionary<string, UIXBackgroundProcess> StartedBackgroundProcesses { get => _startedBackgroundProcesses; set => _startedBackgroundProcesses = value; }
        public static List<InformationListItem> InformationList { get; set; } = new List<InformationListItem>();

        public static string TwitchAuthenticationToken { get; set; } = string.Empty;

        public static string IgdbExtGameSources { get; set; } = string.Empty;

        public static long CurrentPfid { get; set; } = 0;
        public static AppVersion AppVersion { get; set; } = new AppVersion();

        public static ApplicationLauncher AppLauncher { get; set; } = new ApplicationLauncher(null);

        public static AppConfig GetAppConfig()
        {
            return _appConfig;
        }

        public static DataBaseManager GetDataBaseManager()
        {
            return _databaseManager;
        }

        public static T1PROFI? GetCurrentProfile()
        {
            return _t1Profi;
        }

        public static void SetCurrentProfile(T1PROFI? t1Profi)
        {
            _t1Profi = t1Profi;
        }

        public static bool IsApplicationRunning(string fullName)
        {
            return _startedApplications.ContainsKey(fullName);
        }

        public static void SaveAppConfig()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string appConfigText = JsonSerializer.Serialize(GetAppConfig(), options);

            File.WriteAllText(new AppConfig().AppConfigPath, appConfigText);
        }

        public static void Initalize()
        {
            InitializeStartableApps();

            LoadAppConfig();

            AppVersion.Get();

            HandleBackup();

            CheckShowChangeLog();

            MigrationManager.MigrateIfNeeded();

            InitializeIGDBAuthTokenAndExternalGameSources();

            AppVersion.SetAppVersionInConfig();
        }



        public static void StartBackgroundProcesses(UIXApplication app)
        {
            // GlobalKeyInputProcess
            GlobalKeyInputProcess globalKeyInputProcess = app.GetBackgroundProcess<GlobalKeyInputProcess>();
            globalKeyInputProcess.CallDispatcher = app.CallDispatcher;
            globalKeyInputProcess.Start(20);

            // GameRunningProcess
            GameRunningProcess gameRunningProcess = app.GetBackgroundProcess<GameRunningProcess>();
            gameRunningProcess.CallDispatcher = app.CallDispatcher;
            gameRunningProcess.Initialize(new TXPROFI().ReadAll());
            gameRunningProcess.Start(500);

            StartedBackgroundProcesses.Add(typeof(GlobalKeyInputProcess).FullName!, globalKeyInputProcess);
            StartedBackgroundProcesses.Add(typeof(GameRunningProcess).FullName!, gameRunningProcess);
        }

        public static void StopBackgroundProcesses()
        {
            foreach (UIXBackgroundProcess process in StartedBackgroundProcesses.Values)
            {
                if (process.IsRunning())
                    process.Stop();
            }
        }

        public static void RestartGTNApplication()
        {
            var exePath = Environment.ProcessPath!;
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C timeout /t 3 > nul && start \"\" \"{exePath}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });

            System.Windows.Application.Current.Shutdown();
        }

        public static void ShutdownGTNApplication()
        {
            System.Windows.Application.Current.Shutdown();
        }

        public static void InitiateDataBaseManager()
        {
            _databaseManager = new DataBaseManager();
        }

        // [------------------------------------------------]
        // [------------------ PRIVATE ---------------------]
        // [------------------------------------------------]
        private static void InitializeIGDBAuthTokenAndExternalGameSources()
        {

            string clientId = GetAppConfig().AppSettings.TwitchIGDBClientID;
            string clientSecret = GetAppConfig().AppSettings.TwitchIGDBClientSecret;

            if (FnString.IsNullEmptyOrWhitespace(clientId) || FnString.IsNullEmptyOrWhitespace(clientSecret))
                return;

            try
            {
                TwitchAuthenticationToken = FnTwitchAuthentication.GetAccessToken(clientId, clientSecret);
            }
            catch (Exception)
            {
                TwitchAuthenticationToken = string.Empty;
            }

            if (FnString.IsNullEmptyOrWhitespace(TwitchAuthenticationToken))
            {
                InformationList.Add(new InformationListItem(CFMBOXIcon.Error, "Couldn't get auth-token for IGDB!"));
                return;
            }

            try
            {
                IgdbExtGameSources = FnTwitchAuthentication.GetExternalGameSources(new System.Net.Http.HttpClient(), clientId, TwitchAuthenticationToken);
            }
            catch (Exception)
            {
                IgdbExtGameSources = string.Empty;
            }


            if (FnString.IsNullEmptyOrWhitespace(IgdbExtGameSources))
                InformationList.Add(new InformationListItem(CFMBOXIcon.Error, "Couldn't get external game sources from IGDB!"));
        }

        private static void HandleBackup()
        {
            // Backup hier einspielen
            if (_appConfig.AppSettings.BackupType == BackupType.IMPORT_BACKUP)
            {
                try
                {
                    FnBackup.ImportBackup(_appConfig.AppSettings.BackupImportPath);
                }
                catch (Exception)
                {
                    InformationList.Add(new InformationListItem(CFMBOXIcon.Error, "Could not import backupfile:\n" + _appConfig.AppSettings.BackupImportPath + "."));
                }

                // Hier nochmal AppConfig laden, da vorher durch Backupimport überschrieben
                LoadAppConfig();
            }
            else
            {
                if (!_appConfig.AppSettings.AutoBackup)
                    return;

                string backupPath = _appConfig.AppSettings.BackupExportPath;

                if (FnSystem.IsDebug())
                {
                    backupPath = SpecialDirectories.MyDocuments + @"\GameTimeNext_Backup_dev";

                    if (!Directory.Exists(backupPath))
                        Directory.CreateDirectory(backupPath);
                }

                if (!Directory.Exists(backupPath))
                {
                    InformationList.Add(new InformationListItem(CFMBOXIcon.Error, "Could not create auto backup.\nBackup export path doesn't exist."));
                    return;
                }

                try
                {
                    FnBackup.CreateBackupSync(backupPath);
                }
                catch (Exception e)
                {
                    InformationList.Add(new InformationListItem(CFMBOXIcon.Error, "Something went wrong while auto-backup creation!"));
                }
            }
        }

        private static void CheckShowChangeLog()
        {
            string versionOldRaw = AppEnvironment.GetAppConfig().AppVersion;

            AppVersion versionOld = new AppVersion();
            versionOld.Get(versionOldRaw);

            AppVersion currentVersion = AppEnvironment.AppVersion;



            if (currentVersion.IsBiggerThan(versionOld))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("UpdateChanges_v" + currentVersion.VersionText);

                if (currentVersion.IsBeta)
                    sb.Append("beta");

                sb.Append(".txt");

                string text = string.Empty;

                try
                {
                    text = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Core", "Application", "UpdateChanges", sb.ToString()));
                }
                catch (Exception)
                {
                    return;
                }

                if (FnString.IsNullEmptyOrWhitespace(text))
                    return;

                InformationListItem infoListItem = new InformationListItem(CFMBOXIcon.Info, sb.ToString(), text);
                infoListItem.MBoxType = CFMBOXType.Memotext;
                infoListItem.Buttons = CFMBOXResult.Ok;

                InformationList.Add(infoListItem);

            }
        }

        private static void LoadAppConfig()
        {
            string appConfigText = File.ReadAllText(new AppConfig().AppConfigPath);

            if (appConfigText == null || appConfigText.Length == 0)
            {
                _appConfig = new AppConfig();
                return;
            }

            _appConfig = JsonSerializer.Deserialize<AppConfig>(appConfigText);
        }

        private static void InitializeStartableApps()
        {
            string jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Core", "Application", "StartableApps.json");

            if (!File.Exists(jsonFilePath))
            {
                InformationList.Add(new InformationListItem(CFMBOXIcon.Error, $"StartableApps.json not found at: {jsonFilePath}"));
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(jsonFilePath);
                var apps = JsonSerializer.Deserialize<List<SearchableApplication>>(jsonContent);

                if (apps != null && apps.Count > 0)
                {
                    AvailableApplications.AddRange(apps);
                }
                else
                {
                    InformationList.Add(new InformationListItem(CFMBOXIcon.Warning, "No applications found in StartableApps.json"));
                }
            }
            catch (Exception ex)
            {
                InformationList.Add(new InformationListItem(CFMBOXIcon.Error, $"Error loading StartableApps.json: {ex.Message}"));
            }
        }
    }
}
