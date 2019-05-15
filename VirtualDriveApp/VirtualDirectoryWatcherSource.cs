using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace VirtualDrive
{
    internal class VirtualDirectoryWatcherSource : IDisposable
    {
        private volatile object _lockObject = new object();
        private bool _isDisposed;
        private readonly SynchronizationContext _synchronizationContext;
        private readonly Dictionary<VirtualDirectoryWatcher, int> _watchersAllocations = new Dictionary<VirtualDirectoryWatcher, int>();
        private readonly Dictionary<VirtualDirectory, VirtualDirectoryWatcher> _directoryWatchers = new Dictionary<VirtualDirectory, VirtualDirectoryWatcher>();

        public VirtualDirectoryWatcherSource(SynchronizationContext synchronizationContext)
        {
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
        }

        public VirtualDirectoryWatcher Alloc(VirtualDirectory directory)
        {
            lock (_lockObject)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(VirtualDirectoryWatcherSource));

                if (!_directoryWatchers.ContainsKey(directory))
                {
                    var newWatcher = new VirtualDirectoryWatcher(directory, this);
                    _directoryWatchers[directory] = newWatcher;
                    _watchersAllocations[newWatcher] = 0;
                }

                var watcher = _directoryWatchers[directory];
                _watchersAllocations[watcher]++;
                return watcher;
            }
        }

        public bool Free(VirtualDirectoryWatcher watcher)
        {
            lock (_lockObject)
            {
                if (_isDisposed)
                    return false;

                if (!_directoryWatchers.ContainsKey(watcher.Directory))
                    return true;

                var retv = --_watchersAllocations[watcher] <= 0;
                if (!retv)
                    return false;

                _directoryWatchers.Remove(watcher.Directory);
                _watchersAllocations.Remove(watcher);

                return true;
            }
        }

        public void RaiseCreated(VirtualDirectory src, VirtualDirectory directory)
        {
            if (_isDisposed)
                return;
            if (!tryGetWatcher(src, out var watcher))
                return;

            raiseEvent(() => watcher.RaiseCreated(directory));
        }

        public void RaiseCreated(VirtualDirectory src, VirtualFile file)
        {
            if (_isDisposed)
                return;
            if (!tryGetWatcher(src, out var watcher))
                return;

            raiseEvent(() => watcher.RaiseCreated(file));
        }

        public void RaiseUpdated(VirtualDirectory src, VirtualDirectory directory)
        {
            if (_isDisposed)
                return;
            if (!tryGetWatcher(src, out var watcher))
                return;

            raiseEvent(() => watcher.RaiseUpdated(directory));
        }

        public void RaiseUpdated(VirtualDirectory src, VirtualFile file)
        {
            if (_isDisposed)
                return;
            if (!tryGetWatcher(src, out var watcher))
                return;

            raiseEvent(() => watcher.RaiseUpdated(file));
        }

        public void RaiseDeleted(VirtualDirectory src, VirtualDirectory directory)
        {
            if (_isDisposed)
                return;
            if (!tryGetWatcher(src, out var watcher))
                return;

            raiseEvent(() => watcher.RaiseDeleted(directory));
        }

        public void RaiseDeleted(VirtualDirectory src, VirtualFile file)
        {
            if (_isDisposed)
                return;
            if (!tryGetWatcher(src, out var watcher))
                return;

            raiseEvent(() => watcher.RaiseDeleted(file));
        }

        public void RaiseNameChanged(VirtualDirectory src)
        {
            if (_isDisposed)
                return;
            if (!tryGetWatcher(src, out var watcher))
                return;

            raiseEvent(() => watcher.RaiseNameChanged());
        }

        private void raiseEvent(Action eventAction)
        {
            try
            {
                _synchronizationContext.Send(x => eventAction?.Invoke(), null);
            }
            catch (Exception)
            {
                
            }
        }

        private bool tryGetWatcher(VirtualDirectory src, out VirtualDirectoryWatcher watcher)
        {
            lock (_lockObject)
            {

                watcher = null;
                if (_isDisposed)
                    return false;

                var retv = _directoryWatchers.ContainsKey(src);
                if (retv)
                    watcher = _directoryWatchers[src];

                return retv;
            }
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;
                foreach (var watcher in _watchersAllocations.Keys.ToList())
                {
                    _watchersAllocations[watcher] = 0;
                    watcher.Dispose();
                }

                _directoryWatchers.Clear();
                _watchersAllocations.Clear();
            }
        }
    }
}