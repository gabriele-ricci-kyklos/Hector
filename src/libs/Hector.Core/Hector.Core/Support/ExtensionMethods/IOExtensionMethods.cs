using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Hector.Core.Support
{
    public static class IOExtensionMethods
    {
        public static string CombinePaths(this string s, params string[] otherPaths)
        {
            List<string> paths = new List<string> { s };

            if (!otherPaths.IsNullOrEmptyList())
            {
                paths.AddRange(otherPaths);
            }

            return Path.Combine(paths.Where(x => !x.IsNullOrBlankString()).ToArray());
        }
    }
}
