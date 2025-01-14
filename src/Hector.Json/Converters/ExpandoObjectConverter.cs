using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hector.Json.Converters
{
    // credits: https://stackoverflow.com/a/65974452/4499267
    public class ExpandoObjectConverter(FloatFormat floatFormat, UnknownNumberFormat unknownNumberFormat, ObjectFormat objectFormat)
        : JsonConverter<object>
    {
        public FloatFormat FloatFormat { get; } = floatFormat;
        public UnknownNumberFormat UnknownNumberFormat { get; } = unknownNumberFormat;
        public ObjectFormat ObjectFormat { get; } = objectFormat;

        public ExpandoObjectConverter() : this(FloatFormat.Double, UnknownNumberFormat.Error, ObjectFormat.Expando) { }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value.GetType() == typeof(object))
            {
                writer.WriteStartObject();
                writer.WriteEndObject();
            }
            else
            {
                JsonSerializer.Serialize(writer, value, value.GetType(), options);
            }
        }

        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType switch
            {
                JsonTokenType.Null => null!,
                JsonTokenType.False => false,
                JsonTokenType.True => true,
                JsonTokenType.String =>
                    reader.TryGetDateTime(out DateTime dateValue)
                    ? dateValue
                    : reader.TryGetBytesFromBase64(out byte[]? byteArr)
                        ? byteArr
                        : reader.TryGetGuid(out Guid guid)
                            ? guid
                            : reader.GetString() ?? string.Empty,
                JsonTokenType.Number => reader switch
                {
                    _ when reader.TryGetInt32(out int i) => i,
                    _ when reader.TryGetInt64(out long l) => l,
                    _ when FloatFormat == FloatFormat.Decimal && reader.TryGetDecimal(out decimal m) => m,
                    _ when FloatFormat == FloatFormat.Double && reader.TryGetDouble(out double d) => d,
                    _ when UnknownNumberFormat == UnknownNumberFormat.JsonElement =>
                        JsonDocument.ParseValue(ref reader).RootElement.Clone(),
                    _ => throw new JsonException("Cannot parse current value as a number")
                },
                JsonTokenType.StartArray => ReadArray(ref reader, options),
                JsonTokenType.StartObject => ReadObject(ref reader, options),
                _ => throw new JsonException($"Unknown token {reader.TokenType}")
            };

        private object ReadArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            ArrayList list = [];
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return list.Count > 0 ? list.ToArray(list[0].GetType()) : Array.Empty<object>();
                }
                list.Add(Read(ref reader, typeof(object), options));
            }
            throw new JsonException();
        }

        private IDictionary<string, object> ReadObject(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            IDictionary<string, object> dict = CreateDictionary();
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.EndObject:
                        return dict;

                    case JsonTokenType.PropertyName:
                        {
                            string key = reader.GetString()!;
                            reader.Read();
                            dict.Add(key, Read(ref reader, typeof(object), options));
                        }
                        break;

                    default:
                        break;
                }
            }
            throw new JsonException();
        }

        private IDictionary<string, object> CreateDictionary() =>
            ObjectFormat == ObjectFormat.Expando ? new ExpandoObject() : new Dictionary<string, object>();
    }

    public enum FloatFormat
    {
        Double,
        Decimal,
    }

    public enum UnknownNumberFormat
    {
        Error,
        JsonElement,
    }

    public enum ObjectFormat
    {
        Expando,
        Dictionary,
    }
}
