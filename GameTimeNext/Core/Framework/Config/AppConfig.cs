using GameTimeNext.Core.Application.General.UserSettings;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Utils;
using Microsoft.VisualBasic.FileIO;
using System.IO;
using System.Text.Json.Serialization;
using UIX.ViewController.Engine.Utils;
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
        public string DevBackupFolderPath
        {
            get
            {
                string temp = SpecialDirectories.MyDocuments + @"\GameTimeNext_Backup";

                // Modi
                if (AppEnvironment.StartArguments.ContainsKey("m") && !FnString.IsNullEmptyOrWhitespace(AppEnvironment.StartArguments["m"]))
                {
                    temp += "_m" + AppEnvironment.StartArguments["m"];
                }

                temp += "_dev";
                return temp;
            }
        }

        [JsonIgnore]
        public string LogFilePath
        {
            get
            {
                string logFilePath = AppFolderPath + Path.DirectorySeparatorChar + $"ApplicationLog_{DateTime.Now:yyyy-MM-dd}.log";

                return logFilePath;
            }
        }

        [JsonIgnore]
        public string DevGeneratedFilesPath
        {
            get
            {
                string devGeneratedFilesPath = AppDataLocalPath + Path.DirectorySeparatorChar + $"genClass";


                return devGeneratedFilesPath;
            }
        }

        [JsonIgnore]
        public string AppFolderPathNormal
        {
            get
            {
                string temp = RootFolderPath + Path.DirectorySeparatorChar + "GameTimeNXT";

                // Modi
                if (AppEnvironment.StartArguments.ContainsKey("m") && !FnString.IsNullEmptyOrWhitespace(AppEnvironment.StartArguments["m"]))
                {
                    temp += "_m" + AppEnvironment.StartArguments["m"];
                }

                return temp;
            }
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
        public string ImagesSymbolsPath
        {
            get
            {
                return AppFolderPath + Path.DirectorySeparatorChar + "images_and_symbols";
            }
        }

        [JsonIgnore]
        public string ImagesSymbolsPathDefault
        {
            get
            {
                return ImagesSymbolsPath + Path.DirectorySeparatorChar + "default";
            }
        }

        [JsonIgnore]
        public string ImagesSymbolsPathUser
        {
            get
            {
                return ImagesSymbolsPath + Path.DirectorySeparatorChar + "user";
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
