using Hector.Data;
using Hector.Data.Entities.Attributes;
using Hector.Data.Queries;
using Hector.Data.SqlServer;

namespace Hector.Tests.Data
{
    public class AsyncDaoTests
    {
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
            public string TimeNote { get; set; }

            [EntityPropertyInfo(ColumnName = "ReasonID", DbType = PropertyDbType.Integer, IsNullable = false, ColumnOrder = 70)]
            public int ReasonId { get; set; }

            [EntityPropertyInfo(ColumnName = "Hours", DbType = PropertyDbType.Decimal, IsNullable = true, ColumnOrder = 80)]
            public decimal? Hours { get; set; }

            [EntityPropertyInfo(ColumnName = "LastChangeDate", DbType = PropertyDbType.DateTime, IsNullable = true, ColumnOrder = 90)]
            public DateTime? LastChangeDate { get; set; }

            [EntityPropertyInfo(ColumnName = "JobTimeId", DbType = PropertyDbType.Long, IsNullable = false, IsPrimaryKey = true, ColumnOrder = 100)]
            public long JobTimeId { get; set; }
        }

        class Result2
        {
            public int ReasonID { get; set; }
        }

        [Fact]
        public async Task TestAsyncDao()
        {
            AsyncDaoOptions options = new("Data Source=kkritstgdb.kyklos.local;Initial Catalog=Remira_Dev_JEK;Persist Security Info=True;User Id=rit-stg-mix;Password=3VCjmnxQxJVnDQxpHg2s", "dbo", false);
            SqlServerAsyncDaoHelper daoHelper = new();
            SqlServerAsyncDao dao = new(options, daoHelper);

            IQueryBuilder queryBuilder =
                dao
                    .NewQueryBuilder()
                    .SetQuery("select JobID, MemberID from JobTimes");

            var results = await dao.ExecuteSelectQueryAsync<(long, long)>(queryBuilder);
        }
    }
}
