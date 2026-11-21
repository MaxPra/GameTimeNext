using Arcora.Core.Application.General.UserSettings;
using Arcora.Core.Application.TableObjects;
using static Arcora.Core.Application.Profiles.Controller.ProfilesViewController;

namespace Arcora.Core.Framework.Config
{
    internal partial class AppConfig
    {
        #region External
        public FilterCache FilterCache { get; set; } = new FilterCache();

        /// <summary>Library filter of the Big Picture mode, separate from the one of the profiles view.</summary>
        public FilterCache BigPictureFilterCache { get; set; } = new FilterCache();
        public AppSettings AppSettings { get; set; } = new AppSettings();
        public UserSettings UserSettings { get; set; } = new UserSettings();

        public string AppVersion { get; set; } = string.Empty;

        public string RemoteMonitoringPairingCode { get; set; } = string.Empty;
        #endregion

        public AppConfig()
        {
            EnsurePathsExist();
        }
    }
}
