using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    public class ProfilesExecutablesEditViewController : UIXWindowControllerBase
    {

        ProfilesExecutablesEditViewModel? _profilesExecutablesEditViewModel = null;

        public ProfilesExecutablesEditViewController(UIXApplication app) : base(app)
        {
        }

        public class ProfilesExecutablesEditViewReturn() : UIXViewReturn
        {
            public List<Executable> SelectedExecutables { get; set; }
        }

        protected override void Init()
        {
            ViewReturn = new ProfilesExecutablesEditViewReturn();
        }
        protected override void BuildFirst()
        {
            BuildExecutablesList();
        }

        protected override void Build()
        {
        }

        protected override void Check()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected override void Event_Closing()
        {
        }

        protected override void Event_Maximize()
        {
        }

        protected override void Event_Minimize()
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

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        private ProfilesExecutablesEditApp GetApp()
        {
            return (ProfilesExecutablesEditApp)App;
        }

        private ProfilesExecutablesEditView GetWnd()
        {
            return (ProfilesExecutablesEditView)View!;
        }

        private void BuildExecutablesList()
        {
            List<Executable> executables = FnSystem.FindExecutables(GetApp().GameFolder);

            // Nicht Spiel-Exe aussortieren
            executables = FnSystem.SortOutExecutables(executables);

            _profilesExecutablesEditViewModel = new ProfilesExecutablesEditViewModel();
            _profilesExecutablesEditViewModel.Executables = new System.Collections.ObjectModel.ObservableCollection<Executable>(executables);

            GetWnd().DataContext = _profilesExecutablesEditViewModel;
        }

        private void ShowExitWarning()
        {
            CFMBOX cfmbox = new CFMBOX();

            CFMBOXResult result = cfmbox.Show("Question", "Cancel selection of valid executables?\nSome GameTimeNext features may not be available for this profile afterward.", CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

            if (result == CFMBOXResult.Yes)
            {
                Exit(true);
            }
        }

        protected void EV_btnCancel()
        {
            ShowExitWarning();
        }

        protected void EV_btnSelect()
        {
            GetViewReturn<ProfilesExecutablesEditViewReturn>().SelectedExecutables = _profilesExecutablesEditViewModel!.Executables.ToList();
            GetViewReturn<ProfilesExecutablesEditViewReturn>().Canceled = false;

            Exit(true);
        }
    }
}
