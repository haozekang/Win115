using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Win115.Dtos
{
    public sealed class OpenRbListDTOConverter : JsonConverter<OpenRbListDTO>
    {
        public override OpenRbListDTO? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;
            var result = new OpenRbListDTO();
            foreach (var property in root.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "offset":
                        result.Offset = property.Value.GetInt64();
                        break;
                    case "limit":
                        result.Limit = property.Value.GetInt64();
                        break;
                    case "count":
                        result.Count = property.Value.GetString();
                        break;
                    case "rb_pass":
                        result.RbPass = property.Value.GetInt64();
                        break;
                    default:
                        if (property.Value.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        var item = JsonSerializer.Deserialize<OpenRbListDataItemDTO>(property.Value.GetRawText(), options);
                        if (item != null)
                        {
                            result.Items[property.Name] = item;
                        }
                        break;
                }
            }
            return result;
        }

        public override void Write(Utf8JsonWriter writer, OpenRbListDTO value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            if (value.Offset != null)
            {
                writer.WriteNumber("offset", value.Offset.Value);
            }
            if (value.Limit != null)
            {
                writer.WriteNumber("limit", value.Limit.Value);
            }
            if (value.Count != null)
            {
                writer.WriteString("count", value.Count);
            }
            if (value.RbPass != null)
            {
                writer.WriteNumber("rb_pass", value.RbPass.Value);
            }
            foreach (var item in value.Items)
            {
                writer.WritePropertyName(item.Key);

                JsonSerializer.Serialize(writer, item.Value, options);
            }
            writer.WriteEndObject();
        }
    }
}
