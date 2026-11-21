using Arcora.Core.Application.General.UserSettings;
using Arcora.Core.Application.Profiles;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.FrameworkElements;
using UIX.ViewController.Engine.FrameworkElements.Windows;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static UIX.ViewController.Engine.FrameworkElements.UIXContextMenuFactory;

namespace Arcora.Core.Framework
{
    public class ApplicationLauncher
    {

        /// <summary>
        /// Applications that are always open: they are always favorites (started at their place in the start order,
        /// which can be changed like the order of the others), can't be removed from the favorites and can't be closed
        /// (the application itself sets <see cref="UIXApplication.IsClosable"/> to false).
        /// </summary>
        private static readonly Type[] AlwaysStartedApplications = { typeof(ProfilesApp) };

        public TabControl TabControl { get; set; }

        public ApplicationLauncher(TabControl tabControl)
        {
            TabControl = tabControl;

            // Unsaved changes in a tab that isn't selected are asked in that tab
            UIXWindowHost.BringIntoViewHandler = SelectTabOfHost;
        }

        private void SelectTabOfHost(UIXWindowHost host)
        {
            TabItem? tab = TabControl.Items.OfType<TabItem>().FirstOrDefault(t => t.Content is ContentPresenter presenter && ReferenceEquals(presenter.Content, host.Element));

            if (tab != null)
                TabControl.SelectedItem = tab;
        }

        /// <summary>
        /// Opens an application in a tab named after its application definition (see <see cref="GetApplicationTitle"/>).
        /// </summary>
        public TabItem LaunchApplication(Type type, UIXApplication host)
        {
            return LaunchApplication(type.FullName!, host, GetApplicationTitle(type));
        }

        public TabItem LaunchApplication(string className, UIXApplication host, string header)
        {
            Type type = Type.GetType(className)
        ?? throw new Exception($"Application type '{className}' not found.");

            // Application is already open: switch to its tab instead of starting it again
            TabItem? existingTab = FindApplicationTab(type.FullName!);
            if (existingTab != null)
            {
                this.TabControl.SelectedItem = existingTab;
                FocusTabContent(existingTab);
                return null!;
            }

            UIXApplication app = (UIXApplication)Activator.CreateInstance(type)!;

            TabItem tabItem = new TabItem
            {
                Header = header,
                Name = "tabApplication",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };

            tabItem.Uid = app.Icon;

            ContentPresenter presenter = new ContentPresenter
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            tabItem.Tag = type.FullName!; // important for BuildContextMenu/StartedApplications
            tabItem.Content = presenter;


            if (app is IUIXApplicationStarter starter)
                starter.Start(host, new UIXApplicationStartOptions
                {
                    Target = UIXApplicationStartTarget.ContentPresenter,
                    Presenter = presenter
                });

            if (type.FullName != null && !AppEnvironment.StartedApplications.ContainsKey(type.FullName))
                AppEnvironment.StartedApplications.Add(type.FullName, app);

            BuildContextMenu(tabItem, null);

            this.TabControl.Items.Add(tabItem);
            this.TabControl.SelectedItem = tabItem;
            FocusTabContent(tabItem);

            host.MainView.Dispatcher.Invoke(
        DispatcherPriority.Background,
        new Action(() => { }));

            // Return the currently started app if "primary start" is enabled for it in the favorites
            FavoriteApplication favoriteApplication = FnUserSettings.GetFavoriteApplication(AppEnvironment.GetAppConfig().UserSettings.FavApps, type.FullName);
            if (favoriteApplication != null && favoriteApplication.PrimaryStart)
                return tabItem;
            else
                return null!;
        }

        /// <summary>
        /// Moves the keyboard focus from the tab header into the view of the tab once it is shown. Otherwise a tab
        /// opened by keyboard (e.g. Enter in the application search) keeps the focus with its focus ring on the header.
        /// A view that has set its own focus keeps it.
        /// </summary>
        private void FocusTabContent(TabItem tab)
        {
            TabControl.Dispatcher.BeginInvoke(() =>
            {
                if (!tab.IsSelected || !tab.IsKeyboardFocused || tab.Content is not UIElement content)
                    return;

                content.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            }, DispatcherPriority.ContextIdle);
        }

        private TabItem? FindApplicationTab(string typeFullName)
        {
            if (!AppEnvironment.StartedApplications.ContainsKey(typeFullName))
                return null;

            return this.TabControl.Items.OfType<TabItem>().FirstOrDefault(t => t.Tag?.ToString() == typeFullName);
        }

        /// <summary>
        /// True for an application that is always open (see <see cref="AlwaysStartedApplications"/>).
        /// </summary>
        public static bool IsAlwaysStarted(string? typeFullName)
        {
            return AlwaysStartedApplications.Any(t => t.FullName == typeFullName);
        }

        public void StartFavorites(UIXApplication host)
        {
            AppEnvironment.GetAppConfig().UserSettings.FavApps ??= new List<FavoriteApplication>();

            EnsureAlwaysStartedFavorites();

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

            // Finally select the tab with the primary start
            if (primaryTab != null)
                primaryTab.IsSelected = true;

        }

        /// <summary>
        /// Makes every always started application a favorite (in front of the others) if it isn't one yet, so it is
        /// started with the favorites and its place in the start order can be changed.
        /// </summary>
        private static void EnsureAlwaysStartedFavorites()
        {
            List<FavoriteApplication> favApps = AppEnvironment.GetAppConfig().UserSettings.FavApps;

            foreach (Type type in Enumerable.Reverse(AlwaysStartedApplications))
            {
                if (FnUserSettings.ContainsApplication(favApps, type.FullName!))
                    continue;

                FnUserSettings.InsertFavoriteApplicationAtStart(favApps, new FavoriteApplication
                {
                    FullName = type.FullName!,
                    AppName = GetApplicationTitle(type),
                    PrimaryStart = false
                });
            }
        }

        /// <summary>
        /// Name of an application for its tab: the description of its application definition (T1APRDH), or the class
        /// name without one.
        /// </summary>
        private static string GetApplicationTitle(Type type)
        {
            string? description = UIXApplication.AppPropertiesProvider?.Read(type.Name)?.ApplicationDescription;

            return FnString.IsNullEmptyOrWhitespace(description) ? type.Name : description!;
        }

        public void CloseApplication(string typeName, TabItem tab)
        {

            Type type = Type.GetType(typeName!);

            UIXApplication app = AppEnvironment.StartedApplications[typeName];

            if (!app.IsClosable || !app.CanClose())
                return;

            // Windows opened from the tab belong to its view and are closed with it; unsaved changes in them or in
            // the view are asked first, and the tab stays open if the user keeps something open
            if (!app.TryCloseViews())
                return;

            RemoveTab(type!.FullName!, tab);
        }

        /// <summary>
        /// Closes all open applications that can be closed. Applications that can't be closed stay open. If the user keeps
        /// something open when asked about unsaved changes, closing stops there (the remaining tabs stay open).
        /// </summary>
        public void CloseAll()
        {
            foreach (TabItem tab in TabControl.Items.OfType<TabItem>().ToList())
            {
                string? typeName = tab.Tag?.ToString();

                if (typeName == null || !AppEnvironment.StartedApplications.TryGetValue(typeName, out UIXApplication? app))
                    continue;

                if (!app.IsClosable || !app.CanClose())
                    continue;

                // Asks about unsaved changes; the user keeps the application open -> stop instead of asking for every tab
                if (!app.TryCloseViews())
                    return;

                RemoveTab(typeName, tab);
            }
        }

        private static void RemoveTab(string typeName, TabItem tab)
        {
            // Records the application still holds (e.g. an entry open in the edit area of a combined edit) are free again
            if (AppEnvironment.StartedApplications.TryGetValue(typeName, out UIXApplication? app))
                UIXRecordLocks.ReleaseAll(app);

            AppEnvironment.StartedApplications.Remove(typeName);

            TabControl tabControl = ItemsControl.ItemsControlFromItemContainer(tab) as TabControl;

            if (tabControl != null)
                tabControl.Items.Remove(tab);
        }

        public void BuildContextMenu(TabItem tabItem, FavoriteApplication favApp)
        {
            Style contextMenuItemStyle = (Style)System.Windows.Application.Current.FindResource("ModernContextMenuItemStyle");
            Style contextMenuStyle = (Style)System.Windows.Application.Current.FindResource("ModernContextMenuStyle");
            Style contextMenuSeparatorStyle = (Style)System.Windows.Application.Current.FindResource("ModernContextMenuSeparatorStyle");

            UIXApplication application = AppEnvironment.StartedApplications[tabItem.Tag.ToString()];

            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("TabContextMenu");
            contextBuilder.SetStyle(contextMenuStyle);

            if (application.IsClosable)
            {
                contextBuilder.AddItem("ctxtClose", "Close", icon: UIXContextMenuFactory.CreateMdlIcon("\uE8BB"), itemStyle: contextMenuItemStyle);
                contextBuilder.AddItem("ctxtCloseAll", "Close all", icon: UIXContextMenuFactory.CreateMdlIcon("\uE711"), itemStyle: contextMenuItemStyle);
            }

            if (favApp == null)
                contextBuilder.AddItem("ctxtFav", "Add to favorites", icon: UIXContextMenuFactory.CreateMdlIcon("\uE734"), itemStyle: contextMenuItemStyle);
            else
            {
                // An always started application stays a favorite
                if (!IsAlwaysStarted(tabItem.Tag.ToString()))
                    contextBuilder.AddItem("ctxtDeleteFav", "Remove from favorites", icon: UIXContextMenuFactory.CreateMdlIcon("\uE734"), itemStyle: contextMenuItemStyle);

                if (!favApp.PrimaryStart && !FnUserSettings.IsPrimaryStartSet())
                    contextBuilder.AddItem("ctxtSetAsPrimaryStart", "Set as primary start", icon: UIXContextMenuFactory.CreateMdlIcon("\uE840"), itemStyle: contextMenuItemStyle);
                else if (favApp.PrimaryStart)
                    contextBuilder.AddItem("ctxtUnsetAsPrimaryStart", "Unset as primary start", icon: UIXContextMenuFactory.CreateMdlIcon("\uE840"), itemStyle: contextMenuItemStyle);
            }

            var favApps = AppEnvironment.GetAppConfig().UserSettings.FavApps.OrderBy(a => a.Order).ToList();

            // Help text of the application (T1APRDH), shown as Markdown in the CFMBOX
            contextBuilder.AddSeparator(contextMenuSeparatorStyle);
            contextBuilder.AddItem("ctxtHelp", "Help", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Help), itemStyle: contextMenuItemStyle);


            if (contextBuilder.HasItems())
                tabItem.ContextMenu = contextBuilder.Build();
            else
            {
                tabItem.ContextMenu = null;
            }
        }
    }
}
