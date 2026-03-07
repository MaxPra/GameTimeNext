using GameTimeNext.Core.Application.GTXMigration;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;
using UIX.ViewController.Engine.FrameworkElements.Windows;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.General.Controller
{
    internal class MainWindowController : UIXWindowControllerBase
    {
        private string _gtxPath = "C:\\GameTimeX";

        public class WindowReturn : UIXWindowReturn
        {
            public string Test { get; set; } = "Das steht am Anfang drin!";

            public WindowReturn() { }

        }

        private WindowReturn _mainWindowReturn = new WindowReturn();

        public MainWindowController(UIXApplication app) : base(app)
        {
        }

        public override WindowReturn GetWindowReturn()
        {
            return _mainWindowReturn;
        }

        protected override void Init()
        {

        }

        protected override void BuildFirst()
        {

        }

        protected override async Task BuildFirstAsync()
        {
            // DEV-Batch & Metadata-Tab
            if (!Debugger.IsAttached)
            {
                GetWindow().BdDevModeBatch.Visibility = Visibility.Hidden;
            }

            GetWindow().TabMetaData.Visibility = Visibility.Hidden;

            await CheckGTXMigration();

            GetApp().ProfilesApp = new ProfilesApp();
            GetApp().ProfilesApp.Start(GetApp(), GetWindow().CPProfileView);
        }

        protected override void Build()
        {

        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
            switch (eventName)
            {
                case UIXEventNames.Selector.SelectionChanged:

                    if (source is TabControl)
                        Event_TabChanged((TabControl)source);
                    break;
            }
        }

        protected override void Check()
        {
        }

        protected override void FillViewImpl()
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source) { }

        protected override void Event_Closing()
        {
        }

        protected override void Event_Minimize()
        {
        }

        protected override void Event_Maximize()
        {
        }

        /// <summary>
        /// Wird Tab geändert
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_TabChanged(TabControl source)
        {
            if (source.SelectedItem is TabItem ti && ti.Name == "Tab_Profiles")
                GetApp().ProfilesApp.ProfilesView.ViewController.Show(false);
        }

        private async Task CheckGTXMigration()
        {

            string[] gtnFiles = Directory.GetFiles(AppEnvironment.GetAppConfig().CoverFolderPath);
            string loaderTextStart = "Migrating GTX -> GTN ...";

            if (Directory.Exists(_gtxPath) && gtnFiles.Length == 0)
            {
                CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>();
                string msg = "A GameTimeX-Installation was found.\nDo you want to migrate your profiles to GameTimeNXT?";
                CFMBOXResult result = cfmbox.Show("Start GameTimeX migration?", msg, CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

                if (result == CFMBOXResult.Yes)
                {
                    GetApp().Loader.Begin(loaderTextStart);

                    await Task.Run(() =>
                    {
                        try
                        {
                            GTXMigrationHelper gtxMigHelper = new GTXMigrationHelper("C:\\GameTimeX", GetApp().Loader);
                            gtxMigHelper.MigrateToGTNXT();
                        }
                        catch (Exception ex)
                        {
                        }
                        finally
                        {
                            GetApp().Loader.Stop();
                        }
                    });
                }
            }
        }

        private MainWindow GetWindow()
        {
            return (MainWindow)View;
        }

        private MainApp GetApp()
        {
            return (MainApp)App;
        }
    }
}
