using GameTimeNext.Core.Framework.UI.Dialogs;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.General
{
    /// <summary>
    /// Helper functions shared by the main window and the applications in its tabs.
    /// </summary>
    internal static class CFMainApp
    {
        /// <summary>
        /// Shows the help text of an application definition (key = class name of the application) in the help window.
        /// </summary>
        public static void ShowApplicationHelp(UIXApplication app, string applicationId, string title)
        {
            // Read again instead of the properties loaded at the start of the app, so a changed help text shows up immediately
            UIXAppProperties? properties = UIXApplication.AppPropertiesProvider?.Read(applicationId);

            string helpText = properties is null || FnString.IsNullEmptyOrWhitespace(properties.ApplicationHelpText)
                ? "No help is available for this application yet."
                : properties.ApplicationHelpText;

            app.GetApplication<CFMBOX>(UIXApplicationStartTarget.Window)
                .Show($"Help - {title}", helpText, CFMBOXResult.Ok, CFMBOXIcon.Info, CFMBOXType.MarkdownLarge);
        }
    }
}
