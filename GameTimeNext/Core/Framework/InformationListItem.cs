using GameTimeNext.Core.Framework.UI.Dialogs;

namespace GameTimeNext.Core.Framework
{
    public class InformationListItem
    {
        public CFMBOXIcon Icon { get; set; }

        public string Text { get; set; } = string.Empty;

        public InformationListItem(CFMBOXIcon icon, string text)
        {
            Icon = icon;
            Text = text;
        }
    }
}
