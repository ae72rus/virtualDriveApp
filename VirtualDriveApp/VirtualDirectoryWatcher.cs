using System;

namespace VirtualDrive
{
    public sealed class VirtualDirectoryWatcher : IDisposable
    {
#if !DEBUG
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] //no one should see my secrets :)
#endif
        private readonly VirtualDirectoryWatcherSource _source;
        public event EventHandler<DirectoryEventArgs> DirectoryEvent;
        public event EventHandler<FileEventArgs> FileEvent;
        public event EventHandler NameChanged;
        public VirtualDirectory Directory { get; }


        internal VirtualDirectoryWatcher(VirtualDirectory directory, VirtualDirectoryWatcherSource source)
        {
            Directory = directory ?? throw new ArgumentNullException(nameof(directory));
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        internal void RaiseCreated(VirtualDirectory directory)
        {
            DirectoryEvent?.Invoke(this, new DirectoryEventArgs(directory, WatcherEvent.Created));
        }

        internal void RaiseCreated(VirtualFile file)
        {
            FileEvent?.Invoke(this, new FileEventArgs(file, WatcherEvent.Created));
        }

        internal void RaiseUpdated(VirtualDirectory directory)
        {
            DirectoryEvent?.Invoke(this, new DirectoryEventArgs(directory, WatcherEvent.Updated));
        }

        internal void RaiseUpdated(VirtualFile file)
        {
            FileEvent?.Invoke(this, new FileEventArgs(file, WatcherEvent.Updated));
        }

        internal void RaiseDeleted(VirtualDirectory directory)
        {
            DirectoryEvent?.Invoke(this, new DirectoryEventArgs(directory, WatcherEvent.Deleted));
        }

        internal void RaiseDeleted(VirtualFile file)
        {
            FileEvent?.Invoke(this, new FileEventArgs(file, WatcherEvent.Deleted));
        }

        internal void RaiseNameChanged()
        {
            NameChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (!_source.Free(this))
                return;

            NameChanged = null;
            DirectoryEvent = null;
            FileEvent = null;
        }
    }
}