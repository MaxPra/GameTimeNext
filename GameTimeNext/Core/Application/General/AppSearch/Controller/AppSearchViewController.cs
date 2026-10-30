using GameTimeNext.Core.Application.General.AppSearch.ViewModels;
using GameTimeNext.Core.Application.General.AppSearch.Views;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Utils;
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
    public class AppSearchViewController : UIXViewControllerBase<AppSearchApp, AppSearchView>
    {
        public AppSearchViewController(UIXApplication app) : base(app)
        {
        }

        protected override void Init()
        {
        }

        protected override void BuildFirstImpl()
        {
            GetView().txbSearch.Focus();
        }

        protected override async Task BuildFirstImplAsync()
        {
            await BuildTreeAsync();
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

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
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
        }

        protected override async Task TriggeredEventAsync(FrameworkElement source, string eventName)
        {
            if (eventName == UIXEventNames.TextBox.TextChanged && source.Name == "txbSearch")
                await BuildTreeAsync();
        }

        /// <summary>
        /// Shows the menu tree filtered by the search text, with the number of applications found.
        /// </summary>
        private async Task BuildTreeAsync()
        {
            if (!await GetView().dgApplications.LoadDataAsync())
                return;

            string searchText = GetView().txbSearch.Text.Trim();
            bool hasSearch = !FnString.IsNullEmptyOrWhitespace(searchText);

            List<AppSearchTreeGridRow> rows = GetView().dgApplications.Items.OfType<AppSearchTreeGridRow>().ToList();
            int applicationCount = rows.Count(r => !r.IsFolder);

            GetView().txResultCount.Text = applicationCount == 1 ? "1 application" : $"{applicationCount} applications";
            GetView().txNoResults.Text = hasSearch ? $"No results for \"{searchText}\"" : "No menu calls yet";
            FnControls.SetVisible(GetView().pnlNoResults, rows.Count == 0);

            // While searching, the first hit is preselected, so Enter opens it
            if (hasSearch && GetFirstApplicationRow() is AppSearchTreeGridRow firstRow)
            {
                GetView().dgApplications.SelectedItem = firstRow;
                GetView().dgApplications.ScrollIntoView(firstRow);
            }
        }

        private AppSearchTreeGridRow? GetSelectedRow()
        {
            return GetView().dgApplications.SelectedItem as AppSearchTreeGridRow;
        }

        private AppSearchTreeGridRow? GetFirstApplicationRow()
        {
            return GetView().dgApplications.TreeRows.OfType<AppSearchTreeGridRow>().FirstOrDefault(r => !r.IsFolder && r.IsVisibleInTree);
        }

        /// <summary>
        /// Moves the keyboard focus from the search box into the tree, on the selected or the first row.
        /// </summary>
        private void FocusTree()
        {
            UIXTreeGrid dgApplications = GetView().dgApplications;
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
                if (ReferenceEquals(element, GetView().dgApplications))
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
            Window? hostWindow = Window.GetWindow(GetView());

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
