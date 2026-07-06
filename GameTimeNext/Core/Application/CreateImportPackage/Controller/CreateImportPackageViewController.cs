using GameTimeNext.Core.Application.CreateImportPackage.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.Utils;
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

        protected override void Check()
        {
            if (FnString.IsNullEmptyOrWhitespace(GetView().TxbOutputPath.Text))
                AddViewError(GetView().TxbOutputPath, "Output path cannot be empty.");
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

        protected void EV_BtnBrowseOutputPath()
        {
            string outputPath = FnSystemDialogs.ShowFolderDialog("Select Output Path", false);

            if (FnString.IsNullEmptyOrWhitespace(outputPath))
                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show("Select a valid output path!", CFMBOXResult.Ok, CFMBOXIcon.Error);

            GetView().TxbOutputPath.Text = outputPath;
        }

        protected async Task EV_BtnExecute()
        {
            Check();

            if (HasViewErrors())
                return;

            GetApp().Loader.Begin("Creating Import Package...");

            string outputPath = GetView().TxbOutputPath.Text;
            string exportType = GetView().CmbExportType.SelectedValue.ToString()!;

            bool error = false;

            await Task.Run(() =>
            {
                try
                {
                    CFCreateImportPackageApp.CreateImportPackage(outputPath, exportType);
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
                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show("An error occured.", CFMBOXResult.Ok, CFMBOXIcon.Error);
            }
            else
            {
                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show("Import package created successfully", CFMBOXResult.Ok, CFMBOXIcon.Success);
            }


        }
    }
}
