using GameTimeNext.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class T1CTABH : UIXTableObjectBase
    {
        public override bool IsDevSynced => true;

        [UIXSignatureField(0)]
        public string TXTYP { get; set; } = string.Empty;

        [UIXSignatureField(1)]
        public string DESCR { get; set; } = string.Empty;

        [UIXSignatureField(2)]
        public string PERMI { get; set; } = string.Empty;

        [UIXSignatureField(3)]
        public bool PAAC1 { get; set; } = false;

        [UIXSignatureField(4)]
        public string PADE1 { get; set; } = string.Empty;

        [UIXSignatureField(5)]
        public bool PARF1 { get; set; } = false;

        [UIXSignatureField(6)]
        public string PACO1 { get; set; } = string.Empty;

        [UIXSignatureField(7)]
        public string PACT1 { get; set; } = string.Empty;

        [UIXSignatureField(8)]
        public bool PAAC2 { get; set; } = false;

        [UIXSignatureField(9)]
        public string PADE2 { get; set; } = string.Empty;

        [UIXSignatureField(10)]
        public bool PARF2 { get; set; } = false;

        [UIXSignatureField(11)]
        public string PACO2 { get; set; } = string.Empty;

        [UIXSignatureField(12)]
        public string PACT2 { get; set; } = string.Empty;

        [UIXSignatureField(13)]
        public DateTime CRAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(14)]
        public DateTime CHAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(15)]
        public bool NRANA { get; set; } = false;

        [UIXSignatureField(16)]
        public bool EXPRT { get; set; } = false;

        public override void Save()
        {
            new TXCTABH().Save(this);
        }
    }
}
