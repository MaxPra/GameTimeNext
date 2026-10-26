using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.Utils
{
    public partial class FnSystem
    {
        // Possible start arguments:
        // -m <modi_key> : Start the application with a specific modi key.
        // -minimized : Start the application minimized.
        // --force-metadata-migration : Force the application to perform metadata migration on startup.
        // --force-user-perms : Force the application to act if ran as Release build, but with debug paths.
        // --bigpicture : Open the main window directly in the Big Picture mode (TV / controller).

        #region Quick Access Start Arguments
        public static bool RunWithUserPermissions()
        {
            return AppEnvironment.StartArguments.ContainsKey("force-user-perms");
        }
        #endregion

        public static void ParseStartArguments(string[] args)
        {
            Dictionary<string, string?> retValue = new Dictionary<string, string?>();

            string currentKey = string.Empty;

            foreach (string arg in args)
            {
                if (arg.StartsWith('-'))
                {
                    // Key
                    currentKey = arg.ToLowerInvariant().Substring(1);

                    if (currentKey.StartsWith('-'))
                    {
                        // Long Key
                        currentKey = currentKey.Substring(1);
                    }

                    retValue.Add(currentKey, null);
                }
                else
                {
                    // Value
                    if (FnString.IsNullEmptyOrWhitespace(currentKey) || (retValue.ContainsKey(currentKey) && !FnString.IsNullEmptyOrWhitespace(retValue[currentKey]))) continue;

                    retValue[currentKey] = arg;
                }
            }

            AppEnvironment.StartArguments = retValue;
        }

        public static string GetStartArgumentsString()
        {
            List<string> parts = new List<string>();

            foreach (KeyValuePair<string, string?> argument in AppEnvironment.StartArguments)
            {
                if (argument.Key.Length > 1) parts.Add($"--{argument.Key}");
                else parts.Add($"-{argument.Key}");

                if (!FnString.IsNullEmptyOrWhitespace(argument.Value)) parts.Add(argument.Value!);
            }

            return string.Join(" ", parts);
        }
    }
}
