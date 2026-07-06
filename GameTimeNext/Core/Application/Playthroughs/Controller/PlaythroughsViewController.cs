using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Playthroughs.ViewModel;
using GameTimeNext.Core.Application.Playthroughs.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Playthroughs.Controller
{
    public class PlaythroughsViewController : UIXViewControllerBase
    {
        private PlaythroughsViewModel? _viewModel;

        public PlaythroughsViewController(UIXApplication app) : base(app)
        {
            _viewModel = new PlaythroughsViewModel();
        }

        protected override void Init()
        {
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {

            GetView().TxbProfileName.Text = GetApp().AppRunSelections.Gana;

            FnControls.SetEnabled(GetView().TxbProfileName, FnString.IsNullEmptyOrWhitespace(GetApp().AppRunSelections.Gana) && !GetView().ViewIndicator.Contains("SE"));
        }

        protected override async Task BuildFirstImplAsync()
        {
            if (GetView().ViewIndicator.Contains("SE"))
                await BuildDataGridAsync();
        }

        protected override void BuildImpl()
        {
        }

        protected override async Task BuildImplAsync()
        {
        }

        protected override void Check()
        {
        }

        protected override void FillViewImpl()
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
        }

        private PlaythroughsApp GetApp()
        {
            return (PlaythroughsApp)App;
        }

        private PlaythroughsView GetView()
        {
            return (PlaythroughsView)View;
        }

        private async Task BuildDataGridAsync()
        {
            GetApp().Loader.Begin();

            await Task.Run(() =>
            {
                List<PlaythroughDataGridRow> rows = BuildRowsPlaythroughs();

                View.Dispatcher.Invoke(() =>
                {
                    _viewModel = new PlaythroughsViewModel();
                    _viewModel!.PlaythroughDataGridRows = new System.Collections.ObjectModel.ObservableCollection<PlaythroughDataGridRow>(rows);

                    GetView().DataContext = _viewModel;

                    GetApp().Loader.Stop();
                }, DispatcherPriority.Normal);
            });
        }

        private List<PlaythroughDataGridRow> BuildRowsPlaythroughs()
        {
            List<PlaythroughDataGridRow> rows = new List<PlaythroughDataGridRow>();

            UIXQuery query = BuildQueryPlaythroughs();

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {

                    long ptid = UIXQuery.GetInt64(reader, K1PLTHR.Name, K1PLTHR.Fields.PTID);

                    TXPLTHR txplthr = new TXPLTHR();
                    T1PLTHR t1plthr = txplthr.Read(ptid);

                    PlaythroughDataGridRow row = GetView().DgPlaythroughs.CreateNewRow<PlaythroughDataGridRow>();
                    row.COGANA = TFPROFI.GetProfileName(t1plthr.PFID);
                    row.COPTDE = t1plthr.PTDE;
                    row.COPTTY = t1plthr.PTTY;
                    row.COPTCA = t1plthr.PTCA;
                    row.COPTCO = t1plthr.PTCO;
                    row.RowObject = t1plthr;

                    rows.Add(row);
                }
            }

            return rows;
        }

        private UIXQuery BuildQueryPlaythroughs()
        {
            UIXQuery query = new UIXQuery(K1PLTHR.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTID);

            View.Dispatcher.Invoke(() =>
            {

                if (!FnString.IsNullEmptyOrWhitespace(GetView().TxbProfileName.Text))
                {
                    UIXQueryTable table_profiles = query.AddJoinTable(K1PROFI.Name, JoinType.INNER);
                    table_profiles.AddJoinCondition(K1PLTHR.Name, K1PLTHR.Fields.PFID, QueryCompareType.EQUALS, K1PROFI.Name, K1PROFI.Fields.PFID);

                    query.AddWhere(K1PROFI.Name, K1PROFI.Fields.GANA, QueryCompareType.LIKE, GetView().TxbProfileName.Text);
                }

                if (!FnString.IsNullEmptyOrWhitespace(GetView().TxbDescription.Text))
                {
                    query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTDE, QueryCompareType.LIKE, GetView().TxbDescription.Text);
                }

                // ToDo: Playthrough type

                if (GetView().ChbCompleted.IsChecked == true)
                    query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTCO, QueryCompareType.EQUALS, true);

                if (GetView().ChbCanceled.IsChecked == true)
                    query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTCA, QueryCompareType.EQUALS, true);

                if (GetApp().AppRunSelections.Pfid > 0)
                    query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PFID, QueryCompareType.EQUALS, GetApp().AppRunSelections.Pfid);

            });

            return query;
        }

        protected async void EV_BtnRefresh()
        {
            await BuildDataGridAsync();
        }
    }
}
