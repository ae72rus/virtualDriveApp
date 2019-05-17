using System;
using VirtualDrive.Internal.Drive.Operations;
using VirtualDrive.Internal.RawData.Threading;

namespace VirtualDrive.Internal.RawData.Writers
{
    internal class SectorInfoRawWriter : BaseRawWriter
    {
        public SectorInfoRawWriter(VirtualDriveParameters virtualDriveParameters, DriveAccessSynchronizer synchronizer)
            : base(virtualDriveParameters, synchronizer, FixedPositions.SectorsInformation)
        {
            Length = VirtualDriveParameters.SectorsInformationRegionLength;
        }

        protected override bool CheckCanWriteInternal(int bytesLength)
        {
            return InitialPosition + Length > CurrentPosition + bytesLength;
        }

        protected override WriteOperation MakeOperation(OperationHint hint, byte[] bytes, long position)
        {
            return new FileTableOperation(drive => drive.Write(position, bytes), position);
        }

        public WriteOperation SetContentSectorLength(long sectorRegionDataStartPosition, long sectorLength)
        {
            var position = sectorRegionDataStartPosition + ByteHelper.GetLength<int>() + ByteHelper.GetLength<long>();
            var bytes = BitConverter.GetBytes(sectorLength);
            var operation = new FileTableOperation(drive =>
            {
                var retv = drive.Write(position, bytes);
                return retv;
            }, position);
            Synchronizer.EnqueueOperation(operation);
            return operation;
        }

        public void SetCurrentPostion(long value)
        {
            CurrentPosition = value;
        }
    }
}