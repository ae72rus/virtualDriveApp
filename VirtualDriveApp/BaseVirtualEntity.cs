using System;
using System.Diagnostics;
using VirtualDrive.Internal;

namespace VirtualDrive
{
    [DebuggerDisplay("Name: {" + nameof(Name) + "}")]
    public abstract class BaseVirtualEntity
    {
#if !DEBUG
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] //no one should see my secrets :)
#endif
        private readonly BaseEntry _entry;

#if !DEBUG
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] //no one should see my secrets :)
#endif
        internal InternalFileSystem FileSystem { get; }
        internal long Id { get; }

        public abstract string Name { get; set; }

        public DateTime CreatedAt { get; }
        public DateTime LastEditAt => _entry.ModificationTime;

        internal BaseVirtualEntity(InternalFileSystem fileSystem, BaseEntry entry)
        {
            _entry = entry;
            FileSystem = fileSystem;
            Id = entry.Id;
            CreatedAt = entry.CreationTime;
        }
    }
}