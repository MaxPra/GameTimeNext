using GameTimeNext.Core.Framework.DataBase.TableObjects;
using System;

namespace GameTimeNext.Core.Application.TableObjects
{
    internal class TBL_SESSI : TableObjectBase
    {
        public long SEID { get; set; }

        public long PFID { get; set; }

        public DateTime PLFR { get; set; }
        public DateTime PLTO { get; set; }

        public double PLTI { get; set; }

        public DateTime CRAT { get; set; }
        public DateTime CHAT { get; set; }

        public TBL_SESSI()
        {
            SEID = 0;
            PFID = 0;

            PLFR = DateTime.MinValue;
            PLTO = DateTime.MinValue;

            PLTI = 0.0;

            CRAT = DateTime.MinValue;
            CHAT = DateTime.MinValue;

            AcceptChanges();
        }

        protected override string BuildSignature()
        {
            string signature = string.Empty;

            signature += NormalizeLong(SEID) + "|";
            signature += NormalizeLong(PFID) + "|";
            signature += NormalizeDateTime(PLFR) + "|";
            signature += NormalizeDateTime(PLTO) + "|";
            signature += NormalizeDouble(PLTI) + "|";
            signature += NormalizeDateTime(CRAT) + "|";
            signature += NormalizeDateTime(CHAT);

            return signature;
        }
    }
}
