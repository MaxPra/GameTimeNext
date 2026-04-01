using GameTimeNext.Core.Application.DataManagers;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class T1PROFI : UIXTableObjectBase
    {
        [UIXSignatureField(0)]
        public long PFID { get; set; } = 0;

        [UIXSignatureField(1)]
        public string GANA { get; set; }

        [UIXSignatureField(2)]
        public DateTime FIPL { get; set; }

        [UIXSignatureField(3)]
        public DateTime LAPL { get; set; }

        [UIXSignatureField(4)]
        public string PPFN { get; set; }

        [UIXSignatureField(5)]
        public string EXGF { get; set; }

        [UIXSignatureField(6)]
        public long SAID { get; set; }

        [UIXSignatureField(7)]
        public string PRSE { get; set; }

        [UIXSignatureField(8)]
        public string EXEC { get; set; }

        [UIXSignatureField(9)]
        public DateTime CRAT { get; set; }

        [UIXSignatureField(10)]
        public DateTime CHAT { get; set; }

        [UIXSignatureField(11)]
        public string ACCO { get; set; }

        [UIXSignatureField(12)]
        public string ACIN { get; set; }

        [UIXSignatureField(13)]
        public bool ACAC { get; set; }

        [UIXSignatureField(14)]
        public long CUPT { get; set; }

        [UIXSignatureField(15)]
        public double ETMA { get; set; }

        [UIXSignatureField(16)]
        public double ETME { get; set; }

        [UIXSignatureField(17)]
        public double ETCO { get; set; }

        [UIXSignatureField(18)]
        public string ETTY { get; set; }

        [UIXSignatureField(19)]
        public bool ETML { get; set; }

        [UIXSignatureField(20)]
        public bool ARCH { get; set; }

        public BitmapImage CoverImage { get; set; }

        public bool IsPlayable { get; set; } = false;

        public T1PROFI()
        {
            PFID = 0;

            GANA = string.Empty;
            FIPL = DateTime.MinValue;
            LAPL = DateTime.MinValue;

            PPFN = string.Empty;
            EXGF = string.Empty;

            SAID = 0;

            PRSE = string.Empty;
            EXEC = string.Empty;

            CRAT = DateTime.MinValue;
            CHAT = DateTime.MinValue;

            ACCO = string.Empty;
            ACIN = string.Empty;

            ACAC = false;

            CUPT = 0;

            ETMA = 0;
            ETME = 0;
            ETCO = 0;

            ETTY = string.Empty;

            ETML = false;

            ARCH = false;

            AcceptChanges();
        }

        public override void Save()
        {
            TXPROFI tblmProfi = new TXPROFI();
            tblmProfi.Save(this);
        }
    }
}