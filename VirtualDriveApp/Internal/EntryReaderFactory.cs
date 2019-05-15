using System;
using System.Linq;
using VirtualDrive.Internal.RawData;

namespace VirtualDrive.Internal
{
    internal static class EntryReaderFactory
    {
        public static EntryReader Create(byte[] block, long position)
        {
            if (block?.Any() != true)
                throw new ArgumentException(nameof(block));

            var firstByte = block.First();

            switch (firstByte)
            {
                case (byte)ServiceMarks.DirectoryEntry:
                    return new DirectoryEntryReader(block, position);
                case (byte)ServiceMarks.FileEntry:
                    return new FileEntryReader(block, position);
                case (byte)ServiceMarks.Proceed:
                    return null;
                default:
                    throw new InvalidOperationException($"Unknown block at position: {position}");
            }
        }
    }
}