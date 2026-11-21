using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.Metadata.Data
{
    public class T1METAH : UIXTableObjectBase
    {
        public override bool IsDevSynced => true;

        [UIXSignatureField(0)]
        public string MENAM { get; set; } = "";

        [UIXSignatureField(1)]
        public string DESCR { get; set; } = "";

        [UIXSignatureField(2)]
        public string MTYPE { get; set; } = "";

        [UIXSignatureField(3)]
        public bool DSYNC { get; set; } = false;

        [UIXSignatureField(4)]
        public bool GENER { get; set; } = false;

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
            new TXMETAH().Save(this);
        }
    }
}
