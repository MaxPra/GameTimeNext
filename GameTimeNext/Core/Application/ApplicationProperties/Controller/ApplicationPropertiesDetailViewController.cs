using GameTimeNext.Core.Application.ApplicationProperties.DataWrapper;
using GameTimeNext.Core.Application.ApplicationProperties.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using GameTimeNext.Core.Framework.UI.Dialogs;

namespace GameTimeNext.Core.Application.ApplicationProperties.Controller
{
    /// <summary>
    /// Combined edit area of the definitions list: edits the definition held by the data wrapper and saves it directly.
    /// </summary>
    public class ApplicationPropertiesDetailViewController : UIXViewControllerBase<ApplicationPropertiesApp, ApplicationPropertiesDetailView>
    {
        private ApplicationPropertiesDataWrapper? _dataWrapper;

        public ApplicationPropertiesDetailViewController(UIXApplication app) : base(app)
        {
        }

        #region Event-Pipeline-Methods
        protected override void Init()
        {
            _dataWrapper = GetDataWrapper<ApplicationPropertiesDataWrapper>();

            if (_dataWrapper.HasData)
                AddIdentifier("T1APRDH", _dataWrapper.Get());
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {
            // Only a new definition takes the focus; a selected one leaves it in the list for keyboard navigation
            if (_dataWrapper!.HasData && _dataWrapper.Get().State == UIXTableObjectState.New)
                GetView().txbId.Focus();
        }

        protected override void BuildImpl()
        {
            if (!_dataWrapper!.HasData)
                return;

            T1APRDH t1aprdh = _dataWrapper.Get();
            bool isNew = t1aprdh.State == UIXTableObjectState.New;

            // The ID is the primary key: once the definition is saved it can no longer be changed
            FnControls.SetEnabled(GetView().txbId, isNew);

            // A new definition is thrown away, an existing one only loses its unsaved changes
            GetView().btnCancel.Content = isNew ? "Discard" : "Cancel";

            BuildHeader(t1aprdh);
        }

        protected override void CheckImpl()
        {
            // Closed edit area (e.g. after discarding): nothing to check
            if (!_dataWrapper!.HasData)
                return;

            string id = GetView().txbId.Text;

            if (FnString.IsNullEmptyOrWhitespace(id))
                AddViewError(GetView().txbId, GetErrorMessage("GLO0003", "App name"));
            else if (id.Length > 300)
                AddViewError(GetView().txbId, GetErrorMessage("GLO0004", "App name", "300"));
            else if (_dataWrapper.Get().State == UIXTableObjectState.New && new TXAPRDH().Read(id) != null)
                AddViewError(GetView().txbId, GetErrorMessage("GLO0008", "App name", id));

            if (FnString.IsNullEmptyOrWhitespace(GetView().txbDescription.Text))
                AddViewError(GetView().txbDescription, GetErrorMessage("GLO0003", "Description"));
            else if (GetView().txbDescription.Text.Length > 300)
                AddViewError(GetView().txbDescription, GetErrorMessage("GLO0004", "Description", "300"));
        }

        protected override void FillViewImpl()
        {
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
        {
            CFMBOX.AskUnsavedChanges(GetApp(), args);
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }
        #endregion

        private void BuildHeader(T1APRDH t1aprdh)
        {
            string icon = FnApplications.GetApplicationIcon(t1aprdh.APDID);

            GetView().txIcon.Text = FnString.IsNullEmptyOrWhitespace(icon) ? UIXMdlIcons.Tools : icon;
            GetView().txTitle.Text = FnString.IsNullEmptyOrWhitespace(t1aprdh.APDID) ? "New application properties definition" : t1aprdh.APDID;
            GetView().txSubtitle.Text = t1aprdh.APDES;
            FnControls.SetVisible(GetView().txSubtitle, !FnString.IsNullEmptyOrWhitespace(t1aprdh.APDES));
        }

        protected void EV_btnCancel()
        {
            // The definition is only filled from the view on save, so closing the area discards the changes
            if (_dataWrapper!.SourceController is ApplicationPropertiesViewController viewController)
                viewController.CloseDetail();
        }

        protected async Task EV_btnSave()
        {
            // Saved by the pipeline already: show the changes in the list, close the edit area, no selection
            if (_dataWrapper!.SourceController is ApplicationPropertiesViewController viewController)
                await viewController.BuildListAsync(string.Empty);
        }
    }
}
