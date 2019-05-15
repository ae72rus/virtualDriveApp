using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VirtualDrive.Tests
{
    [TestClass]
    public class A1_VirtualPathTests
    {
        [TestMethod]
        public void T1_CombieTest()
        {
            var part1 = "foo";
            var part2 = "bar";

            var path = VirtualPath.Combine(part1, part2);
            Assert.AreEqual("foo/bar", path);
        }

        [TestMethod]
        public void T2_CombieTest()
        {
            var part1 = "foo/";
            var part2 = "/bar";

            var path = VirtualPath.Combine(part1, part2);
            Assert.AreEqual("foo/bar", path);
        }

        [TestMethod]
        public void T3_CombieTest()
        {
            var part1 = "/foo/";
            var part2 = "/bar/";

            var path = VirtualPath.Combine(part1, part2);
            Assert.AreEqual("foo/bar", path);
        }

        [TestMethod]
        public void T4_GetFilenameTest()
        {
            var path = "foo/bar/someFile.dat";
            var filename = VirtualPath.GetFileName(path);
            Assert.AreEqual("someFile.dat", filename);
        }

        [TestMethod]
        public void T5_GetDirectoryNameTest()
        {
            var path = "foo/bar/someFile.dat";
            var filename = VirtualPath.GetDirectoryName(path);
            Assert.AreEqual("foo/bar", filename);
        }

        [TestMethod]
        public void T6_GetFileExtensionTest()
        {
            var path = "foo/bar/someFile.dat";
            var extension = VirtualPath.GetFileExtension(path);
            Assert.AreEqual("dat", extension);
        }

        [TestMethod]
        public void T7_GetFilenameWithoutExtensionTest()
        {
            var path = "foo/bar/someFile.dat";
            var filename = VirtualPath.GetFileNameWithoutExtension(path);
            Assert.AreEqual("someFile", filename);
        }
    }
}