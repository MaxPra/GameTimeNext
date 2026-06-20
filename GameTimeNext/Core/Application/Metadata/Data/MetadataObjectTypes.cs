namespace GameTimeNext.Core.Application.Metadata.Data
{
    public static class MetadataObjectTypes
    {
        private static readonly IReadOnlyList<Entry> _entries = new List<Entry>
        {
            new Entry("mE", "Table Object")
        };

        public static IReadOnlyList<Entry> GetEntries()
        {
            return _entries;
        }

        public static string GetText(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            Entry? entry = _entries.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
                return key;

            return entry.Text;
        }

        public sealed class Entry
        {
            public Entry(string key, string text)
            {
                Key = key;
                Text = text;
            }

            public string Key { get; }
            public string Text { get; }
        }
    }
}
