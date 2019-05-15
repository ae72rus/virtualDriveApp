using VirtualDrive.Internal.Drive.Operations;
using VirtualDrive.Internal.RawData.Threading;

namespace VirtualDrive.Internal.RawData.Writers
{
    internal class VirtualDriveParametersRawWriter : BaseRawWriter
    {
        public VirtualDriveParametersRawWriter(DriveAccessSynchronizer synchronizer)
            : base(null, synchronizer, FixedPositions.VirtualDriveParameters)
        {
        }

        protected override bool CheckCanWriteInternal(int bytesLength)
        {
            return bytesLength == ByteHelper.GetLength<int>() * 2;
        }

        protected override WriteOperation MakeOperation(OperationHint hint, byte[] bytes, long position)
        {
            return new FileTableOperation(drive => drive.Write(position, bytes), position);
        }
    }
}