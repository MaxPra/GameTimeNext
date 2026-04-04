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
        public bool EnableSessionTimeQuery { get; set; }

        [UIXSignatureField(8)]
        public string MonitoringKey { get; set; }

        [UIXSignatureField(9)]
        public string SteamGridDbKey { get; set; }

        [UIXSignatureField(10)]
        public string TwitchIGDBClientID { get; set; }

        [UIXSignatureField(11)]
        public string TwitchIGDBClientSecret { get; set; }

        [UIXSignatureField(12)]
        public string BackupType { get; set; }

        [UIXSignatureField(13)]
        public string BackupExportPath { get; set; }

        [UIXSignatureField(14)]
        public string BackupImportPath { get; set; }

        [UIXSignatureField(15)]
        public bool AutoBackup { get; set; }

        [UIXSignatureField(16)]
        public bool AutoDelete { get; set; }

        [UIXSignatureField(17)]
        public bool BreakReminder { get; set; }

        [UIXSignatureField(18)]
        public double BreakReminderHrs { get; set; }



        public AppSettings()
        {
            AutomaticGameProfileSwitching = false;
            ActivateBlackoutKeyCombination = false;
            AllowProfileSpecificStyleChanges = false;

            MonitoringKeyActive = false;
            ShowToastNotification = false;
            ShowMonitoringIndicator = false;
            BlackoutSideMonitors = false;
            EnableSessionTimeQuery = false;
            MonitoringKey = KeyInput.virtualKeyMap[KeyInput.VirtualKey.VK_NONE];
            SteamGridDbKey = string.Empty;
            TwitchIGDBClientID = string.Empty;
            TwitchIGDBClientSecret = string.Empty;

            BackupType = string.Empty;
            BackupExportPath = string.Empty;
            BackupImportPath = string.Empty;
            AutoBackup = false;
            AutoDelete = false;

            BreakReminder = false;
            BreakReminderHrs = 0.0;

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