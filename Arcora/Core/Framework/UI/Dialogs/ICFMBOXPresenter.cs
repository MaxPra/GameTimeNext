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

        /// <summary>
        /// Replaces the text of the message that is shown right now (e.g. the countdown of a CFMBOXTimeout).
        /// </summary>
        void UpdateMessage(string message);

        /// <summary>
        /// Closes the message that is shown right now: Show returns result (e.g. when a CFMBOXTimeout runs out).
        /// </summary>
        void Close(CFMBOXResult result);
    }
}
