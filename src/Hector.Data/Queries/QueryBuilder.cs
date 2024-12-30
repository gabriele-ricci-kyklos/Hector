using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Hector.Data.Queries
{
    public record SqlParameter(Type Type, string Name, object Value);

    public interface IQueryBuilder
    {
        string Query { get; }
        SqlParameter[] Parameters { get; }

        IQueryBuilder SetQuery(string query);
        IQueryBuilder AddParam(SqlParameter param);
        IQueryBuilder AddParam(Type type, string name, object value);
        IQueryBuilder AddParam<T>(string name, T value) where T : notnull;
        string GetQueryWithReplacedParameters();
    }

    internal class QueryBuilder : IQueryBuilder
    {
        private readonly IAsyncDaoHelper _asyncDaoHelper;
        private readonly string? _schema;
        private readonly Dictionary<string, SqlParameter> _parameters;
        private readonly Dictionary<string, string> _sqlFuncMapping;

        private string _rawQuery = string.Empty;

        internal QueryBuilder(IAsyncDaoHelper asyncDaoHelper, string? schema, SqlParameter[]? parameters = null)
        {
            _asyncDaoHelper = asyncDaoHelper;
            _schema = schema;
            _parameters = parameters?.ToDictionary(x => x.Name) ?? [];

            _sqlFuncMapping =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    { "substring", _asyncDaoHelper.SubstringFunction },
                    { "ltrim", _asyncDaoHelper.TrimStartFunction },
                    { "rtrim", _asyncDaoHelper.TrimEndFunction },
                    { "trim", _asyncDaoHelper.TrimFunction },
                    { "upper", _asyncDaoHelper.UpperFunction },
                    { "lower", _asyncDaoHelper.LowerFunction },
                    { "length", _asyncDaoHelper.LengthFunction },
                    { "replace", _asyncDaoHelper.ReplaceFunction },
                    { "isnull", _asyncDaoHelper.IsNullFunction }
                };
        }

        public string Query => PrepareQuery(_rawQuery);

        public SqlParameter[] Parameters => _parameters.Values.ToArray();

        public IQueryBuilder SetQuery(string query)
        {
            _rawQuery = query;
            return this;
        }

        public IQueryBuilder AddParam(SqlParameter param)
        {
            _parameters.Add(param.Name, param);
            return this;
        }

        public IQueryBuilder AddParam(Type type, string name, object value) => AddParam(new(type, name, value));

        public IQueryBuilder AddParam<T>(string name, T value) where T : notnull => AddParam(new(typeof(T), name, value));

        public string GetQueryWithReplacedParameters()
        {
            string query = PrepareQuery(_rawQuery);
            query = ResolveQueryParameterValues(query);
            return query;
        }

        private string PrepareQuery(string query)
        {
            string workedQuery = ResolveQueryFields(query);
            workedQuery = ResolveQueryTables(workedQuery);
            workedQuery = ResolveQueryFunctions(workedQuery);
            workedQuery = ResolveQuerySequences(workedQuery);
            workedQuery = ResolveQueryStoredProcedures(workedQuery);
            workedQuery = ResolveQueryParameters(workedQuery);
            return workedQuery;
        }

        private string ResolveQueryFunctions(string query) => ResolveStandardQueryPlaceholders(query, @"\$FN\{([^\}]*)\}", true, ResolveFunctionPlaceholder);
        private string ResolveQueryTables(string query) => ResolveStandardQueryPlaceholders(query, @"\$T\{([0-9A-Z_\sa-z]+)\}", true, ResolveTablePlaceholder);
        private string ResolveQueryFields(string query) => ResolveStandardQueryPlaceholders(query, @"\$F\{([0-9A-Z_\sa-z]+)\}", true, ResolveFieldPlaceholder);
        private string ResolveQuerySequences(string query) => ResolveStandardQueryPlaceholders(query, @"\$S\{([0-9A-Z_\sa-z]+)\}", true, ResolveSequencePlaceholder);
        private string ResolveQueryStoredProcedures(string query) => ResolveStandardQueryPlaceholders(query, @"\$SP\{([0-9A-Z_\sa-z]+)\}", true, ResolveTablePlaceholder);
        private string ResolveQueryParameters(string query) => ResolveStandardQueryPlaceholders(query, @"\$P\{([0-9A-Z_\sa-z]+)\}", true, ResolveParameterPlaceholder);
        private string ResolveQueryParameterValues(string query) => ResolveStandardQueryPlaceholders(query, @$"\{_asyncDaoHelper.ParameterPrefix}([0-9A-Z_\sa-z]+)", true, ResolveParameterValuePlaceholder);

        private string ResolveStandardQueryPlaceholders(string query, string pattern, bool shouldEscapeName, Action<string, bool, StringBuilder> predicate)
        {
            StringBuilder output = new(query.Length);
            int cursor = 0;
            MatchCollection matches = Regex.Matches(query, pattern);
            foreach (Match match in matches.Cast<Match>())
            {
                if (!match.Success || match.Groups.Count != 2)
                {
                    continue;
                }

                output.Append(query[cursor..match.Index]);

                string placeholderValue = match.Groups[1].Value.Trim();

                predicate(placeholderValue, shouldEscapeName, output);

                cursor = match.Index + match.Length;
            }

            if (cursor < query.Length)
            {
                output.Append(query[cursor..]);
            }

            return output.ToString();
        }

        private void ResolveFieldPlaceholder(string placeholderValue, bool shouldEscapeName, StringBuilder output)
        {
            if (shouldEscapeName)
            {
                placeholderValue = _asyncDaoHelper.EscapeValue(placeholderValue);
            }

            output
                .Append(placeholderValue);
        }

        private void ResolveTablePlaceholder(string placeholderValue, bool shouldEscapeName, StringBuilder output)
        {
            if (shouldEscapeName)
            {
                placeholderValue = _asyncDaoHelper.EscapeValue(placeholderValue);
            }

            output
                .Append(_schema)
                .Append('.')
                .Append(placeholderValue);
        }

        private void ResolveFunctionPlaceholder(string placeholderValue, bool shouldEscapeName, StringBuilder output)
        {
            string[] tokens = placeholderValue.Split('(');
            if (tokens.Length != 2)
            {
                return;
            }

            string funcName = tokens[0];
            string[] funcArgs =
                tokens[1]
                    .Remove(tokens[1].Length - 1)
                    .Split(',')
                    .Select(x => x.Trim())
                    .ToArray();

            string? funcStr = _sqlFuncMapping.NetCoreGetValueOrDefault(funcName);
            if (funcStr.IsNullOrBlankString())
            {
                funcStr = funcName;

                output
                    .Append(funcStr)
                    .Append("(")
                    .Append(funcArgs.StringJoin(", "))
                    .Append(")");
            }
            else
            {
                output
                    .Append(string.Format(funcStr, funcArgs));
            }
        }

        private void ResolveSequencePlaceholder(string placeholderValue, bool shouldEscapeName, StringBuilder output)
        {
            if (shouldEscapeName)
            {
                placeholderValue = _asyncDaoHelper.EscapeValue(placeholderValue);
            }

            placeholderValue = $"{_schema}.{placeholderValue}";

            string? funcStr = string.Format(_asyncDaoHelper.SequenceValue, placeholderValue);

            output
                .Append(funcStr);
        }

        private void ResolveParameterPlaceholder(string placeholderValue, bool shouldEscapeName, StringBuilder output)
        {
            if (!_parameters.ContainsKey(placeholderValue.Trim()))
            {
                throw new NotSupportedException($"No sql parameters proveded for param {placeholderValue}");
            }

            output
                .Append(_asyncDaoHelper.ParameterPrefix)
                .Append(placeholderValue);
        }

        private void ResolveParameterValuePlaceholder(string placeholderValue, bool shouldEscapeName, StringBuilder output)
        {
            if (!_parameters.ContainsKey(placeholderValue.Trim()))
            {
                throw new NotSupportedException($"No sql parameters proveded for param {placeholderValue}");
            }

            output
                .Append(_parameters[placeholderValue].Value);
        }
    }
}
