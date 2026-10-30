using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Playthroughs.ViewModel;
using GameTimeNext.Core.Application.Playthroughs.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Utils;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Playthroughs.Selectors
{
    public class PlaythroughsView_PlaythroughsSelectorAdapter : UIXSelectorAdapterBase<PlaythroughsApp, PlaythroughsView, UIXTreeGrid, PlaythroughDataGridRow>
    {
        /// <summary>
        /// Filters of the toolbar, read on the UI thread before the query runs.
        /// </summary>
        private sealed record PlaythroughFilter(string Gana, string Ptde, string Ptty, bool Completed, bool Canceled, bool Paused, long Pfid);

        public PlaythroughsView_PlaythroughsSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        protected override async Task<IEnumerable<PlaythroughDataGridRow>> ExecuteAsync(CancellationToken token)
        {
            PlaythroughFilter filter = new PlaythroughFilter(
                GetView().TxbProfileName.Text,
                GetView().TxbDescription.Text,
                GetView().CmbPlaythroughType.SelectedValue?.ToString() ?? string.Empty,
                GetView().ChbCompleted.IsChecked == true,
                GetView().ChbCanceled.IsChecked == true,
                GetView().ChbPaused.IsChecked == true,
                GetApp().AppRunSelections.Pfid);

            return await Task.Run(() => BuildRows(filter), token);
        }

        /// <summary>
        /// Builds the tree rows: one row per profile (ordered by name) with its playthroughs below.
        /// </summary>
        private List<PlaythroughDataGridRow> BuildRows(PlaythroughFilter filter)
        {
            List<PlaythroughDataGridRow> rows = new List<PlaythroughDataGridRow>();

            UIXQuery query = BuildQueryPlaythroughs(filter);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    long ptid = UIXQuery.GetInt64(reader, K1PLTHR.Name, K1PLTHR.Fields.PTID);

                    T1PLTHR t1plthr = new TXPLTHR().Read(ptid);

                    long pfid = BuildRowsProfiles(rows, t1plthr.PFID, filter);

                    BuildRowsPlaythroughs(rows, pfid, t1plthr);
                }
            }

            return rows;
        }

        /// <summary>
        /// Adds the parent row of the profile, if it does not exist yet, and returns the PFID.
        /// </summary>
        private long BuildRowsProfiles(List<PlaythroughDataGridRow> rows, long pfid, PlaythroughFilter filter)
        {
            UIXStructureKey structureKey = new UIXStructureKey().Add(pfid);

            if (rows.Exists(r => r.StructureKey == structureKey))
                return pfid;

            T1PROFI t1profi = new TXPROFI().Read(pfid);

            PlaythroughDataGridRow row = GetSelector().CreateNewRow<PlaythroughDataGridRow>();
            row.StructureKey = structureKey;
            // Initially expanded; after a refresh the grid restores the state the user left
            row.IsExpanded = true;
            row.CONAME = t1profi?.GANA ?? string.Empty;
            row.COPTPLMI = GetProfilePlaytimeInMinutes(pfid, filter);
            row.COPTPL = FnTime.FormatMinutesAsHoursAndMinutes(row.COPTPLMI, FnTime.NoValue);
            row.RowObject = t1profi;

            rows.Add(row);

            return pfid;
        }

        /// <summary>
        /// Playtime of the profile: sum of the sessions of its playthroughs that match the filters of the list.
        /// </summary>
        private static double GetProfilePlaytimeInMinutes(long pfid, PlaythroughFilter filter)
        {
            UIXQuery query = BuildQueryProfilePlaytime(pfid, filter);

            using (var reader = query.Execute())
                if (reader.Read())
                    return UIXQuery.GetDouble(reader, "TotalPlaytime");

            return 0;
        }

        /// <summary>
        /// Adds the row of the playthrough below its profile.
        /// </summary>
        private void BuildRowsPlaythroughs(List<PlaythroughDataGridRow> rows, long pfid, T1PLTHR t1plthr)
        {
            PlaythroughDataGridRow row = GetSelector().CreateNewRow<PlaythroughDataGridRow>();
            row.StructureKey = new UIXStructureKey().Add(pfid).Add(t1plthr.PTID);
            row.IsPlaythroughRow = true;
            row.CONAME = t1plthr.PTDE;
            row.COPTTY = TFCTABD.GetDescription("pL", t1plthr.PTTY);
            row.COPTCO = t1plthr.PTCO;
            row.COPTCA = t1plthr.PTCA;
            row.COPTPA = t1plthr.PTPA;
            row.COPTPLMI = TFPLTHR.GetPlaytimeInMinutes(t1plthr.PTID);
            row.COPTPL = FnTime.FormatMinutesAsHoursAndMinutes(row.COPTPLMI, FnTime.NoValue);
            row.RowObject = t1plthr;

            rows.Add(row);
        }

        private static UIXQuery BuildQueryPlaythroughs(PlaythroughFilter filter)
        {
            UIXQuery query = new UIXQuery(K1PLTHR.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTID);

            // Profile join for the name filter and the order of the profile rows
            UIXQueryTable table_profiles = query.AddJoinTable(K1PROFI.Name, JoinType.INNER);
            table_profiles.AddJoinCondition(K1PLTHR.Name, K1PLTHR.Fields.PFID, QueryCompareType.EQUALS, K1PROFI.Name, K1PROFI.Fields.PFID);

            query.AddOrderBy(K1PROFI.Name, K1PROFI.Fields.GANA, OrderDirection.ASC);
            query.AddOrderBy(K1PLTHR.Name, K1PLTHR.Fields.PTID, OrderDirection.ASC);

            AddWherePlaythroughFilters(query, filter);

            return query;
        }

        private static UIXQuery BuildQueryProfilePlaytime(long pfid, PlaythroughFilter filter)
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddSum(K1SESSI.Name, K1SESSI.Fields.PLTI, "TotalPlaytime");

            UIXQueryTable table_playthroughs = query.AddJoinTable(K1PLTHR.Name, JoinType.INNER);
            table_playthroughs.AddJoinCondition(K1SESSI.Name, K1SESSI.Fields.PTID, QueryCompareType.EQUALS, K1PLTHR.Name, K1PLTHR.Fields.PTID);

            UIXQueryTable table_profiles = query.AddJoinTable(K1PROFI.Name, JoinType.INNER);
            table_profiles.AddJoinCondition(K1PLTHR.Name, K1PLTHR.Fields.PFID, QueryCompareType.EQUALS, K1PROFI.Name, K1PROFI.Fields.PFID);

            query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PFID, QueryCompareType.EQUALS, pfid);

            AddWherePlaythroughFilters(query, filter);

            return query;
        }

        /// <summary>
        /// Filters of the toolbar. The query has to join T1PLTHR and T1PROFI.
        /// </summary>
        private static void AddWherePlaythroughFilters(UIXQuery query, PlaythroughFilter filter)
        {
            if (!FnString.IsNullEmptyOrWhitespace(filter.Gana))
                query.AddWhere(K1PROFI.Name, K1PROFI.Fields.GANA, QueryCompareType.LIKE, filter.Gana);

            if (!FnString.IsNullEmptyOrWhitespace(filter.Ptde))
                query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTDE, QueryCompareType.LIKE, filter.Ptde);

            if (!FnString.IsNullEmptyOrWhitespace(filter.Ptty))
                query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTTY, QueryCompareType.EQUALS, filter.Ptty);

            if (filter.Completed)
                query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTCO, QueryCompareType.EQUALS, true);

            if (filter.Canceled)
                query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTCA, QueryCompareType.EQUALS, true);

            if (filter.Paused)
                query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTPA, QueryCompareType.EQUALS, true);

            if (filter.Pfid > 0)
                query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PFID, QueryCompareType.EQUALS, filter.Pfid);
        }
    }
}
