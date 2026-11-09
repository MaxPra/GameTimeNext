using GameTimeNext.Core.Application.TableObjects;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Codetables.Api
{
    /// <summary>
    /// Finds the API class <c>CTAPI_&lt;Name&gt;</c> by the name stored in the codetable.
    /// </summary>
    public static class CTAPIFactory
    {
        private const string ClassPrefix = "CTAPI_";

        private static Dictionary<string, Type>? _apiTypes;

        /// <summary>
        /// True if there is an API class for the name.
        /// </summary>
        public static bool Exists(string name)
        {
            return FindType(name) != null;
        }

        /// <summary>
        /// Creates the API for the name, or returns null for an empty or unknown name.
        /// </summary>
        public static CTAPIBase? Create(string name)
        {
            Type? type = FindType(name);

            return type == null ? null : (CTAPIBase?)Activator.CreateInstance(type);
        }

        /// <summary>
        /// Name of the API of a parameter of a codetable (1 or 2), empty if it has none.
        /// </summary>
        public static string GetApiName(T1CTABH list, int parameterIndex)
        {
            return (parameterIndex == 1 ? list.PAAP1 : list.PAAP2).Trim();
        }

        private static Type? FindType(string name)
        {
            if (FnString.IsNullEmptyOrWhitespace(name))
                return null;

            _apiTypes ??= typeof(CTAPIFactory).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(CTAPIBase).IsAssignableFrom(t) && t.Name.StartsWith(ClassPrefix, StringComparison.Ordinal))
                .ToDictionary(t => t.Name.Substring(ClassPrefix.Length), t => t, StringComparer.OrdinalIgnoreCase);

            return _apiTypes.TryGetValue(name.Trim(), out Type? type) ? type : null;
        }
    }
}
