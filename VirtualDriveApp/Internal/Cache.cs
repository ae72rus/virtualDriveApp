using System;

namespace VirtualDrive.Internal
{
    internal class Cache : IDisposable
    {
        public NamesCache FileNames { get; } = new NamesCache();
        public NamesCache DirectoryNames { get; } = new NamesCache();
        public VirtualDirectoriesCache Directories { get; } = new VirtualDirectoriesCache();
        public VirtualFilesCache Files { get; } = new VirtualFilesCache();

        public void Dispose()
        {
            FileNames?.Dispose();
            DirectoryNames?.Dispose();
            Directories?.Dispose();
            Files?.Dispose();
        }
    }
}