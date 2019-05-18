using System;
using System.Collections.Generic;
using VirtualDrive.Internal.Drive.Operations;
using VirtualDrive.Internal.RawData.Threading;

namespace VirtualDrive.Internal.RawData.Writers
{
    internal class EntryRawWriter : OptimizedRawWriter
    {
        public EntryRawWriter(VirtualDriveParameters virtualDriveParameters, DriveAccessSynchronizer synchronizer, long initialPosition)
            : base(virtualDriveParameters, synchronizer, initialPosition)
        {
            Length = virtualDriveParameters.EntriesTableSectorLength;
        }

        public void SetCurrentPosition(long position)
        {
            if (position > Length)
                throw new AccessViolationException("Attempt to access protected memory occured");

            CurrentPosition = position;
        }

        public void SetInitialPosition(long position)
        {
            InitialPosition = position;
            CurrentPosition = position;
        }

        protected override bool CheckCanWriteInternal(int bytesLength)
        {
            return VirtualDriveParameters.EntriesTableSectorLength - CurrentPosition > bytesLength;
        }

        protected override bool HandleAddedAvailableBlock(DriveBlock block)
        {
            //min entry length = 42
            if (block.Length < 42)
                return false;

            var blockLengthBytes = BitConverter.GetBytes(block.Length);

            var bytesList = new List<byte>(blockLengthBytes)
            {
                (byte)ServiceMarks.Proceed
            };

            var operation = new FileTableOperation(drive => drive.Write(block.Position, bytesList.ToArray()), block.Position);
            Synchronizer.EnqueueOperation(operation);
            return true;
        }

        protected override bool TryGetAvailableBlock(int length, out DriveBlock block)
        {
            return base.TryGetAvailableBlock(length, out block) && block.Length >= 42;
        }

        protected override WriteOperation MakeOperation(OperationHint hint, byte[] bytes, long position)
        {
            return new FileTableOperation(drive => drive.Write(position, bytes), position);
        }

        public WriteOperation WriteTo(long position, byte[] data)
        {
            var operation = new FileTableOperation(drive => drive.Write(position, data), position);
            Synchronizer.EnqueueOperation(operation);
            return operation;
        }
    }
}