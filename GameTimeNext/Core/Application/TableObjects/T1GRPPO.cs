using GameTimeNext.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class T1GRPPO : UIXTableObjectBase
    {
        public override bool IsDevSynced => false;

        [UIXSignatureField(0)]
        public long GPID { get; set; } = 0;

        [UIXSignatureField(1)]
        public long GRID { get; set; } = 0;

        [UIXSignatureField(2)]
        public long PFID { get; set; } = 0;

        [UIXSignatureField(3)]
        public DateTime CRAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(4)]
        public DateTime CHAT { get; set; } = DateTime.MinValue;

        public override void Save()
        {
            new TXGRPPO().Save(this);
        }
    }
}
