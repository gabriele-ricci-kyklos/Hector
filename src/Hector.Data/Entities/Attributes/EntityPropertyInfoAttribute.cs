using System;

namespace Hector.Data.Entities.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class EntityPropertyInfoAttribute : Attribute
    {
        public virtual bool IsNullable { get; set; }
        public virtual string ColumnName { get; set; } = null!;
        public virtual bool IsPrimaryKey { get; set; }

        public virtual PropertyDbType DbType { get; set; }

        private int _maxLen = -1;
        public virtual int MaxLength
        {
            get { return _maxLen; }
            set
            {
                if (DbType != PropertyDbType.String && value > 0)
                {
                    throw new NotSupportedException($"{nameof(MaxLength)} cannot be used with db type different from String. Field name: {ColumnName}. DbType: {DbType}");
                }
                _maxLen = value;
            }
        }

        private int _numPrecision = 0;
        public virtual int NumericPrecision
        {
            get { return _numPrecision; }
            set
            {
                if (value > 0 && DbType != PropertyDbType.Numeric)
                {
                    throw new NotSupportedException($"{nameof(NumericPrecision)} can be used only with db type {PropertyDbType.Numeric}. Field name: {ColumnName}. DbType: {DbType}");
                }
                _numPrecision = value;
            }
        }

        private int _numScale = 0;
        public virtual int NumericScale
        {
            get { return _numScale; }
            set
            {
                if (value > 0 && DbType != PropertyDbType.Numeric)
                {
                    throw new NotSupportedException($"{nameof(NumericScale)} can be used only with db type {PropertyDbType.Numeric}. Field name: {ColumnName}. DbType: {DbType}");
                }
                _numScale = value;
            }
        }

        public string? Note { get; set; }

        public short ColumnOrder { get; set; }
    }
}
