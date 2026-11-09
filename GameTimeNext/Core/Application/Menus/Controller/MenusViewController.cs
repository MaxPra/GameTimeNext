using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Menus.DataWrapper;
using GameTimeNext.Core.Application.Menus.ViewModel;
using GameTimeNext.Core.Application.Menus.Views;
using GameTimeNext.Core.Application.TableObjects;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

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
            // Set loader texts
            GetApp().Loader.SetRandomTexts(
                                            "Loading menus...",
                                            "Building the menu tree...",
                                            "Folding and unfolding folders...",
                                            "Arranging every menu entry...",
                                            "Planting the menu tree...",
                                            "Checking which entries are developer tools..."
                                            );

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
            if (!await GetView().dgMenus.LoadDataAsync())
                return;

            MenusTreeGridRow? selectedRow = selectMeid == 0
                ? null
                : GetView().dgMenus.Items.OfType<MenusTreeGridRow>().FirstOrDefault(r => (r.RowObject as T1MENUE)?.MEID == selectMeid);

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
        }

        /// <summary>
        /// Opens a new, unsaved entry of the given type below the parent in the edit area.
        /// </summary>
        public void CreateNewEntry(string mety, long mepa)
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
