using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Config;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TFAPRDH
    {
        /// <summary>
        /// Placeholder in the description (APDES) and help text (APHPT) for the displayed application name.
        /// </summary>
        public const string ApplicationNamePlaceholder = "{ApplicationName}";

        /// <summary>
        /// Application properties definitions that may be called from a menu (APMCP), ordered by ID.
        /// </summary>
        public static List<T1APRDH> GetMenuCallableApplications()
        {
            return new TXAPRDH().ReadAll()
                .Where(a => a.APMCP)
                .OrderBy(a => a.APDID, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Replaces the placeholders of a description or help text for display. The module "Application Properties
        /// Definition" shows and edits the stored text with its placeholders.
        /// </summary>
        public static string ResolvePlaceholders(string text)
        {
            return text.Replace(ApplicationNamePlaceholder, AppConfig.Root.ApplicationName, StringComparison.Ordinal);
        }
    }
}
