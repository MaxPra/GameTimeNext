using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.DataBase;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;

namespace GameTimeNext.Core.Framework
{
    internal class AppEnvironment
    {
        private static AppConfig _appConfig = new AppConfig();
        private static DataBaseManager _databaseManager = new DataBaseManager();
        private static T1PROFI? _t1Profi = new T1PROFI();
        private static ContentControl _loader = new ContentControl();

        // [------------------------------------------------]
        // [------------------ PUBLIC ----------------------]
        // [------------------------------------------------]

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


        public static void SaveAppConfig()
        {
            string appConfigText = JsonSerializer.Serialize(GetAppConfig());

            File.WriteAllText(new AppConfig().AppConfigPath, appConfigText);
        }

        public static void ShowLoader()
        {
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _loader.Visibility = System.Windows.Visibility.Visible;
            }, System.Windows.Threading.DispatcherPriority.Render);
        }

        public static void HideLoader()
        {
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _loader.Visibility = System.Windows.Visibility.Collapsed;
            }, System.Windows.Threading.DispatcherPriority.Render);
        }

        public static void SetLoader(ContentControl control)
        {
            _loader = control;
        }

        public static void Initalize()
        {
            LoadAppConfig();

            InitiateDataBaseManager();
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
    }
}
