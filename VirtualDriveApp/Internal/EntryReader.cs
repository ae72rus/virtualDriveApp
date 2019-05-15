using System;
using VirtualDrive.Internal.RawData;

namespace VirtualDrive.Internal
{
    internal abstract class EntryReader<TEntry> : EntryReader
        where TEntry : BaseEntry
    {
        protected EntryReader(byte[] block, long position) : base(block, position)
        {
        }

        public abstract TEntry GetEntry();

        protected override T GetEntryInternal<T>()
        {
            var entry = GetEntry();
            if (entry == null)
                return null;

            return entry as T ?? throw new InvalidCastException();
        }
    }

    internal abstract class EntryReader
    {
        private BaseEntry _baseEntry;
        public byte[] Block { get; }
        public long Position { get; }
        public abstract ServiceMarks Mark { get; }

        protected EntryReader(byte[] block, long position)
        {
            Block = block;
            Position = position;
        }

        public T GetEntry<T>() where T : BaseEntry
        {
            var retv = _baseEntry ?? (_baseEntry = GetEntryInternal<T>());
            retv.Position = Position;
            return retv as T ?? throw new InvalidCastException();
        }

        protected abstract T GetEntryInternal<T>() where T : BaseEntry;
    }
}