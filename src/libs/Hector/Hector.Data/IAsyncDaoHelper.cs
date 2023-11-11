namespace Hector.Data
{
    public interface IAsyncDaoHelper
    {
        string ParameterPrefix { get; }
        string StringConcatOperator { get; }
        string EscapeLeftStr { get; }
        string EscapeRightStr { get; }
        string SubstringFunction { get; }
        string TrimStartFunction { get; }
        string TrimEndFunction { get; }
        string TrimFunction { get; }
        string UpperFunction { get; }
        string LowerFunction { get; }
        string LengthFunction { get; }
        string ReplaceFunction { get; }
        string IsNullFunction { get; }
        string DummyTableName { get; }
        abstract string SequenceValue { get; }

        string EscapeFieldName(string fieldName);
    }
}
