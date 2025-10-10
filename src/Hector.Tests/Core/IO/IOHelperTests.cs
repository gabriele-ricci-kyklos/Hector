using FluentAssertions;
using Hector.IO;

namespace Hector.Tests.Core.IO
{
    public class IOHelperTests
    {
        [Fact]
        public void TestGetDirectoryName()
        {
            IOHelper.GetDirectoryName("C:\\MyDir").Should().NotBeNullOrWhiteSpace();
            var act = () => IOHelper.GetDirectoryName("C:\\");
            act.Should().Throw<DirectoryNotFoundException>();
        }

        [Fact]
        public void TestDeleteFolders()
        {
            const string baseDirPath = @"C:\Temp\Hector";

            Directory.CreateDirectory(baseDirPath);

            string dir1 = baseDirPath.CombinePaths("Folder1");
            string dir1a = dir1.CombinePaths("a");
            string dir1b = dir1.CombinePaths("b");
            string dir2 = baseDirPath.CombinePaths("Folder2");
            string dir2a = dir2.CombinePaths("a");

            Directory.CreateDirectory(dir1);
            Directory.CreateDirectory(dir1a);
            Directory.CreateDirectory(dir1b);

            IOHelper.DeleteFolders(baseDirPath, ["b"]);

            Directory.Exists(dir1a).Should().BeTrue();
            Directory.Exists(dir1b).Should().BeFalse();

            Directory.CreateDirectory(dir1);
            Directory.CreateDirectory(dir1a);
            Directory.CreateDirectory(dir2);
            Directory.CreateDirectory(dir2a);

            IOHelper.DeleteFolders(baseDirPath, ["a"], ["Folder1"]);

            Directory.Exists(dir1a).Should().BeTrue();
            Directory.Exists(dir2a).Should().BeFalse();

            string fileName = baseDirPath.CombinePaths("a.txt");
            File.WriteAllText(fileName, "a");

            IOHelper.DeleteFolders(baseDirPath, deleteFiles: true);

            File.Exists(fileName).Should().BeFalse();
            Directory.Exists(dir1).Should().BeFalse();

            Directory.Delete(baseDirPath);
        }
    }
}
