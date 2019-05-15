using System;
using System.IO;

namespace VirtualDrive.Tests
{
    public class TestPhysicalFile : IDisposable
    {
        private static int _fileIdx;
        public string Filename { get; } = $"testDrive{_fileIdx++}.vdd";

        public TestPhysicalFile()
        {
            cleanup();
        }

        private void cleanup()
        {
            if (File.Exists(Filename))
                File.Delete(Filename);
        }

        public void Dispose()
        {
            cleanup();
        }
    }
}