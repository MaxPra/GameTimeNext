using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Metadata.Data
{
    public class T1METAP : UIXTableObjectBase
    {
        public override bool IsDevSynced => true;

        private DateTime _crat;
        private DateTime _chat;

        [UIXSignatureField(0)]
        public string MENAM { get; set; }

        [UIXSignatureField(1)]
        public string PONAM { get; set; }

        [UIXSignatureField(2)]
        public string DESCR { get; set; }

        [UIXSignatureField(3)]
        public string DATYP { get; set; }

        [UIXSignatureField(4)]
        public int DALEN { get; set; }

        [UIXSignatureField(5)]
        public int PORDE { get; set; }

        [UIXSignatureField(6)]
        public bool PRIMK { get; set; }

        [UIXSignatureField(7)]
        public bool AUTOI { get; set; }

        [UIXSignatureField(8)]
        public DateTime CRAT
        {
            get => _crat;
            set
            {
                if (_crat == value)
                    return;

                _crat = value;
                CRUS = FnOpSys.GetCurrentUserName();
            }
        }

        [UIXSignatureField(9)]
        public string CRUS { get; set; }

        [UIXSignatureField(10)]
        public DateTime CHAT
        {
            get => _chat;
            set
            {
                if (_chat == value)
                    return;

                _chat = value;
                CHUS = FnOpSys.GetCurrentUserName();
            }
        }

        [UIXSignatureField(11)]
        public string CHUS { get; set; }

        public T1METAP()
        {
            MENAM = string.Empty;
            PONAM = string.Empty;
            DESCR = string.Empty;
            DATYP = string.Empty;
            DALEN = 0;
            PORDE = 0;
            PRIMK = false;
            AUTOI = false;
            CRAT = DateTime.MinValue;
            CRUS = string.Empty;
            CHAT = DateTime.MinValue;
            CHUS = string.Empty;

            AcceptChanges();
        }

        public override void Save()
        {
            TXMETAP txmetap = new TXMETAP();
            txmetap.Save(this);
        }
    }
}
