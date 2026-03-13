using GameTimeNext.Core.Application.General.UserSettings;
using System.Windows;
using System.Windows.Controls;
using UIX.ViewController.Engine.FrameworkElements;
using UIX.ViewController.Engine.Runnables;
using static UIX.ViewController.Engine.FrameworkElements.UIXContextMenuFactory;

namespace GameTimeNext.Core.Framework
{
    public class ApplicationLauncher
    {

        public TabControl TabControl { get; set; }

        public ApplicationLauncher(TabControl tabControl)
        {
            TabControl = tabControl;
        }

        public TabItem LaunchApplication(string className, UIXApplication host, string header)
        {
            Type type = Type.GetType(className);

            if (type == null)
                throw new Exception($"Application type '{className}' not found.");

            UIXApplication app = (UIXApplication)Activator.CreateInstance(type);

            if (type.Name == "ProfilesApp")
                app.IsClosable = false;

            TabItem tabItem = new TabItem
            {
                Header = header,
                Name = "tabApplication",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };

            ContentPresenter presenter = new ContentPresenter
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            tabItem.Tag = type;
            tabItem.Content = presenter;

            if (app is IUIXApplicationStarter starter)
                starter.Start(host, presenter);

            if (type.FullName != null && !AppEnvironment.StartedApplications.ContainsKey(type.FullName))
                AppEnvironment.StartedApplications.Add(type.FullName, app);

            BuildContextMenu(tabItem, null);

            this.TabControl.Items.Add(tabItem);
            this.TabControl.SelectedItem = tabItem;

            // Wenn derzeitige gestartete App in den Favoriten "primary start" aktiviert hat, dann zurückgeben
            FavoriteApplication favoriteApplication = FnUserSettings.GetFavoriteApplication(AppEnvironment.GetAppConfig().UserSettings.FavApps, type.FullName);
            if (favoriteApplication != null && favoriteApplication.PrimaryStart)
                return tabItem;
            else
                return null!;
        }

        public void StartFavorites(UIXApplication host)
        {

            if (AppEnvironment.GetAppConfig().UserSettings.FavApps == null || AppEnvironment.GetAppConfig().UserSettings.FavApps.Count == 0)
                return;

            List<FavoriteApplication> favAppsOrdered = AppEnvironment.GetAppConfig().UserSettings.FavApps.OrderBy(a => a.Order).ToList();

            TabItem? primaryTab = null;
            foreach (FavoriteApplication favApp in favAppsOrdered)
            {
                string tabName = favApp.AppName;
                string fullName = favApp.FullName;

                TabItem primaryTabTemp = LaunchApplication(fullName, host, tabName);

                if (primaryTabTemp != null)
                    primaryTab = primaryTabTemp;
            }

            // Am Ende den Tab mit dem primären Start selektieren
            if (primaryTab != null)
                primaryTab.IsSelected = true;

        }

        public void CloseApplication(string typeName, TabItem tab)
        {

            Type type = Type.GetType(typeName!);

            AppEnvironment.StartedApplications.Remove(type!.FullName!);

            TabControl tabControl = ItemsControl.ItemsControlFromItemContainer(tab) as TabControl;

            if (tabControl != null)
                tabControl.Items.Remove(tab);
        }

        public void BuildContextMenu(TabItem tabItem, FavoriteApplication favApp)
        {
            Style contextMenuItemStyle = (Style)System.Windows.Application.Current.FindResource("ModernContextMenuItemStyle");
            Style contextMenuStyle = (Style)System.Windows.Application.Current.FindResource("ModernContextMenuStyle");

            UIXApplication application = AppEnvironment.StartedApplications[tabItem.Tag.ToString()];

            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("TabContextMenu");
            contextBuilder.SetStyle(contextMenuStyle);

            if (application.IsClosable)
                contextBuilder.AddItem("ctxtClose", "Close", icon: UIXContextMenuFactory.CreateMdlIcon("\uE8BB"), itemStyle: contextMenuItemStyle);

            if (favApp == null)
                contextBuilder.AddItem("ctxtFav", "Add to favorites", icon: UIXContextMenuFactory.CreateMdlIcon("\uE734"), itemStyle: contextMenuItemStyle);
            else
            {
                contextBuilder.AddItem("ctxtDeleteFav", "Remove from favorites", icon: UIXContextMenuFactory.CreateMdlIcon("\uE734"), itemStyle: contextMenuItemStyle);

                if (!favApp.PrimaryStart && !FnUserSettings.IsPrimaryStartSet())
                    contextBuilder.AddItem("ctxtSetAsPrimaryStart", "Set as primary start", icon: UIXContextMenuFactory.CreateMdlIcon("\uE840"), itemStyle: contextMenuItemStyle);
            }

            if (contextBuilder.HasItems())
                tabItem.ContextMenu = contextBuilder.Build();
            else
            {
                tabItem.ContextMenu = null;
            }
        }
    }
}
