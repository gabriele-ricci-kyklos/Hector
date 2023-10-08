namespace Hector.Data
{
    public interface IAsyncDaoHelper
    {
        string ParameterStartPrefix { get; }
        string EscapeField(string fieldName);
    }
}
