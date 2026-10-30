using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Menus.DataWrapper;
using GameTimeNext.Core.Application.Menus.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using GameTimeNext.Core.Framework.UI.Dialogs;

namespace GameTimeNext.Core.Application.Menus.Controller
{
    /// <summary>
    /// Combined edit area of the menu tree: edits the entry held by the data wrapper and saves it directly.
    /// </summary>
    public class MenusDetailViewController : UIXViewControllerBase<MenusApp, MenusDetailView>
    {
        private MenusDataWrapper? _dataWrapper;
        private List<T1APRDH> _applications = new List<T1APRDH>();

        public MenusDetailViewController(UIXApplication app) : base(app)
        {
        }

        #region Event-Pipeline-Methods
        protected override void Init()
        {
            _dataWrapper = GetDataWrapper<MenusDataWrapper>();

            AddSource("T1CTABD", new TXCTABD());

            if (_dataWrapper.HasData)
                AddIdentifier("T1MENUE", _dataWrapper.Get());
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {
            FillApplications();
        }

        protected override void BuildImpl()
        {
            if (!_dataWrapper!.HasData)
                return;

            T1MENUE t1menue = _dataWrapper.Get();
            bool isFolder = GetSelectedType() == "01";

            // The type decides about the children, so it is only chosen for a new entry
            FnControls.SetEnabled(GetView().cmbType, t1menue.State == UIXTableObjectState.New);

            // A new entry is thrown away, an existing one only loses its unsaved changes
            GetView().btnCancel.Content = t1menue.State == UIXTableObjectState.New ? "Discard" : "Cancel";
            FnControls.SetVisible(GetView().pnlApplication, !isFolder);

            BuildHeader(t1menue, isFolder);
        }

        protected override void CheckImpl()
        {
            // Closed edit area (e.g. after discarding): nothing to check
            if (!_dataWrapper!.HasData)
                return;

            if (FnString.IsNullEmptyOrWhitespace(GetSelectedType()))
                AddViewError(GetView().cmbType, GetErrorMessage("GLO0003", "Type"));

            if (FnString.IsNullEmptyOrWhitespace(GetView().txbDescription.Text))
                AddViewError(GetView().txbDescription, GetErrorMessage("GLO0003", "Description"));
            else if (GetView().txbDescription.Text.Length > 200)
                AddViewError(GetView().txbDescription, GetErrorMessage("GLO0004", "Description", "200"));

            if (GetSelectedType() != "02")
                return;

            string apdid = GetSelectedApplication();

            if (FnString.IsNullEmptyOrWhitespace(apdid))
                AddViewError(GetView().cmbApplication, GetErrorMessage("GLO0003", "Application"));
            else if (FnApplications.ResolveApplicationType(apdid) == null)
                AddViewError(GetView().cmbApplication, GetErrorMessage("GLO0017", $"Application \"{apdid}\""));
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

        /// <summary>
        /// Applications that may be called from a menu; the first entry stands for "none".
        /// </summary>
        private void FillApplications()
        {
            _applications = TFAPRDH.GetMenuCallableApplications();

            UIXManualCodetable codetable = new UIXManualCodetable();

            foreach (T1APRDH t1aprdh in _applications)
                codetable.AddEntry(t1aprdh.APDID, t1aprdh.APDES);

            codetable.ApplyTo(GetView().cmbApplication);
        }

        private void BuildHeader(T1MENUE t1menue, bool isFolder)
        {
            string apdid = isFolder ? string.Empty : GetSelectedApplication();
            string icon = isFolder ? UIXMdlIcons.Folder : FnApplications.GetApplicationIcon(apdid);

            GetView().txIcon.Text = FnString.IsNullEmptyOrWhitespace(icon) ? UIXMdlIcons.Play : icon;

            if (!FnString.IsNullEmptyOrWhitespace(t1menue.MEDE))
                GetView().txTitle.Text = t1menue.MEDE;
            else
                GetView().txTitle.Text = isFolder ? "New folder" : "New application call";

            string typeDescription = TFCTABD.GetDescription("mN", GetSelectedType());
            GetView().txSubtitle.Text = FnString.IsNullEmptyOrWhitespace(apdid) ? typeDescription : $"{typeDescription} · {apdid}";

            GetView().txApplicationDescription.Text = _applications.FirstOrDefault(a => a.APDID == apdid)?.APDES ?? string.Empty;
            FnControls.SetVisible(GetView().txApplicationDescription, !FnString.IsNullEmptyOrWhitespace(GetView().txApplicationDescription.Text));
        }

        private string GetSelectedType()
        {
            return (GetView().cmbType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;
        }

        private string GetSelectedApplication()
        {
            return (GetView().cmbApplication.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;
        }

        protected void EV_btnCancel()
        {
            // The entry is only filled from the view on save, so closing the area discards the changes
            if (_dataWrapper!.SourceController is MenusViewController menusViewController)
                menusViewController.CloseDetail();
        }

        protected async Task EV_btnSave()
        {
            // Saved by the pipeline already: show the changes in the tree, close the edit area, no selection
            if (_dataWrapper!.SourceController is MenusViewController menusViewController)
                await menusViewController.BuildTreeAsync(0);
        }
    }
}
