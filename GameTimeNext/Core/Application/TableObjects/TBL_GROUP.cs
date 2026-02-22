using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Framework.DataBase.TableObjects;
using System;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.TableObjects
{
    public class TBL_GROUP : UIXTableObjectBase
    {
        [UIXSignatureField(0)]
        public long GRID { get; set; }

        [UIXSignatureField(1)]
        public string GRNA { get; set; }

        [UIXSignatureField(2)]
        public DateTime CRAT { get; set; }

        [UIXSignatureField(3)]
        public DateTime CHAT { get; set; }

        [UIXSignatureField(4)]
        public string GTYP { get; set; }

        public bool? IsSelected { get; set; } = false;

        public TBL_GROUP()
        {
            GRID = 0;
            GRNA = string.Empty;
            GTYP = string.Empty;

            CRAT = DateTime.MinValue;
            CHAT = DateTime.MinValue;

            AcceptChanges();
        }

        public override void Save()
        {
            TBLM_GROUP tblmGroup = new TBLM_GROUP();
            tblmGroup.Save(this);
        }
    }
}
