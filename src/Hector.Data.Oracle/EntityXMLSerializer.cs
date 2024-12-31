using Hector.Data.Entities;
using Hector.Data.Entities.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Hector.Data.Oracle
{
    internal class EntityXMLSerializer<T> where T : IBaseEntity
    {
        private const string _xmlArrayName = "A";
        private const string _xmlTypeName = "T";

        private readonly EntityDefinition<T> _entityDefinition;
        private readonly Dictionary<string, string> _propertyNameMapping;
        private readonly IAsyncDaoHelper _daoHelper;

        public EntityXMLSerializer(EntityDefinition<T> entityDefinition, IAsyncDaoHelper daoHelper)
        {
            _entityDefinition = entityDefinition;
            _propertyNameMapping = CreatePropertyNameMapping(_entityDefinition.PropertyInfoList);
            _daoHelper = daoHelper;
        }

        public string SerializeEntityDefinition(string schema)
        {
            string fields =
                _entityDefinition
                    .PropertyInfoList
                    .Select
                    (x =>
                        x.DbType switch
                        {
                            PropertyDbType.DateTime => $"TO_DATE(Y.{x.ColumnName}, 'yyyy-mm-dd hh24:mi:ss') AS {_daoHelper.EscapeValue(x.ColumnName)}",
                            PropertyDbType.Blob or PropertyDbType.ByteArray => $"{schema}HexToBlob(Y.{x.ColumnName}) AS {_daoHelper.EscapeValue(x.ColumnName)}",
                            _ => $"Y.{x.ColumnName} AS {_daoHelper.EscapeValue(x.ColumnName)}"
                        }
                    )
                    .StringJoin($",{Environment.NewLine}");

            string columns =
                _entityDefinition
                    .PropertyInfoList
                    .Select
                    (x =>
                    {
                        string typeName =
                        x.DbType switch
                        {
                            PropertyDbType.DateTime => $"NVARCHAR2(30)",
                            PropertyDbType.Blob or PropertyDbType.ByteArray => "CLOB",
                            _ => _daoHelper.MapDbTypeToSqlType(x)
                        };

                        return @$"    {x.ColumnName} {typeName} path '{_propertyNameMapping[x.ColumnName]}'";
                    })
                    .StringJoin($",{Environment.NewLine}");

            return
                @$"
SELECT
{fields} 
FROM 
XMLTABLE (
'/A/T' PASSING XMLTYPE({_daoHelper.ParameterPrefix}xmlData)
COLUMNS
{columns}
) Y";
        }

        public string SerializeEntityValues(IEnumerable<T> entityList)
        {
            StringBuilder sb = new();
            sb.Append($"<{_xmlArrayName}>");

            foreach (T item in entityList)
            {
                sb.Append($"<{_xmlTypeName}>");

                foreach (EntityPropertyInfo propertyInfo in _entityDefinition.PropertyInfoList)
                {
                    string newPropName = _propertyNameMapping[propertyInfo.ColumnName];
                    object value = _entityDefinition.TypeAccessor[item, propertyInfo.PropertyName];

                    sb.Append($"<{newPropName}>");
                    sb.Append(FormatValueToXML(value));
                    sb.Append($"</{newPropName}>");
                }

                sb.Append($"</{_xmlTypeName}>");
            }

            sb.Append($"</{_xmlArrayName}>");

            return sb.ToString();
        }

        private string FormatValueToXML(object value)
        {
            if (value is null)
            {
                return string.Empty;
            }

            const string dateFormat = "yyyy-MM-dd HH:mm:ss";
            if (value is DateTime dt)
            {
                return dt.ToString(dateFormat);
            }

            if (value is byte[] byteArray)
            {
                return BitConverter.ToString(byteArray).Replace("-", "");
            }

            if (value is bool b)
            {
                return b ? "1" : "0";
            }

            if (IsFloatingPointNumberType(value.GetType()))
            {
                return string.Format("{0}", CultureInfo.InvariantCulture, value);
            }

            return value.ToString();
        }

        private static bool IsFloatingPointNumberType(Type? type) =>
            Type.GetTypeCode(type) switch
            {
                TypeCode.Decimal
                or TypeCode.Double
                or TypeCode.Single => true,
                _ => false
            };

        private static Dictionary<string, string> CreatePropertyNameMapping(EntityPropertyInfo[] props)
        {
            string format = $"D{Math.Truncate(1 + Math.Log10(props.Length))}";
            Dictionary<string, string> mapping = [];

            for (int i = 0; i < props.Length; ++i)
            {
                mapping.Add(props[i].ColumnName, "P" + (i + 1).ToString(format));
            }

            return mapping;
        }
    }
}
