using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hector.Core.Support
{
    public class IOUtilities
    {
        //guidelines: https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
        public static bool CopyFolderTo(string sourcePath, string destPath, bool overwiteFiles = false, bool copySubDirectories = true)
        {
            sourcePath.AssertHasText(nameof(sourcePath));
            destPath.AssertHasText(nameof(destPath));

            DirectoryInfo sourceDir = new DirectoryInfo(sourcePath);
            DirectoryInfo destDir = new DirectoryInfo(destPath);

            if (!sourceDir.Exists)
            {
                throw new DirectoryNotFoundException($"The directory {sourcePath} has not been found");
            }

            if (!destDir.Exists)
            {
                destDir.Create();
            }

            IList<Task<bool>> taskList = new List<Task<bool>>();

            foreach (FileInfo fi in sourceDir.EnumerateFiles())
            {
                taskList
                    .Add
                    (
                        Task.Run(() =>
                        {
                            try
                            {
                                string tempPath = Path.Combine(destDir.FullName, fi.Name);
                                fi.CopyTo(tempPath, overwiteFiles);
                                return true;
                            }
                            catch (Exception)
                            {
                                return false;
                            }
                        })
                    );
            }

            if (copySubDirectories)
            {
                foreach (DirectoryInfo di in sourceDir.EnumerateDirectories())
                {
                    taskList
                        .Add
                        (
                            Task.Run(() =>
                            {
                                try
                                {
                                    string tempPath = Path.Combine(destPath, di.Name);
                                    CopyFolderTo(di.FullName, tempPath, overwiteFiles, true);
                                    return true;
                                }
                                catch (Exception)
                                {
                                    return false;
                                }
                            })
                        );
                }
            }

            Task.WaitAll(taskList.ToArray());

            if (taskList.Any(x => !x.Result))
            {
                return false;
            }

            return true;
        }

        //credits: https://stackoverflow.com/a/50405099/4499267
        public static bool EmptyFolder(string pathName)
        {
            pathName.AssertHasText(nameof(pathName));

            bool errors = false;
            DirectoryInfo dir = new DirectoryInfo(pathName);

            if (!dir.Exists)
            {
                return false;
            }

            foreach (FileInfo fi in dir.EnumerateFiles())
            {
                try
                {
                    fi.IsReadOnly = false;
                    fi.Delete();

                    //Wait for the item to disapear (avoid 'dir not empty' error).
                    while (fi.Exists)
                    {
                        System.Threading.Thread.Sleep(10);
                        fi.Refresh();
                    }
                }
                catch (Exception)
                {
                    errors = true;
                }
            }

            IList<DirectoryInfo> diList = new List<DirectoryInfo>();
            IList<Task<bool>> taskList = new List<Task<bool>>();

            foreach (DirectoryInfo di in dir.EnumerateDirectories())
            {
                taskList.Add(Task.Run(() => EmptyFolder(di.FullName)));
                diList.Add(di);
            }

            Task.WaitAll(taskList.ToArray());

            foreach (DirectoryInfo di in diList)
            {
                try
                {
                    di.Delete();

                    //Wait for the item to disapear (avoid 'dir not empty' error).
                    while (di.Exists)
                    {
                        System.Threading.Thread.Sleep(10);
                        di.Refresh();
                    }
                }
                catch (Exception)
                {
                    errors = true;
                }
            }

            if (taskList.Any(x => !x.Result))
            {
                errors = true;
            }

            return !errors;
        }

        public static bool DeleteFolder(string pathName)
        {
            if (!EmptyFolder(pathName))
            {
                return false;
            }

            try
            {
                Directory.Delete(pathName, true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
