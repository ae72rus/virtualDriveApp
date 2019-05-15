using VirtualDrive.Internal.RawData;

namespace VirtualDrive.Internal
{
    internal class DirectoryEntryReader : EntryReader<DirectoryEntry>
    {
        public override ServiceMarks Mark => ServiceMarks.DirectoryEntry;

        public DirectoryEntryReader(byte[] block, long position) : base(block, position)
        {
        }

        public override DirectoryEntry GetEntry() => DirectoryEntry.Read(Block);
    }
}