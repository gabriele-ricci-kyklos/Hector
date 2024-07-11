using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hector.Json.Converters
{
    public sealed class NumberToStringConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType switch
            {
                JsonTokenType.Number => ConvertNumberToString(reader),
                JsonTokenType.String => reader.GetString(),
                _ => throw new NotSupportedException($"Cannot handle the current value")
            };

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

        private static string ConvertNumberToString(Utf8JsonReader reader)
        {
            if (reader.TryGetInt32(out int int32value))
            {
                return int32value.ToString();
            }
            else if (reader.TryGetInt64(out long int64value))
            {
                return int64value.ToString();
            }
            else if (reader.TryGetInt16(out short int16value))
            {
                return int16value.ToString();
            }
            else
            {
                return reader.GetString() ?? throw new NotSupportedException($"Cannot handle the current value");
            }
        }
    }
}
