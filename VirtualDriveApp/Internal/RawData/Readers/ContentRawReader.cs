using System.Collections.Generic;
using System.Threading;
using VirtualDrive.Internal.Drive.Operations;
using VirtualDrive.Internal.RawData.Threading;

namespace VirtualDrive.Internal.RawData.Readers
{
    internal class ContentRawReader : BaseRawReader
    {
        private int _length;

        public ContentRawReader(DriveAccessSynchronizer synchronizer, long initialPosition, int length)
            : base(synchronizer, initialPosition)
        {
            _length = length;
        }

        public void SetInitialPosition(long position)
        {
            InitialPosition = position;
        }

        public void SetLength(int length)
        {
            _length = length;
        }

        public override bool CheckCanRead(int count)
        {
            if (Length == -1)
                return CurrentPosition + count <= Synchronizer.GetDriveLength();

            return CurrentPosition + count <= InitialPosition + Length;
        }

        public ReadOperation Read(IEnumerable<DriveBlock> blocks, byte[] buffer, int offset, CancellationToken cancellationToken)
        {
            var operation = new ReadOperation((drive, token) =>
            {
                if (token.IsCancellationRequested)
                    return 0;

                var readBytesLength = 0;
                var currentOffset = offset;
                foreach (var block in blocks)
                {
                    if (token.IsCancellationRequested)
                        return 0;

                    drive.Position = block.Position;
                    var read = drive.Read(buffer, currentOffset, block.Length);
                    currentOffset += read;
                    readBytesLength += block.Length;
                }

                return readBytesLength;
            }, cancellationToken);

            Synchronizer.EnqueueOperation(operation);
            return operation;
        }

        protected override int GetLength()
        {
            return _length;
        }
    }
}