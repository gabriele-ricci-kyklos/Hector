using Hector;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hector.Json.Converters
{
    public sealed class StringToInt16Converter : StringToNumberConverter<short>
    {
    }

    public sealed class StringToInt32Converter : StringToNumberConverter<int>
    {
    }

    public sealed class StringToInt64Converter : StringToNumberConverter<long>
    {
    }

    public abstract class StringToNumberConverter<T> : JsonConverter<T> where T : struct
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType switch
            {
                JsonTokenType.Number => GetRawNumberValue(reader).ConvertTo<T>(),
                JsonTokenType.String => (reader.GetString() ?? string.Empty).ConvertTo<T>(),
                _ => throw new NotSupportedException($"Cannot handle the current value")
            };

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteRawValue(value.ToString() ?? string.Empty);
        }

        private static object GetRawNumberValue(Utf8JsonReader reader)
        {
            object value;

            if (reader.TryGetInt32(out int int32value))
            {
                value = int32value.ToString();
            }
            else if (reader.TryGetInt64(out long int64value))
            {
                value = int64value.ToString();
            }
            else if (reader.TryGetInt16(out short int16value))
            {
                value = int16value.ToString();
            }
            else
            {
                value = reader.GetString() ?? throw new NotSupportedException($"Cannot handle the current value");
            }

            return value;
        }
    }
}
