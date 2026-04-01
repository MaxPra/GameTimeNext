using GameTimeNext.Core.Application.General.UserSettings;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Utils;
using System.IO;
using System.Text.Json.Serialization;
using static GameTimeNext.Core.Application.Profiles.Controller.ProfilesViewController;

namespace GameTimeNext.Core.Framework.Config
{
    internal class AppConfig
    {

        #region Internal
        /// <summary>
        /// Liefert den RootFolder (Dokumente)
        /// </summary>
        [JsonIgnore]
        public string RootFolderPath
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); }
        }

        /// <summary>
        /// Liefert den App-Folder Pfad
        /// </summary>
        [JsonIgnore]
        public string AppFolderPath
        {
            get
            {
                string appFolderPath = AppFolderPathNormal;

                if (FnSystem.IsDebug())
                {
                    appFolderPath += "_dev";
                }

                return appFolderPath;
            }
        }

        [JsonIgnore]
        public string AppFolderPathNormal
        {
            get => RootFolderPath + Path.DirectorySeparatorChar + "GameTimeNXT";
        }

        [JsonIgnore]
        public string DataFolderPath
        {
            get
            {
                return AppFolderPath + Path.DirectorySeparatorChar + "Data";
            }
        }

        [JsonIgnore]
        public string CoverFolderPath
        {
            get
            {
                return DataFolderPath + Path.DirectorySeparatorChar + "profile_covers";
            }
        }

        [JsonIgnore]
        public string CoverFolderTempPath
        {
            get
            {
                return CoverFolderPath + Path.DirectorySeparatorChar + "temp_covers";
            }
        }

        [JsonIgnore]
        public string DataBaseFilePath
        {
            get
            {
                return DataFolderPath + Path.DirectorySeparatorChar + "GameTimeNextDb.db";
            }
        }

        [JsonIgnore]
        public string AppConfigPath
        {
            get
            {
                return AppFolderPath + Path.DirectorySeparatorChar + "AppConfig.gtnconf";
            }
        }

        [JsonIgnore]
        public string AppDataLocalPath
        {
            get
            {

                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + Path.DirectorySeparatorChar + "GameTimeNext";
            }
        }

        [JsonIgnore]
        public string AppDataLocalPathSteamGridDBCovers
        {
            get
            {

                return AppDataLocalPath + Path.DirectorySeparatorChar + "tmp_steamgriddbcovers";
            }
        }

        [JsonIgnore]
        public string AppDataLocalPathTempCovers
        {
            get
            {
                return AppDataLocalPath + Path.DirectorySeparatorChar + "tmp_covers";
            }
        }
        #endregion



        #region External

        public FilterCache FilterCache { get; set; } = new FilterCache();
        public AppSettings AppSettings { get; set; } = new AppSettings();
        public UserSettings UserSettings { get; set; } = new UserSettings();

        public string AppVersion { get; set; } = string.Empty;

        #endregion

        public AppConfig()
        {

        }
    }
}
