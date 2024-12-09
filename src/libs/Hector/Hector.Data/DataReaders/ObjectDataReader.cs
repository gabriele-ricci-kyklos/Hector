using Hector.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Hector.Data.DataReaders
{
    // credits: https://stackoverflow.com/a/2258448/4499267

    public abstract class ObjectDataReader : IDataReader
    {
        protected readonly Type Type;
        protected readonly Dictionary<string, PropertyInfo> Members = [];
        protected readonly Dictionary<int, PropertyInfo> IndexedMembers = [];
        protected readonly string[] Names = [];

        protected bool Closed = false;

        protected ObjectDataReader(Type type)
        {
            Type = type;
            Members = GetMembers();
            IndexedMembers = GetIndexedMembers();
            Names = Members.Keys.ToArray();
        }

        protected virtual Dictionary<string, PropertyInfo> GetMembers() =>
            Type.GetHierarchicalOrderedPropertyList().ToDictionary(x => x.Name);

        protected virtual Dictionary<int, PropertyInfo> GetIndexedMembers() =>
            Members.Select((x, i) => (x.Value, Index: i)).ToDictionary(x => x.Index, x => x.Value);

        public abstract bool Read();
        public abstract object GetValue(int i);


        public int FieldCount => Members.Count;

        public object this[int i] => GetValue(i);

        public object this[string name] => Members[name];

        public int Depth => throw new NotSupportedException();

        public bool IsClosed => Closed;

        public int RecordsAffected => -1;



        public void Close() => Closed = true;

        public virtual void Dispose()
        {
        }

        public bool GetBoolean(int i) => (bool)GetValue(i);

        public byte GetByte(int i) => (byte)GetValue(i);

        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public char GetChar(int i) => (char)GetValue(i);

        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }

        public string GetDataTypeName(int i) => GetFieldType(i).Name;

        public DateTime GetDateTime(int i) => (DateTime)GetValue(i);

        public decimal GetDecimal(int i) => (decimal)GetValue(i);

        public double GetDouble(int i) => (double)GetValue(i);

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        public Type GetFieldType(int i) => IndexedMembers[i].PropertyType;

        public float GetFloat(int i) => (float)GetValue(i);

        public Guid GetGuid(int i) => (Guid)GetValue(i);

        public short GetInt16(int i) => (short)GetValue(i);

        public int GetInt32(int i) => (int)GetValue(i);

        public long GetInt64(int i) => (long)GetValue(i);

        public virtual string GetName(int i) => IndexedMembers[i].Name;

        public virtual int GetOrdinal(string name) => Array.IndexOf(Names, name);

        public virtual DataTable? GetSchemaTable()
        {
            DataTable dt = new();
            dt.BeginLoadData();

            foreach (PropertyInfo field in Members.Values)
            {
                dt.Columns.Add(new DataColumn(field.Name, field.PropertyType.GetNonNullableType()));
            }

            dt.EndLoadData();
            dt.AcceptChanges();

            return dt;
        }

        public string GetString(int i) => (string)GetValue(i);

        public int GetValues(object[] values)
        {
            int i = 0;
            for (; i < Members.Count; i++)
            {
                if (values.Length <= i)
                {
                    return i;
                }
                values[i] = GetValue(i);
            }
            return i;
        }

        public bool IsDBNull(int i) => GetValue(i) is null;

        public bool NextResult() => throw new NotSupportedException();
    }
}
