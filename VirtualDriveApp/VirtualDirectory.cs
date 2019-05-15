using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VirtualDrive.Internal;

namespace VirtualDrive
{
    public class VirtualDirectory : BaseVirtualEntity
    {
        public override string Name
        {
            get => FileSystem.GetDirectoryName(Id);
            set => FileSystem.RenameDirectory(this, value);
        }

        internal VirtualDirectory(InternalFileSystem fileSystem, DirectoryEntry entry)
            : base(fileSystem, entry)
        {
        }

        public VirtualFile CreateFile(string name)
        {
            return FileSystem.CreateFile(this, name);
        }

        public VirtualDirectory CreateDirectory(string name)
        {
            return FileSystem.CreateDirectory(this, name);
        }

        public IEnumerable<VirtualFile> GetFiles(bool recursive = false, string matchPattern = null)
        {
            return FileSystem.GetDirectoryFiles(this, recursive, matchPattern);
        }

        public IEnumerable<VirtualDirectory> GetDirectories(bool recursive = false, string matchPattern = null)
        {
            return FileSystem.GetNestedDirectories(this, recursive, matchPattern);
        }

        public async Task<VirtualDirectory> CopyTo(VirtualDirectory targetDirectory, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken)
        {
            return await FileSystem.CopyDirectory(this, targetDirectory, progressCallback, cancellationToken);
        }

        public async Task<VirtualDirectory> MoveTo(VirtualDirectory targetDirectory, Action<ProgressArgs> progressCallback,
            CancellationToken cancellationToken)
        {
            return await FileSystem.MoveDirectory(this, targetDirectory, progressCallback, cancellationToken);
        }

        public VirtualDirectoryWatcher GetWatcher()
        {
            return FileSystem.Watch(this);
        }

        public void Remove()
        {
            FileSystem.Remove(this);
        }
    }
}