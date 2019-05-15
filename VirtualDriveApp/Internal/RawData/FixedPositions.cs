namespace VirtualDrive.Internal.RawData
{
    internal static class FixedPositions
    {
        public static long VirtualDriveParameters => 0;
        public static long SectorsInformation => 8;// right after VD parameters
    }
}