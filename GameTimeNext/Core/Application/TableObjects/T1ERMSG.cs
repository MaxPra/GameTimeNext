using GameTimeNext.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class T1ERMSG : UIXTableObjectBase
    {
        public override bool IsDevSynced => true;

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
            new TXERMSG().Save(this);
        }
    }
}
