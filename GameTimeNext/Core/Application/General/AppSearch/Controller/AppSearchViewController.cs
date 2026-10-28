using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.General.AppSearch.ViewModels;
using GameTimeNext.Core.Application.General.AppSearch.Views;
using GameTimeNext.Core.Application.Menus;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Utils;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.General.AppSearch.Controller
{
    public class AppSearchViewController : UIXViewControllerBase
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

        private List<T1MENUE> _entries = new List<T1MENUE>();
        private Dictionary<long, MenuCall> _menuCalls = new Dictionary<long, MenuCall>();

        public AppSearchViewController(UIXApplication app) : base(app)
        {
        }

        protected override void Init()
        {
        }

        protected override void BuildFirstImpl()
        {
            GetWnd().txbSearch.Focus();
        }

        protected override async Task BuildFirstImplAsync()
        {
            await LoadMenuCallsAsync();
        }

        protected override void BuildImpl()
        {
        }

        protected override void CheckImpl()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void FillViewImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void Event_Closing()
        {
        }

        protected override void Event_Maximize()
        {
        }

        protected override void Event_Minimize()
        {
        }

        protected override void HandleUIEventImpl(FrameworkElement source, string eventName, RoutedEventArgs args)
        {
            if (eventName == UIXEventNames.UIElement.KeyDown && args is KeyEventArgs keyArgs)
            {
                // ESC or CTRL+M -> Close window
                if (keyArgs.Key == Key.Escape ||
                    (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && keyArgs.Key == Key.M))
                {
                    Exit(true);
                    keyArgs.Handled = true;
                    return;
                }

                if (source.Name == "txbSearch")
                {
                    if (keyArgs.Key == Key.Down)
                    {
                        FocusTree();
                        keyArgs.Handled = true;
                    }
                    else if (keyArgs.Key == Key.Enter)
                    {
                        // Quick launch: the selected application, otherwise the first one found
                        ActivateRow(GetSelectedRow() ?? GetFirstApplicationRow());
                        keyArgs.Handled = true;
                    }
                }
                else if (IsInTree(source) && keyArgs.Key == Key.Enter)
                {
                    ActivateRow(GetSelectedRow());
                    keyArgs.Handled = true;
                }
            }
            else if (eventName == UIXEventNames.UIElement.MouseDoubleClick && IsInTree(source))
            {
                ActivateRow(GetSelectedRow());
            }
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
            if (eventName == UIXEventNames.TextBox.TextChanged && source.Name == "txbSearch")
                BuildTree();
        }

        private AppSearchApp GetApp()
        {
            return (AppSearchApp)App;
        }

        private AppSearchView GetWnd()
        {
            return (AppSearchView)View;
        }

        /// <summary>
        /// Loads the menus and the application calls the current user can launch, then shows the tree.
        /// </summary>
        private async Task LoadMenuCallsAsync()
        {
            GetApp().Loader.Begin();

            await Task.Run(() =>
            {
                List<T1MENUE> entries = TFMENUE.GetAllMenuEntries();
                Dictionary<long, MenuCall> menuCalls = GetMenuCalls(entries);

                View.Dispatcher.Invoke(() =>
                {
                    _entries = entries;
                    _menuCalls = menuCalls;

                    BuildTree();

                    GetApp().Loader.Stop();

                }, DispatcherPriority.Normal);
            });
        }

        /// <summary>
        /// Application calls by menu entry ID. Left out: applications without menu call permission (APMCP) and
        /// those that can't be resolved or hosted in a tab. Dev tools (DEVT) are filtered in BuildTree.
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
        /// Shows the menu tree filtered by the search text: matching application calls, everything below a
        /// matching folder, and the folders above them. Folders without an application call below are left out.
        /// </summary>
        private void BuildTree()
        {
            string searchText = GetWnd().txbSearch.Text.Trim();
            bool hasSearch = !FnString.IsNullEmptyOrWhitespace(searchText);

            Dictionary<long, T1MENUE> entriesById = _entries.ToDictionary(e => e.MEID);
            ILookup<long, T1MENUE> childrenByParent = _entries.ToLookup(e => entriesById.ContainsKey(e.MEPA) ? e.MEPA : 0);

            bool Matches(T1MENUE t1menue) =>
                !hasSearch ||
                t1menue.MEDE.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                (_menuCalls.TryGetValue(t1menue.MEID, out MenuCall? menuCall) &&
                 (menuCall.TabName.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                  menuCall.Description.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)));

            // Visible: the matching application calls (also below a matching folder) and their folders
            HashSet<long> visibleIds = new HashSet<long>();
            bool showDevTools = ShowDevTools();

            foreach (T1MENUE t1menue in _entries.Where(e => _menuCalls.ContainsKey(e.MEID)))
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

            GetWnd().dgApplications.ItemsSource = new ObservableCollection<AppSearchTreeGridRow>(rows);

            int applicationCount = rows.Count(r => !r.IsFolder);

            GetWnd().txResultCount.Text = applicationCount == 1 ? "1 application" : $"{applicationCount} applications";
            GetWnd().txNoResults.Text = hasSearch ? $"No results for \"{searchText}\"" : "No menu calls yet";
            FnControls.SetVisible(GetWnd().pnlNoResults, rows.Count == 0);

            // While searching, the first hit is preselected, so Enter opens it
            if (hasSearch && GetFirstApplicationRow() is AppSearchTreeGridRow firstRow)
            {
                GetWnd().dgApplications.SelectedItem = firstRow;
                GetWnd().dgApplications.ScrollIntoView(firstRow);
            }
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

                AppSearchTreeGridRow row = GetWnd().dgApplications.CreateNewRow<AppSearchTreeGridRow>();
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

        private AppSearchTreeGridRow? GetSelectedRow()
        {
            return GetWnd().dgApplications.SelectedItem as AppSearchTreeGridRow;
        }

        private AppSearchTreeGridRow? GetFirstApplicationRow()
        {
            return GetWnd().dgApplications.TreeRows.OfType<AppSearchTreeGridRow>().FirstOrDefault(r => !r.IsFolder && r.IsVisibleInTree);
        }

        /// <summary>
        /// Moves the keyboard focus from the search box into the tree, on the selected or the first row.
        /// </summary>
        private void FocusTree()
        {
            UIXTreeGrid dgApplications = GetWnd().dgApplications;
            object? item = dgApplications.SelectedItem ?? dgApplications.Items.Cast<object>().FirstOrDefault();

            if (item == null)
                return;

            dgApplications.SelectedItem = item;
            dgApplications.CurrentCell = new DataGridCellInfo(item, dgApplications.Columns[0]);
            dgApplications.ScrollIntoView(item);
            dgApplications.UpdateLayout();

            // The cell takes the focus, not the row: a DataGridRow can't be focused, the focus would stay in the
            // search box and every further arrow down would start here again on the same row
            if (dgApplications.ItemContainerGenerator.ContainerFromItem(item) is DataGridRow row)
                FnVisualTree.FindDescendant<DataGridCell>(row)?.Focus();
        }

        private bool IsInTree(DependencyObject? element)
        {
            for (; element != null; element = element is Visual ? VisualTreeHelper.GetParent(element) : LogicalTreeHelper.GetParent(element))
            {
                if (ReferenceEquals(element, GetWnd().dgApplications))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Opens the application of the row; a folder row is expanded or collapsed instead.
        /// </summary>
        private void ActivateRow(AppSearchTreeGridRow? row)
        {
            if (row == null)
                return;

            if (row.IsFolder)
            {
                row.IsExpanded = !row.IsExpanded;
                return;
            }

            LaunchApplication(row.ClassName, row.TabName);
        }

        private void LaunchApplication(string className, string appName)
        {
            UIXApplication hostApplication = GetApp().HostApplication;
            Window? hostWindow = Window.GetWindow(GetWnd());

            void LaunchAfterClose(object? sender, EventArgs args)
            {
                if (hostWindow != null)
                    hostWindow.Closed -= LaunchAfterClose;

                hostApplication.MainView.Dispatcher.BeginInvoke(() =>
                {
                    AppEnvironment.AppLauncher.LaunchApplication(className, hostApplication, appName);

                }, DispatcherPriority.ApplicationIdle);
            }

            if (hostWindow != null)
                hostWindow.Closed += LaunchAfterClose;

            Exit(true);

            if (hostWindow == null)
                LaunchAfterClose(null, EventArgs.Empty);
        }
    }
}
