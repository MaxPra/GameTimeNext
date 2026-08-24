using GameTimeNext.Core.Application.General.UserSettings;
using GameTimeNext.Core.Application.TableObjects;
using static GameTimeNext.Core.Application.Profiles.Controller.ProfilesViewController;

namespace GameTimeNext.Core.Framework.Config
{
    internal partial class AppConfig
    {
        #region External
        public FilterCache FilterCache { get; set; } = new FilterCache();
        public AppSettings AppSettings { get; set; } = new AppSettings();
        public UserSettings UserSettings { get; set; } = new UserSettings();

        public string AppVersion { get; set; } = string.Empty;
        #endregion

        public AppConfig()
        {
            EnsurePathsExist();
        }
    }
}
