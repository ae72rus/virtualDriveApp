namespace VirtualDrive
{
    public class VirtualDriveParameters
    {
        private static VirtualDriveParameters _default;

        public static VirtualDriveParameters Default => _default
                                                        ?? (_default = new VirtualDriveParameters(4 * 1024 * 1024, 1 * 1024 * 1024));

        public int SectorsInformationRegionLength { get; }
        public int EntriesTableSectorLength { get; }
        public VirtualDriveParameters(int entriesTableSectorLength, int sectorsInformationRegionLength)
        {
            SectorsInformationRegionLength = sectorsInformationRegionLength;
            EntriesTableSectorLength = entriesTableSectorLength;
        }
    }
}