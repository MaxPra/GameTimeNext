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
        private static TBL_PROFI? _tblProfi = new TBL_PROFI();
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

        public static TBL_PROFI? GetCurrentProfile()
        {
            return _tblProfi;
        }

        public static void SetCurrentProfile(TBL_PROFI? tblProfi)
        {
            _tblProfi = tblProfi;
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
