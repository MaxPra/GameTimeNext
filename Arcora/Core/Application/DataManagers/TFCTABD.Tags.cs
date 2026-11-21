using GameTimeNext.Core.Application.TableObjects;

namespace GameTimeNext.Core.Application.DataManagers
{
    /// <summary>
    /// Tags of the games: entries of codetable <see cref="TagCodetable"/> (key = TXNUM, name = DESCR, parameter 1 = hidden Y / N).
    /// Delivered tags have a "D_" key, tags of the user a "U_" key.
    /// </summary>
    public partial class TFCTABD
    {
        public const string TagCodetable = "tG";

        private const string HiddenYes = "Y";

        /// <summary>
        /// Reads the tags sorted by name. Hidden tags are only included on request.
        /// </summary>
        public static List<T1CTABD> GetTags(bool includeHidden)
        {
            return new TXCTABD().GetEntries(TagCodetable)
                .Where(t => includeHidden || !IsTagHidden(t))
                .OrderBy(t => t.DESCR, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Keys of the tags that are not hidden.
        /// </summary>
        public static HashSet<string> GetVisibleTagKeys()
        {
            return GetTags(includeHidden: false).Select(t => t.TXNUM).ToHashSet(StringComparer.Ordinal);
        }

        public static bool IsTagHidden(T1CTABD tag)
        {
            return tag.PARM1 == HiddenYes;
        }
    }
}
