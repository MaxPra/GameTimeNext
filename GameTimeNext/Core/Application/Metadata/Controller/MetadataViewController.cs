using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Application.Metadata.Views;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.DataBase.Migration;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Metadata.Controller
{
    public class MetadataViewController : UIXViewControllerBase<MetadataApp, MetadataView>
    {
        public MetadataViewController(UIXApplication app) : base(app)
        {
        }

        protected override void Init()
        {
            AddSource("T1CTABD", new TXCTABD());
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {
        }

        protected override async Task BuildFirstImplAsync()
        {
            await GetView().DgMetadata.LoadDataAsync();
        }

        protected override void BuildImpl()
        {
        }

        protected override async Task BuildImplAsync()
        {
        }

        protected override void CheckImpl()
        {
        }

        protected override void FillViewImpl()
        {
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected void EV_BtnAdd()
        {
            MetadataEditApp metadataEditApp = GetApp().GetApplication<MetadataEditApp>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
            metadataEditApp.CreateNew(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await GetView().DgMetadata.LoadDataAsync();
            });
        }

        protected async Task EV_BtnRefresh()
        {
            await GetView().DgMetadata.LoadDataAsync();
        }

        protected async Task EV_BtnGenerateAll()
        {
            try
            {
                GetApp().Loader.Begin();

                List<T1METAH> t1metahs = TFMETAH.GetMetadata(
                    GetView().TxbTableObjectName.Text,
                    GetView().TxbDescription.Text,
                    GetView().CmbType.SelectedValue?.ToString() ?? string.Empty);

                foreach (T1METAH t1metah in t1metahs)
                {
                    await Task.Run(() =>
                    {
                        MigrationFactory.Metadata.CodeGenerator.GenerateFor(GetApp(), t1metah);
                        MigrationFactory.FromCsv.MigrateTables(MigrationFactory.ImportType.MetadataGenerator);
                    });

                    t1metah.GENER = true;
                    new TXMETAH().Save(t1metah);
                }

                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("MET0002"), CFMBOXResult.Ok, CFMBOXIcon.Success);
            }
            catch (Exception ex)
            {
                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("GLO0013", "generating the classes", ex.Message), CFMBOXResult.Ok, CFMBOXIcon.Error);
            }
            finally
            {
                GetApp().Loader.Stop();
            }

            await GetView().DgMetadata.LoadDataAsync();
        }

        protected async Task EV_BtnGenerateMetadataTables()
        {
            try
            {
                GetApp().Loader.Begin();

                await Task.Run(() => MigrationFactory.Metadata.CodeGenerator.GenerateForMetadata(GetApp()));

                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("MET0002"), CFMBOXResult.Ok, CFMBOXIcon.Success);
            }
            catch (Exception ex)
            {
                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("GLO0013", "generating the classes", ex.Message), CFMBOXResult.Ok, CFMBOXIcon.Error);
            }
            finally
            {
                GetApp().Loader.Stop();
            }
        }

        protected void EV_BtnOpenLog()
        {
            Process.Start("explorer.exe", AppConfig.LogsDirectoryPath);
        }

        protected void EV_BtnOpenGenClass()
        {
            Process.Start("explorer.exe", AppConfig.Dev.GenClassDirectoryPath);
        }
    }
}
