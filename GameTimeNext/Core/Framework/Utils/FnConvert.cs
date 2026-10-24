using UIX.ViewController.Engine.Migration.Types;

namespace GameTimeNext.Core.Framework.Utils
{
    public class FnConvert
    {
        /// <summary>
        /// Converts a string into a list.
        /// </summary>
        /// <param name="listStr"></param>
        /// <returns></returns>
        public static List<string> ToList(string listStr)
        {
            List<string> list = listStr.Split(";").ToList();

            return list;
        }

        /// <summary>
        /// Converts a list into a string.
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static string FromListToString(List<string> list)
        {

            string listStr = "";

            for (int i = 0; i < list.Count; i++)
            {
                listStr += list[i].Trim();

                if (i + 1 == list.Count)
                    listStr += ";";
            }

            return listStr;
        }

        /// <summary>
        /// Converts UI text (current culture, see UIXSqliteDataType.ConversionType.UI) into a double; 0 if empty or invalid.
        /// </summary>
        public static double ToDouble(string str)
        {
            try
            {
                object? value = UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.UI, UIXSqliteDataType.ConversionType.CSharp, str, UIXSqliteDataType.GetByCsType(typeof(double)));
                return value is double result ? result : 0;
            }
            catch (FormatException)
            {
                return 0;
            }
        }
    }
}
