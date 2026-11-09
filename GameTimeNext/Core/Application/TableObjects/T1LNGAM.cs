using GameTimeNext.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class T1LNGAM : UIXTableObjectBase
    {
        public override bool IsDevSynced => false;

        [UIXSignatureField(0)]
        public long LGID { get; set; } = 0;

        [UIXSignatureField(1)]
        public string LNTY { get; set; } = "";

        [UIXSignatureField(2)]
        public string LNID { get; set; } = "";

        [UIXSignatureField(3)]
        public string GANA { get; set; } = "";

        [UIXSignatureField(4)]
        public string LGST { get; set; } = "";

        [UIXSignatureField(5)]
        public long PFID { get; set; } = 0;

        [UIXSignatureField(6)]
        public DateTime CRAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(7)]
        public string CRUS { get; set; } = "";

        [UIXSignatureField(8)]
        public DateTime CHAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(9)]
        public string CHUS { get; set; } = "";

        public override void Save()
        {
            new TXLNGAM().Save(this);
        }
    }
}
