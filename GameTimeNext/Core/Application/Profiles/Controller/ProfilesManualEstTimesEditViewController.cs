using System.Globalization;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    public class ProfilesManualEstTimesEditViewController : UIXViewControllerBase<ProfilesEditApp, ProfilesManualEstTimesEditView>
    {
        public ProfilesManualEstTimesEditViewController(UIXApplication app) : base(app)
        {
        }

        public class ProfilesManualEstTimesCache()
        {
            public double estTimeMain { get; set; }
            public double estTimeMainExtra { get; set; }
            public double estTimeCompletionist { get; set; }

            public string ProfileName { get; set; }
        }

        public class ProfilesManualEstTimesEditViewReturn : UIXViewReturn
        {
            public double estTimeMain { get; set; }
            public double estTimeMainExtra { get; set; }
            public double estTimeCompletionist { get; set; }
        }

        protected override void Init()
        {
            ViewReturn = new ProfilesManualEstTimesEditViewReturn();
        }

        protected override void BuildFirstImpl()
        {
        }

        protected override void BuildImpl()
        {
        }

        protected override void CheckImpl()
        {
            if (!FnControls.ContainsOnlyNumericValue(GetView().txbEstTimeMain))
                AddViewError(GetView().txbEstTimeMain, GetErrorMessage("GLO0009"));

            if (!FnControls.ContainsOnlyNumericValue(GetView().txbEstTimeMainExtra))
                AddViewError(GetView().txbEstTimeMainExtra, GetErrorMessage("GLO0009"));

            if (!FnControls.ContainsOnlyNumericValue(GetView().txbEstTimeCompletionist))
                AddViewError(GetView().txbEstTimeCompletionist, GetErrorMessage("GLO0009"));

            if (FnString.IsNullEmptyOrWhitespace(GetView().txbEstTimeMain.Text))
                AddViewError(GetView().txbEstTimeMain, GetErrorMessage("GLO0003", "Est. time main"));

            if (FnString.IsNullEmptyOrWhitespace(GetView().txbEstTimeMainExtra.Text))
                AddViewError(GetView().txbEstTimeMainExtra, GetErrorMessage("GLO0003", "Est. time extra"));

            if (FnString.IsNullEmptyOrWhitespace(GetView().txbEstTimeCompletionist.Text))
                AddViewError(GetView().txbEstTimeCompletionist, GetErrorMessage("GLO0003", "Est. time completionist"));
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
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
            GetView().txbEstTimeMain.Text = GetApp().ManualEstTimesCache.estTimeMain.ToString(CultureInfo.CurrentCulture);
            GetView().txbEstTimeMainExtra.Text = GetApp().ManualEstTimesCache.estTimeMainExtra.ToString(CultureInfo.CurrentCulture);
            GetView().txbEstTimeCompletionist.Text = GetApp().ManualEstTimesCache.estTimeCompletionist.ToString(CultureInfo.CurrentCulture);
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void Event_Closing()
        {
        }

        protected override void Event_Minimize()
        {
        }

        protected override void Event_Maximize()
        {
        }

        protected void EV_btnOpenHowLongToBeat()
        {
            CFProfilesEditApp.OpenHowLongToBeatForGame(GetApp().ManualEstTimesCache.ProfileName);
        }

        protected void EV_btnApply()
        {
            Check();

            if (ViewErrors.Count > 0)
                return;

            GetViewReturn<ProfilesManualEstTimesEditViewReturn>().estTimeMain = FnConvert.ToDouble(GetView().txbEstTimeMain.Text);
            GetViewReturn<ProfilesManualEstTimesEditViewReturn>().estTimeMainExtra = FnConvert.ToDouble(GetView().txbEstTimeMainExtra.Text);
            GetViewReturn<ProfilesManualEstTimesEditViewReturn>().estTimeCompletionist = FnConvert.ToDouble(GetView().txbEstTimeCompletionist.Text);
            GetViewReturn<ProfilesManualEstTimesEditViewReturn>().Canceled = false;

            Exit(true);
        }

        protected void EV_btnCancel()
        {
            Exit(true);
        }
    }
}
