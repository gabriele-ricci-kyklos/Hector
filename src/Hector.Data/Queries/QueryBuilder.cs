using Hector.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

namespace Hector.Data.Queries
{
    public interface IQueryBuilder
    {
        public string Query { get; }
        public SqlParameter[] Parameters { get; }
    }

    public class QueryBuilder : IQueryBuilder
    {
        private readonly IAsyncDaoHelper _asyncDaoHelper;
        private readonly List<SqlParameter> _parameters;
        private readonly Dictionary<string, string> _sqlFuncMapping;

        public QueryBuilder(IAsyncDaoHelper asyncDaoHelper)
        {
            _asyncDaoHelper = asyncDaoHelper;
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

        public string Query => PrepareQuery();

        public SqlParameter[] Parameters => _parameters.ToArray();

        private string PrepareQuery()
        {
            throw new NotImplementedException();
        }

        public string ResolveQueryFunctions(string query)
        {
            StringBuilder output = new(query.Length);
            int cursor = 0;
            MatchCollection matches = Regex.Matches(query, @"\$FN\{([^\}]*)\}");
            foreach (Match match in matches.Cast<Match>())
            {
                if (!match.Success || match.Groups.Count != 2)
                {
                    continue;
                }

                output.Append(query[cursor..match.Index]);

                string funcGroupValue = match.Groups[1].Value;
                string[] tokens = funcGroupValue.Split('(');
                if (tokens.Length != 2)
                {
                    continue;
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
                        .Append(") ");
                }
                else
                {
                    output
                        .Append(string.Format(funcStr, funcArgs))
                        .Append(" ");
                }

                cursor = match.Index + match.Length;
            }

            if (cursor < query.Length)
            {
                output.Append(query[cursor..].Trim());
            }

            return output.ToString();
        }
    }
}
