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
        public DateTime CRAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(7)]
        public DateTime CHAT { get; set; } = DateTime.MinValue;

        public override void Save()
        {
            new TXSCRSP().Save(this);
        }
    }
}
