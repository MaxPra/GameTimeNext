using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UserInput;
using System.Text.Json;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class AppSettings : UIXTableObjectBase
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        [UIXSignatureField(0)]
        public bool AutomaticGameProfileSwitching { get; set; }

        [UIXSignatureField(1)]
        public bool ActivateBlackoutKeyCombination { get; set; }

        [UIXSignatureField(2)]
        public bool AllowProfileSpecificStyleChanges { get; set; }

        [UIXSignatureField(3)]
        public bool MonitoringKeyActive { get; set; }

        [UIXSignatureField(4)]
        public bool ShowToastNotification { get; set; }

        [UIXSignatureField(5)]
        public bool ShowMonitoringIndicator { get; set; }

        [UIXSignatureField(6)]
        public bool BlackoutSideMonitors { get; set; }

        [UIXSignatureField(7)]
        public string MonitoringKey { get; set; }

        public AppSettings()
        {
            AutomaticGameProfileSwitching = false;
            ActivateBlackoutKeyCombination = false;
            AllowProfileSpecificStyleChanges = false;

            MonitoringKeyActive = false;
            ShowToastNotification = false;
            ShowMonitoringIndicator = false;
            BlackoutSideMonitors = false;
            MonitoringKey = KeyInput.virtualKeyMap[KeyInput.VirtualKey.VK_NONE];

            AcceptChanges();
        }

        public override void Save()
        {
            AppEnvironment.GetAppConfig().AppSettings = this;
            AppEnvironment.SaveAppConfig();

            AcceptChanges();
        }
    }
}