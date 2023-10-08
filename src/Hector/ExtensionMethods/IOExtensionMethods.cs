using System;
using System.IO;
using System.Linq;

namespace Hector.Core
{
    public static class IOExtensionMethods
    {
        public static string CombinePaths(this string s, params string[] otherPaths) =>
            Path.Combine(s.AsArray().Union(otherPaths.ToEmptyIfNull().Where(x => x.IsNotNullAndNotBlank())).ToArray());

        public static bool IsUncPath(this string path)
        {
            try
            {
                return new Uri(path).IsUnc;
            }
            catch (ArgumentNullException)
            {
                throw;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool HasNetworkDrive(this string path)
        {
            try
            {
                return new DriveInfo(path).DriveType == DriveType.Network;
            }
            catch (ArgumentNullException)
            {
                throw;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception)
            {
                return false;
            }
        }


        public static bool IsAbsolutePath(this string path)
        {
            if (path.IsNullOrBlankString() || path.IndexOfAny(Path.GetInvalidPathChars()) != -1 || !Path.IsPathRooted(path))
            {
                return false;
            }


            string pathRoot = Path.GetPathRoot(path);
            if (pathRoot.Length <= 2 && pathRoot != "/") // Accepts X:\ and \\UNC\PATH, rejects empty string, \ and X:, but accepts / to support Linux
            {
                return false;
            }

            if (pathRoot[0] != '\\' || pathRoot[1] != '\\')
            {
                return true; // Rooted and not a UNC path
            }

            return pathRoot.Trim('\\').IndexOf('\\') != -1; // A UNC server name without a share name (e.g "\\NAME" or "\\NAME\") is invalid
        }
    }
}
