using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Metadata.Data
{
    public class T1METAH : UIXTableObjectBase
    {
        public override bool IsDevSynced => true;

        private DateTime _crat;
        private DateTime _chat;

        [UIXSignatureField(0)]
        public string MENAM { get; set; }

        [UIXSignatureField(1)]
        public string DESCR { get; set; }

        [UIXSignatureField(2)]
        public string MTYPE { get; set; }

        [UIXSignatureField(3)]
        public bool DSYNC { get; set; }

        [UIXSignatureField(4)]
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

        [UIXSignatureField(5)]
        public string CRUS { get; set; }

        [UIXSignatureField(6)]
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

        [UIXSignatureField(7)]
        public string CHUS { get; set; }

        public T1METAH()
        {
            MENAM = string.Empty;
            DESCR = string.Empty;
            MTYPE = string.Empty;
            DSYNC = false;
            CRAT = DateTime.MinValue;
            CRUS = string.Empty;
            CHAT = DateTime.MinValue;
            CHUS = string.Empty;

            AcceptChanges();
        }

        public override void Save()
        {
            TXMETAH txmetah = new TXMETAH();
            txmetah.Save(this);
        }
    }
}
