using GameTimeNext.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class T1CTABD : UIXTableObjectBase
    {
        public override bool IsDevSynced => true;

        [UIXSignatureField(0)]
        public string TXTYP { get; set; } = "";

        [UIXSignatureField(1)]
        public string TXNUM { get; set; } = "";

        [UIXSignatureField(2)]
        public string DESCR { get; set; } = "";

        [UIXSignatureField(3)]
        public string PARM1 { get; set; } = "";

        [UIXSignatureField(4)]
        public string PARM2 { get; set; } = "";

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
            new TXCTABD().Save(this);
        }
    }
}
