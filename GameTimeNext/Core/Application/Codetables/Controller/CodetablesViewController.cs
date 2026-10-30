using GameTimeNext.Core.Application.Codetables.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Codetables.Controller
{
    public class CodetablesViewController : UIXViewControllerBase<CodetablesApp, CodetablesView>
    {
        public CodetablesViewController(UIXApplication app) : base(app)
        {
        }

        #region Event-Pipeline-Methods
        protected override void Init()
        {
            AddSource("T1CTABD", new TXCTABD());
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {
            // Show the add button only in debug / development mode
            FnControls.SetVisible(GetView().BtnAddCodetable, FnSystem.IsDebug() && !FnSystem.RunWithUserPermissions());
        }

        protected override async Task BuildFirstImplAsync()
        {
            await GetView().DgCodetables.LoadDataAsync();
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
        #endregion

        protected async Task EV_BtnAddCodetable()
        {
            CodetablesEditApp codetablesEditApp = GetApp().GetApplication<CodetablesEditApp>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
            codetablesEditApp.CreateNew(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await GetView().DgCodetables.LoadDataAsync();
            });
        }

        protected async Task EV_BtnRefresh()
        {
            await GetView().DgCodetables.LoadDataAsync();
        }
    }
}
