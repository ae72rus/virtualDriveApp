using System.IO;
using VirtualDrive.Internal.Drive.Operations;
using VirtualDrive.Internal.RawData.Threading;

namespace VirtualDrive.Internal.RawData.Writers
{
    internal abstract class BaseRawWriter
    {
        protected DriveAccessSynchronizer Synchronizer { get; }
        private volatile object _lockObject = new object();
        protected VirtualDriveParameters VirtualDriveParameters { get; }

        public long CurrentPosition { get; protected set; }

        public long InitialPosition { get; protected set; }
        public long Length { get; protected set; }

        protected BaseRawWriter(VirtualDriveParameters virtualDriveParameters, DriveAccessSynchronizer synchronizer, long initialPosition)
        {
            Synchronizer = synchronizer;
            VirtualDriveParameters = virtualDriveParameters;
            CurrentPosition = initialPosition;
            InitialPosition = initialPosition;
        }

        public WriteOperation Write(byte[] bytes)
        {
            var length = bytes.Length;
            var hint = GetOperationHint();

            var position = 0L;
            lock (_lockObject)
            {
                if (!CheckCanWriteInternal(length))
                    throw new IOException("Cannot perform write operation");

                position = GetWritePositionLocked(hint, length);
                SetCurrentPositionLocked(hint, length);
            }

            var operation = MakeOperation(hint, bytes, position);


            Synchronizer.EnqueueOperation(operation);
            return operation;
        }

        protected virtual long GetWritePositionLocked(OperationHint hint, int length)
        {
            return CurrentPosition;
        }
        protected virtual void SetCurrentPositionLocked(OperationHint hint, int writtenBytesCount)
        {
            CurrentPosition += writtenBytesCount;
        }

       
        protected virtual OperationHint GetOperationHint()
        {
            return new OperationHint();
        }

        public bool CheckCanWrite(int bytesLength)
        {
            lock (_lockObject)
                return (long)CurrentPosition + (long)bytesLength <= int.MaxValue && CheckCanWriteInternal(bytesLength);
        }

        protected abstract bool CheckCanWriteInternal(int bytesLength);

        protected abstract WriteOperation MakeOperation(OperationHint hint, byte[] bytes, long position);
    }
}