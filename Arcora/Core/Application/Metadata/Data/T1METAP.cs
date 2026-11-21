using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.Metadata.Data
{
    public class T1METAP : UIXTableObjectBase
    {
        public override bool IsDevSynced => true;

        [UIXSignatureField(0)]
        public string MENAM { get; set; } = "";

        [UIXSignatureField(1)]
        public string PONAM { get; set; } = "";

        [UIXSignatureField(2)]
        public string DESCR { get; set; } = "";

        [UIXSignatureField(3)]
        public string DATYP { get; set; } = "";

        [UIXSignatureField(4)]
        public int DALEN { get; set; } = 0;

        [UIXSignatureField(5)]
        public int PORDE { get; set; } = 0;

        [UIXSignatureField(6)]
        public bool PRIMK { get; set; } = false;

        [UIXSignatureField(7)]
        public bool AUTOI { get; set; } = false;

        [UIXSignatureField(8)]
        public bool DEFAK { get; set; } = false;

        [UIXSignatureField(9)]
        public string DEFVL { get; set; } = "";

        [UIXSignatureField(10)]
        public DateTime CRAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(11)]
        public string CRUS { get; set; } = "";

        [UIXSignatureField(12)]
        public DateTime CHAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(13)]
        public string CHUS { get; set; } = "";

        public override void Save()
        {
            new TXMETAP().Save(this);
        }
    }
}
