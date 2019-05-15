using VirtualDrive.Internal.RawData.Threading;

namespace VirtualDrive.Internal.RawData.Readers
{
    internal class VirtualDriveParametersRawReader : BaseRawReader
    {
        public VirtualDriveParametersRawReader(DriveAccessSynchronizer synchronizer)
            : base(synchronizer, FixedPositions.VirtualDriveParameters)
        {
        }

        public override bool CheckCanRead(int count)
        {
            return CurrentPosition + count <= InitialPosition + Length;
        }

        protected override int GetLength()
        {
            return ByteHelper.GetLength<int>() * 2;
        }
    }
}