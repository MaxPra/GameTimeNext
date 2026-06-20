using GameTimeNext.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class T1GROUP : UIXTableObjectBase
    {
        public override bool IsDevSynced => false;

        [UIXSignatureField(0)]
        public int GRID { get; set; }

        [UIXSignatureField(1)]
        public string GRNA { get; set; } = string.Empty;

        [UIXSignatureField(2)]
        public string GTYP { get; set; } = string.Empty;

        [UIXSignatureField(3)]
        public string CRAT { get; set; } = string.Empty;

        [UIXSignatureField(4)]
        public string CHAT { get; set; } = string.Empty;

        public bool? IsSelected { get; set; } = false;


        public override void Save()
        {
            new TXGROUP().Save(this);
        }
    }
}
