using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Menus.DataWrapper;
using GameTimeNext.Core.Application.Menus.ViewModel;
using GameTimeNext.Core.Application.Menus.Views;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using UIX.ViewController.Engine.Viewmodel;
using static UIX.ViewController.Engine.FrameworkElements.UIXContextMenuFactory;

namespace GameTimeNext.Core.Application.Menus.Controller
{
    public class MenusViewController : UIXViewControllerBase<MenusApp, MenusView>
    {
        private MenusDataWrapper? _dataWrapper;

        public MenusViewController(UIXApplication app) : base(app)
        {
        }

        #region Event-Pipeline-Methods
        protected override void Init()
        {
            _dataWrapper = GetDataWrapper<MenusDataWrapper>();
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {
            // Nothing is selected yet: no edit area
            FnControls.SetVisible(GetView().cpMenuDetail, false);
        }

        protected override async Task BuildFirstImplAsync()
        {
            await BuildTreeAsync(0);
        }

        protected override void BuildImpl()
        {
        }

        protected override void CheckImpl()
        {
        }

        protected override void FillViewImpl()
        {
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
            FnControls.SetVisible(GetView().cpMenuDetail, true);
        }

        protected override void Event_Closing()
        {
            Exit(true);
        }
        #endregion

        /// <summary>
        /// Rebuilds the tree and selects the entry with the given ID, which opens it in the edit area.
        /// 0 = no selection, the edit area is closed.
        /// </summary>
        public async Task BuildTreeAsync(long selectMeid)
        {
            GetApp().Loader.Begin();

            try
            {
                string filter = GetView().txbDescription.Text;

                List<MenusTreeGridRow> rows = await Task.Run(() => BuildRows(filter));

                View.Dispatcher.Invoke(() =>
                {
                    GetView().dgMenus.ItemsSource = new ObservableCollection<MenusTreeGridRow>(rows);

                    MenusTreeGridRow? selectedRow = selectMeid == 0 ? null : rows.FirstOrDefault(r => (r.RowObject as T1MENUE)?.MEID == selectMeid);

                    if (selectedRow != null)
                    {
                        GetView().dgMenus.ExpandTo(selectedRow);
                        GetView().dgMenus.SelectedItem = selectedRow;
                        GetView().dgMenus.ScrollIntoView(selectedRow);
                    }
                    else
                    {
                        CloseDetail();
                    }
                }, DispatcherPriority.Normal);
            }
            finally
            {
                GetApp().Loader.Stop();
            }
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

                MenusTreeGridRow row = GetView().dgMenus.CreateNewRow<MenusTreeGridRow>();
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

        /// <summary>
        /// Opens a new, unsaved entry of the given type below the parent in the edit area.
        /// </summary>
        private void CreateNewEntry(string mety, long mepa)
        {
            T1MENUE t1menue = new TXMENUE().CreateNew();
            t1menue.METY = mety;
            t1menue.MEPA = mepa;
            t1menue.MEOR = TFMENUE.GetNextSortOrder(mepa);

            GetView().dgMenus.SelectedItem = null;

            _dataWrapper!.Set(t1menue);
            FnControls.SetVisible(GetView().cpMenuDetail, true);

            _dataWrapper.TargetController.Open(true);
        }

        /// <summary>
        /// Closes the edit area without saving and clears the selection.
        /// </summary>
        public void CloseDetail()
        {
            GetView().dgMenus.SelectedItem = null;

            _dataWrapper!.Set(null);
            FnControls.SetVisible(GetView().cpMenuDetail, false);
        }

        private T1MENUE? GetSelectedEntry()
        {
            return (GetView().dgMenus.SelectedItem as MenusTreeGridRow)?.RowObject as T1MENUE;
        }

        private void BuildContextMenu(DataGridRow dgRow, T1MENUE t1menue)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("MenusContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            // First entry = double click
            contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Edit), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            if (t1menue.METY == "01")
            {
                contextBuilder.AddItem("ctxtNewFolder", "New folder", icon: UIXContextMenuFactory.CreateMdlIcon(""), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
                contextBuilder.AddItem("ctxtNewApplication", "New application call", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Add), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            }

            contextBuilder.AddItem("ctxtMoveUp", "Move up", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Up), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtMoveDown", "Move down", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Down), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Delete), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            dgRow.ContextMenu = contextBuilder.Build();
        }

        protected void EV_dgMenus_CtxtOpening(FrameworkElement target)
        {
            if (target is not DataGridRow row)
                return;

            if (row.DataContext is not MenusTreeGridRow menuRow || menuRow.RowObject is not T1MENUE t1menue)
                return;

            GetView().dgMenus.SelectedItem = menuRow;

            BuildContextMenu(row, t1menue);

            row.ContextMenu!.PlacementTarget = row;
            row.ContextMenu.Placement = PlacementMode.MousePoint;
            row.ContextMenu.IsOpen = true;
        }

        protected void EV_ctxtEdit()
        {
            GetApp().MenusDetailView!.txbDescription.Focus();
        }

        protected void EV_ctxtNewFolder()
        {
            if (GetSelectedEntry() is T1MENUE parent)
                CreateNewEntry("01", parent.MEID);
        }

        protected void EV_ctxtNewApplication()
        {
            if (GetSelectedEntry() is T1MENUE parent)
                CreateNewEntry("02", parent.MEID);
        }

        protected async Task EV_ctxtMoveUp()
        {
            if (GetSelectedEntry() is T1MENUE t1menue && TFMENUE.Move(t1menue, -1))
                await BuildTreeAsync(t1menue.MEID);
        }

        protected async Task EV_ctxtMoveDown()
        {
            if (GetSelectedEntry() is T1MENUE t1menue && TFMENUE.Move(t1menue, 1))
                await BuildTreeAsync(t1menue.MEID);
        }

        protected async Task EV_ctxtDelete()
        {
            if (GetSelectedEntry() is not T1MENUE t1menue)
                return;

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);

            // A folder takes everything below it along
            bool hasChildren = TFMENUE.GetChildren(t1menue.MEID).Count > 0;
            string message = hasChildren
                ? GetErrorMessage("GLO0002", $"the folder \"{t1menue.MEDE}\"")
                : GetErrorMessage("GLO0001", $"\"{t1menue.MEDE}\"");

            if (cfmbox.Show(message, CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question) != CFMBOXResult.Yes)
                return;

            TFMENUE.DeleteWithChildren(t1menue.MEID);

            await BuildTreeAsync(t1menue.MEPA);
        }

        protected void EV_btnAddFolder()
        {
            CreateNewEntry("01", 0);
        }

        protected void EV_btnAddApplication()
        {
            CreateNewEntry("02", 0);
        }

        protected async Task EV_btnRefresh()
        {
            await BuildTreeAsync(GetSelectedEntry()?.MEID ?? 0);
        }
    }
}
