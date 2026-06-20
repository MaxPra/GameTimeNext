using GameTimeNext.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class T1GROUP : UIXTableObjectBase
    {
        public override bool IsDevSynced => false;

        [UIXSignatureField(0)]
        public long GRID { get; set; } = 0;

        [UIXSignatureField(1)]
        public string GRNA { get; set; } = string.Empty;

        [UIXSignatureField(2)]
        public string GTYP { get; set; } = string.Empty;

        [UIXSignatureField(3)]
        public DateTime CRAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(4)]
        public DateTime CHAT { get; set; } = DateTime.MinValue;
        public bool? IsSelected { get; set; } = false;
        public override void Save()
        {
            new TXGROUP().Save(this);
        }
    }
}
