using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Menus.ViewModel;
using GameTimeNext.Core.Application.Menus.Views;
using GameTimeNext.Core.Application.TableObjects;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Menus.Selectors
{
    public class MenusView_MenusSelectorAdapter : UIXSelectorAdapterBase<MenusApp, MenusView, UIXTreeGrid, MenusTreeGridRow>
    {
        public MenusView_MenusSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        protected override async Task<IEnumerable<MenusTreeGridRow>> ExecuteAsync(CancellationToken token)
        {
            string filter = GetView().txbDescription.Text;

            return await Task.Run(() => BuildRows(filter), token);
        }

        /// <summary>
        /// Rows in tree order: each entry followed by its children, siblings by sort order.
        /// With a filter, only matching entries and the folders above them are shown.
        /// </summary>
        private List<MenusTreeGridRow> BuildRows(string filter)
        {
            List<T1MENUE> entries = TFMENUE.GetAllMenuEntries();
            Dictionary<long, T1MENUE> entriesById = entries.ToDictionary(e => e.MEID);
            ILookup<long, T1MENUE> childrenByParent = entries.ToLookup(e => entriesById.ContainsKey(e.MEPA) ? e.MEPA : 0);

            HashSet<long>? visibleIds = null;

            if (!FnString.IsNullEmptyOrWhitespace(filter))
            {
                visibleIds = new HashSet<long>();

                foreach (T1MENUE entry in entries.Where(e => e.MEDE.Contains(filter, StringComparison.CurrentCultureIgnoreCase)))
                {
                    // Add the entry and its parent folders
                    for (T1MENUE? current = entry; current != null && visibleIds.Add(current.MEID); )
                        current = entriesById.GetValueOrDefault(current.MEPA);
                }
            }

            List<MenusTreeGridRow> rows = new List<MenusTreeGridRow>();

            AddRows(rows, childrenByParent, 0, new UIXStructureKey(), visibleIds, new HashSet<long>());

            return rows;
        }

        private void AddRows(List<MenusTreeGridRow> rows, ILookup<long, T1MENUE> childrenByParent, long mepa, UIXStructureKey parentKey, HashSet<long>? visibleIds, HashSet<long> addedIds)
        {
            foreach (T1MENUE t1menue in childrenByParent[mepa])
            {
                if (visibleIds != null && !visibleIds.Contains(t1menue.MEID))
                    continue;

                // Guards against cycles in the parent chain
                if (!addedIds.Add(t1menue.MEID))
                    continue;

                bool isFolder = t1menue.METY == "01";

                MenusTreeGridRow row = GetSelector().CreateNewRow<MenusTreeGridRow>();
                row.StructureKey = parentKey.Add(t1menue.MEID);
                row.IsExpanded = true;
                row.IsFolder = isFolder;
                row.Icon = CFMenusApp.GetEntryIcon(t1menue);
                row.COMEDE = t1menue.MEDE;
                row.COMETY = TFCTABD.GetDescription("mN", t1menue.METY);
                row.COMEAP = isFolder ? string.Empty : t1menue.MEAP;
                row.RowObject = t1menue;

                rows.Add(row);

                AddRows(rows, childrenByParent, t1menue.MEID, row.StructureKey, visibleIds, addedIds);
            }
        }
    }
}
