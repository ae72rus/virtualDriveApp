using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualDrive.Internal;

namespace VirtualDrive.Tests
{
    [TestClass]
    public class A0_FileSystemTests
    {
        [TestInitialize]
        public void Init()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        }

        [TestMethod]
        public void T01_InitializeTest()
        {
            using (var f = new TestPhysicalFile())
            using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
            {
                Assert.IsTrue(File.Exists(f.Filename), "FS file was not created");
                var file = new FileInfo(f.Filename);
                Assert.IsTrue(file.Length > 0, "FS file is empty");
            }
        }

        [TestMethod]
        public void T02_RootTest()
        {
            using (var f = new TestPhysicalFile())
            using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
            {
                var root = fs.GetRoot();
                Assert.IsNotNull(root, "Unable to get root directory");
                Assert.AreEqual(string.Empty, root.Name, "Root directory has wrong name");
            }
        }

        [TestMethod]
        public void T03_ReopenTest()
        {
            using (var f = new TestPhysicalFile())
            {
                using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
                {
                }

                using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
                {
                    var root = fs.GetRoot();
                    Assert.IsNotNull(root, "Unable to get root directory");
                    Assert.AreEqual(string.Empty, root.Name, "Root directory has wrong name");
                }
            }
        }

        [TestMethod]
        public void T04_CreateFileTest()
        {
            using (var f = new TestPhysicalFile())
            using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
            {
                var file = fs.GetRoot().CreateFile("testfile.txt");
                Assert.IsNotNull(file, "Unable to create a file");
                Assert.AreEqual($"{VirtualPath.Separator}testfile.txt", file.Name, "Created file has wrong name");
            }
        }

        [TestMethod]
        public void T05_CreateFilesAndReopenTest()
        {
            using (var f = new TestPhysicalFile())
            {
                using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
                {
                    for (var i = 0; i < 100; i++)
                        fs.GetRoot().CreateFile($"testfile{i}.txt");
                }

                using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
                {
                    var files = fs.GetRoot().GetFiles().ToList();
                    Assert.AreEqual(100, files.Count, "Unable to get saved files");
                    for (var i = 0; i < 100; i++)
                    {
                        var file = files[i];
                        Assert.AreEqual($"{VirtualPath.Separator}testfile{i}.txt", file.Name, "Created file has wrong name");
                    }
                }
            }
        }

        [TestMethod]
        public void T06_CreateDirectoryTest()
        {
            using (var f = new TestPhysicalFile())
            using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
            {
                var directory = fs.GetRoot().CreateDirectory("testDirectory");
                Assert.IsNotNull(directory, "Unable to create a directory");
                Assert.AreEqual($"{VirtualPath.Separator}testDirectory", directory.Name, "Created directory has wrong name");
            }
        }

        [TestMethod]
        public void T07_CreateDirectoriesAndReopenTest()
        {
            using (var f = new TestPhysicalFile())
            {
                using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
                {
                    for (var i = 0; i < 100; i++)
                        fs.GetRoot().CreateDirectory($"testDirectory{i}");
                }

                using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
                {
                    var directories = fs.GetRoot().GetDirectories().ToList();
                    Assert.AreEqual(100, directories.Count, "Unable to get saved directories");
                    for (var i = 0; i < 100; i++)
                    {
                        var directory = directories[i];
                        Assert.AreEqual($"{VirtualPath.Separator}testDirectory{i}", directory.Name, "Created directory has wrong name");
                    }
                }
            }
        }

        [TestMethod]
        public void T08_RenameFileTest()
        {
            using (var f = new TestPhysicalFile())
            using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
            {
                var file = fs.GetRoot().CreateFile("testfile.txt");
                file.Name = "renamedfile.txt";
                Assert.AreEqual($"{VirtualPath.Separator}renamedfile.txt", file.Name, "Created file has wrong name");
            }
        }

        [TestMethod]
        public void T09_RenameFileReopenTest()
        {
            using (var f = new TestPhysicalFile())
            {
                using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
                {
                    var file = fs.GetRoot().CreateFile("testfile.txt");
                    file.Name = "renamedfile.txt";
                }

                using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
                {
                    var file = fs.GetRoot().GetFiles().FirstOrDefault();
                    Assert.IsNotNull(file, "Unable to get saved file");

                    Assert.AreEqual($"{VirtualPath.Separator}renamedfile.txt", file.Name, "Saved file has wrong name");
                }
            }
        }

        [TestMethod]
        public void T10_RenameDirectoryTest()
        {
            using (var f = new TestPhysicalFile())
            using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
            {
                var directory = fs.GetRoot().CreateDirectory("testDirectory");
                directory.Name = "renamedDirectory";
                Assert.AreEqual($"{VirtualPath.Separator}renamedDirectory", directory.Name, "Created directory has wrong name");
            }
        }

        [TestMethod]
        public void T11_RenameDirectoryReopenTest()
        {
            using (var f = new TestPhysicalFile())
            {
                using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
                {
                    var directory = fs.GetRoot().CreateDirectory("testDirectory");
                    directory.Name = "renamedDirectory";
                }

                using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
                {
                    var directory = fs.GetRoot().GetDirectories().FirstOrDefault();
                    Assert.IsNotNull(directory, "Unable to get saved directory");

                    Assert.AreEqual($"{VirtualPath.Separator}renamedDirectory", directory.Name, "Saved directory has wrong name");
                }
            }
        }
    }
}
