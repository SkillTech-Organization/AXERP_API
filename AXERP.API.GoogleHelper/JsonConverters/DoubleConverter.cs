using Newtonsoft.Json;
using System.Diagnostics;

namespace AXERP.API.GoogleHelper.JsonConverters
{
    public class DoubleConverter : JsonConverter<double?>
    {
        public override double? ReadJson(JsonReader reader, Type objectType, double? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var raw = reader.Value.ToString();
            var cleared = raw.Replace(" ", "").Trim();
            Debug.WriteLine($"Raw: {raw}, cleared: {cleared}");
            return double.TryParse(cleared, out double result) ? result : null;
        }

        public override void WriteJson(JsonWriter writer, double? value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }
    }
}
