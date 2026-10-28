using GameTimeNext.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class T1MENUE : UIXTableObjectBase
    {
        public override bool IsDevSynced => true;

        [UIXSignatureField(0)]
        public long MEID { get; set; } = 0;

        [UIXSignatureField(1)]
        public long MEPA { get; set; } = 0;

        [UIXSignatureField(2)]
        public string METY { get; set; } = "";

        [UIXSignatureField(3)]
        public string MEDE { get; set; } = "";

        [UIXSignatureField(4)]
        public string MEAP { get; set; } = "";

        [UIXSignatureField(5)]
        public bool DEVT { get; set; } = false;

        [UIXSignatureField(6)]
        public long MEOR { get; set; } = 0;

        [UIXSignatureField(7)]
        public DateTime CRAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(8)]
        public string CRUS { get; set; } = "";

        [UIXSignatureField(9)]
        public DateTime CHAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(10)]
        public string CHUS { get; set; } = "";

        public override void Save()
        {
            new TXMENUE().Save(this);
        }
    }
}
