using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.General.AppSearch.ViewModels;
using GameTimeNext.Core.Application.General.AppSearch.Views;
using GameTimeNext.Core.Application.Menus;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Utils;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.General.AppSearch.Selectors
{
    public class AppSearchView_ApplicationsSelectorAdapter : UIXSelectorAdapterBase<AppSearchApp, AppSearchView, UIXTreeGrid, AppSearchTreeGridRow>
    {
        /// <summary>
        /// Application call of a menu that can be launched by the current user.
        /// </summary>
        private sealed class MenuCall
        {
            public required string ClassName { get; init; }
            public required string TabName { get; init; }
            public required string Description { get; init; }
        }

        private List<T1MENUE>? _entries;
        private Dictionary<long, MenuCall> _menuCalls = new Dictionary<long, MenuCall>();

        public AppSearchView_ApplicationsSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        /// <summary>
        /// The menus are read once, a new search text only filters them again.
        /// </summary>
        protected override async Task<IEnumerable<AppSearchTreeGridRow>> ExecuteAsync(CancellationToken token)
        {
            string searchText = GetView().txbSearch.Text.Trim();

            if (_entries == null)
            {
                (List<T1MENUE> entries, Dictionary<long, MenuCall> menuCalls) = await Task.Run(() =>
                {
                    List<T1MENUE> entries = TFMENUE.GetAllMenuEntries();
                    return (entries, GetMenuCalls(entries));
                }, token);

                _entries = entries;
                _menuCalls = menuCalls;
            }

            return BuildRows(searchText);
        }

        /// <summary>
        /// Application calls by menu entry ID. Left out: applications without menu call permission (APMCP) and
        /// those that can't be resolved or hosted in a tab. Dev tools (DEVT) are filtered in BuildRows.
        /// </summary>
        private static Dictionary<long, MenuCall> GetMenuCalls(List<T1MENUE> entries)
        {
            Dictionary<string, T1APRDH> callableApplications = TFAPRDH.GetMenuCallableApplications().ToDictionary(a => a.APDID, StringComparer.Ordinal);

            Dictionary<long, MenuCall> menuCalls = new Dictionary<long, MenuCall>();

            foreach (T1MENUE t1menue in entries.Where(e => e.METY == "02"))
            {
                if (!callableApplications.TryGetValue(t1menue.MEAP, out T1APRDH? t1aprdh))
                    continue;

                Type? type = FnApplications.ResolveApplicationType(t1menue.MEAP);

                if (type?.FullName == null || !typeof(IUIXApplicationStarter).IsAssignableFrom(type))
                    continue;

                menuCalls[t1menue.MEID] = new MenuCall
                {
                    ClassName = type.FullName,
                    TabName = t1menue.MEDE,
                    Description = t1aprdh.APDES
                };
            }

            return menuCalls;
        }

        /// <summary>
        /// Menu entries marked as dev tool (DEVT) are only shown in debug / dev mode.
        /// </summary>
        private static bool ShowDevTools()
        {
            return FnSystem.IsDebug() && !FnSystem.RunWithUserPermissions();
        }

        /// <summary>
        /// The menu tree filtered by the search text: matching application calls, everything below a matching
        /// folder, and the folders above them. Folders without an application call below are left out.
        /// </summary>
        private List<AppSearchTreeGridRow> BuildRows(string searchText)
        {
            List<T1MENUE> entries = _entries ?? new List<T1MENUE>();
            bool hasSearch = !FnString.IsNullEmptyOrWhitespace(searchText);

            Dictionary<long, T1MENUE> entriesById = entries.ToDictionary(e => e.MEID);
            ILookup<long, T1MENUE> childrenByParent = entries.ToLookup(e => entriesById.ContainsKey(e.MEPA) ? e.MEPA : 0);

            bool Matches(T1MENUE t1menue) =>
                !hasSearch ||
                t1menue.MEDE.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                (_menuCalls.TryGetValue(t1menue.MEID, out MenuCall? menuCall) &&
                 (menuCall.TabName.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                  menuCall.Description.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)));

            // Visible: the matching application calls (also below a matching folder) and their folders
            HashSet<long> visibleIds = new HashSet<long>();
            bool showDevTools = ShowDevTools();

            foreach (T1MENUE t1menue in entries.Where(e => _menuCalls.ContainsKey(e.MEID)))
            {
                List<T1MENUE> path = GetPath(t1menue, entriesById);

                // A dev tool entry, or one in a dev tool folder
                if (!showDevTools && path.Any(e => e.DEVT))
                    continue;

                if (!path.Any(Matches))
                    continue;

                foreach (T1MENUE entry in path)
                    visibleIds.Add(entry.MEID);
            }

            List<AppSearchTreeGridRow> rows = new List<AppSearchTreeGridRow>();
            AddRows(rows, childrenByParent, 0, new UIXStructureKey(), visibleIds, new HashSet<long>());

            return rows;
        }

        /// <summary>
        /// The entry and its folders up to the root, guarded against cycles in the parent chain.
        /// </summary>
        private static List<T1MENUE> GetPath(T1MENUE t1menue, Dictionary<long, T1MENUE> entriesById)
        {
            List<T1MENUE> path = new List<T1MENUE>();
            HashSet<long> seenIds = new HashSet<long>();

            for (T1MENUE? current = t1menue; current != null && seenIds.Add(current.MEID); current = entriesById.GetValueOrDefault(current.MEPA))
                path.Add(current);

            return path;
        }

        private void AddRows(List<AppSearchTreeGridRow> rows, ILookup<long, T1MENUE> childrenByParent, long mepa, UIXStructureKey parentKey, HashSet<long> visibleIds, HashSet<long> addedIds)
        {
            foreach (T1MENUE t1menue in childrenByParent[mepa])
            {
                if (!visibleIds.Contains(t1menue.MEID) || !addedIds.Add(t1menue.MEID))
                    continue;

                _menuCalls.TryGetValue(t1menue.MEID, out MenuCall? menuCall);

                AppSearchTreeGridRow row = GetSelector().CreateNewRow<AppSearchTreeGridRow>();
                row.StructureKey = parentKey.Add(t1menue.MEID);
                row.IsExpanded = true;
                row.IsFolder = menuCall == null;
                row.Icon = CFMenusApp.GetEntryIcon(t1menue);
                row.COMEDE = t1menue.MEDE;
                row.ClassName = menuCall?.ClassName ?? string.Empty;
                row.TabName = menuCall?.TabName ?? string.Empty;
                row.RowObject = t1menue;

                rows.Add(row);

                AddRows(rows, childrenByParent, t1menue.MEID, row.StructureKey, visibleIds, addedIds);
            }
        }
    }
}
