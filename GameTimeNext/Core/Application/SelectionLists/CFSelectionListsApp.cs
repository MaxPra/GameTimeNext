using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.SelectionLists
{
    /// <summary>
    /// Helper functions of the module "Selection Lists": the user friendly view on the codetables.
    /// </summary>
    public static class CFSelectionListsApp
    {
        public const int MaxParameterCount = 2;

        // Permission of the codetables a user may change (key of codetable "cP")
        private const string UserPermission = "U";

        // Prefix of the entries a user creates, and the one reserved for entries delivered with the app
        private const string UserKeyPrefix = "U_";
        private const string DeliveredKeyPrefix = "D_";

        // Characters of a generated key: no 0 / O and 1 / I, so a key can't be misread
        private const string KeyCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const int KeyLength = 5;

        // Control types of the parameters (codetable "cO")
        private const string ControlTypeComboBox = "01";
        private const string ControlTypeTextBox = "02";

        /// <summary>
        /// True if the codetable is one of the lists a user may change.
        /// </summary>
        public static bool IsUserList(T1CTABH list)
        {
            return list.PERMI == UserPermission;
        }

        /// <summary>
        /// The codetable if it exists and the user may add entries to it, otherwise null.
        /// </summary>
        public static T1CTABH? GetExtendableList(string txtyp)
        {
            if (FnString.IsNullEmptyOrWhitespace(txtyp))
                return null;

            T1CTABH? list = new TXCTABH().Read(txtyp);

            return list != null && IsUserList(list) ? list : null;
        }

        /// <summary>
        /// Opens the dialog for a new entry in the list and calls <paramref name="created"/> with its key once it is saved.
        /// </summary>
        public static void CreateEntry(UIXApplication app, T1CTABH list, Action<string> created)
        {
            SelectionListEntryEditApp editApp = app.GetApplication<SelectionListEntryEditApp>(UIXApplicationStartTarget.Window);

            editApp.CreateNew(result =>
            {
                if (result.HasChanged)
                    created(result.Key);
            }, list);
        }

        /// <summary>
        /// True if the user may change or delete the entry: entries delivered with the app are locked.
        /// </summary>
        public static bool IsEntryEditable(T1CTABH list, T1CTABD entry)
        {
            if (!IsUserList(list))
                return false;

            return !(list.NRANA && entry.TXNUM.StartsWith(DeliveredKeyPrefix, StringComparison.Ordinal));
        }

        /// <summary>
        /// True if the list has the parameter and the module can show it (combo box or text box).
        /// </summary>
        public static bool IsParameterSupported(T1CTABH list, int index)
        {
            if (!list.GetValue<bool>($"PAAC{index}"))
                return false;

            string controlType = list.GetValue<string>($"PACO{index}") ?? string.Empty;

            return controlType == ControlTypeComboBox || controlType == ControlTypeTextBox;
        }

        public static bool IsParameterComboBox(T1CTABH list, int index)
        {
            return list.GetValue<string>($"PACO{index}") == ControlTypeComboBox;
        }

        public static string GetParameterLabel(T1CTABH list, int index)
        {
            return list.GetValue<string>($"PADE{index}") ?? string.Empty;
        }

        public static string GetParameterHint(T1CTABH list, int index)
        {
            return list.GetValue<string>($"PTOL{index}") ?? string.Empty;
        }

        /// <summary>
        /// Value of a parameter of an entry as the user knows it: the description for a parameter chosen from another list,
        /// otherwise the entered text.
        /// </summary>
        public static string GetParameterText(T1CTABH list, int index, T1CTABD entry)
        {
            if (!IsParameterSupported(list, index))
                return string.Empty;

            string value = index == 1 ? entry.PARM1 : entry.PARM2;

            if (FnString.IsNullEmptyOrWhitespace(value) || !IsParameterComboBox(list, index))
                return value;

            string sourceList = list.GetValue<string>($"PACT{index}") ?? string.Empty;
            string description = TFCTABD.GetDescription(sourceList, value);

            return FnString.IsNullEmptyOrWhitespace(description) ? value : description;
        }

        /// <summary>
        /// Creates a short key that doesn't exist in the list yet. The user never sees it.
        /// </summary>
        public static string GenerateKey(string txtyp)
        {
            TXCTABD txctabd = new TXCTABD();

            while (true)
            {
                char[] chars = new char[KeyLength];

                for (int i = 0; i < chars.Length; i++)
                    chars[i] = KeyCharacters[Random.Shared.Next(KeyCharacters.Length)];

                string key = UserKeyPrefix + new string(chars);

                if (!txctabd.Exists(txtyp, key))
                    return key;
            }
        }
    }
}
