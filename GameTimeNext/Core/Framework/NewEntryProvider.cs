using GameTimeNext.Core.Application.SelectionLists;
using GameTimeNext.Core.Application.TableObjects;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework
{
    /// <summary>
    /// Lets the engine offer "Add new entry" in every ComboBox that edits a field and shows a codetable with the permission
    /// "User"; the entry is created in the entry dialog of the module "Selection Lists".
    /// </summary>
    public class NewEntryProvider : IUIXNewEntryProvider
    {
        private const string NewEntryText = "Add new entry";

        public string GetNewEntryText(UIXComboBoxSourceDefinition source)
        {
            return GetExtendableList(source) != null ? NewEntryText : string.Empty;
        }

        public void CreateEntry(UIXApplication app, UIXComboBoxSourceDefinition source, Action<string> created)
        {
            if (GetExtendableList(source) is T1CTABH list)
                CFSelectionListsApp.CreateEntry(app, list, created);
        }

        // Only the entries of a codetable (T1CTABD, filtered by its text type) can be created, and only in lists the user may change
        private static T1CTABH? GetExtendableList(UIXComboBoxSourceDefinition source)
        {
            if (source.From != K1CTABD.Name || source.WhereField != K1CTABD.Fields.TXTYP)
                return null;

            return CFSelectionListsApp.GetExtendableList(source.WhereValue);
        }
    }
}
