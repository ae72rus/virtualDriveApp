using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualDrive.Extensions;
using VirtualDrive.Internal;

namespace VirtualDrive.Tests
{
    [TestClass]
    public partial class A3_ApiTests
    {
        [TestInitialize]
        public void Init()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        }

        private IVirtualFileSystem getEmptyApi(TestPhysicalFile physicalFile)
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            var retv = new VirtualFileSystem(physicalFile.Filename);

            return retv;
        }

        private IVirtualFileSystem getTestApi(TestPhysicalFile physicalFile)
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            var retv = new VirtualFileSystem(physicalFile.Filename);
            retv.CreateDirectory("A1/B1/C1");
            retv.CreateDirectory("A2/B2/C2");

            retv.CreateFile("A1/a1.txt").WriteAllText("0000");
            retv.CreateFile("A1/B1/b1.txt").WriteAllText("1111");
            retv.CreateFile("A1/B1/C1/c1.dat").WriteAllText("2222");

            retv.CreateFile("A2/a2.txt").WriteAllText("3333");
            retv.CreateFile("A2/B2/b2.txt").WriteAllText("4444");
            retv.CreateFile("A2/B2/C2/c2.dat").WriteAllText("5555");

            return retv;
        }

        [TestMethod]
        public void T01_ApiGetRootTest()
        {
            using (var f = new TestPhysicalFile())
            {
                //check on created FS
                using (var api = getEmptyApi(f))
                {
                    var root = api.GetRootDirectory();
                    var rootFromPath = api.GetDirectory(null);
                    Assert.IsNotNull(root);
                    Assert.IsNotNull(rootFromPath);
                    Assert.AreEqual(rootFromPath, root);
                }

                //check on opened FS
                using (var api = getEmptyApi(f))
                {
                    var root = api.GetRootDirectory();
                    var rootFromPath = api.GetDirectory(null);
                    Assert.IsNotNull(root);
                    Assert.IsNotNull(rootFromPath);
                    Assert.AreEqual(rootFromPath, root);
                }
            }
        }

        [TestMethod]
        public void T02_ApiCreateDirectoryTest()
        {
            using (var f = new TestPhysicalFile())
            {
                var directoryName = "TestDirectory";
                //check on created FS
                using (var api = getEmptyApi(f))
                {
                    api.CreateDirectory(directoryName);
                    var nestedDirs = api.GetRootDirectory().GetDirectories().ToArray();
                    Assert.AreEqual(1, nestedDirs.Length);
                    Assert.AreEqual($"{VirtualPath.Separator}{directoryName}", nestedDirs[0].Name);
                }

                //check on opened FS
                using (var api = getEmptyApi(f))
                {
                    var nestedDirs = api.GetRootDirectory().GetDirectories().ToArray();
                    Assert.AreEqual(1, nestedDirs.Length);
                    Assert.AreEqual($"{VirtualPath.Separator}{directoryName}", nestedDirs[0].Name);
                }
            }
        }

        [TestMethod]
        public void T03_ApiCreateDirectoryRecursiveTest()
        {
            using (var f = new TestPhysicalFile())
            {
                var directoryName_L1 = "TestDirectory1";
                var directoryName_L2 = "TestDirectory2";
                var directoryName_L3 = "TestDirectory3";
                //check on created FS
                using (var api = getEmptyApi(f))
                {
                    api.CreateDirectory($"{directoryName_L1}/{directoryName_L2}/{directoryName_L3}");
                    var nestedDir_L1 = api.GetRootDirectory().GetDirectories().FirstOrDefault();
                    Assert.IsNotNull(nestedDir_L1);
                    var nestedDir_L2 = nestedDir_L1.GetDirectories().FirstOrDefault();
                    Assert.IsNotNull(nestedDir_L2);
                    var nestedDir_L3 = nestedDir_L2.GetDirectories().FirstOrDefault();
                    Assert.IsNotNull(nestedDir_L3);

                    Assert.AreEqual($"/{directoryName_L1}", nestedDir_L1.Name);
                    Assert.AreEqual($"/{directoryName_L1}/{directoryName_L2}", nestedDir_L2.Name);
                    Assert.AreEqual($"/{directoryName_L1}/{directoryName_L2}/{directoryName_L3}", nestedDir_L3.Name);
                }

                //check on opened FS
                using (var api = getEmptyApi(f))
                {
                    var nestedDir_L1 = api.GetRootDirectory().GetDirectories().FirstOrDefault();
                    Assert.IsNotNull(nestedDir_L1);
                    var nestedDir_L2 = nestedDir_L1.GetDirectories().FirstOrDefault();
                    Assert.IsNotNull(nestedDir_L2);
                    var nestedDir_L3 = nestedDir_L2.GetDirectories().FirstOrDefault();
                    Assert.IsNotNull(nestedDir_L3);

                    Assert.AreEqual($"/{directoryName_L1}", nestedDir_L1.Name);
                    Assert.AreEqual($"/{directoryName_L1}/{directoryName_L2}", nestedDir_L2.Name);
                    Assert.AreEqual($"/{directoryName_L1}/{directoryName_L2}/{directoryName_L3}", nestedDir_L3.Name);
                }
            }
        }

        [TestMethod]
        public void T04_ApiCreateFileTest()
        {
            using (var f = new TestPhysicalFile())
            {
                var directoryName = "TestDirectory";
                var fileName = "TestFile.txt";
                var fileContent = "test content";
                //check on created FS
                using (var api = getEmptyApi(f))
                {
                    api.CreateDirectory(directoryName);
                    var file = api.CreateFile($"/{directoryName}/{fileName}");
                    file.WriteAllText(fileContent);
                }

                //check on opened FS
                using (var api = getEmptyApi(f))
                {
                    var testDirectory = api.GetRootDirectory().GetDirectories().FirstOrDefault();
                    Assert.IsNotNull(testDirectory);
                    var testFile = testDirectory.GetFiles().FirstOrDefault();
                    Assert.IsNotNull(testFile);

                    var readContent = testFile.ReadAllText();

                    Assert.AreEqual($"/{directoryName}/{fileName}", testFile.Name);
                    Assert.AreEqual(fileContent, readContent);
                }
            }
        }

        [TestMethod]
        public void T05_ApiRenameTest()
        {
            using (var f = new TestPhysicalFile())
            {
                var initialDirectoryName = "TestDirectory";
                var initialFileName = "TestFile.txt";
                var finalDirectoryName = "RenamedTestDirectory";
                var finalFileName = "RenamedTestFile.txt";
                var fileContent = "test content";
                //check on created FS
                using (var api = getEmptyApi(f))
                {
                    api.CreateDirectory(initialDirectoryName);
                    var file = api.CreateFile($"/{initialDirectoryName}/{initialFileName}");
                    file.WriteAllText(fileContent);

                    api.RenameDirectory(initialDirectoryName, finalDirectoryName);
                    api.RenameFile($"{finalDirectoryName}/{initialFileName}", $"{finalDirectoryName}/{finalFileName}");
                }

                //check on opened FS
                using (var api = getEmptyApi(f))
                {
                    var testDirectory = api.GetRootDirectory().GetDirectories().FirstOrDefault();
                    Assert.IsNotNull(testDirectory);
                    var testFile = testDirectory.GetFiles().FirstOrDefault();
                    Assert.IsNotNull(testFile);

                    var readContent = testFile.ReadAllText();

                    Assert.AreEqual($"/{finalDirectoryName}/{finalFileName}", testFile.Name);
                    Assert.AreEqual(fileContent, readContent);
                }
            }
        }

        [TestMethod]
        public async Task T06_ApiCopyFileTest()
        {
            using (var f = new TestPhysicalFile())
            {
                var initialDirectoryName = "TestDirectory";
                var initialFileName = "TestFile.txt";
                var finalDirectoryName = "RenamedTestDirectory";
                var finalFileName = "RenamedTestFile.txt";
                var fileContent = "test content";
                //check on created FS
                using (var api = getEmptyApi(f))
                {
                    api.CreateDirectory(initialDirectoryName);
                    api.CreateDirectory(finalDirectoryName);
                    var file = api.CreateFile($"/{initialDirectoryName}/{initialFileName}");
                    file.WriteAllText(fileContent);

                    await api.CopyFile($"{initialDirectoryName}/{initialFileName}", $"{finalDirectoryName}/{finalFileName}",
                          args => { }, CancellationToken.None);
                }

                //check on opened FS
                using (var api = getEmptyApi(f))
                {
                    var testDirectory = api.GetRootDirectory().GetDirectories().FirstOrDefault(x => x.Name == $"/{finalDirectoryName}");
                    Assert.IsNotNull(testDirectory);
                    var testFile = testDirectory.GetFiles().FirstOrDefault();
                    Assert.IsNotNull(testFile);

                    var readContent = testFile.ReadAllText();

                    Assert.AreEqual($"/{finalDirectoryName}/{finalFileName}", testFile.Name);
                    Assert.AreEqual(fileContent, readContent);
                }
            }
        }

        [TestMethod]
        public async Task T07_ApiCopyDirectoryTest()
        {
            using (var f = new TestPhysicalFile())
            {
                var initialContainerDir = "initial";
                var finalContainerDir = "final";
                var initialDirectoryName = "TestDirectory";
                var finalDirectoryName = "RenamedTestDirectory";
                var filename = "file.txt";
                var fileContent = "test content";
                //check on created FS
                using (var api = getEmptyApi(f))
                {
                    api.CreateDirectory($"{initialContainerDir}/{initialDirectoryName}");
                    api.CreateDirectory(finalContainerDir);
                    var file = api.CreateFile($"/{initialContainerDir}/{initialDirectoryName}/{filename}");
                    file.WriteAllText(fileContent);

                    await api.CopyDirectory($"{initialContainerDir}/{initialDirectoryName}",
                                            $"{finalContainerDir}/{finalDirectoryName}",
                                              args => { },
                                              CancellationToken.None);
                }

                //check on opened FS
                using (var api = getEmptyApi(f))
                {
                    var initialFile = api.GetRootDirectory().GetDirectories()
                        .FirstOrDefault(x => x.Name == $"/{initialContainerDir}")
                        ?.GetDirectories().FirstOrDefault()
                        ?.GetFiles().FirstOrDefault();

                    Assert.IsNotNull(initialFile);
                    initialFile.WriteAllText("!!!"); // change initial file's content to make sure we dealing with different files

                    var containerDir = api.GetRootDirectory().GetDirectories().FirstOrDefault(x => x.Name == $"/{finalContainerDir}");
                    Assert.IsNotNull(containerDir);
                    var copiedDir = containerDir.GetDirectories()
                        .FirstOrDefault(x => x.Name == $"/{finalContainerDir}/{finalDirectoryName}");
                    Assert.IsNotNull(copiedDir);
                    var testFile = copiedDir.GetFiles().FirstOrDefault();
                    Assert.IsNotNull(testFile);

                    var readContent = testFile.ReadAllText();

                    Assert.AreEqual($"/{finalContainerDir}/{finalDirectoryName}/{filename}", testFile.Name);
                    Assert.AreEqual(fileContent, readContent);
                }
            }
        }

        [TestMethod]
        public async Task T08_ApiMoveFileTest()
        {
            using (var f = new TestPhysicalFile())
            {
                var initialDirectoryName = "TestDirectory";
                var initialFileName = "TestFile.txt";
                var finalDirectoryName = "RenamedTestDirectory";
                var finalFileName = "RenamedTestFile.txt";
                var fileContent = "test content";
                //check on created FS
                using (var api = getEmptyApi(f))
                {
                    api.CreateDirectory(initialDirectoryName);
                    api.CreateDirectory(finalDirectoryName);
                    var file = api.CreateFile($"/{initialDirectoryName}/{initialFileName}");
                    file.WriteAllText(fileContent);

                    await api.MoveFile($"{initialDirectoryName}/{initialFileName}", $"{finalDirectoryName}/{finalFileName}",
                        args => { }, CancellationToken.None);
                }

                //check on opened FS
                using (var api = getEmptyApi(f))
                {
                    var initialDirectory = api.GetRootDirectory().GetDirectories().FirstOrDefault(x => x.Name == $"/{initialDirectoryName}");
                    Assert.IsNotNull(initialDirectory);
                    var initialFile = initialDirectory.GetFiles().FirstOrDefault();
                    Assert.IsNull(initialFile);

                    var testDirectory = api.GetRootDirectory().GetDirectories().FirstOrDefault(x => x.Name == $"/{finalDirectoryName}");
                    Assert.IsNotNull(testDirectory);
                    var testFile = testDirectory.GetFiles().FirstOrDefault();
                    Assert.IsNotNull(testFile);

                    var readContent = testFile.ReadAllText();

                    Assert.AreEqual($"/{finalDirectoryName}/{finalFileName}", testFile.Name);
                    Assert.AreEqual(fileContent, readContent);
                }
            }
        }

        [TestMethod]
        public async Task T09_ApiMoveDirectoryTest()
        {
            using (var f = new TestPhysicalFile())
            {
                var initialContainerDir = "initial";
                var finalContainerDir = "final";
                var initialDirectoryName = "TestDirectory";
                var finalDirectoryName = "RenamedTestDirectory";
                var filename = "file.txt";
                var fileContent = "test content";
                //check on created FS
                using (var api = getEmptyApi(f))
                {
                    api.CreateDirectory($"{initialContainerDir}/{initialDirectoryName}");
                    api.CreateDirectory(finalContainerDir);
                    var file = api.CreateFile($"/{initialContainerDir}/{initialDirectoryName}/{filename}");
                    file.WriteAllText(fileContent);

                    await api.MoveDirectory($"{initialContainerDir}/{initialDirectoryName}",
                                            $"{finalContainerDir}/{finalDirectoryName}",
                                              args => { },
                                              CancellationToken.None);
                }

                //check on opened FS
                using (var api = getEmptyApi(f))
                {
                    var initialFile = api.GetRootDirectory().GetDirectories()
                        .FirstOrDefault(x => x.Name == $"/{initialContainerDir}")
                        ?.GetDirectories().FirstOrDefault()
                        ?.GetFiles().FirstOrDefault();

                    Assert.IsNull(initialFile);

                    var containerDir = api.GetRootDirectory().GetDirectories().FirstOrDefault(x => x.Name == $"/{finalContainerDir}");
                    Assert.IsNotNull(containerDir);
                    var copiedDir = containerDir.GetDirectories()
                        .FirstOrDefault(x => x.Name == $"/{finalContainerDir}/{finalDirectoryName}");
                    Assert.IsNotNull(copiedDir);
                    var testFile = copiedDir.GetFiles().FirstOrDefault();
                    Assert.IsNotNull(testFile);

                    var readContent = testFile.ReadAllText();

                    Assert.AreEqual($"/{finalContainerDir}/{finalDirectoryName}/{filename}", testFile.Name);
                    Assert.AreEqual(fileContent, readContent);
                }
            }
        }

        [TestMethod]
        public void T10_ApiFilesSearchTest()
        {
            using (var f = new TestPhysicalFile())
            {
                using (var api = getTestApi(f))
                {
                    var found = api.FindFiles(true, "*").ToList();
                    Assert.AreEqual(6, found.Count);
                }
            }
        }

        [TestMethod]
        public void T11_ApiFilesSearchTest()
        {
            using (var f = new TestPhysicalFile())
            using (var api = getTestApi(f))
            {
                var found = api.FindFiles(true, "*.tXt").ToList();
                Assert.AreEqual(4, found.Count);
            }
        }

        [TestMethod]
        public void T12_ApiFilesSearchTest()
        {
            using (var f = new TestPhysicalFile())
            using (var api = getTestApi(f))
            {
                var found = api.FindFiles(true, "?1.*").ToList();
                Assert.AreEqual(3, found.Count);
            }
        }

        [TestMethod]
        public void T13_ApiDirectoriesSearchTest()
        {
            using (var f = new TestPhysicalFile())
            {
                using (var api = getTestApi(f))
                {
                    var found = api.FindDirectories(true, "*").ToList();
                    Assert.AreEqual(6, found.Count);
                }
            }
        }

        [TestMethod]
        public void T14_ApiDirectoriesSearchTest()
        {
            using (var f = new TestPhysicalFile())
            using (var api = getTestApi(f))
            {
                var found = api.FindDirectories(true, "?1").ToList();
                Assert.AreEqual(3, found.Count);
            }
        }

        [TestMethod]
        public void T15_ApiDirectoriesSearchTest()
        {
            using (var f = new TestPhysicalFile())
            using (var api = getTestApi(f))
            {
                var found = api.FindDirectories(true, "a*").ToList();
                Assert.AreEqual(2, found.Count);
            }
        }

        [TestMethod]
        public void T16_ApiDeleteFileTest()
        {
            using (var f = new TestPhysicalFile())
            using (var api = getTestApi(f))
            {
                var allFiles = api.FindFiles(true, "*").ToList();
                Assert.AreEqual(6, allFiles.Count);
                api.DeleteFile("a1/b1/c1/c1.dat");
                allFiles = api.FindFiles(true, "*").ToList();
                Assert.AreEqual(5, allFiles.Count);
            }
        }

        [TestMethod]
        public void T17_ApiDeleteDirectoryTest()
        {
            using (var f = new TestPhysicalFile())
            using (var api = getTestApi(f))
            {
                var allDirectories = api.FindDirectories(true, "*").ToList();
                var allFiles = api.FindFiles(true, "*").ToList();
                Assert.AreEqual(6, allDirectories.Count);
                Assert.AreEqual(6, allFiles.Count);

                api.DeleteDirectory("a1");

                allDirectories = api.FindDirectories(true, "*").ToList();
                allFiles = api.FindFiles(true, "*").ToList();
                Assert.AreEqual(3, allDirectories.Count);
                Assert.AreEqual(3, allFiles.Count);
            }
        }

        [TestMethod]
        public async Task T18_ApiImportFileTest()
        {
            var importFileName = "testImport.txt";
            var imnportFileContent = "1234567890";
            File.Create(importFileName).Dispose();
            File.WriteAllText(importFileName, imnportFileContent);
            using (var f = new TestPhysicalFile())
            using (var api = getEmptyApi(f))
            {
                var fileInfo = new FileInfo(importFileName);
                await api.ImportFile(fileInfo, "", args => { }, CancellationToken.None);
                var importedFile = api.GetRootDirectory().GetFiles().FirstOrDefault();
                Assert.IsNotNull(importedFile);
                var importedContent = importedFile.ReadAllText();
                Assert.AreEqual(imnportFileContent, importedContent);
            }

            File.Delete(importFileName);
        }

        [TestMethod]
        public async Task T19_ApiImportFileTest()
        {
            using (var f = new TestPhysicalFile())
            using (var f1 = new TestPhysicalFile())
            using (var srcApi = getTestApi(f))
            using (var api = getEmptyApi(f1))
            {
                var fileToImport = srcApi.GetFile("a1/a1.txt");
                await api.ImportFile(fileToImport, "", args => { }, CancellationToken.None);

                var importedFile = api.GetRootDirectory().GetFiles().FirstOrDefault();
                Assert.IsNotNull(importedFile);
                var importedContent = importedFile.ReadAllText();
                Assert.AreEqual("0000", importedContent);
            }
        }

        [TestMethod]
        public async Task T20_ApiImportDirectoryTest()
        {
            var importDirectoryName = "importDirectory";
            Directory.CreateDirectory($"{importDirectoryName}\\nestedDirectory");
            File.Create($"{importDirectoryName}\\0.txt").Dispose();
            File.Create($"{importDirectoryName}\\nestedDirectory\\1.txt").Dispose();
            File.WriteAllText($"{importDirectoryName}\\0.txt", "1234567890");
            File.WriteAllText($"{importDirectoryName}\\nestedDirectory\\1.txt", "0987654321");
            using (var f = new TestPhysicalFile())
            using (var api = getEmptyApi(f))
            {
                var directoryToImport = new DirectoryInfo("importDirectory");
                await api.ImportDirectory(directoryToImport, "", args => { }, CancellationToken.None);

                var file0 = api.GetRootDirectory()
                    .GetDirectories(false, "importDirectory")
                    .FirstOrDefault()
                    ?.GetFiles(false, "0.txt")
                    .FirstOrDefault();

                Assert.IsNotNull(file0);

                var file1 = api.GetRootDirectory()
                    .GetDirectories(false, "importDirectory")
                    .FirstOrDefault()
                    ?.GetDirectories(false, "nestedDirectory")
                    .FirstOrDefault()
                    ?.GetFiles(false, "1.txt")
                    .FirstOrDefault();

                Assert.IsNotNull(file1);

                Assert.AreEqual("1234567890", file0.ReadAllText());
                Assert.AreEqual("0987654321", file1.ReadAllText());
            }

            Directory.Delete($"{importDirectoryName}\\nestedDirectory", true);
        }

        [TestMethod]
        public async Task T21_ApiImportDirectoryTest()
        {
            using (var f = new TestPhysicalFile())
            using (var f1 = new TestPhysicalFile())
            using (var srcApi = getTestApi(f))
            using (var api = getEmptyApi(f1))
            {
                var directoryToImport = srcApi.GetDirectory("a1");
                await api.ImportDirectory(directoryToImport, "", args => { }, CancellationToken.None);

                var dirs = api.FindDirectories(true, "*").ToList();
                foreach (var dir in dirs)
                {
                    var dirName = VirtualPath.GetFileName(dir.Name);
                    var file = dir.GetFiles(false, $"{dirName}.???").FirstOrDefault();
                    Assert.IsNotNull(file);
                    if (dirName.Equals("a1", StringComparison.InvariantCultureIgnoreCase))
                        Assert.AreEqual("0000", file.ReadAllText());
                    else if (dirName.Equals("b1", StringComparison.InvariantCultureIgnoreCase))
                        Assert.AreEqual("1111", file.ReadAllText());
                    else if (dirName.Equals("c1", StringComparison.InvariantCultureIgnoreCase))
                        Assert.AreEqual("2222", file.ReadAllText());
                }
            }
        }
    }
}