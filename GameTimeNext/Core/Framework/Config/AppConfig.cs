using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        public AppConfig()
        {

        }



    }
}
