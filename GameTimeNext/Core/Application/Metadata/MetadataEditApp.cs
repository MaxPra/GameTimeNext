using GameTimeNext.Core.Application.Metadata.Controller;
using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Application.Metadata.Views;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Metadata
{
    public class MetadataEditApp : UIXApplication
    {
        public MetadataEditView? MetadataEditView { get; set; }
        public MetadataEditViewController? MetadataEditViewController { get; set; }
        public T1METAH? T1METAH { get; set; } = new T1METAH();

        public override void InitializeApplicationOutput()
        {
            MetadataEditView = new MetadataEditView();
            MainView = MetadataEditView;

            MetadataEditViewController = new MetadataEditViewController(this);
            MetadataEditView.WndController = MetadataEditViewController;
        }

        public void CreateNew()
        {
            T1METAH = new TXMETAH().CreateNew();
            MetadataEditView!.ViewIndicator.Clear();
            MetadataEditView!.ViewIndicator.Add("CN");
            MetadataEditView!.Title = "Create New Metadata";
            MetadataEditViewController!.Show(true);
        }

        public void Edit(T1METAH t1metah)
        {
            T1METAH = t1metah;

            MetadataEditView!.ViewIndicator.Clear();
            MetadataEditView!.ViewIndicator.Add("ED");

            MetadataEditView!.Title = "Edit Metadata";
            MetadataEditViewController!.Show(true);
        }
    }
}
