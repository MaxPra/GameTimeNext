using Arcora.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace Arcora.Core.Application.TableObjects
{
    public class T1ERMSG : UIXTableObjectBase
    {
        [UIXKeyField]
        [UIXSignatureField(0)]
        public string ERMNR { get; set; } = "";

        [UIXSignatureField(1)]
        public string ERMSG { get; set; } = "";

        [UIXSignatureField(2)]
        public string ERHIN { get; set; } = "";

        [UIXSignatureField(3)]
        public DateTime CRAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(4)]
        public string CRUS { get; set; } = "";

        [UIXSignatureField(5)]
        public DateTime CHAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(6)]
        public string CHUS { get; set; } = "";

        public override void Save()
        {
            UIXDataManagerBase.Create<TXERMSG>(App).Save(this);
        }
    }
}
