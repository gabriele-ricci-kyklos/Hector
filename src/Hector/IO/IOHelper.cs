using System;
using System.IO;
using System.Linq;

namespace Hector.IO
{
    public static class IOHelper
    {
        public static string GetDirectoryName(string path) => Path.GetDirectoryName(path) ?? throw new DirectoryNotFoundException("No directory retrieved");

        public static void DeleteFolders(string path, string[]? dirsToDelete = null, string[]? pathTokensToExclude = null, bool deleteFiles = false)
        {
            foreach (string dir in Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories))
            {
                DirectoryInfo di = new(dir);
                if (pathTokensToExclude is not null && FindDirectories(di, pathTokensToExclude))
                {
                    continue;
                }

                string dirName = di.Name;
                if (dirsToDelete is null || dirsToDelete.Contains(dirName, StringComparer.OrdinalIgnoreCase))
                {
                    di.Delete(true);
                }
            }

            if (!deleteFiles)
            {
                return;
            }

            foreach (string file in Directory.EnumerateFiles(path))
            {
                File.Delete(file);
            }
        }

        private static bool FindDirectories(DirectoryInfo di, string[] directoryNames)
        {
            bool found = false;
            while (di.Parent is not null)
            {
                if (directoryNames.Contains(di.Name, StringComparer.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
                di = di.Parent;
            }
            return found;
        }
    }
}
