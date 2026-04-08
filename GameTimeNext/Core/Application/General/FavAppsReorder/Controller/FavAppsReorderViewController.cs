using GameTimeNext.Core.Application.General.FavAppsReorder.ViewModels;
using GameTimeNext.Core.Application.General.FavAppsReorder.Views;
using GameTimeNext.Core.Application.General.UserSettings;
using GameTimeNext.Core.Framework;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.General.FavAppsReorder.Controller
{
    public class FavAppsReorderViewController : UIXWindowControllerBase
    {
        private FavAppsReorderViewModel? _favAppsReorderViewModel;
        private object? _draggedItem;
        private ListViewItem? _lastDragTargetItem;
        private bool _insertAfterTarget;
        private List<FavoriteApplication>? _originalFavAppsOrder = null;


        public class FavAppsRecorderViewReturn : UIXViewReturn
        {
            public bool HasChanged { get; set; } = false;
        }

        public FavAppsReorderViewController(UIXApplication app) : base(app)
        {
        }

        protected override void Init()
        {
            ViewReturn = new FavAppsRecorderViewReturn();

            _favAppsReorderViewModel = new FavAppsReorderViewModel();
        }

        protected override void BuildFirst()
        {
            BuildFavoriteApplicationsList();
            RegisterDragDropEvents();
            GetWnd().lvFavAppsReorder.Focus();
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
            ViewReturn = new FavAppsRecorderViewReturn
            {
                HasChanged = HasOrderChanged()
            };

            Exit(false);
        }

        protected override void Event_Maximize()
        {
        }

        protected override void Event_Minimize()
        {
        }

        protected override void HandleUIEventImpl(FrameworkElement source, string eventName, RoutedEventArgs args)
        {

            base.HandleUIEventImpl(source, eventName, args);

            if (eventName == UIXEventNames.UIElement.KeyDown && args is KeyEventArgs keyArgs)
            {
                if (keyArgs.Key == Key.Escape)
                {
                    Exit(true);
                    keyArgs.Handled = true;
                    return;
                }
            }
            else if (eventName == UIXEventNames.UIElement.PreviewMouseLeftButtonDown && args is MouseButtonEventArgs mouseArgs)
            {
                if (source is ListViewItem listViewItem)
                {
                    _draggedItem = listViewItem.DataContext;
                    DragDrop.DoDragDrop(listViewItem, _draggedItem, DragDropEffects.Move);
                    mouseArgs.Handled = true;
                }
            }
            else if (eventName == UIXEventNames.UIElement.DragOver && args is DragEventArgs dragOverArgs)
            {
                dragOverArgs.Effects = DragDropEffects.Move;
                dragOverArgs.Handled = true;
            }
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        private FavAppsReorderApp GetApp()
        {
            return (FavAppsReorderApp)App;
        }

        private FavAppsReorderView GetWnd()
        {
            return (FavAppsReorderView)View;
        }

        private void BuildFavoriteApplicationsList()
        {
            List<FavoriteApplication> favoriteApps = GetFavoriteApplications();

            _favAppsReorderViewModel = new FavAppsReorderViewModel();
            _favAppsReorderViewModel.FavoriteApplications = new System.Collections.ObjectModel.ObservableCollection<FavoriteApplication>(favoriteApps);

            View.DataContext = _favAppsReorderViewModel;

            GetWnd().lvFavAppsReorder.SelectedIndex = -1;

            _originalFavAppsOrder = favoriteApps;
        }

        private List<FavoriteApplication> GetFavoriteApplications()
        {
            return AppEnvironment.GetAppConfig().UserSettings.FavApps;
        }

        private void RegisterDragDropEvents()
        {
            var listView = GetWnd().lvFavAppsReorder;

            listView.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnPreviewMouseLeftButtonDown), true);
            listView.AddHandler(UIElement.DropEvent, new DragEventHandler(OnDrop), true);
            listView.AddHandler(UIElement.DragOverEvent, new DragEventHandler(OnDragOver), true);
            listView.AddHandler(UIElement.DragLeaveEvent, new DragEventHandler(OnDragLeave), true);
        }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject dep)
            {
                var listViewItem = FindParent<ListViewItem>(dep);
                if (listViewItem != null)
                {
                    HandleUIEvent(listViewItem, UIXEventNames.UIElement.PreviewMouseLeftButtonDown, e);
                }
            }
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (_lastDragTargetItem != null)
            {
                _lastDragTargetItem.Tag = null;
                _lastDragTargetItem = null;
            }

            if (e.OriginalSource is DependencyObject dep)
            {
                var targetListViewItem = FindParent<ListViewItem>(dep);
                if (targetListViewItem != null)
                {
                    var targetItem = targetListViewItem.DataContext;

                    if (_draggedItem != null && targetItem != null && targetItem != _draggedItem)
                    {
                        ReorderItems(_draggedItem, targetItem);
                    }

                    _draggedItem = null;
                    e.Handled = true;
                    return;
                }
            }

            _draggedItem = null;
            e.Handled = true;
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (e.OriginalSource is DependencyObject dep)
            {
                var targetListViewItem = FindParent<ListViewItem>(dep);

                if (targetListViewItem != null)
                {
                    Point mousePosition = e.GetPosition(targetListViewItem);
                    double itemHeight = targetListViewItem.ActualHeight;
                    bool isUpperHalf = mousePosition.Y < (itemHeight / 2);

                    _insertAfterTarget = !isUpperHalf;

                    string newTag = isUpperHalf ? "DragTargetTop" : "DragTargetBottom";

                    if (_lastDragTargetItem != targetListViewItem || (string?)_lastDragTargetItem?.Tag != newTag)
                    {
                        if (_lastDragTargetItem != null)
                        {
                            _lastDragTargetItem.Tag = null;
                        }

                        targetListViewItem.Tag = newTag;
                        _lastDragTargetItem = targetListViewItem;
                    }
                }
            }

            HandleUIEvent(GetWnd().lvFavAppsReorder, UIXEventNames.UIElement.DragOver, e);
        }

        private void OnDragLeave(object sender, DragEventArgs e)
        {
            if (_lastDragTargetItem != null)
            {
                _lastDragTargetItem.Tag = null;
                _lastDragTargetItem = null;
            }
        }

        private T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject? parent = System.Windows.Media.VisualTreeHelper.GetParent(child);

            while (parent != null)
            {
                if (parent is T typedParent)
                    return typedParent;

                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        private void ReorderItems(object draggedItem, object targetItem)
        {
            if (_favAppsReorderViewModel?.FavoriteApplications == null)
                return;

            int draggedIndex = _favAppsReorderViewModel.FavoriteApplications.IndexOf((FavoriteApplication)draggedItem);
            int targetIndex = _favAppsReorderViewModel.FavoriteApplications.IndexOf((FavoriteApplication)targetItem);

            if (draggedIndex >= 0 && targetIndex >= 0)
            {
                _favAppsReorderViewModel.FavoriteApplications.RemoveAt(draggedIndex);

                int newIndex = targetIndex;
                if (_insertAfterTarget)
                {
                    newIndex++;
                }

                if (draggedIndex < targetIndex)
                {
                    newIndex--;
                }

                newIndex = Math.Max(0, Math.Min(newIndex, _favAppsReorderViewModel.FavoriteApplications.Count));

                _favAppsReorderViewModel.FavoriteApplications.Insert(newIndex, (FavoriteApplication)draggedItem);

                // -- Hier Favorite Applications lt. jetziger Listbox neu anordnen und in der AppConfig aktualisieren
                FnUserSettings.ReorderFavoriteApplications(_favAppsReorderViewModel.FavoriteApplications.ToList());

                AppEnvironment.GetAppConfig().UserSettings.FavApps = _favAppsReorderViewModel.FavoriteApplications.ToList();

                AppEnvironment.SaveAppConfig();

                GetViewReturn<FavAppsRecorderViewReturn>().HasChanged = HasOrderChanged();
            }
        }

        private bool HasOrderChanged()
        {
            if (_favAppsReorderViewModel?.FavoriteApplications == null || _originalFavAppsOrder == null)
                return false;
            var currentOrder = _favAppsReorderViewModel.FavoriteApplications.ToList();
            if (currentOrder.Count != _originalFavAppsOrder.Count)
                return true;
            for (int i = 0; i < currentOrder.Count; i++)
            {
                if (currentOrder[i].FullName != _originalFavAppsOrder[i].FullName)
                    return true;
            }
            return false;
        }
    }
}
