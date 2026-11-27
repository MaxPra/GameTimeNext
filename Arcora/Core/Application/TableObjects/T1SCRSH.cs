using Arcora.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace Arcora.Core.Application.TableObjects
{
    public class T1SCRSH : UIXTableObjectBase
    {
        [UIXKeyField]
        [UIXSignatureField(0)]
        public long SSID { get; set; } = 0;

        [UIXSignatureField(1)]
        public string SSNA { get; set; } = "";

        [UIXSignatureField(2)]
        public string SSDE { get; set; } = "";

        [UIXSignatureField(3)]
        public string ADID { get; set; } = "";

        [UIXSignatureField(4)]
        public string ADNA { get; set; } = "";

        [UIXSignatureField(5)]
        public DateTime CRAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(6)]
        public string CRUS { get; set; } = "";

        [UIXSignatureField(7)]
        public DateTime CHAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(8)]
        public string CHUS { get; set; } = "";

        public override void Save()
        {
            UIXDataManagerBase.Create<TXSCRSH>(App).Save(this);
        }
    }
}
