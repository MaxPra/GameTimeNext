using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using System.IO;
using System.Windows;
using UIX.ViewController.Engine.FrameworkElements;
using static UIX.ViewController.Engine.FrameworkElements.UIXContextMenuFactory;

namespace GameTimeNext.Core.Application.Profiles
{
    /// <summary>
    /// Builds the contextMenu Items for the profiles context menu
    /// </summary>
    public class ProfilesContextMenuBuilder
    {

        public static Style contextMenuItemStyle = (Style)System.Windows.Application.Current.FindResource("ModernContextMenuItemStyle");
        public static Style contextMenuStyle = (Style)System.Windows.Application.Current.FindResource("ModernContextMenuStyle");
        public static Style contextMenuSeparatorStyle = (Style)System.Windows.Application.Current.FindResource("ModernContextMenuSeparatorStyle");

        public static void BuildContextPlayhtrougths(ContextMenuBuilder contextBuilder, T1PROFI t1profi)
        {
            contextBuilder.AddSeparator(contextMenuSeparatorStyle);

            MenuItemBuilder playthroughSubMenu = UIXContextMenuFactory.Item("ctxtSubMenuPlaythroughs", "Playthrough");
            playthroughSubMenu.SetStyle(contextMenuItemStyle);

            T1PLTHR currentPlaythrough = TFPLTHR.GetCurrentPlaythrough(t1profi.PFID);

            if (currentPlaythrough != null && !currentPlaythrough.PTCO && !currentPlaythrough.PTCA)
            {
                playthroughSubMenu.AddItem("ctxtCompleteProfile", "Current playthrough completed", icon: UIXContextMenuFactory.CreateMdlIcon("\uE930"), itemStyle: contextMenuItemStyle);
                playthroughSubMenu.AddItem("ctxtCancelPlaythrough", "Cancel current playthrough", icon: UIXContextMenuFactory.CreateMdlIcon("\uE711"), itemStyle: contextMenuItemStyle);
            }
            else
            {
                playthroughSubMenu.AddItem("ctxtStartNewPlaythrough", "Start new playthrough", icon: UIXContextMenuFactory.CreateMdlIcon("\uE72C"), itemStyle: contextMenuItemStyle);
            }

            contextBuilder.AddSubMenu(playthroughSubMenu);
        }

        public static void BuildContextArchive(ContextMenuBuilder contextBuilder, T1PROFI t1profi)
        {
            // Archivieren hinzufügen
            if (t1profi.ARCH)
                contextBuilder.AddItem("ctxtUnarchiveProfile", "Unarchive profile", icon: UIXContextMenuFactory.CreateMdlIcon("\uE8B7"), itemStyle: contextMenuItemStyle);

            else
                contextBuilder.AddItem("ctxtArchiveProfile", "Archive profile", icon: UIXContextMenuFactory.CreateMdlIcon("\uE8B7"), itemStyle: contextMenuItemStyle);
        }

        public static void BuildContextSteam(ContextMenuBuilder contextBuilder, T1PROFI t1profi)
        {
            if (t1profi.SAID == 0)
                return;

            MenuItemBuilder steamSubMenu = UIXContextMenuFactory.Item("ctxtSubMenuSteam", "Steam");
            steamSubMenu.SetStyle(contextMenuItemStyle);

            steamSubMenu.AddItem("ctxtOpenSteamLibrary", "Show steam library", icon: UIXContextMenuFactory.CreateMdlIcon("\uE7FC"), itemStyle: contextMenuItemStyle);

            contextBuilder.AddSubMenu(steamSubMenu);

            contextBuilder.AddSeparator(ProfilesContextMenuBuilder.contextMenuSeparatorStyle);
        }

        public static void BuildContextShowGameFolder(ContextMenuBuilder contextBuilder, T1PROFI t1profi)
        {
            if (!Directory.Exists(t1profi.EXGF))
                return;

            contextBuilder.AddSeparator(contextMenuSeparatorStyle);

            contextBuilder.AddItem("ctxtOpenGameFolder", "Open Gamefolder", icon: UIXContextMenuFactory.CreateMdlIcon("\uE838"), itemStyle: contextMenuItemStyle);
        }
    }
}
