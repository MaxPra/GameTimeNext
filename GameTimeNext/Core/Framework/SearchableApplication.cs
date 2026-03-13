namespace GameTimeNext.Core.Framework
{
    public class SearchableApplication
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string ClassName { get; set; }

        public override string ToString()
        {
            return Name ?? string.Empty;
        }
    }
}
