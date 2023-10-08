namespace Hector.Data.SqlServer
{
    public class SqlServerAsyncDaoHelper : IAsyncDaoHelper
    {
        public string ParameterStartPrefix => "@";

        public string EscapeField(string fieldName) => $"[{fieldName}]";
    }
}
