using GameTimeNext.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class T1PLTHR : UIXTableObjectBase
    {
        [UIXSignatureField(0)]
        public long PTID { get; set; }

        [UIXSignatureField(1)]
        public long PFID { get; set; }

        [UIXSignatureField(2)]
        public string PTTY { get; set; }

        [UIXSignatureField(3)]
        public string PTDE { get; set; }

        [UIXSignatureField(4)]
        public bool PTCO { get; set; }

        [UIXSignatureField(5)]
        public bool PTCA { get; set; }

        [UIXSignatureField(6)]
        public DateTime CRAT { get; set; }

        [UIXSignatureField(7)]
        public DateTime CHAT { get; set; }

        public T1PLTHR()
        {
            PTID = 0;
            PFID = 0;

            PTTY = string.Empty;
            PTDE = string.Empty;

            PTCO = false;
            PTCA = false;

            CRAT = DateTime.MinValue;
            CHAT = DateTime.MinValue;

            AcceptChanges();
        }

        public override void Save()
        {
            TXPLTHR tblmPlthr = new TXPLTHR();
            tblmPlthr.Save(this);
        }
    }
}