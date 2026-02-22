
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using GameTimeNext.Core.Application.Profiles.Views;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    internal class ProfileDetailSubViewController : UIXViewControllerBase
    {

        ProfilesSubViewDataWrapper? _dataWrapper;

        protected override void Init()
        {
            _dataWrapper = GetDataWrapper<ProfilesSubViewDataWrapper>();

            if (_dataWrapper == null)
                return;

            AddIdentifier("TBL_PROFI", _dataWrapper.GetTypedTableObject());
        }

        protected override void Build()
        {

        }

        protected override void BuildFirst()
        {
            //GetView().TxtMegaBox.Text = _dataWrapper.GetTypedTableObject().GANA;
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void Check()
        {
        }

        protected override void FillViewImpl()
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }

        private ProfileDetailSubView GetView()
        {
            return (ProfileDetailSubView)this.View;
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {

        }
    }
}
