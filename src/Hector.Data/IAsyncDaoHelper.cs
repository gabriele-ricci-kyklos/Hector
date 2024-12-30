using Hector.Data.Entities;

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
        string SequenceValue { get; }

        string EscapeValue(string value);
        string BuildParameterName(string name);
        string BuildParameterName(int i);

        (int? Precision, int? Scale) GetNumericPrecision(EntityPropertyInfo entityPropertyInfo);
    }
}
