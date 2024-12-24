using FastMember;
using Hector;
using Hector.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Hector.Data.DataReaders
{
    // credits: https://stackoverflow.com/a/2258448/4499267
    // Later understood that a DataReader is always about enumeration and moved to DbDataReader

    public class EnumerableDbDataReader<T> : DbDataReader
    {
        protected readonly TypeAccessor _typeAccessor;
        protected readonly IEnumerator _enumerator;
        protected readonly Type _type;
        protected readonly Dictionary<string, PropertyInfo> _members = [];
        protected readonly Dictionary<int, PropertyInfo> _indexedMembers = [];
        protected readonly string[] _memberNames = [];

        protected bool _isClosed = false;
        protected object? _currentObject;

        public EnumerableDbDataReader(IEnumerable<T> values)
            : this(typeof(T), values)
        {
        }

        public EnumerableDbDataReader(Type type, IEnumerable values)
        {
            _type = typeof(T);
            _members = GetMembers();
            _indexedMembers = GetIndexedMembers();
            _memberNames = _members.Keys.ToArray();
            _typeAccessor = TypeAccessor.Create(_type);
            _enumerator = values.GetEnumerator();
        }

        protected virtual Dictionary<string, PropertyInfo> GetMembers() =>
            _type.GetHierarchicalOrderedPropertyList().ToDictionary(x => x.Name);

        protected virtual Dictionary<int, PropertyInfo> GetIndexedMembers() =>
            _members.Select((x, i) => (x.Value, Index: i)).ToDictionary(x => x.Index, x => x.Value);

        public override bool Read()
        {
            bool returnValue = _enumerator.MoveNext();
            _currentObject = returnValue ? _enumerator.Current : _type.IsValueType ? Activator.CreateInstance(_type) : null;
            return returnValue;
        }

        public override object GetValue(int ordinal) => _typeAccessor[_currentObject, _indexedMembers[ordinal].Name];

        public override int FieldCount => _members.Count;

        public override object this[int ordinal] => GetValue(ordinal);

        public override object this[string name] => _typeAccessor[_currentObject, _members[name].Name];

        public override int Depth => throw new NotSupportedException();

        public override bool IsClosed => _isClosed;

        public override int RecordsAffected => -1;

        public override bool HasRows => throw new NotImplementedException();

        public override bool GetBoolean(int i) => (bool)GetValue(i);

        public override byte GetByte(int i) => (byte)GetValue(i);

        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
        {
            if (GetValue(ordinal) is not byte[] value)
            {
                return 0L;
            }

            byte[] data =
                value
                .Skip((int)dataOffset)
                .Take(length)
                .ToArray();

            Array.Copy(data, 0, buffer ?? [], bufferOffset, data.Length);

            return data.Length;
        }

        public override char GetChar(int i) => (char)GetValue(i);

        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
        {
            string value = GetString(ordinal);
            if (value is null)
            {
                return 0L;
            }

            string subStr = value.SafeSubstring((int)dataOffset, Math.Min(length, value.Length));

            Array.Copy(subStr.ToCharArray(), 0, buffer ?? [], bufferOffset, subStr.Length);

            return subStr.Length;
        }

        public override string GetDataTypeName(int i) => GetFieldType(i).Name;

        public override DateTime GetDateTime(int i) => (DateTime)GetValue(i);

        public override decimal GetDecimal(int i) => (decimal)GetValue(i);

        public override double GetDouble(int i) => (double)GetValue(i);

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        public override Type GetFieldType(int i) => _indexedMembers[i].PropertyType;

        public override float GetFloat(int i) => (float)GetValue(i);

        public override Guid GetGuid(int i) => (Guid)GetValue(i);

        public override short GetInt16(int i) => (short)GetValue(i);

        public override int GetInt32(int i) => (int)GetValue(i);

        public override long GetInt64(int i) => (long)GetValue(i);

        public override string GetName(int i) => _indexedMembers[i].Name;

        public override int GetOrdinal(string name) => Array.IndexOf(_memberNames, name);

        public override string GetString(int ordinal) => (string)GetValue(ordinal);

        public override int GetValues(object[] values)
        {
            int i = 0;
            for (; i < _members.Count; i++)
            {
                if (values.Length <= i)
                {
                    return i;
                }
                values[i] = GetValue(i);
            }
            return i;
        }

        public override bool IsDBNull(int ordinal) => GetValue(ordinal) is null;

        public override bool NextResult()
        {
            throw new NotImplementedException();
        }

        public override void Close() => _isClosed = true;

        public override IEnumerator GetEnumerator() => _members.GetEnumerator();

        public override DataTable? GetSchemaTable()
        {
            DataTable dt = new();
            dt.BeginLoadData();

            foreach (PropertyInfo field in _members.Values)
            {
                dt.Columns.Add(new DataColumn(field.Name, field.PropertyType.GetNonNullableType()));
            }

            dt.EndLoadData();
            dt.AcceptChanges();

            return dt;
        }
    }
}
