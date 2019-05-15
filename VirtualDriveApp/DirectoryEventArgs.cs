using System;

namespace VirtualDrive
{
    public sealed class DirectoryEventArgs : EventArgs
    {
        public VirtualDirectory Directory { get; }
        public WatcherEvent Event { get; }
        internal DirectoryEventArgs(VirtualDirectory directory, WatcherEvent @event)
        {
            Directory = directory;
            Event = @event;
        }

    }
}