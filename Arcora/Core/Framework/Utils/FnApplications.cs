using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.Utils
{
    public static class FnApplications
    {
        private static Dictionary<string, Type>? _applicationTypes;

        /// <summary>
        /// Application type with the given class name (APDID of T1APRDH), or null if there is none.
        /// </summary>
        public static Type? ResolveApplicationType(string className)
        {
            if (FnString.IsNullEmptyOrWhitespace(className))
                return null;

            _applicationTypes ??= typeof(FnApplications).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(UIXApplication).IsAssignableFrom(t))
                .GroupBy(t => t.Name, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

            return _applicationTypes.TryGetValue(className, out Type? type) ? type : null;
        }

        /// <summary>
        /// Icon of the application as declared by its UIXAppIcon attribute, or an empty string.
        /// </summary>
        public static string GetApplicationIcon(string className)
        {
            Type? type = ResolveApplicationType(className);

            return type == null ? string.Empty : UIXAppIconAttribute.GetIcon(type);
        }
    }
}
