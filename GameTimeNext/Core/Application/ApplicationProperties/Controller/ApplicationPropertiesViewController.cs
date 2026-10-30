using GameTimeNext.Core.Application.ApplicationProperties.DataWrapper;
using GameTimeNext.Core.Application.ApplicationProperties.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.ApplicationProperties.Controller
{
    public class ApplicationPropertiesViewController : UIXViewControllerBase<ApplicationPropertiesApp, ApplicationPropertiesView>
    {
        private ApplicationPropertiesDataWrapper? _dataWrapper;

        public ApplicationPropertiesViewController(UIXApplication app) : base(app)
        {
        }

        #region Event-Pipeline-Methods
        protected override void Init()
        {
            _dataWrapper = GetDataWrapper<ApplicationPropertiesDataWrapper>();
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {
            // Nothing is selected yet: no edit area
            FnControls.SetVisible(GetView().cpApplicationPropertiesDetail, false);
        }

        protected override async Task BuildFirstImplAsync()
        {
            await BuildListAsync(string.Empty);
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
            FnControls.SetVisible(GetView().cpApplicationPropertiesDetail, true);
        }
        #endregion

        /// <summary>
        /// Rebuilds the list and selects the definition with the given ID, which opens it in the edit area.
        /// Empty = no selection, the edit area is closed.
        /// </summary>
        public async Task BuildListAsync(string selectApdid)
        {
            if (!await GetView().dgApplicationProperties.LoadDataAsync())
                return;

            ApplicationPropertiesDataGridRow? selectedRow = FnString.IsNullEmptyOrWhitespace(selectApdid)
                ? null
                : GetView().dgApplicationProperties.Items.OfType<ApplicationPropertiesDataGridRow>().FirstOrDefault(r => r.APAPDID == selectApdid);

            if (selectedRow != null)
            {
                GetView().dgApplicationProperties.SelectedItem = selectedRow;
                GetView().dgApplicationProperties.ScrollIntoView(selectedRow);
            }
            else
            {
                CloseDetail();
            }
        }

        /// <summary>
        /// Closes the edit area without saving and clears the selection.
        /// </summary>
        public void CloseDetail()
        {
            GetView().dgApplicationProperties.SelectedItem = null;

            _dataWrapper!.Set(null);
            FnControls.SetVisible(GetView().cpApplicationPropertiesDetail, false);
        }

        /// <summary>
        /// Opens the definition, a new and unsaved one or a saved one, in the edit area.
        /// </summary>
        public void OpenInDetail(T1APRDH t1aprdh)
        {
            GetView().dgApplicationProperties.SelectedItem = null;

            _dataWrapper!.Set(t1aprdh);
            FnControls.SetVisible(GetView().cpApplicationPropertiesDetail, true);

            _dataWrapper.TargetController.Open(true);
        }

        private T1APRDH? GetSelectedDefinition()
        {
            return (GetView().dgApplicationProperties.SelectedItem as ApplicationPropertiesDataGridRow)?.RowObject as T1APRDH;
        }

        protected void EV_btnAddApplicationProperties()
        {
            OpenInDetail(new TXAPRDH().CreateNew());
        }

        protected async Task EV_btnRefresh()
        {
            await BuildListAsync(GetSelectedDefinition()?.APDID ?? string.Empty);
        }
    }
}
