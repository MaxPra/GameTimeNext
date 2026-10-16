using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.Utils
{
    public partial class FnSystem
    {
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
