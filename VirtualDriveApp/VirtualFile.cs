using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VirtualDrive.Internal;

namespace VirtualDrive
{
    public class VirtualFile : BaseVirtualEntity
    {
        public override string Name
        {
            get => FileSystem.GetFileName(Id);
            set => FileSystem.RenameFile(this, value);
        }

        public long Length
        {
            get => FileSystem.GetFileLength(this);
            set => FileSystem.SetFileLength(this, value);
        }

        internal VirtualFile(InternalFileSystem fileSystem, FileEntry entry)
            : base(fileSystem, entry)
        {

        }

        public Stream Open(FileMode mode, FileAccess access)
        {
            return FileSystem.OpenFile(this, mode, access);
        }

        public async Task<VirtualFile> MoveTo(VirtualDirectory targetDirectory, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken)
        {
            return await FileSystem.MoveFile(this, targetDirectory, progressCallback, cancellationToken);
        }

        public async Task<VirtualFile> CopyTo(VirtualDirectory targetDirectory, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken)
        {
            return await FileSystem.CopyFile(this, targetDirectory, progressCallback, cancellationToken);
        }

        public void Remove()
        {
            FileSystem.Remove(this);
        }
    }
}