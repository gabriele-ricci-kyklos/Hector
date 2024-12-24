using System;
using System.Text.Json;
using Hector;

namespace Hector.Json
{
    public static class JsonExtensionMethods
    {
        private static readonly JsonSerializerOptions _optionsIndented = new(JsonSerializerDefaults.Web) { WriteIndented = true, PropertyNamingPolicy = null };
        private static readonly JsonSerializerOptions _optionsNotIndented = new(JsonSerializerDefaults.Web) { WriteIndented = false, PropertyNamingPolicy = null };

        public static string ToJson(this object? item, bool indent = false)
        {
            if (item is null)
            {
                return string.Empty;
            }

            JsonSerializerOptions options = indent ? _optionsIndented : _optionsNotIndented;
            return JsonSerializer.Serialize(item, options);
        }

        public static T? FromJson<T>(this string? json)
        {
            if (json.IsNullOrBlankString())
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(json!, _optionsNotIndented);
        }

        public static object? GetRawValue(this JsonElement jsonElement) =>
            jsonElement.ValueKind switch
            {
                JsonValueKind.Number => jsonElement.GetDouble(),
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.True or JsonValueKind.False => jsonElement.GetBoolean(),
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                _ => throw new InvalidCastException($"The {nameof(JsonValueKind)} returned is not handled")
            };
    }
}
