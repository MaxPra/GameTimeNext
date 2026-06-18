using GameTimeNext.Core.Application.TableObjects;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TFCTABD
    {
        public static string GetDescription(string txtyp, string txnum)
        {
            TXCTABD txctabd = new TXCTABD();
            T1CTABD? t1ctabd = txctabd.Read(txtyp, txnum);

            return t1ctabd?.DESCR ?? string.Empty;
        }
    }
}
