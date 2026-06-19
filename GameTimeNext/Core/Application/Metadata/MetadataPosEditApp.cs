using GameTimeNext.Core.Application.Metadata.Controller;
using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Application.Metadata.Views;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Metadata
{
    public class MetadataPosEditApp : UIXApplication
    {
        public MetadataPosEditView? MetadataPosEditView { get; set; }
        public MetadataPosEditViewController? MetadataPosEditViewController { get; set; }
        public T1METAP? T1METAP { get; set; } = new T1METAP();

        public override void InitializeApplicationOutput()
        {
            MetadataPosEditView = new MetadataPosEditView();
            MainView = MetadataPosEditView;

            MetadataPosEditViewController = new MetadataPosEditViewController(this);
            MetadataPosEditView.WndController = MetadataPosEditViewController;
        }

        public void CreateNew(T1METAH t1metah)
        {
            T1METAP = new TXMETAP().CreateNew();
            T1METAP.MENAM = t1metah.MENAM;

            MetadataPosEditView!.ViewIndicator.Clear();
            MetadataPosEditView!.ViewIndicator.Add("CN");
            MetadataPosEditView!.Title = "Create Metadata Field";
            MetadataPosEditViewController!.Show(true);
        }

        public void Edit(T1METAP t1metap)
        {
            T1METAP = t1metap;
            MetadataPosEditView?.ViewIndicator.Clear();
            MetadataPosEditView?.ViewIndicator.Add("ED");

            MetadataPosEditView!.Title = "Edit Metadata Field";
            MetadataPosEditViewController!.Show(true);
        }
    }
}
