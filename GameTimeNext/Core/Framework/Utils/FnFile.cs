using System.IO;

namespace GameTimeNext.Core.Framework.Utils
{
    public class FnFile
    {
        public static void EnsureFileExists(string path)
        {
            if (!File.Exists(path))
            {
                FileStream fs = File.Create(path);
                fs.Close();
            }
        }
    }
}
