namespace VirtualDrive.Internal.RawData
{
    public enum ServiceMarks
    {
        EntriesSector = 0x0,
        ContentSector = 0x1,
        FileEntry = 0x10,
        DirectoryEntry = 0x11,
        Proceed = 0xff
    }
}