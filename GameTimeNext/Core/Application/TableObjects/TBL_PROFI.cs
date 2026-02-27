using GameTimeNext.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class TBL_PROFI : UIXTableObjectBase
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
        public DateTime PLSP { get; set; }

        [UIXSignatureField(10)]
        public DateTime CRAT { get; set; }

        [UIXSignatureField(11)]
        public DateTime CHAT { get; set; }

        public TBL_PROFI()
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

            PLSP = DateTime.MinValue;

            CRAT = DateTime.MinValue;
            CHAT = DateTime.MinValue;

            AcceptChanges();
        }

        public override void Save()
        {
            TBLM_PROFI tblmProfi = new TBLM_PROFI();
            tblmProfi.Save(this);
        }
    }
}
