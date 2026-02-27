
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using GameTimeNext.Core.Application.Profiles.Views;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    internal class ProfilesDetailSubViewController : UIXViewControllerBase
    {

        ProfilesSubViewDataWrapper? _dataWrapper;

        public ProfilesDetailSubViewController(UIXApplication app) : base(app)
        {
        }

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

        private ProfilesDetailView GetView()
        {
            return (ProfilesDetailView)this.View;
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {

        }
    }
}
