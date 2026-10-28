using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Utils;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Menus
{
    public static class CFMenusApp
    {
        /// <summary>
        /// Icon of a menu entry: folder icon for folders, the application's icon for application calls.
        /// </summary>
        public static string GetEntryIcon(T1MENUE t1menue)
        {
            if (t1menue.METY == "01")
                return UIXMdlIcons.Folder;

            string icon = FnApplications.GetApplicationIcon(t1menue.MEAP);

            return FnString.IsNullEmptyOrWhitespace(icon) ? UIXMdlIcons.Play : icon;
        }
    }
}
