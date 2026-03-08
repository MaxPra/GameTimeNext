using GameTimeNext.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    internal class T1GRPPO : UIXTableObjectBase
    {
        [UIXSignatureField(0)]
        public long GPID { get; set; }

        [UIXSignatureField(1)]
        public long GRID { get; set; }

        [UIXSignatureField(2)]
        public long PFID { get; set; }

        [UIXSignatureField(3)]
        public DateTime CRAT { get; set; }

        [UIXSignatureField(4)]
        public DateTime CHAT { get; set; }

        public T1GRPPO()
        {
            GPID = 0;

            GRID = 0;
            PFID = 0;

            CRAT = DateTime.MinValue;
            CHAT = DateTime.MinValue;

            AcceptChanges();
        }

        public override void Save()
        {
            TXGRPPO tblmGrppo = new TXGRPPO();
            tblmGrppo.Save(this);
        }
    }
}