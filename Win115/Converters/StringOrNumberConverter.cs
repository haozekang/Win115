using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Win115.Converters
{
    public class StringOrNumberConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                // 根据实际情况选择 GetInt32 / GetInt64 / GetDecimal
                if (reader.TryGetInt64(out long numberValue))
                {
                    return numberValue.ToString();
                }
                return reader.GetDouble().ToString();
            }
            return reader.GetString() ?? string.Empty;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            if (long.TryParse(value, out long result))
            {
                writer.WriteNumberValue(result);
            }
            else
            {
                writer.WriteStringValue(value);
            }
        }
    }
}
