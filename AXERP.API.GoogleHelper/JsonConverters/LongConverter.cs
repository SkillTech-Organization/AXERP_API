using Newtonsoft.Json;

namespace AXERP.API.GoogleHelper.JsonConverters
{
    public class LongConverter : JsonConverter<long?>
    {
        public override long? ReadJson(JsonReader reader, Type objectType, long? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var raw = reader.Value.ToString();
            var cleared = raw.Replace(" ", "").Trim();
            return long.TryParse(cleared, out long result) ? result : null;
        }

        public override void WriteJson(JsonWriter writer, long? value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }
    }
}
