using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualDrive.Extensions;
using VirtualDrive.Internal;

namespace VirtualDrive.Tests
{
    [TestClass]
    public class A2_ReadWriteTests
    {
        [TestInitialize]
        public void Init()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        }

        [TestMethod]
        public void WriteReadTest()
        {
            using (var f = new TestPhysicalFile())
            using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
            {
                var testString = "TestContent1234567890";
                var bytes = Encoding.UTF8.GetBytes(testString);
                var length = bytes.Length;

                using (var stream = fs.GetRoot().CreateFile("TestFile.txt").Open(FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    Assert.IsTrue(stream.CanWrite, "Stream does not allow writing");
                    stream.Write(bytes, 0, bytes.Length);
                    Assert.AreEqual(length, stream.Position, "Stream position is wrong");
                }

                using (var stream = fs.GetRoot().GetFiles().First().Open(FileMode.Open, FileAccess.Read))
                {
                    Assert.AreEqual(length, stream.Length, "Stream length is wrong");
                    var buffer = new byte[4096];
                    var readBytesCount = stream.Read(buffer, 0, (int)stream.Length);
                    Assert.AreEqual(length, readBytesCount, "Wrong bytes number is read");
                    var readBytes = buffer.Take(readBytesCount).ToArray();
                    var readString = Encoding.UTF8.GetString(readBytes);
                    Assert.AreEqual(testString, readString, "Read string is wrong");
                }
            }
        }

        [TestMethod]
        public void WriteReopenReadTest()
        {
            using (var f = new TestPhysicalFile())
            {
                var testString = "TestContent1234567890";
                var bytes = Encoding.UTF8.GetBytes(testString);
                var length = bytes.Length;
                using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
                {

                    using (var stream = fs.GetRoot().CreateFile("TestFile.txt")
                        .Open(FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        Assert.IsTrue(stream.CanWrite, "Stream does not allow writing");
                        stream.Write(bytes, 0, bytes.Length);
                        Assert.AreEqual(length, stream.Position, "Stream position is wrong");
                    }
                }

                using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
                {
                    using (var stream = fs.GetRoot().GetFiles().First().Open(FileMode.Open, FileAccess.Read))
                    {
                        Assert.AreEqual(length, stream.Length, "Stream length is wrong");
                        Assert.IsFalse(stream.CanWrite, "Stream should not allow writing");
                        var buffer = new byte[4096];
                        var readBytesCount = stream.Read(buffer, 0, (int)stream.Length);
                        Assert.AreEqual(length, readBytesCount, "Wrong bytes number is read");
                        var readBytes = buffer.Take(readBytesCount).ToArray();
                        var readString = Encoding.UTF8.GetString(readBytes);
                        Assert.AreEqual(testString, readString, "Read string is wrong");
                    }
                }
            }
        }

        [TestMethod]
        public async Task CopyFileTest()
        {
            using (var f = new TestPhysicalFile())
            {
                var testString = "1234567890";
                var dirAname = "dir_A";
                var dirBname = "dir_B";
                var filename = "testfile.txt";
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
                {
                    var file = fs.GetRoot().CreateDirectory(dirAname).CreateFile(filename);
                    file.WriteAllText(testString);
                    var dirB = fs.GetRoot().CreateDirectory(dirBname);
                    await file.CopyTo(dirB, args => { }, CancellationToken.None);
                }

                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
                {
                    var dirA = fs.GetRoot().GetDirectories(false, dirAname).FirstOrDefault();
                    Assert.IsNotNull(dirA, "Created directory A not found");
                    var dirB = fs.GetRoot().GetDirectories(false, dirBname).FirstOrDefault();
                    Assert.IsNotNull(dirB, "Created directory B not found");

                    var aFiles = dirA.GetFiles().ToArray();
                    var bFiles = dirB.GetFiles().ToArray();
                    Assert.AreEqual(1, aFiles.Length, "Dir A should contain 1 file");
                    Assert.AreEqual(1, bFiles.Length, "Dir B should contain 1 file");

                    var aFileName = VirtualPath.GetFileName(aFiles[0].Name);
                    var bFileName = VirtualPath.GetFileName(bFiles[0].Name);
                    Assert.AreEqual(bFileName, aFileName, "File names mismatch");

                    var AfileContent = aFiles[0].ReadAllText();
                    var BfileContent = bFiles[0].ReadAllText();
                    Assert.AreEqual(testString, AfileContent, "File A content mismatch");
                    Assert.AreEqual(testString, BfileContent, "File B content mismatch");

                    aFiles[0].WriteAllText("!!!!");
                    AfileContent = aFiles[0].ReadAllText();
                    Assert.AreEqual("!!!!", AfileContent, "File A content mismatch");
                    BfileContent = bFiles[0].ReadAllText();
                    Assert.AreEqual(testString, BfileContent, "File B content depends of file A content");
                }
            }
        }

        [TestMethod]
        public async Task MoveFileTest()
        {
            using (var f = new TestPhysicalFile())
            {
                var testString = "1234567890";
                var dirAname = "dir_A";
                var dirBname = "dir_B";
                var filename = "testfile.txt";
                using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
                {
                    var file = fs.GetRoot().CreateDirectory(dirAname).CreateFile(filename);
                    file.WriteAllText(testString);
                    var dirB = fs.GetRoot().CreateDirectory(dirBname);
                    await file.MoveTo(dirB, args => { }, CancellationToken.None);
                }

                using (var fs = new InternalFileSystem(SynchronizationContext.Current, f.Filename, VirtualDriveParameters.Default))
                {
                    var dirA = fs.GetRoot().GetDirectories(false, dirAname).FirstOrDefault();
                    Assert.IsNotNull(dirA, "Created directory A not found");
                    var dirB = fs.GetRoot().GetDirectories(false, dirBname).FirstOrDefault();
                    Assert.IsNotNull(dirB, "Created directory B not found");

                    var aFiles = dirA.GetFiles().ToArray();
                    var bFiles = dirB.GetFiles().ToArray();
                    Assert.AreEqual(0, aFiles.Length, "Dir A should not contain any files");
                    Assert.AreEqual(1, bFiles.Length, "Dir B should contain 1 file");

                    var bFileName = VirtualPath.GetFileName(bFiles[0].Name);
                    Assert.AreEqual(filename, bFileName, "File names mismatch");
                    var BfileContent = bFiles[0].ReadAllText();
                    Assert.AreEqual(testString, BfileContent, "File B content mismatch");
                }
            }
        }
    }
}