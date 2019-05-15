using VirtualDrive.Internal.RawData.Threading;

namespace VirtualDrive.Internal.RawData.Readers
{
    internal class EntriesTableRawReader : BaseRawReader
    {
        private readonly VirtualDriveParameters _parameters;

        public EntriesTableRawReader(DriveAccessSynchronizer synchronizer, VirtualDriveParameters parameters, long initialPosition)
            : base(synchronizer, initialPosition)
        {
            _parameters = parameters;
        }

        public void SetInitialPosition(long position)
        {
            InitialPosition = position;
            CurrentPosition = position;
        }

        public override bool CheckCanRead(int count)
        {
            var retv =  CurrentPosition + count <= InitialPosition + Length;
            return retv;
        }

        protected override int GetLength()
        {
            return _parameters.EntriesTableSectorLength;
        }
    }
}