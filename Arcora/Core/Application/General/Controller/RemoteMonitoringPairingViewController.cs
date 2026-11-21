using Arcora.Core.Application.General.Views;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace Arcora.Core.Application.General.Controller
{
    /// <summary>
    /// QR code popup of the title bar: filled on every opening, so address and port are always the current ones.
    /// </summary>
    public class RemoteMonitoringPairingViewController : UIXViewControllerBase<MainApp, RemoteMonitoringPairingView>
    {
        public RemoteMonitoringPairingViewController(UIXApplication app) : base(app)
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

        protected override void BuildImpl()
        {
        }

        protected override void CheckImpl()
        {
        }

        protected override void FillViewImpl()
        {
            CFRemoteMonitoring.RemoteMonitoringPairing pairing = CFRemoteMonitoring.GetPairing();

            GetView().imgQrCode.Source = pairing.QrCode;
            GetView().txtAddress.Text = pairing.Text;
            GetView().txtPairingCode.Text = pairing.PairingCode;
            FnControls.SetVisible(GetView().brdQrCode, pairing.QrCode is not null);
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
    }
}
