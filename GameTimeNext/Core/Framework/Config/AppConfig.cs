using System.IO;
using System.Text.Json.Serialization;

namespace GameTimeNext.Core.Framework.Config
{
    internal class AppConfig
    {
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
                return RootFolderPath + Path.DirectorySeparatorChar + "GameTimeNXT";
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
        public string SteamGridDbAPIKey
        {
            get
            {
                string? apiKey = Environment.GetEnvironmentVariable("SteamGridDB_API_KEY");

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    throw new InvalidOperationException("Environmentvariable 'STEAMGRIDDB_API_KEY' missing.");
                }

                return apiKey;
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

                return AppDataLocalPath + Path.DirectorySeparatorChar + "Temp_SteamGridDBCovers";
            }
        }

        public AppConfig()
        {

        }



    }
}
