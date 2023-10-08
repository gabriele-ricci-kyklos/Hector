namespace Hector.Data.Oracle
{
    public class OracleAsyncDaoHelper : IAsyncDaoHelper
    {
        public string ParameterStartPrefix => ":";

        public string EscapeField(string fieldName) => $"\"{fieldName}\"";
    }
}
