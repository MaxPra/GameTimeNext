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
    public class ProfilesManualEstTimesEditViewController : UIXWindowControllerBase
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

        protected override void BuildFirst()
        {
        }

        protected override void Build()
        {
        }

        protected override void Check()
        {
            if (!FnControls.ContainsOnlyNumericValue(GetWnd().txbEstTimeMain))
                AddViewError(GetWnd().txbEstTimeMain, "Invalid input: only numeric values valid!");

            if (!FnControls.ContainsOnlyNumericValue(GetWnd().txbEstTimeMainExtra))
                AddViewError(GetWnd().txbEstTimeMainExtra, "Invalid input: only numeric values valid!");

            if (!FnControls.ContainsOnlyNumericValue(GetWnd().txbEstTimeCompletionist))
                AddViewError(GetWnd().txbEstTimeCompletionist, "Invalid input: only numeric values valid!");

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().txbEstTimeMain.Text))
                AddViewError(GetWnd().txbEstTimeMain, "Invalid input: please enter a value.");

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().txbEstTimeMainExtra.Text))
                AddViewError(GetWnd().txbEstTimeMainExtra, "Invalid input: please enter a value.");

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().txbEstTimeCompletionist.Text))
                AddViewError(GetWnd().txbEstTimeCompletionist, "Invalid input: please enter a value.");
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void FillViewImpl()
        {
            GetWnd().txbEstTimeMain.Text = GetApp().ManualEstTimesCache.estTimeMain.ToString();
            GetWnd().txbEstTimeMainExtra.Text = GetApp().ManualEstTimesCache.estTimeMainExtra.ToString();
            GetWnd().txbEstTimeCompletionist.Text = GetApp().ManualEstTimesCache.estTimeCompletionist.ToString();
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

        private ProfilesManualEstTimesEditView GetWnd()
        {
            return (ProfilesManualEstTimesEditView)View;
        }

        private ProfilesEditApp GetApp()
        {
            return (ProfilesEditApp)App;
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

            GetViewReturn<ProfilesManualEstTimesEditViewReturn>().estTimeMain = FnConvert.ToDouble(GetWnd().txbEstTimeMain.Text);
            GetViewReturn<ProfilesManualEstTimesEditViewReturn>().estTimeMainExtra = FnConvert.ToDouble(GetWnd().txbEstTimeMainExtra.Text);
            GetViewReturn<ProfilesManualEstTimesEditViewReturn>().estTimeCompletionist = FnConvert.ToDouble(GetWnd().txbEstTimeCompletionist.Text);
            GetViewReturn<ProfilesManualEstTimesEditViewReturn>().Canceled = false;

            Exit(true);
        }

        protected void EV_btnCancel()
        {
            Exit(true);
        }
    }
}
