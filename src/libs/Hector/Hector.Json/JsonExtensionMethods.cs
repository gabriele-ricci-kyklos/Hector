using System.Text.Json;
using Hector.Core;

namespace Hector.Json
{
    public static class JsonExtensionMethods
    {
        private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

        public static string ToJson(this object? item, bool indent = false)
        {
            if (item is null)
            {
                return string.Empty;
            }

            _options.WriteIndented = indent;
            return JsonSerializer.Serialize(item, _options);
        }

        public static T? FromJson<T>(this string? json)
        {
            if (json.IsNullOrBlankString())
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(json!, _options);
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
