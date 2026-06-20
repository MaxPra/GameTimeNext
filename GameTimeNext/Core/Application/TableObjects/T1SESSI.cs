using GameTimeNext.Core.Application.DataManagers;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class T1SESSI : UIXTableObjectBase
    {
        public override bool IsDevSynced => false;

        [UIXSignatureField(0)]
        public long SEID { get; set; } = 0;

        [UIXSignatureField(1)]
        public long PFID { get; set; } = 0;

        [UIXSignatureField(2)]
        public long PTID { get; set; } = 0;

        [UIXSignatureField(3)]
        public DateTime PLFR { get; set; } = DateTime.MinValue;

        [UIXSignatureField(4)]
        public DateTime PLTO { get; set; } = DateTime.MinValue;

        [UIXSignatureField(5)]
        public double PLTI { get; set; } = 0d;

        [UIXSignatureField(6)]
        public DateTime CRAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(7)]
        public DateTime CHAT { get; set; } = DateTime.MinValue;

        [UIXSignatureField(8)]
        public bool TESTE { get; set; } = false;

        public override void Save()
        {
            new TXSESSI().Save(this);
        }
    }
}
