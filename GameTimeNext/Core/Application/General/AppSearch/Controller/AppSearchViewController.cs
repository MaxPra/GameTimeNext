using GameTimeNext.Core.Application.General.AppSearch.ViewModels;
using GameTimeNext.Core.Application.General.AppSearch.Views;
using GameTimeNext.Core.Framework;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.General.AppSearch.Controller
{
    public class AppSearchViewController : UIXWindowControllerBase
    {

        private AppSearchViewModel? _appSearchViewModel;

        public AppSearchViewController(UIXApplication app) : base(app)
        {
        }

        protected override void Init()
        {
            _appSearchViewModel = new AppSearchViewModel();

            AllowOnlyEnterInTextBox = false;
        }

        protected override void BuildFirst()
        {
            BuildSearchableApplicationsListBox();

            GetWnd().txbSearch.Focus();

        }

        protected override void Build()
        {
        }

        protected override void Check()
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
                        if (_appSearchViewModel?.SearchableApplications?.Count > 0)
                        {
                            GetWnd().lvApplicationSearch.SelectedIndex = 0;
                            GetWnd().lvApplicationSearch.Focus();
                            GetWnd().lvApplicationSearch.ScrollIntoView(GetWnd().lvApplicationSearch.SelectedItem);
                            keyArgs.Handled = true;
                        }
                    }
                    else if (keyArgs.Key == Key.Enter)
                    {
                        LaunchSelectedApplication();
                        keyArgs.Handled = true;
                    }
                }
                else if (source.Name == "lvApplicationSearch" || source is System.Windows.Controls.ListViewItem)
                {
                    if (keyArgs.Key == Key.Enter)
                    {
                        LaunchSelectedApplication();
                        keyArgs.Handled = true;
                    }
                }
            }
            else if (eventName == UIXEventNames.UIElement.MouseDoubleClick)
            {
                if (source.Name == "lvApplicationSearch" || source is System.Windows.Controls.ListViewItem)
                {
                    LaunchSelectedApplication();
                }
            }
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        private AppSearchApp GetApp()
        {
            return (AppSearchApp)App;
        }

        private AppSearchView GetWnd()
        {
            return (AppSearchView)View;
        }

        private async Task BuildSearchableApplicationsListBox()
        {
            GetApp().Loader.Begin();

            string searchText = GetWnd().txbSearch.Text;

            await Task.Run(() =>
            {
                List<SearchableApplication> searchableApplications = GetSearchableApplications(searchText);

                View.Dispatcher.Invoke(() =>
                {
                    _appSearchViewModel = new AppSearchViewModel();
                    _appSearchViewModel.SearchableApplications = new System.Collections.ObjectModel.ObservableCollection<SearchableApplication>(searchableApplications);

                    View.DataContext = _appSearchViewModel;

                    GetApp().Loader.Stop();

                }, DispatcherPriority.Normal);
            });
        }

        private List<SearchableApplication> GetSearchableApplications(string searchText)
        {
            if (FnString.IsNullEmptyOrWhitespace(searchText))
                return AppEnvironment.AvailableApplications;

            return AppEnvironment.AvailableApplications
                .Where(a => a.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private void LaunchSelectedApplication()
        {
            SearchableApplication? selected = _appSearchViewModel?.SelectedSearchableApplication;

            if (selected == null)
                return;

            if (!AppEnvironment.StartedApplications.ContainsKey(selected.ClassName))
                AppEnvironment.AppLauncher.LaunchApplication(selected.ClassName, GetApp(), selected.Name);

            Exit(true);
        }

        protected async void EV_txbSearch()
        {
            await BuildSearchableApplicationsListBox();
        }
    }
}

