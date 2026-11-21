using GameTimeNext.Core.Application.Metadata.Controller;
using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Application.Metadata.Selectors;
using GameTimeNext.Core.Application.Metadata.Views;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Metadata
{
    public class MetadataEditApp : UIXApplication
    {
        public MetadataEditView? MetadataEditView { get; set; }
        public MetadataEditViewController? MetadataEditViewController { get; set; }
        public T1METAH? T1METAH { get; set; } = new T1METAH();

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(MetadataEditApp));
        }

        public override void InitializeApplicationOutput()
        {
            MetadataEditView = new MetadataEditView();
            MainView = MetadataEditView;

            MetadataEditViewController = new MetadataEditViewController(this);
            MetadataEditView.ViewController = MetadataEditViewController;
            MetadataEditView.DgFields.SelectorAdapter = new MetadataEditView_FieldsSelectorAdapter(this);
            MetadataEditView.DgFields.SelectorController = new MetadataEditView_FieldsSelectorController(this);
        }

        public void CreateNew(Action<MetadataEditViewController.MetadataEditViewReturn> callback)
        {
            T1METAH = new TXMETAH().CreateNew();
            MetadataEditView!.ViewIndicator.Clear();
            MetadataEditView!.ViewIndicator.Add("CN");
            MetadataEditView!.Title = "Create New Metadata";

            ShowAndReturn(callback);
        }

        public void Edit(Action<MetadataEditViewController.MetadataEditViewReturn> callback, T1METAH t1metah)
        {
            T1METAH = t1metah;

            MetadataEditView!.ViewIndicator.Clear();
            MetadataEditView!.ViewIndicator.Add("ED");

            MetadataEditView!.Title = "Edit Metadata";

            ShowAndReturn(callback);
        }

        /// <summary>
        /// The view stays open after saving and is mostly left with Esc / the close button, which doesn't run a result callback
        /// (only Exit does): the dialog is modal, so Show returns once it is closed, however it was closed, and the callback
        /// gets the result then.
        /// </summary>
        private void ShowAndReturn(Action<MetadataEditViewController.MetadataEditViewReturn> callback)
        {
            MetadataEditViewController!.Show();

            callback(MetadataEditViewController.GetEditReturn());
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
        }
    }
}
