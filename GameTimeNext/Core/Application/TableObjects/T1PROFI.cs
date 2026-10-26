using GameTimeNext.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class T1PROFI : UIXTableObjectBase
    {
        public override bool IsDevSynced => false;

        [UIXSignatureField(0)]
        public long PFID { get; set; } = 0;

        [UIXSignatureField(1)]
        public string GANA { get; set; } = "";

        [UIXSignatureField(2)]
        public DateTime FIPL { get; set; } = DateTime.MinValue;

        [UIXSignatureField(3)]
        public DateTime LAPL { get; set; } = DateTime.MinValue;

        [UIXSignatureField(4)]
        public string PPFN { get; set; } = "";

        [UIXSignatureField(5)]
        public string EXGF { get; set; } = "";

        [UIXSignatureField(6)]
        public long SAID { get; set; } = 0;

        [UIXSignatureField(7)]
        public string PRSE { get; set; } = "";

        [UIXSignatureField(8)]
        public string EXEC { get; set; } = "";

        [UIXSignatureField(9)]
        public string ACCO { get; set; } = "";

        [UIXSignatureField(10)]
        public string ACIN { get; set; } = "";

        [UIXSignatureField(11)]
        public bool ACAC { get; set; } = false;

        [UIXSignatureField(12)]
        public long CUPT { get; set; } = 0;

        [UIXSignatureField(13)]
        public double ETMA { get; set; } = 0;

        [UIXSignatureField(14)]
        public double ETME { get; set; } = 0;

        [UIXSignatureField(15)]
        public double ETCO { get; set; } = 0;

        [UIXSignatureField(16)]
        public string ETTY { get; set; } = "";

        [UIXSignatureField(17)]
        public bool ETML { get; set; } = false;

        [UIXSignatureField(18)]
        public bool ARCH { get; set; } = false;

        [UIXSignatureField(19)]
        public string PLAFO { get; set; } = "";

        [UIXSignatureField(20)]
        public DateTime CRAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(21)]
        public string CRUS { get; set; } = "";

        [UIXSignatureField(22)]
        public DateTime CHAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(23)]
        public string CHUS { get; set; } = "";

        public override void Save()
        {
            new TXPROFI().Save(this);
        }
    }
}
