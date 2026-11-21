using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameTimeNext.Core.Framework.Components.Base
{

    public class GTNComponent<TSelf> where TSelf : GTNComponent<TSelf>, new()
    {
        [JsonIgnore]
        public string RawValue { get; set; }


        public GTNComponent()
        {
            RawValue = string.Empty;
        }

        public GTNComponent(string rawValue)
        {
            RawValue = rawValue;
        }

        public TSelf Dezerialize()
        {
            if (RawValue == null)
                return new TSelf();

            if (RawValue.Length == 0)
                return new TSelf();

            var instance = JsonSerializer.Deserialize<TSelf>(RawValue);

            if (instance is null)
                instance = new TSelf();

            return instance;
        }

        public string Serialize()
        {
            string rawValue = JsonSerializer.Serialize((TSelf)this);

            RawValue = rawValue;

            return rawValue;
        }
    }
}
