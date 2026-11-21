using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Playthroughs;
using GameTimeNext.Core.Application.Profiles.BackgroundProcesses;
using GameTimeNext.Core.Application.Profiles.Controller;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.LauncherIntegration;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.Utils;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static UIX.ViewController.Engine.FrameworkElements.UIXContextMenuFactory;

namespace GameTimeNext.Core.Application.Profiles.Selectors
{
    public class ProfilesView_ProfilesSelectorController : UIXSelectorControllerBase<ProfilesApp, ProfilesView, UIXListBox>
    {
        public ProfilesView_ProfilesSelectorController(UIXApplication app) : base(app)
        {
        }

        private T1PROFI GetSelectedProfile()
        {
            return GetSelectedItem<ProfilesListBoxItem>()?.ItemObject as T1PROFI
                ?? throw new InvalidOperationException("No profile is currently selected.");
        }

        // Rebuilds the list and selects the profile (0 = the current one or the first)
        private Task RebuildListAsync(long pfid)
        {
            return GetViewController<ProfilesViewController>().BuildProfilesListBoxAsync(pfid);
        }

        private static GameRunningProcess GetGameRunningProcess()
        {
            return (GameRunningProcess)AppEnvironment.StartedBackgroundProcesses[typeof(GameRunningProcess).FullName!];
        }

        protected void EV_SelectionChanged()
        {
            ProfilesListBoxItem? item = GetSelectedItem<ProfilesListBoxItem>();

            if (item?.ItemObject is not T1PROFI t1profi)
                return;

            AppEnvironment.SetCurrentProfile(t1profi);

            // Badge "New" of the automatic import: gone once the user selected the game (not the selection of the list build)
            if (item.COISNEW && !GetViewController<ProfilesViewController>().IsSelectingProfile && TFPROFI.ClearNewMarkOnSelection(GetApp(), t1profi))
                item.COISNEW = false;
        }

        protected void EV_CtxtOpening(FrameworkElement target)
        {
            if (target is not ListBoxItem listBoxItem || listBoxItem.DataContext is not ProfilesListBoxItem profi)
                return;

            BuildContextMenu(listBoxItem, (T1PROFI)profi.ItemObject!);
        }

        private static void BuildContextMenu(ListBoxItem lbi, T1PROFI t1profi)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("ProfilesListBoxContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            bool isSealed = TFPROFI.IsSealed(t1profi);

            // Unwrap (sealed game)
            if (isSealed)
                ProfilesContextMenuBuilder.BuildContextUnwrap(contextBuilder);

            // Always Edit and Delete in principle
            contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(""), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(""), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            // A sealed game offers nothing else until it is unwrapped
            if (isSealed)
            {
                lbi.ContextMenu = contextBuilder.Build();
                return;
            }

            // Game Folder
            ProfilesContextMenuBuilder.BuildContextShowGameFolder(contextBuilder, t1profi);

            // Playthrough
            ProfilesContextMenuBuilder.BuildContextPlayhtrougths(contextBuilder, t1profi);

            // Launcher
            ProfilesContextMenuBuilder.BuildContextLauncher(contextBuilder, t1profi);

            // Archive
            ProfilesContextMenuBuilder.BuildContextArchive(contextBuilder, t1profi);

            lbi.ContextMenu = contextBuilder.HasItems() ? contextBuilder.Build() : null;
        }

        /// <summary>
        /// Show the game in the library of its launcher
        /// </summary>
        protected void EV_ctxtOpenInLauncher()
        {
            FnLauncher.OpenInLauncher(GetSelectedProfile());
        }

        protected void EV_ctxtUnwrapGame()
        {
            GetViewController<ProfilesViewController>().UnwrapSelectedProfile();
        }

        protected void EV_ctxtEdit()
        {
            T1PROFI t1profi = GetSelectedProfile();

            ProfilesEditApp app = GetApp().GetApplication<ProfilesEditApp>(UIXApplicationStartTarget.Window);
            app.Edit(t1profi, async r =>
            {
                if (!r.Canceled)
                {
                    GameRunningProcess grp = GetGameRunningProcess();

                    T1PROFI t1profi = GetApp().GetDataManager<TXPROFI>().Read(r.PFID);
                    grp.AddT1profi(t1profi);
                    grp.AddExecutables(t1profi.PFID, FnExecutables.GetAllActiveExecutablesFromDBObj(t1profi));

                    await RebuildListAsync(r.PFID);
                }
            });
        }

        protected async Task EV_ctxtDelete()
        {
            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);
            T1PROFI selectedT1profi = GetSelectedProfile();
            bool deleted = false;

            // The profile and everything that is deleted with it must not be open elsewhere or monitored (record lock);
            // checked before asking, so nothing is half deleted
            FnRecordLock.Run(GetApp(), () =>
            {
                if (!cfmbox.ConfirmDelete(GetErrorMessage("GLO0002", $"\"{selectedT1profi.GANA}\"")))
                    return;

                string coverPath = Path.Combine(AppConfig.Storage.ProfileCoversDirectoryPath ?? string.Empty, selectedT1profi.PPFN);
                File.Delete(coverPath);

                TFPROFI.DeleteT1PROFIAndLinkedData(GetApp(), selectedT1profi);

                deleted = true;
            }, TFPROFI.GetProfileWithLinkedRecords(GetApp(), selectedT1profi.PFID).ToArray());

            if (deleted)
            {
                GameRunningProcess grp = GetGameRunningProcess();
                grp.RemoveT1profi(selectedT1profi);
                grp.RemoveExecutables(selectedT1profi.PFID);

                await RebuildListAsync(0);
            }
        }

        protected void EV_ctxtOpenGameFolder()
        {
            T1PROFI t1profi = GetSelectedProfile();

            if (FnString.IsNullEmptyOrWhitespace(t1profi.EXGF))
            {
                CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);
                cfmbox.Show(GetErrorMessage("PRO0006"), CFMBOXResult.Ok, CFMBOXIcon.Info);
                return;
            }

            if (!Directory.Exists(t1profi.EXGF))
            {
                CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);
                cfmbox.Show(GetErrorMessage("GLO0014", "game folder", t1profi.EXGF), CFMBOXResult.Ok, CFMBOXIcon.Info);
                return;
            }

            FnOpSys.OpenExplorer(t1profi.EXGF);
        }

        protected async Task EV_ctxtCompleteProfile()
        {
            T1PROFI t1profi = GetSelectedProfile();
            T1PLTHR t1plthr = TFPLTHR.GetCurrentPlaythrough(GetApp(), t1profi.PFID);

            TFPLTHR.UpdatePlaythroughStatus(GetApp(), ref t1plthr, TFPLTHR.PlaythroughStatus.Completed);

            await RebuildListAsync(t1profi.PFID);
        }

        protected void EV_ctxtStartNewPlaythrough()
        {
            T1PROFI t1profi = GetSelectedProfile();

            // A new playthrough becomes the current one: not while the game is monitored or the profile is being edited
            FnRecordLock.Run(GetApp(), () =>
            {
                PlaythroughEditApp app = GetApp().GetApplication<PlaythroughEditApp>();
                app.CreateNew(async r =>
                {
                    if (r.HasChanged)
                    {
                        await RebuildListAsync(t1profi.PFID);
                    }
                }, t1profi);
            }, t1profi);
        }

        protected async Task EV_ctxtCancelPlaythrough()
        {
            T1PROFI t1profi = GetSelectedProfile();
            T1PLTHR t1plthr = TFPLTHR.GetCurrentPlaythrough(GetApp(), t1profi.PFID);

            TFPLTHR.UpdatePlaythroughStatus(GetApp(), ref t1plthr, TFPLTHR.PlaythroughStatus.Canceled);

            await RebuildListAsync(t1profi.PFID);
        }

        protected async Task EV_ctxtArchiveProfile()
        {
            if (SetArchived(GetSelectedProfile(), true))
                await RebuildListAsync(0);
        }

        protected async Task EV_ctxtUnarchiveProfile()
        {
            if (SetArchived(GetSelectedProfile(), false))
                await RebuildListAsync(0);
        }

        // Read again under the record lock, so changes made elsewhere since the list was loaded aren't overwritten
        private bool SetArchived(T1PROFI t1profi, bool archived)
        {
            return FnRecordLock.Run(GetApp(), () =>
            {
                TXPROFI txprofi = GetApp().GetDataManager<TXPROFI>();
                T1PROFI current = txprofi.Read(t1profi.PFID) ?? t1profi;

                current.ARCH = archived;
                txprofi.Save(current);
            }, t1profi);
        }

        protected void EV_ctxtShowPlaythroughs()
        {
            T1PROFI t1profi = GetSelectedProfile();
            PlaythroughsApp app = GetApp().GetApplication<PlaythroughsApp>();
            app.AppRunSelections = new PlaythroughAppRunSelections
            {
                Pfid = t1profi.PFID,
                Gana = t1profi.GANA
            };

            app.ShowWindow(async r =>
            {
                if (r.HasChanged)
                {
                    await RebuildListAsync(t1profi.PFID);
                }
            });
        }
    }
}
