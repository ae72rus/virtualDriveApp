using System;
using VirtualDrive.Internal.RawData;

namespace VirtualDrive.Internal
{
    internal abstract class BaseEntry : IByteSource
    {
        private string _name;
        public long Id { get; set; }
        public int Length => GetEntryLength();
        public long DirectoryId { get; set; }
        public long Position { get; set; }
        public ServiceMarks Mark => GetMark();

        public string Name
        {
            get => _name;
            set
            {
                if (Id != 0 && string.IsNullOrWhiteSpace(value))//root has empty name
                    throw new InvalidOperationException("Name could not be empty");

                _name = value;
            }
        }

        public DateTime CreationTime { get; set; } = DateTime.Now;
        public DateTime ModificationTime { get; set; } = DateTime.Now;

        public abstract byte[] GetBytes();
        public abstract int GetEntryLength();
        protected abstract ServiceMarks GetMark();
    }
}