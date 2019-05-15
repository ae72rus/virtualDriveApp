using System;
using System.Collections.Generic;
using System.Linq;

namespace VirtualDrive.Internal
{
    internal class Locker
    {

        #region Nested stuff

        internal class EntryLocker
        {
            private volatile object _lockObject = new object();
            private readonly Dictionary<BaseEntry, int> _dictionary = new Dictionary<BaseEntry, int>();

            public void Lock(BaseEntry entry)
            {
                lock (_lockObject)
                {
                    if (!_dictionary.ContainsKey(entry))
                        _dictionary.Add(entry, 0);

                    _dictionary[entry]++;
                }
            }

            public void Unlock(BaseEntry entry)
            {
                lock (_lockObject)
                {
                    if (CheckIsLocked(entry))
                        _dictionary[entry]--;
                }
            }

            public bool CheckIsLocked(BaseEntry entry)
            {
                lock (_lockObject)
                    return _dictionary.ContainsKey(entry) && _dictionary[entry] > 0;
            }
        }

        internal enum Operation
        {
            Read,
            Write
        }

        internal class EntryLockerAccessorFactory
        {
            public static Func<Locker, EntryLocker> GetEntryLockerAccessor(Operation operation)
            {
                switch (operation)
                {
                    case Operation.Read:
                        return locker => locker._readLocker;
                    case Operation.Write:
                        return locker => locker._writeLocker;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
                }
            }
        }

        internal abstract class LockerOperation : IDisposable
        {
            private bool _isDisposed;
            private readonly Locker _locker;
            private readonly IEnumerable<BaseEntry> _entries;
            private readonly Operation _operation;

            protected LockerOperation(Locker locker, IEnumerable<BaseEntry> entries,
                Operation operation)
            {
                _locker = locker ?? throw new ArgumentNullException(nameof(locker));
                _entries = entries ?? throw new ArgumentNullException(nameof(entries));
                _operation = operation;

                foreach (var entry in _entries)
                    EntryLockerAccessorFactory.GetEntryLockerAccessor(_operation)(_locker).Lock(entry);
            }

            public void Dispose()
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;
                foreach (var entry in _entries)
                    EntryLockerAccessorFactory.GetEntryLockerAccessor(_operation)(_locker).Unlock(entry);
            }
        }

        internal class ReadLockerOperation : LockerOperation
        {
            public ReadLockerOperation(Locker locker, IEnumerable<BaseEntry> entries)
                : base(locker, entries, Operation.Read)
            {
            }
        }

        internal class WriteLockerOperation : LockerOperation
        {
            public WriteLockerOperation(Locker locker, IEnumerable<BaseEntry> entries)
                : base(locker, entries, Operation.Write)
            {
            }
        }
        #endregion

        private volatile object _readLockObject = new object();
        private volatile object _writeLockObject = new object();

        private readonly EntryLocker _writeLocker = new EntryLocker();
        private readonly EntryLocker _readLocker = new EntryLocker();

        public LockerOperation LockReading(BaseEntry entry)
        {
            return LockReading(new[] { entry });
        }

        public LockerOperation LockWriting(BaseEntry entry)
        {
            return LockWriting(new[] { entry });
        }

        public LockerOperation LockReading(IEnumerable<BaseEntry> entries)
        {
            lock (_readLockObject)
                return new ReadLockerOperation(this, entries);
        }

        public LockerOperation LockWriting(IEnumerable<BaseEntry> entries)
        {
            lock (_writeLockObject)
                return new WriteLockerOperation(this, entries);
        }

        public bool CanRead(BaseEntry entry)
        {
            lock (_readLockObject)
            {
                var entryLocker = EntryLockerAccessorFactory.GetEntryLockerAccessor(Operation.Read)(this);
                return !entryLocker.CheckIsLocked(entry);
            }
        }

        public bool CanRead(IEnumerable<BaseEntry> entries)
        {
            lock (_readLockObject)
                return entries.All(CanRead);
        }

        public bool CanWrite(BaseEntry entry)
        {
            lock (_writeLockObject)
            {
                var entryLocker = EntryLockerAccessorFactory.GetEntryLockerAccessor(Operation.Write)(this);
                return !entryLocker.CheckIsLocked(entry);
            }
        }

        public bool CanWrite(IEnumerable<BaseEntry> entries)
        {
            lock (_readLockObject)
                return entries.All(CanWrite);
        }

        public bool TryLockWriting(BaseEntry entry, out LockerOperation operation)
        {
            lock (_writeLockObject)
            {
                operation = null;
                if (!CanWrite(entry))
                    return false;

                operation = LockWriting(entry);
                return true;
            }
        }

        public bool TryLockWriting(IEnumerable<BaseEntry> entries, out LockerOperation operation)
        {
            lock (_writeLockObject)
            {
                operation = null;
                if (!CanWrite(entries))
                    return false;

                operation = LockWriting(entries);
                return true;
            }
        }
    }
}