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
    public class CreateImportPackageViewController : UIXViewControllerBase<CreateImportPackageApp, CreateImportPackageView>
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

        /// <summary>
        /// Removes the previous packages and leftover temp folders, but keeps the folder itself: a deleted folder
        /// that is still watched (e.g. by Visual Studio) stays "delete pending", and creating it again fails with
        /// access denied until the last handle is closed.
        /// </summary>
        private static void ClearOutputDirectory(string outputDirPath)
        {
            if (!Directory.Exists(outputDirPath))
                return;

            foreach (string filePath in Directory.GetFiles(outputDirPath))
                File.Delete(filePath);

            foreach (string directoryPath in Directory.GetDirectories(outputDirPath))
                Directory.Delete(directoryPath, true);
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

                    ClearOutputDirectory(outputDirPath);

                    MigrationFactory.ImportPackage.ExportPackage(outputDirPath);
                }
                catch (Exception ex)
                {
                    FnLog.AddError(this, "Creating the import package failed.", ex);
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
