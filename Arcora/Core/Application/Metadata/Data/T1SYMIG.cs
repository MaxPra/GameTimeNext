using UIX.ViewController.Engine.DataBaseObjects;

namespace Arcora.Core.Application.Metadata.Data
{
    public class T1SYMIG : UIXTableObjectBase
    {
        [UIXKeyField]
        [UIXSignatureField(0)]
        public string MGTY { get; set; } = "";

        [UIXKeyField]
        [UIXSignatureField(1)]
        public string MGID { get; set; } = "";

        [UIXSignatureField(2)]
        public string MGAV { get; set; } = "";

        [UIXSignatureField(3)]
        public DateTime CRAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(4)]
        public string CRUS { get; set; } = "";

        [UIXSignatureField(5)]
        public DateTime CHAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(6)]
        public string CHUS { get; set; } = "";

        public override void Save()
        {
            UIXDataManagerBase.Create<TXSYMIG>(App).Save(this);
        }
    }
}
