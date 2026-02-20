using GameTimeNext.Core.Framework.DataBase.TableObjects;
using System;

namespace GameTimeNext.Core.Application.TableObjects
{
    internal class TBL_GRPPO : TableObjectBase
    {
        public long GPID { get; set; }

        public long GRID { get; set; }
        public long PFID { get; set; }

        public DateTime CRAT { get; set; }
        public DateTime CHAT { get; set; }

        public TBL_GRPPO()
        {
            GPID = 0;

            GRID = 0;
            PFID = 0;

            CRAT = DateTime.MinValue;
            CHAT = DateTime.MinValue;

            AcceptChanges();
        }

        protected override string BuildSignature()
        {
            string signature = string.Empty;

            signature += NormalizeLong(GPID) + "|";
            signature += NormalizeLong(GRID) + "|";
            signature += NormalizeLong(PFID) + "|";
            signature += NormalizeDateTime(CRAT) + "|";
            signature += NormalizeDateTime(CHAT);

            return signature;
        }
    }
}
