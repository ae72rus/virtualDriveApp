using System;
using System.Collections.Generic;
using System.Linq;
using VirtualDrive.Internal.Drive.Operations;
using VirtualDrive.Internal.RawData.Threading;

namespace VirtualDrive.Internal.RawData.Writers
{
    internal class ContentRawWriter : OptimizedRawWriter
    {
        private volatile object _allocateLock = new object();

        public ContentRawWriter(VirtualDriveParameters virtualDriveParameters, DriveAccessSynchronizer synchronizer, long initialPosition)
            : base(virtualDriveParameters, synchronizer, initialPosition)
        {
        }

        protected override bool CheckCanWriteInternal(int bytesLength)
        {
            return InitialPosition + Length > CurrentPosition + bytesLength;
        }

        protected override WriteOperation MakeOperation(OperationHint hint, byte[] bytes, long position)
        {
            return new WriteOperation(drive => drive.Write(position, bytes), position);
        }

        public void SetCurrentPosition(long position)
        {
            CurrentPosition = position;
        }

        public void SetInitialPosition(long position)
        {
            InitialPosition = position;
            CurrentPosition = position;
        }

        public void SetLength(long length)
        {
            Length = length;
        }

        public WriteOperation Write(ICollection<DriveBlock> blocks, byte[] buffer, int offset)
        {
            var operation = new WriteOperation(drive =>
            {
                var retv = 0L;
                var currentOffset = offset;
                foreach (var block in blocks)
                {
                    drive.Position = block.Position;
                    drive.Write(buffer, currentOffset, block.Length);
                    currentOffset += block.Length;
                    retv += block.Length;
                }

                return retv;
            }, blocks.Last().Position);

            CurrentPosition = blocks.Last().Position + blocks.Last().Length;

            Synchronizer.EnqueueOperation(operation);
            return operation;
        }

        public IEnumerable<DriveBlock> AllocateSpace(int dataLength)
        {
            lock (_allocateLock)
            {
                var dataLeft = dataLength;
                while (dataLeft > 0)
                {
                    if (!TryGetAvailableBlock(dataLength, out var block))
                    {
                        CurrentPosition = InitialPosition + Length;
                        block = new DriveBlock
                        {
                            Length = dataLength,
                            Position = CurrentPosition
                        };

                        CurrentPosition += block.Length;
                        Length += block.Length;
                    }

                    yield return block;
                    dataLeft -= block.Length;
                }
            }
        }
    }
}