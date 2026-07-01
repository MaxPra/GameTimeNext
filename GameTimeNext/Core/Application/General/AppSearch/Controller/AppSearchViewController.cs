using GameTimeNext.Core.Application.CreateImportPackage;
using GameTimeNext.Core.Application.General.AppSearch.ViewModels;
using GameTimeNext.Core.Application.General.AppSearch.Views;
using GameTimeNext.Core.Application.Metadata;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Utils;
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

        protected override void BuildFirstImpl()
        {
            BuildSearchableApplicationsListBox();

            GetWnd().txbSearch.Focus();

        }

        protected override void BuildImpl()
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

                // Aussortieren von Anwendungen, welche nicht für alle User sichtbar sein sollen
                searchableApplications = SortOutSearchableApplications(searchableApplications);

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

        private List<SearchableApplication> SortOutSearchableApplications(List<SearchableApplication> applications)
        {

            List<SearchableApplication> sortedApplications = new List<SearchableApplication>();

            if (applications == null)
                return new List<SearchableApplication>();

            foreach (var app in applications)
            {
                // CreateImportPackage wird ausgenommen (außer wenn in Debug-/Dev-Modus)
                if (app.ClassName != typeof(CreateImportPackageApp).FullName! && app.ClassName != typeof(MetadataApp).FullName! || FnSystem.IsDebug())
                {
                    sortedApplications.Add(app);
                }
            }

            return sortedApplications;
        }

        private void LaunchSelectedApplication()
        {
            SearchableApplication? selected = _appSearchViewModel?.SelectedSearchableApplication;

            if (selected == null)
                return;

            string className = selected.ClassName;
            string appName = selected.Name;
            UIXApplication hostApplication = GetApp().HostApplication;

            void LaunchAfterClose(object? sender, EventArgs args)
            {
                GetWnd().Closed -= LaunchAfterClose;

                hostApplication.MainView.Dispatcher.BeginInvoke(() =>
                {
                    if (!AppEnvironment.StartedApplications.ContainsKey(className))
                        AppEnvironment.AppLauncher.LaunchApplication(className, hostApplication, appName);
                }, DispatcherPriority.ApplicationIdle);
            }

            GetWnd().Closed += LaunchAfterClose;

            Exit(true);
        }

        protected async void EV_txbSearch()
        {
            await BuildSearchableApplicationsListBox();
        }
    }
}

