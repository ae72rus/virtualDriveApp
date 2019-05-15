using System;
using VirtualDrive.Internal.Drive.Operations;
using VirtualDrive.Internal.RawData.Threading;

namespace VirtualDrive.Internal.RawData.Readers
{
    internal abstract class BaseRawReader
    {
        protected DriveAccessSynchronizer Synchronizer { get; }
        public long InitialPosition { get; protected set; }
        public long CurrentPosition { get; protected set; }
        public long Length => GetLength();

        protected BaseRawReader(DriveAccessSynchronizer synchronizer, long initialPosition)
        {
            Synchronizer = synchronizer;
            InitialPosition = initialPosition;
            CurrentPosition = initialPosition;
        }

        public ReadOperation Read(byte[] buffer, int offset, int count)
        {
            if (!CheckCanRead(count))
                throw new InvalidOperationException("Unable to perform read operation");

            var position = CurrentPosition;
            var operation = new ReadOperation(drive =>
            {
                drive.Position = position;
                return drive.Read(buffer, offset, count);
            });
            CurrentPosition += count;
            Synchronizer.EnqueueOperation(operation);
            return operation;
        }

        public ReadOperation ReadFrom(long position, byte[] buffer, int offset, int count)
        {
            CurrentPosition = position;
            return Read(buffer, offset, count);
        }

        public abstract bool CheckCanRead(int count);

        protected abstract int GetLength();
    }
}