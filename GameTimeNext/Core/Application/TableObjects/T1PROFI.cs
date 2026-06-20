using GameTimeNext.Core.Application.DataManagers;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class T1PROFI : UIXTableObjectBase
    {
        public override bool IsDevSynced => false;

        [UIXSignatureField(0)]
        public long PFID { get; set; } = 0;

        [UIXSignatureField(1)]
        public string GANA { get; set; } = string.Empty;

        [UIXSignatureField(2)]
        public DateTime FIPL { get; set; } = DateTime.MinValue;

        [UIXSignatureField(3)]
        public DateTime LAPL { get; set; } = DateTime.MinValue;

        [UIXSignatureField(4)]
        public string PPFN { get; set; } = string.Empty;

        [UIXSignatureField(5)]
        public string EXGF { get; set; } = string.Empty;

        [UIXSignatureField(6)]
        public long SAID { get; set; } = 0;

        [UIXSignatureField(7)]
        public string PRSE { get; set; } = string.Empty;

        [UIXSignatureField(8)]
        public string EXEC { get; set; } = string.Empty;

        [UIXSignatureField(9)]
        public DateTime CRAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(10)]
        public DateTime CHAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(11)]
        public string ACCO { get; set; } = string.Empty;

        [UIXSignatureField(12)]
        public string ACIN { get; set; } = string.Empty;

        [UIXSignatureField(13)]
        public bool ACAC { get; set; } = false;

        [UIXSignatureField(14)]
        public long CUPT { get; set; } = 0;

        [UIXSignatureField(15)]
        public double ETMA { get; set; } = 0d;

        [UIXSignatureField(16)]
        public double ETME { get; set; } = 0d;

        [UIXSignatureField(17)]
        public double ETCO { get; set; } = 0d;

        [UIXSignatureField(18)]
        public string ETTY { get; set; } = string.Empty;

        [UIXSignatureField(19)]
        public bool ETML { get; set; } = false;

        [UIXSignatureField(20)]
        public bool ARCH { get; set; } = false;
        public BitmapImage CoverImage { get; set; }

        public bool IsPlayable { get; set; } = false;
        public override void Save()
        {
            new TXPROFI().Save(this);
        }
    }
}
