using VirtualDrive.Internal.RawData;

namespace VirtualDrive.Internal
{
    internal class FileEntryReader : EntryReader<FileEntry>
    {
        public override ServiceMarks Mark => ServiceMarks.FileEntry;


        public FileEntryReader(byte[] block, long position) : base(block, position)
        {
        }

        public override FileEntry GetEntry() => FileEntry.Read(Block);
    }
}