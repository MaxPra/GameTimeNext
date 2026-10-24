using Microsoft.Win32;

namespace GameTimeNext.Core.Framework.Utils
{
    public class FnSystemDialogs
    {

        /// <summary>
        /// Shows the Windows file dialog
        /// </summary>
        /// <param name="title"></param>
        /// <param name="filter"></param>
        /// <param name="multiselect"></param>
        /// <returns>chosen file path</returns>
        public static string ShowFileDialog(string title, string filter, bool multiselect)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Title = title;
            dialog.Filter = filter;
            dialog.Multiselect = multiselect;

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                return dialog.FileName;
            }

            return string.Empty;
        }

        /// <summary>
        /// Shows the Windows folder dialog
        /// </summary>
        /// <param name="title"></param>
        /// <param name="multiselect"></param>
        /// <returns>chosen folder path</returns>
        public static string ShowFolderDialog(string title, bool multiselect)
        {
            OpenFolderDialog dialog = new OpenFolderDialog();

            dialog.Title = title;
            dialog.Multiselect = multiselect;

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                return dialog.FolderName;
            }

            return string.Empty;
        }
    }
}
