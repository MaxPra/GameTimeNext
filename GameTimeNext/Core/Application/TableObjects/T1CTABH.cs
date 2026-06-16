using GameTimeNext.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class T1CTABH : UIXTableObjectBase
    {
        [UIXSignatureField(0)]
        public string TXTYP { get; set; }

        [UIXSignatureField(1)]
        public string DESCR { get; set; }

        [UIXSignatureField(2)]
        public string PERMI { get; set; }

        [UIXSignatureField(3)]
        public DateTime CRAT { get; set; }

        [UIXSignatureField(4)]
        public DateTime CHAT { get; set; }

        public T1CTABH()
        {
            TXTYP = string.Empty;
            DESCR = string.Empty;
            PERMI = string.Empty;

            CRAT = DateTime.MinValue;
            CHAT = DateTime.MinValue;

            AcceptChanges();
        }

        public override void Save()
        {
            TXCTABH tblmCtabh = new TXCTABH();
            tblmCtabh.Save(this);
        }
    }
}