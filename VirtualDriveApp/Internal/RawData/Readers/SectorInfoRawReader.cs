using VirtualDrive.Internal.RawData.Threading;

namespace VirtualDrive.Internal.RawData.Readers
{
    internal class SectorInfoRawReader : BaseRawReader
    {
        private readonly VirtualDriveParameters _parameters;

        public SectorInfoRawReader(DriveAccessSynchronizer synchronizer, VirtualDriveParameters parameters)
            : base(synchronizer, FixedPositions.SectorsInformation)
        {
            _parameters = parameters;
        }

        public override bool CheckCanRead(int count)
        {
            return CurrentPosition + count <= InitialPosition + Length;
        }

        protected override int GetLength()
        {
            return _parameters.SectorsInformationRegionLength;
        }
    }
}