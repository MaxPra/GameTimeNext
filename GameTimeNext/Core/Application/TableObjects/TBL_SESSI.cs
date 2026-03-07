using GameTimeNext.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    internal class TBL_SESSI : UIXTableObjectBase
    {
        [UIXSignatureField(0)]
        public long SEID { get; set; }

        [UIXSignatureField(1)]
        public long PFID { get; set; }

        [UIXSignatureField(2)]
        public DateTime PLFR { get; set; }

        [UIXSignatureField(3)]
        public DateTime PLTO { get; set; }

        [UIXSignatureField(4)]
        public double PLTI { get; set; }

        [UIXSignatureField(5)]
        public DateTime CRAT { get; set; }

        [UIXSignatureField(6)]
        public DateTime CHAT { get; set; }

        public TBL_SESSI()
        {
            SEID = 0;
            PFID = 0;

            PLFR = DateTime.MinValue;
            PLTO = DateTime.MinValue;

            PLTI = 0.0;

            CRAT = DateTime.MinValue;
            CHAT = DateTime.MinValue;

            AcceptChanges();
        }

        public override void Save()
        {
            TBLM_SESSI tblmSessi = new TBLM_SESSI();
            tblmSessi.Save(this);
        }
    }
}