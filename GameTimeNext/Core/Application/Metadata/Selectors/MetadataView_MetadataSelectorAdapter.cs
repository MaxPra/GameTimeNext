using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Metadata.Controller;
using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Application.Metadata.Views;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Metadata.Selectors
{
    public class MetadataView_MetadataSelectorAdapter : UIXSelectorAdapterBase<MetadataApp, MetadataView, UIXDataGrid, MetadataDataGridRow>
    {
        public MetadataView_MetadataSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        protected override async Task<IEnumerable<MetadataDataGridRow>> ExecuteAsync(CancellationToken token)
        {
            // Filters are read on the UI thread, the query runs in the background
            string menam = GetView().TxbTableObjectName.Text;
            string descr = GetView().TxbDescription.Text;
            string mtype = GetView().CmbType.SelectedValue?.ToString() ?? string.Empty;

            return await Task.Run(() => BuildRows(TFMETAH.GetMetadata(menam, descr, mtype)), token);
        }

        private List<MetadataDataGridRow> BuildRows(List<T1METAH> t1metahs)
        {
            List<MetadataDataGridRow> metadataRows = new List<MetadataDataGridRow>();

            foreach (T1METAH t1metah in t1metahs)
            {
                MetadataDataGridRow row = GetSelector().CreateNewRow<MetadataDataGridRow>();
                row.COMENAM = t1metah.MENAM;
                row.CODESCR = t1metah.DESCR;
                row.COMTYPE = TFCTABD.GetDescription("mT", t1metah.MTYPE);
                row.COGENER = !t1metah.GENER;
                row.RowObject = t1metah;

                metadataRows.Add(row);
            }

            return metadataRows;
        }
    }
}
