using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hector.Core.Serialization.Json.Support.Newtonsoft.Converters
{
    public class DecimalFormatConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            if (objectType == null)
            {
                return false;
            }

            TypeCode typeCode = Type.GetTypeCode(objectType);

            switch (typeCode)
            {
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
            }
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);

            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteValue(string.Empty);
            }
            decimal d = Convert.ToDecimal(value);
            if (d % 1 == 0)
            {
                writer.WriteValue(string.Format("{0:0}", d));
            }
            else
            {
                writer.WriteValue(d);
            }
        }
    }
}
