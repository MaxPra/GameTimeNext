using GameTimeNext.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class T1CTABD : UIXTableObjectBase
    {
        public override bool IsDevSynced => true;

        [UIXSignatureField(0)]
        public string TXTYP { get; set; } = string.Empty;

        [UIXSignatureField(1)]
        public string TXNUM { get; set; } = string.Empty;

        [UIXSignatureField(2)]
        public string DESCR { get; set; } = string.Empty;

        [UIXSignatureField(3)]
        public DateTime CRAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(4)]
        public DateTime CHAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(5)]
        public string PARM1 { get; set; } = string.Empty;

        [UIXSignatureField(6)]
        public string PARM2 { get; set; } = string.Empty;

        public override void Save()
        {
            new TXCTABD().Save(this);
        }
    }
}
