using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.Profiles.BackgroundProcesses;
using GameTimeNext.Core.Application.Settings;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.DataBase;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;

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
        public static long CurrentPfid { get; set; } = 0;

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

            InitiateDataBaseManager();
        }

        public static void StartBackgroundProcesses(UIXApplication app)
        {
            // GlobalKeyInputProcess
            GlobalKeyInputProcess globalKeyInputProcess = app.GetBackgroundProcess<GlobalKeyInputProcess>();
            globalKeyInputProcess.CallDispatcher = app.CallDispatcher;
            globalKeyInputProcess.Start(20);

            StartedBackgroundProcesses.Add(typeof(GlobalKeyInputProcess).FullName!, globalKeyInputProcess);
        }

        public static void InitiateDataBaseManager()
        {
            _databaseManager = new DataBaseManager();
        }

        // [------------------------------------------------]
        // [------------------ PRIVATE ---------------------]
        // [------------------------------------------------]

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
            // Aufrufbare Anwendungen hinzufügen
            AvailableApplications.Add(new SearchableApplication
            {
                Code = "P",
                Name = "Profiles",
                ClassName = typeof(ProfilesApp).FullName
            });
            AvailableApplications.Add(new SearchableApplication
            {
                Code = "SE",
                Name = "Settings",
                ClassName = typeof(SettingsApp).FullName
            });
        }
    }
}
