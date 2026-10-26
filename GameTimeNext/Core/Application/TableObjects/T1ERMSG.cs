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
        public DateTime CRAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(3)]
        public string CRUS { get; set; } = "";

        [UIXSignatureField(4)]
        public DateTime CHAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(5)]
        public string CHUS { get; set; } = "";

        [UIXSignatureField(6)]
        public string ERHIN { get; set; } = "";

        public override void Save()
        {
            new TXERMSG().Save(this);
        }
    }
}
