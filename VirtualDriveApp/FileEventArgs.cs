using System;

namespace VirtualDrive
{
    public sealed class FileEventArgs : EventArgs
    {
        public VirtualFile File { get; }
        public WatcherEvent Event { get; }
        internal FileEventArgs(VirtualFile file, WatcherEvent @event)
        {
            File = file;
            Event = @event;
        }
    }
}