namespace GameTimeNext.Core.Framework.UI.Dialogs
{
    /// <summary>
    /// Shows CFMBOX messages in another way than the dialog window (e.g. the Big Picture mode in its own style).
    /// </summary>
    public interface ICFMBOXPresenter
    {
        /// <summary>
        /// Shows the message modally and returns the chosen button, or null if this message type isn't supported
        /// (the dialog window is shown then).
        /// </summary>
        CFMBOXResult? Show(string title, string message, CFMBOXResult buttons, CFMBOXIcon icon, CFMBOXType type);
    }
}
