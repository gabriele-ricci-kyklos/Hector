namespace Hector.Data
{
    public abstract class BaseAsyncDaoHelper : IAsyncDaoHelper
    {
        public abstract string ParameterPrefix { get; }

        public abstract string StringConcatOperator { get; }

        public abstract string EscapeLeftStr { get; }

        public abstract string EscapeRightStr { get; }

        public abstract string SubstringFunction { get; }

        public abstract string TrimStartFunction { get; }

        public abstract string TrimEndFunction { get; }

        public abstract string TrimFunction { get; }

        public abstract string UpperFunction { get; }

        public abstract string LowerFunction { get; }

        public abstract string LengthFunction { get; }

        public abstract string ReplaceFunction { get; }

        public abstract string IsNullFunction { get; }

        public abstract string SequenceValue { get; }

        public virtual string DummyTableName => string.Empty;

        public string EscapeFieldName(string? fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return string.Empty;
            }

            fieldName = fieldName.Trim();

            if (!fieldName.StartsWith(EscapeLeftStr))
            {
                fieldName = EscapeLeftStr + fieldName;
            }
            if (!fieldName.EndsWith(EscapeRightStr))
            {
                fieldName += EscapeRightStr;
            }

            return fieldName;
        }
    }
}
