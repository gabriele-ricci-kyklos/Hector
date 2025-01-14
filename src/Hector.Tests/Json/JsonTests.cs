using FluentAssertions;
using System.Dynamic;
using System.Text.Json;

namespace Hector.Tests.Json
{
    public class JsonTests
    {
        private static readonly JsonSerializerOptions _options = new() { Converters = { new Hector.Json.Converters.ExpandoObjectConverter() } };

        [Fact]
        public void TestExpandoConverterWrite()
        {
            dynamic obj = new ExpandoObject();
            obj.Prop1 = 1;
            obj.Prop2 = "lol";

            string json = JsonSerializer.Serialize<ExpandoObject>(obj, _options);
            json.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void TestExpandoConverterRead()
        {
            string testJson = @"
{
  ""isActive"": true,
  ""isVerified"": false,
  ""timestamp"": ""2025-01-14T12:34:56Z"",
  ""byteArray"": ""SGVsbG8gV29ybGQh"",
  ""uniqueId"": ""d3b07384-d9a6-4f98-b56b-678ab1f8f7cd"",
  ""description"": ""This is a sample string"",
  ""nullableProperty"": null,
  ""smallNumber"": 12345,
  ""mediumNumber"": 123456789,
  ""largeNumber"": 1234567890123456789,
  ""price"": 12345.67,
  ""customObject"": {
    ""property1"": ""Custom String"",
    ""property2"": 987,
    ""property3"": true
  }
}";
            dynamic obj = JsonSerializer.Deserialize<dynamic>(testJson, _options) ?? throw new Exception();
            (obj as object).Should().NotBeNull();
            (obj.isActive is bool).Should().BeTrue();
            (obj.isVerified is bool).Should().BeTrue();
            (obj.timestamp is DateTime).Should().BeTrue();
            (obj.uniqueId is Guid).Should().BeTrue();
            (obj.description is string).Should().BeTrue();
            (obj.nullableProperty is null).Should().BeTrue();
            (obj.smallNumber is int).Should().BeTrue();
            (obj.mediumNumber is int).Should().BeTrue();
            (obj.largeNumber is long).Should().BeTrue();
            (obj.price is double).Should().BeTrue();
            (obj.customObject is ExpandoObject).Should().BeTrue();
        }
    }
}
