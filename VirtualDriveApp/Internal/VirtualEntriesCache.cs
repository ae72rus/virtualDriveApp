using System;
using System.Collections.Generic;

namespace VirtualDrive.Internal
{
    internal class VirtualEntriesCache<TVirtualEntry> : IDisposable where TVirtualEntry : BaseVirtualEntity
    {
        private volatile object _lockObject = new object();
        private readonly Dictionary<long, TVirtualEntry> _cachedData = new Dictionary<long, TVirtualEntry>();

        public bool TryGet(long id, out TVirtualEntry entry)
        {
            lock (_lockObject)
                return _cachedData.TryGetValue(id, out entry);
        }

        public void Add(long id, TVirtualEntry entry)
        {
            lock (_lockObject)
                _cachedData[id] = entry;
        }

        public void Remove(long id)
        {
            lock (_lockObject)
            {
                if (_cachedData.ContainsKey(id))
                    _cachedData.Remove(id);
            }
        }

        public void Dispose()
        {
            lock (_lockObject)
                _cachedData.Clear();
        }
    }
}