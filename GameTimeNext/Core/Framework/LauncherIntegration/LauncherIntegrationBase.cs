using System.Diagnostics;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.LauncherIntegration
{
    /// <summary>
    /// Base class of all launcher integrations: optional features are switched off by default.
    /// </summary>
    public abstract class LauncherIntegrationBase : ILauncherIntegration
    {
        public abstract string LauncherType { get; }

        public virtual bool SupportsImport => false;

        public virtual bool SupportsLaunchArguments => false;

        public virtual bool SupportsOpenInLauncher => false;

        public virtual List<LauncherGame> GetInstalledGames()
        {
            return new List<LauncherGame>();
        }

        public abstract bool IsGameInstalled(string gameId);

        public abstract void StartGame(string gameId, string launchArgs);

        public virtual void OpenInLauncher(string gameId)
        {
        }

        /// <summary>
        /// Starts a file, shortcut or URI through the Windows shell.
        /// </summary>
        protected static void StartShell(string fileName, string arguments = "", string workingDirectory = "")
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true
            };

            if (!FnString.IsNullEmptyOrWhitespace(workingDirectory))
                startInfo.WorkingDirectory = workingDirectory;

            Process.Start(startInfo);
        }
    }
}
