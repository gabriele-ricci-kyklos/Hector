using Hector.Data.Entities;
using Hector.Data.Entities.Attributes;

namespace Hector.Tests.Data
{
    [EntityInfo(Alias = "b", TableName = "BULK")]
    public class BulkItem : IBaseEntity
    {
        [EntityPropertyInfo(ColumnName = "ID", DbType = PropertyDbType.Long, IsNullable = false, IsPrimaryKey = true, ColumnOrder = 10)]
        public long Id { get; set; }

        [EntityPropertyInfo(ColumnName = "CODE", DbType = PropertyDbType.String, MaxLength = 5, IsNullable = false, ColumnOrder = 20)]
        public string? Code { get; set; }
    }

    public class Result
    {
        [EntityPropertyInfo(ColumnName = "JobID", DbType = PropertyDbType.Long, IsNullable = false, ColumnOrder = 10)]
        public long JobId2 { get; set; }

        [EntityPropertyInfo(ColumnName = "MemberID", DbType = PropertyDbType.Long, IsNullable = false, ColumnOrder = 20)]
        public long MemberId { get; set; }

        [EntityPropertyInfo(ColumnName = "DateOfWork", DbType = PropertyDbType.DateTime, IsNullable = false, ColumnOrder = 30)]
        public DateTime DateOfWork { get; set; }

        [EntityPropertyInfo(ColumnName = "AmountTimeToInvoice", DbType = PropertyDbType.Decimal, IsNullable = true, ColumnOrder = 40)]
        public decimal? AmountTimeToInvoice { get; set; }

        [EntityPropertyInfo(ColumnName = "FreeAmountTime", DbType = PropertyDbType.Decimal, IsNullable = true, ColumnOrder = 50)]
        public decimal? FreeAmountTime { get; set; }

        [EntityPropertyInfo(ColumnName = "TimeNote", DbType = PropertyDbType.String, IsNullable = true, MaxLength = 4000, ColumnOrder = 60)]
        public string? TimeNote { get; set; }

        [EntityPropertyInfo(ColumnName = "ReasonID", DbType = PropertyDbType.Integer, IsNullable = false, ColumnOrder = 70)]
        public int ReasonId { get; set; }

        [EntityPropertyInfo(ColumnName = "Hours", DbType = PropertyDbType.Decimal, IsNullable = true, ColumnOrder = 80)]
        public decimal? Hours { get; set; }

        [EntityPropertyInfo(ColumnName = "LastChangeDate", DbType = PropertyDbType.DateTime, IsNullable = true, ColumnOrder = 90)]
        public DateTime? LastChangeDate { get; set; }

        [EntityPropertyInfo(ColumnName = "JobTimeId", DbType = PropertyDbType.Long, IsNullable = false, IsPrimaryKey = true, ColumnOrder = 100)]
        public long JobTimeId { get; set; }
    }
}
