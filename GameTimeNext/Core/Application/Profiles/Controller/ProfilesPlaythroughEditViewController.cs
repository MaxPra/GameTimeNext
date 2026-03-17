using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.TableObjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    public class ProfilesPlaythroughEditViewController : UIXWindowControllerBase
    {
        public ProfilesPlaythroughEditViewController(UIXApplication app) : base(app)
        {
        }

        public class ProfilesPlaythroughEditViewReturn : UIXViewReturn
        {
        }

        protected override void Init()
        {
            ViewReturn = new ProfilesPlaythroughEditViewReturn();
        }

        protected override void BuildFirst()
        {
            FillCmbTypes();
        }

        protected override void Build()
        {

        }

        protected override void Check()
        {
            if (FnString.IsNullEmptyOrWhitespace(GetWnd().txbDescription.Text))
                AddViewError(GetWnd().txbDescription, "Invalid input: description has to be specified.");
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected override void Event_Closing()
        {
            Exit(false);
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
            ComboBoxItem combItem = GetWnd().cmbType.SelectedItem as ComboBoxItem;

            TFPLTHR.CreateNewPlaythrough(GetApp().T1profi.PFID, description: GetWnd().txbDescription.Text, type: combItem!.Tag.ToString()!);
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
            if (source is ComboBox cmb && eventName == UIXEventNames.Selector.SelectionChanged)
            {
                if (cmb.Name == GetWnd().cmbType.Name)
                    FillTextBoxDescription();
            }
        }

        private void FillTextBoxDescription()
        {
            ComboBoxItem combItem = GetWnd().cmbType.SelectedItem as ComboBoxItem;

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().txbDescription.Text) && combItem.Tag == PlaythroughType.NEW_PLAYTHROUGH)
            {
                GetWnd().txbDescription.Text = "Playthrough #" + (TFPLTHR.GetCurrentPlaythroughCount(GetApp().T1profi.PFID, PlaythroughType.NEW_PLAYTHROUGH) + 1);
            }
            else
            {
                GetWnd().txbDescription.Text = string.Empty;
            }
        }

        private void FillCmbTypes()
        {
            GetWnd().cmbType.Items.Add(new ComboBoxItem { Content = "New Playthrough", Tag = PlaythroughType.NEW_PLAYTHROUGH });
            GetWnd().cmbType.Items.Add(new ComboBoxItem { Content = "DLC", Tag = PlaythroughType.DLC });
        }

        private ProfilesPlaythroughEditView GetWnd()
        {
            return (ProfilesPlaythroughEditView)View;
        }

        private ProfilesPlaythroughEditApp GetApp()
        {
            return (ProfilesPlaythroughEditApp)App;
        }

        protected void EV_btnSave()
        {
            GetViewReturn<ProfilesPlaythroughEditViewReturn>().Canceled = false;
            Exit(true);
        }

        protected void EV_btnCancel()
        {
            Exit(true);
        }
    }
}
