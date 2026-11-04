using GameTimeNext.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class T1SCRSP : UIXTableObjectBase
    {
        public override bool IsDevSynced => false;

        [UIXSignatureField(0)]
        public long SPID { get; set; } = 0;

        [UIXSignatureField(1)]
        public long SSID { get; set; } = 0;

        [UIXSignatureField(2)]
        public string MOID { get; set; } = "";

        [UIXSignatureField(3)]
        public string MONA { get; set; } = "";

        [UIXSignatureField(4)]
        public bool MOEN { get; set; } = false;

        [UIXSignatureField(5)]
        public bool MOPR { get; set; } = false;

        [UIXSignatureField(6)]
        public long MOWI { get; set; } = 0;

        [UIXSignatureField(7)]
        public long MOHE { get; set; } = 0;

        [UIXSignatureField(8)]
        public long MORF { get; set; } = 0;

        [UIXSignatureField(9)]
        public string MOOR { get; set; } = "";

        [UIXSignatureField(10)]
        public string MOPL { get; set; } = "";

        [UIXSignatureField(11)]
        public long MOSC { get; set; } = 0;

        [UIXSignatureField(12)]
        public DateTime CRAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(13)]
        public string CRUS { get; set; } = "";

        [UIXSignatureField(14)]
        public DateTime CHAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(15)]
        public string CHUS { get; set; } = "";

        public override void Save()
        {
            new TXSCRSP().Save(this);
        }
    }
}
