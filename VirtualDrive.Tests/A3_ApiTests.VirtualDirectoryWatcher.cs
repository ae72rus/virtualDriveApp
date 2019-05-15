using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VirtualDrive.Tests
{
    public partial class A3_ApiTests
    {
        [TestMethod]
        public async Task T22_ApiWatcherCreatedTest()
        {
            using (var f = new TestPhysicalFile())
            using (var api = getEmptyApi(f))
            {
                var root = api.GetRootDirectory();
                using (var watcher = api.Watch(root))
                {
                    VirtualDirectory directoryFromEvent = null;
                    VirtualFile fileFromEvent = null;
                    watcher.DirectoryEvent += (s, e) =>
                    {
                        if (e.Event == WatcherEvent.Created)
                            directoryFromEvent = e.Directory;
                    };

                    watcher.FileEvent += (s, e) =>
                    {
                        if (e.Event == WatcherEvent.Created)
                            fileFromEvent = e.File;
                    };

                    var createdDir = root.CreateDirectory("testDirectory");
                    var createdFile = root.CreateFile("testDirectory/testFile.txt");

                    await Task.Delay(50); // event requires some time to fire

                    Assert.AreEqual(createdDir, directoryFromEvent);
                    Assert.AreEqual(createdFile, fileFromEvent);
                }
            }
        }

        [TestMethod]
        public async Task T23_ApiWatcherUpdatedTest()
        {
            using (var f = new TestPhysicalFile())
            using (var api = getEmptyApi(f))
            {
                var root = api.GetRootDirectory();
                using (var watcher = api.Watch(root))
                {
                    VirtualDirectory directoryFromEvent = null;
                    VirtualFile fileFromEvent = null;
                    watcher.DirectoryEvent += (s, e) =>
                    {
                        if (e.Event == WatcherEvent.Updated)
                            directoryFromEvent = e.Directory;
                    };

                    watcher.FileEvent += (s, e) =>
                    {
                        if (e.Event == WatcherEvent.Updated)
                            fileFromEvent = e.File;
                    };

                    var createdDir = root.CreateDirectory("testDirectory");
                    var createdFile = root.CreateFile("testDirectory/testFile.txt");
                    createdDir.Name = "testDirectoryRenamed";
                    createdFile.Name = "testFileRenamed.txt";

                    await Task.Delay(50); // event requires some time to fire

                    Assert.AreEqual("/testDirectoryRenamed", createdDir.Name);
                    Assert.AreEqual("/testFileRenamed.txt", createdFile.Name);
                    Assert.AreEqual(createdDir, directoryFromEvent);
                    Assert.AreEqual(createdFile, fileFromEvent);
                }
            }
        }

        [TestMethod]
        public async Task T24_ApiWatcherDeletedTest()
        {
            using (var f = new TestPhysicalFile())
            using (var api = getEmptyApi(f))
            {
                var root = api.GetRootDirectory();
                using (var watcher = api.Watch(root))
                {
                    VirtualDirectory directoryFromEvent = null;
                    VirtualFile fileFromEvent = null;
                    watcher.DirectoryEvent += (s, e) =>
                    {
                        if (e.Event == WatcherEvent.Deleted)
                            directoryFromEvent = e.Directory;
                    };

                    watcher.FileEvent += (s, e) =>
                    {
                        if (e.Event == WatcherEvent.Deleted)
                            fileFromEvent = e.File;
                    };

                    var createdDir = root.CreateDirectory("testDirectory");
                    var createdFile = root.CreateFile("testDirectory/testFile.txt");
                    createdDir.Remove();
                    createdFile.Remove();

                    await Task.Delay(50); // event requires some time to fire

                    Assert.AreEqual(createdDir, directoryFromEvent);
                    Assert.AreEqual(createdFile, fileFromEvent);
                }
            }
        }

        [TestMethod]
        public async Task T25_ApiWatcherNameChangedTest()
        {
            using (var f = new TestPhysicalFile())
            using (var api = getEmptyApi(f))
            {
                var root = api.GetRootDirectory();
                var dir = root.CreateDirectory("testDirectory");
                using (var watcher = api.Watch(dir))
                {
                    var eventFired = false;
                    watcher.NameChanged += (s, e) => eventFired = true;

                    dir.Name = "testDirectoryRenamed";

                    await Task.Delay(50); // event requires some time to fire

                    Assert.IsTrue(eventFired);
                }
            }
        }
    }
}