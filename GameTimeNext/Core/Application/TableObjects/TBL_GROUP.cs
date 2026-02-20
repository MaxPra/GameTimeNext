using GameTimeNext.Core.Framework.DataBase.TableObjects;
using System;

namespace GameTimeNext.Core.Application.TableObjects
{
    internal class TBL_GROUP : TableObjectBase
    {
        public long GRID { get; set; }
        public string GRNA { get; set; }

        public DateTime CRAT { get; set; }
        public DateTime CHAT { get; set; }

        public TBL_GROUP()
        {
            GRID = 0;
            GRNA = string.Empty;

            CRAT = DateTime.MinValue;
            CHAT = DateTime.MinValue;

            AcceptChanges();
        }

        protected override string BuildSignature()
        {
            string signature = string.Empty;

            signature += NormalizeLong(GRID) + "|";
            signature += NormalizeString(GRNA) + "|";
            signature += NormalizeDateTime(CRAT) + "|";
            signature += NormalizeDateTime(CHAT);

            return signature;
        }
    }
}
