using Hector.Core.Serialization.Json.Support.Newtonsoft.Converters;
using Newtonsoft.Json;

namespace Hector.Core.Serialization.Json.ExtensionMethods
{
    public static class JsonSerializationExtensionMethods
    {
        public static string FormatAsJSon(this object item, bool indent = false, bool trimZeroInDecimal = false)
        {
            if (item == null)
            {
                return string.Empty;
            }

            string objSer =
                trimZeroInDecimal
                ?
                JsonConvert.SerializeObject
                (
                    item,
                    indent ? Formatting.Indented : Formatting.None,
                    new DecimalFormatConverter()
                )
                :
                JsonConvert.SerializeObject
                (
                    item,
                    indent ? Formatting.Indented : Formatting.None
                );

            return objSer;
        }
    }
}
