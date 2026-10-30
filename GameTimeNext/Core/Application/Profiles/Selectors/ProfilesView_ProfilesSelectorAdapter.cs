using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.Utils;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Profiles.Selectors
{
    /// <summary>
    /// Game tiles of the library: all profiles matching the library filter and the name search.
    /// </summary>
    public class ProfilesView_ProfilesSelectorAdapter : UIXSelectorAdapterBase<ProfilesApp, ProfilesView, UIXListBox, ProfilesListBoxItem>
    {
        // UI thread time per portion of tiles; the rest of a frame stays free for rendering and the loader animation
        private const double TilePortionBudgetMs = 8;

        public ProfilesView_ProfilesSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        /// <summary>
        /// True if the last load was restricted by the library filter.
        /// </summary>
        public bool FilterActive { get; private set; }

        protected override async Task<IEnumerable<ProfilesListBoxItem>> ExecuteAsync(CancellationToken token)
        {
            string nameSearch = GetView().tbSearchProfileName.Text;

            (List<ProfilesListBoxItem> profileItems, bool filterActive) = await Task.Run(() =>
            {
                (List<T1PROFI> t1profis, bool isFiltered) = TFPROFI.GetFilteredProfiles(GetApp().FilterCache, nameSearch);

                return (BuildProfileListBoxItems(t1profis), isFiltered);
            }, token);

            FilterActive = filterActive;

            return profileItems;
        }

        /// <summary>
        /// The tiles are added in portions, each followed by layout and a rendered frame: a game case (3D template) is
        /// expensive to create, and creating all of them in one layout pass blocked the UI thread, so the loader
        /// animation (driven by the UI thread) stood still while the library was loading. The portion size adapts to
        /// the measured time per tile.
        /// </summary>
        protected override async Task SetItemsAsync(ObservableCollection<ProfilesListBoxItem> items, CancellationToken token)
        {
            ObservableCollection<ProfilesListBoxItem> shownItems = new ObservableCollection<ProfilesListBoxItem>();
            GetSelector().ItemsSource = shownItems;

            Stopwatch watch = new Stopwatch();
            int portionSize = 1;
            int index = 0;

            while (index < items.Count)
            {
                int portionEnd = Math.Min(items.Count, index + portionSize);
                int added = portionEnd - index;

                watch.Restart();

                for (; index < portionEnd; index++)
                    shownItems.Add(items[index]);

                // Layout (creates the new cases) and rendering have a higher priority: they run before this continues
                await Dispatcher.Yield(DispatcherPriority.Background);

                // A newer load has started: it fills the list
                if (token.IsCancellationRequested)
                    return;

                double msPerTile = Math.Max(0.25, watch.Elapsed.TotalMilliseconds / added);
                portionSize = Math.Clamp((int)(TilePortionBudgetMs / msPerTile), 1, 24);
            }
        }

        private List<ProfilesListBoxItem> BuildProfileListBoxItems(List<T1PROFI> t1profiles)
        {
            List<ProfilesListBoxItem> result = new List<ProfilesListBoxItem>(t1profiles.Count);
            TXCTABD txctabd = new TXCTABD();

            for (int i = 0; i < t1profiles.Count; i++)
            {
                T1PROFI prof = t1profiles[i];
                try
                {
                    T1CTABD? t1ctabdPf = txctabd.Read("pF", prof.PLAFO);
                    T1CTABD? t1ctabdIm = t1ctabdPf == null
                        ? null
                        : txctabd.Read("iM", t1ctabdPf.PARM2);

                    string platformSymbolFile = t1ctabdIm?.PARM1 ?? string.Empty;
                    string platformSymbolFullPath =
                        AppConfig.Storage.ImagesSymbolsDirectoryPath +
                        platformSymbolFile;

                    ProfilesListBoxItem item = new ProfilesListBoxItem
                    {
                        ItemObject = prof,
                        COCOVIM = CFProfilesApp.LoadProfileCover(prof.PPFN),
                        COISPLA = FnSystem.IsExeFoundInPath(prof.EXGF),
                        COPLFPA = File.Exists(platformSymbolFullPath) ? FnImage.LoadImageWithoutLock(platformSymbolFullPath, 50, 50) : null,
                        COISSEA = TFPROFI.IsSealed(prof)
                    };

                    result.Add(item);
                }
                catch (Exception ex)
                {
                    FnLog.AddError(GetApp(), $"Error while building profile list box item {prof.GANA}. Continued with next item", ex);
                }
            }

            return result;
        }
    }
}
