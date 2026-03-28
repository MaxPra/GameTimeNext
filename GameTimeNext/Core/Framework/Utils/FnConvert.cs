namespace GameTimeNext.Core.Framework.Utils
{
    public class FnConvert
    {
        /// <summary>
        /// Wandelt einen String in eine liste um.
        /// </summary>
        /// <param name="listStr"></param>
        /// <returns></returns>
        public static List<string> ToList(string listStr)
        {
            List<string> list = listStr.Split(";").ToList();

            return list;
        }

        /// <summary>
        /// Wandelt eine Liste in einen String um.
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

        public static double ToDouble(string str)
        {
            double result = 0;

            if (Double.TryParse(str, out result))
            {
                return result;
            }

            return 0;
        }
    }
}
