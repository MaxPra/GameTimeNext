using GameTimeNext.Core.Application.CreateImportPackage.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.DataBase.Migration;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.CreateImportPackage.Controller
{
    public class CreateImportPackageViewController : UIXViewControllerBase
    {
        public CreateImportPackageViewController(UIXApplication app) : base(app)
        {
        }

        protected override void Init()
        {
            AddSource("T1CTABD", new TXCTABD());
        }

        protected override void BuildFirstImpl()
        {
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

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
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

        private CreateImportPackageApp GetApp()
        {
            return (CreateImportPackageApp)App;
        }

        private CreateImportPackageView GetView()
        {
            return (CreateImportPackageView)View;
        }

        protected async Task EV_btnExecute()
        {
            Check();

            if (HasViewErrors())
                return;

            GetApp().Loader.Begin("Creating Import Package...");

            bool error = false;

            await Task.Run(() =>
            {
                try
                {
                    string outputDirPath = AppConfig.Dev.ImportPackagesDirectoryPath;

                    if (Directory.Exists(outputDirPath))
                    {
                        Directory.Delete(outputDirPath, true);
                        FnDirectory.EnsureDirectoryExists(outputDirPath);
                    }

                    MigrationFactory.ImportPackage.ExportPackage(outputDirPath);
                }
                catch (Exception)
                {
                    error = true;
                }

                finally
                {
                    GetApp().Loader.Stop();
                }

            });

            if (error)
            {
                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("GLO0012", "creating the import package"), CFMBOXResult.Ok, CFMBOXIcon.Error);
            }
            else
            {
                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("GLO0011", "The import package"), CFMBOXResult.Ok, CFMBOXIcon.Success);
            }
        }

        protected async Task EV_btnTestImport()
        {
            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);

            try
            {
                GetApp().Loader.Begin();
                GetApp().Loader.SetText("Inmidst of testing ^^");

                bool useProdDb = GetView().chbUseProdDb.IsChecked.Equals(true);
                GetApp().StartTestImportPackage(useProdDb);
                cfmbox.Show(GetErrorMessage("IMP0001"), CFMBOXResult.Ok, CFMBOXIcon.Info);
                GetApp().EndTestImportPackage();
            }
            catch (Exception ex)
            {
                string errMsg = GetErrorMessage("GLO0012", "testing the import package");
                cfmbox.Show(errMsg, CFMBOXResult.Ok, CFMBOXIcon.Error);
                FnLog.AddError(this, errMsg, ex);
            }
            finally
            {
                GetApp().Loader.Stop();
            }
        }
    }
}
