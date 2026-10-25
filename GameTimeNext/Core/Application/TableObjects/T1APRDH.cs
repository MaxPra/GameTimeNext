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

        public override void Save()
        {
            new TXAPRDH().Save(this);
        }
    }
}
