using GameTimeNext.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class T1PLTHR : UIXTableObjectBase
    {
        public override bool IsDevSynced => false;

        [UIXSignatureField(0)]
        public long PTID { get; set; } = 0;

        [UIXSignatureField(1)]
        public long PFID { get; set; } = 0;

        [UIXSignatureField(2)]
        public string PTTY { get; set; } = string.Empty;

        [UIXSignatureField(3)]
        public string PTDE { get; set; } = string.Empty;

        [UIXSignatureField(4)]
        public bool PTCO { get; set; } = false;

        [UIXSignatureField(5)]
        public DateTime CRAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(6)]
        public DateTime CHAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(7)]
        public bool PTCA { get; set; } = false;

        public override void Save()
        {
            new TXPLTHR().Save(this);
        }
    }
}
