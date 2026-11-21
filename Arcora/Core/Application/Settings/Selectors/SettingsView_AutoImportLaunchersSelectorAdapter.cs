using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.Settings.Viewmodels;
using GameTimeNext.Core.Application.Settings.Views;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.LauncherIntegration;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Settings.Selectors
{
    /// <summary>
    /// Launchers of the automatic game import: every launcher that supports an import, checked unless it is left out
    /// in the settings (AutoImportExcludedLaunchers).
    /// </summary>
    public class SettingsView_AutoImportLaunchersSelectorAdapter : UIXSelectorAdapterBase<SettingsApp, SettingsView, UIXListBox, SettingsAutoImportLauncherListItem>
    {
        public SettingsView_AutoImportLaunchersSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        protected override async Task<IEnumerable<SettingsAutoImportLauncherListItem>> ExecuteAsync(CancellationToken token)
        {
            string excludedLaunchers = AppEnvironment.GetAppConfig().AppSettings.AutoImportExcludedLaunchers;

            return await Task.Run(() =>
            {
                HashSet<string> excluded = CFProfilesApp.SplitLauncherTypes(excludedLaunchers).ToHashSet(StringComparer.Ordinal);

                return FnLauncher.GetImportLauncherTypes()
                    .Select(t => new SettingsAutoImportLauncherListItem
                    {
                        LauncherType = t,
                        CONAME = FnLauncher.GetLauncherName(t),
                        COISSEL = !excluded.Contains(t)
                    })
                    .ToList();
            }, token);
        }
    }
}
