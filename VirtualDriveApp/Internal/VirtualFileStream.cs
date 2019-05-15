using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VirtualDrive.Internal
{
    internal class VirtualFileStream : Stream
    {
#if !DEBUG
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] //no one should see my secrets :)
#endif
        private readonly InternalFileSystem _fileSystem;
#if !DEBUG
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] //no one should see my secrets :)
#endif
        private readonly FileEntry _fileEntry;

        private readonly FileMode _mode;
        private readonly FileAccess _access;
        private bool _isDisposed;
        private long _position;
        private readonly VirtualFile _file;

        public VirtualFile File
        {
            get
            {
                checkDisposed();
                return _file;
            }
        }

#if !DEBUG
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] //no one should see my secrets :)
#endif
        internal ICollection<Locker.LockerOperation> LockerOperations { get; }

        public override bool CanRead => _access.HasFlag(FileAccess.Read) && Position < Length - 1;
        public override bool CanSeek { get; } = true;
        public override bool CanWrite => _access.HasFlag(FileAccess.Write);
        public override long Length
        {
            get
            {
                checkDisposed();
                return _fileSystem.GetFileLength(File);
            }
        }

        public override long Position
        {
            get
            {
                checkDisposed();
                return _position;
            }
            set
            {
                checkDisposed();
                _position = value;
            }
        }

        internal VirtualFileStream(InternalFileSystem fileSystem, FileEntry fileEntry, VirtualFile file, ICollection<Locker.LockerOperation> lockerOperations, FileMode mode, FileAccess access)
        {
            LockerOperations = lockerOperations;
            _fileSystem = fileSystem;
            _fileEntry = fileEntry;
            _mode = mode;
            _access = access;
            _file = file;
            init();
        }

        private void init()
        {
            switch (_mode)
            {
                case FileMode.Create:
                case FileMode.CreateNew:
                    checkWritePermition();
                    SetLength(0);
                    break;
                case FileMode.Open:
                    checkReadPermition();
                    break;
                case FileMode.OpenOrCreate:
                    checkReadPermition();
                    checkWritePermition();
                    break;
                case FileMode.Truncate:
                    checkWritePermition();
                    SetLength(0);
                    break;
                case FileMode.Append:
                    checkWritePermition();
                    Position = Length;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void checkWritePermition()
        {
            if (!_access.HasFlag(FileAccess.Write))
                throw new AccessViolationException();
        }

        private void checkReadPermition()
        {
            if (!_access.HasFlag(FileAccess.Read) && !_access.HasFlag(FileAccess.Write))
                throw new AccessViolationException();
        }

        public override void Flush()
        {

        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            checkDisposed();
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
            }

            if (Position - Length < 0)
                throw new ArgumentException(nameof(offset));

            return Position;
        }

        public override void SetLength(long value)
        {
            checkDisposed();
            _fileSystem.SetFileLength(_fileEntry, value).Wait();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {//IO operation is being running at separate thread
            checkDisposed();
            return ReadAsync(buffer, offset, count, CancellationToken.None).Result;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {//IO operation is being running at separate thread
            checkDisposed();
            WriteAsync(buffer, offset, count, CancellationToken.None).Wait();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            checkDisposed();
            checkReadPermition();
            checkArgs(buffer, count, offset);
            var realCount = Length - Position < count ? (int)(Length - Position) : count;
            return _fileSystem.Read(_fileEntry, Position, buffer, offset, realCount, cancellationToken)
                .ContinueWith(x =>
                {
                    if (x.IsCompleted && !cancellationToken.IsCancellationRequested)
                        Position += x.Result;
                    return x.Result;
                }, cancellationToken);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            checkDisposed();
            checkWritePermition();
            checkArgs(buffer, count, offset);
            handleLength(count);

            _fileEntry.ModificationTime = DateTime.Now;

            return _fileSystem.Write(_fileEntry, Position, buffer, offset, count)
                .ContinueWith(x =>
                {
                    if (x.IsCompleted && !cancellationToken.IsCancellationRequested)
                        Position += count;
                }, cancellationToken);
        }

        private void checkArgs(byte[] buffer, int count, int offset)
        {
            if (offset + count > buffer.Length)
                throw new ArgumentException();
            if (count < 0 || count < 0)
                throw new IndexOutOfRangeException();
        }

        private void handleLength(int count)
        {
            if (Position + count <= Length)
                return;

            var newLength = Position + count;
            SetLength(newLength);
        }

        private void checkDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("Stream is disposed");
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            base.Dispose(disposing);
            foreach (var lockerOperation in LockerOperations)
                lockerOperation.Dispose();
        }
    }
}
