namespace Hector.Data.SqlServer
{
    public class SqlServerAsyncDaoHelper : BaseAsyncDaoHelper
    {
        public override string ParameterPrefix => "@";

        public override string StringConcatOperator => "+";

        public override string EscapeLeftStr => "[";

        public override string EscapeRightStr => "]";

        public override string SubstringFunction => "substring({0}, {1}, {2})";

        public override string TrimStartFunction => "ltrim({0})";

        public override string TrimEndFunction => "rtrim({0})";

        public override string TrimFunction => "ltrim(rtrim({0}))";

        public override string UpperFunction => "upper({0})";

        public override string LowerFunction => "lower({0})";

        public override string LengthFunction => "len({0})";

        public override string ReplaceFunction => "replace({0}, {1})";

        public override string IsNullFunction => "isnull({0}, {1})";
    }
}
