using GameTimeNext.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class T1APRDH : UIXTableObjectBase
    {
        public override bool IsDevSynced => true;

        [UIXSignatureField(0)]
        public string APDID { get; set; } = "";

        [UIXSignatureField(1)]
        public string APDES { get; set; } = "";

        [UIXSignatureField(2)]
        public string APHPT { get; set; } = "";

        [UIXSignatureField(3)]
        public bool APMCP { get; set; } = false;

        [UIXSignatureField(4)]
        public DateTime CRAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(5)]
        public string CRUS { get; set; } = "";

        [UIXSignatureField(6)]
        public DateTime CHAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(7)]
        public string CHUS { get; set; } = "";

        public override void Save()
        {
            new TXAPRDH().Save(this);
        }
    }
}
