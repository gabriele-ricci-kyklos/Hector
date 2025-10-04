using System;

namespace Hector.Excel
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class ExcelFieldAttribute : Attribute
    {
        public string ColumnName { get; set; }
        public short Order { get; set; }

        public ExcelFieldAttribute(short order)
            : this(string.Empty, order)
        {
        }

        public ExcelFieldAttribute(string columnName)
            : this(columnName, -1)
        {
        }

        public ExcelFieldAttribute(string columnName, short order)
        {
            if (columnName.IsNullOrBlankString() && order < 0)
            {
                throw new NotSupportedException("Invalid values provided");
            }

            ColumnName = columnName;
            Order = order;
        }
    }
}
