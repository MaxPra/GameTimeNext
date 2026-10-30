using GameTimeNext.Core.Application.ErrorMessages.Views;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.ErrorMessages.Controller
{
    public class ErrorMessagesViewController : UIXViewControllerBase<ErrorMessagesApp, ErrorMessagesView>
    {
        public ErrorMessagesViewController(UIXApplication app) : base(app)
        {
        }

        #region Event-Pipeline-Methods
        protected override void Init()
        {
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {
        }

        protected override async Task BuildFirstImplAsync()
        {
            await GetView().dgErrorMessages.LoadDataAsync();
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

        protected void EV_btnAddErrorMessage()
        {
            ErrorMessagesEditApp editApp = GetApp().GetApplication<ErrorMessagesEditApp>(UIXApplicationStartTarget.Window);
            editApp.CreateNew(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await GetView().dgErrorMessages.LoadDataAsync();
            });
        }

        protected async Task EV_btnRefresh()
        {
            await GetView().dgErrorMessages.LoadDataAsync();
        }
    }
}
