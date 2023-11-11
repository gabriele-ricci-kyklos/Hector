using Hector.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Hector.Data.Queries
{
    public interface IQueryBuilder
    {
        public string Query { get; }
        public SqlParameter[] Parameters { get; }

        public IQueryBuilder SetQuery(string query);
    }

    public class QueryBuilder : IQueryBuilder
    {
        private readonly IAsyncDaoHelper _asyncDaoHelper;
        private readonly string? _schema;
        private readonly List<SqlParameter> _parameters;
        private readonly Dictionary<string, string> _sqlFuncMapping;

        private string _rawQuery = string.Empty;

        public QueryBuilder(IAsyncDaoHelper asyncDaoHelper, string? schema)
        {
            _asyncDaoHelper = asyncDaoHelper;
            _schema = schema;
            _parameters = new();

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

        public SqlParameter[] Parameters => _parameters.ToArray();

        public IQueryBuilder SetQuery(string query)
        {
            _rawQuery = query;
            return this;
        }

        private string PrepareQuery(string query)
        {
            string workedQuery = ResolveQueryFields(query);
            workedQuery = ResolveQueryTables(workedQuery);
            workedQuery = ResolveQueryFunctions(workedQuery);
            workedQuery = ResolveQuerySequences(workedQuery);
            workedQuery = ResolveQueryStoredProcedures(workedQuery);
            return workedQuery;
        }

        private string ResolveQueryFunctions(string query) => ResolveStandardQueryPlaceholders(query, @"\$FN\{([^\}]*)\}", true, ResolveFunctionPlaceholder);
        private string ResolveQueryTables(string query) => ResolveStandardQueryPlaceholders(query, @"\$T\{([0-9A-Z_\sa-z]+)\}", true, ResolveTablePlaceholder);
        private string ResolveQueryFields(string query) => ResolveStandardQueryPlaceholders(query, @"\$F\{([0-9A-Z_\sa-z]+)\}", true, ResolveFieldPlaceholder);
        private string ResolveQuerySequences(string query) => ResolveStandardQueryPlaceholders(query, @"\$S\{([0-9A-Z_\sa-z]+)\}", true, ResolveSequencePlaceholder);
        private string ResolveQueryStoredProcedures(string query) => ResolveStandardQueryPlaceholders(query, @"\$SP\{([0-9A-Z_\sa-z]+)\}", true, ResolveTablePlaceholder);

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
                placeholderValue = _asyncDaoHelper.EscapeFieldName(placeholderValue);
            }

            output
                .Append(placeholderValue);
        }

        private void ResolveTablePlaceholder(string placeholderValue, bool shouldEscapeName, StringBuilder output)
        {
            if (shouldEscapeName)
            {
                placeholderValue = _asyncDaoHelper.EscapeFieldName(placeholderValue);
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
                    .Split(",")
                    .Select(x => x.Trim())
                    .ToArray();

            string? funcStr = _sqlFuncMapping.GetValueOrDefault(funcName);
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
                placeholderValue = _asyncDaoHelper.EscapeFieldName(placeholderValue);
            }

            placeholderValue = $"{_schema}.{placeholderValue}";

            string? funcStr = string.Format(_asyncDaoHelper.SequenceValue, placeholderValue);

            output
                .Append(funcStr);
        }
    }
}
