using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arcora.Core.Framework.Components.Base
{

    public class ArcoraComponent<TSelf> where TSelf : ArcoraComponent<TSelf>, new()
    {
        [JsonIgnore]
        public string RawValue { get; set; }


        public ArcoraComponent()
        {
            RawValue = string.Empty;
        }

        public ArcoraComponent(string rawValue)
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
