using GameTimeNext.Core.Framework.UI.Dialogs;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework
{
    public class InformationListItem
    {
        public CFMBOXIcon Icon { get; set; }

        public string Text { get; set; } = string.Empty;

        public Action YesAction = new Action(() => { });

        public CFMBOXResult Buttons = CFMBOXResult.Ok;

        public CFMBOXType MBoxType { get; set; } = CFMBOXType.Default;

        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Number of the error message shown instead of Text; resolved only when the item is shown.
        /// </summary>
        public string MessageNumber { get; private set; } = string.Empty;

        public object?[] MessageParameters { get; private set; } = [];

        public InformationListItem(CFMBOXIcon icon, string text)
        {
            Icon = icon;
            Text = text;
        }

        public InformationListItem(CFMBOXIcon icon, string title, string text)
        {
            Icon = icon;
            Text = text;
            Title = title;
        }

        /// <summary>
        /// Creates an item for an error message. Items are mostly added during startup, before the error message
        /// provider is registered, so the text is looked up later in GetText.
        /// </summary>
        public static InformationListItem FromErrorMessage(CFMBOXIcon icon, string messageNumber, params object?[] parameters)
        {
            return new InformationListItem(icon, string.Empty)
            {
                MessageNumber = messageNumber,
                MessageParameters = parameters
            };
        }

        /// <summary>
        /// Text to show: the resolved error message if the item was created from one, otherwise Text.
        /// </summary>
        public string GetText(UIXApplication app)
        {
            return FnString.IsNullEmptyOrWhitespace(MessageNumber) ? Text : app.GetErrorMessage(MessageNumber, MessageParameters);
        }
    }
}
