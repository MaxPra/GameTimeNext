using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.Utils
{
    public partial class FnSystem
    {
        // Possible start arguments:
        // -m <modi_key> : Start the application with a specific modi key.
        // -minimized : Start the application minimized.
        // --force-metadata-migration : Force the application to perform metadata migration on startup.

        public static void ParseStartArguments(string[] args)
        {
            Dictionary<string, string?> retValue = new Dictionary<string, string?>();

            string currentKey = string.Empty;

            foreach (string arg in args)
            {
                if (arg.StartsWith('-'))
                {
                    // Key
                    currentKey = arg.ToLower().Substring(1);

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
    }
}
