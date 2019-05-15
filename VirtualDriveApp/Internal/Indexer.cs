using System;
using System.Collections.Generic;

namespace VirtualDrive.Internal
{
    internal class Indexer : IDisposable
    {
        private volatile object _filesLockObject = new object();
        private volatile object _directoriesLockObject = new object();
        private long _newFileId;
        private long _newDirectoryId;
        private readonly Dictionary<long, List<FileEntry>> _directoryFiles = new Dictionary<long, List<FileEntry>>();
        private readonly Dictionary<long, List<DirectoryEntry>> _nestedDirectories = new Dictionary<long, List<DirectoryEntry>>();
        private readonly Dictionary<long, FileEntry> _files = new Dictionary<long, FileEntry>();
        private readonly Dictionary<long, DirectoryEntry> _directories = new Dictionary<long, DirectoryEntry>();

        public long GetNewFileId()
        {
            lock (_filesLockObject)
            {
                return _newFileId++;
            }
        }

        public long GetNewDirectoryId()
        {
            lock (_directoriesLockObject)
            {
                return _newDirectoryId++;
            }
        }

        public void Populate(IEnumerable<BaseEntry> entries)
        {
            lock (_directoriesLockObject)
                lock (_filesLockObject)
                {
                    foreach (var entry in entries)
                    {
                        switch (entry)
                        {
                            case DirectoryEntry directoryEntry:
                                _directories[directoryEntry.Id] = directoryEntry;
                                if (_newDirectoryId <= directoryEntry.Id)
                                    _newDirectoryId = directoryEntry.Id + 1;
                                break;
                            case FileEntry fileEntry:
                                _files[fileEntry.Id] = fileEntry;
                                if (_newFileId <= fileEntry.Id)
                                    _newFileId = fileEntry.Id + 1;
                                break;
                        }
                    }

                    foreach (var directoryEntry in _directories.Values)
                    {
                        if (directoryEntry.DirectoryId == -1) //if root dir
                            continue;

                        if (!_nestedDirectories.ContainsKey(directoryEntry.DirectoryId))
                            _nestedDirectories[directoryEntry.DirectoryId] = new List<DirectoryEntry>();

                        _nestedDirectories[directoryEntry.DirectoryId].Add(directoryEntry);
                    }

                    foreach (var fileEntry in _files.Values)
                    {
                        if (!_directoryFiles.ContainsKey(fileEntry.DirectoryId))
                            _directoryFiles[fileEntry.DirectoryId] = new List<FileEntry>();

                        _directoryFiles[fileEntry.DirectoryId].Add(fileEntry);
                    }
                }
        }

        public void AddEntry(BaseEntry entry)
        {
            switch (entry)
            {
                case FileEntry file:
                    addFile(file);
                    break;
                case DirectoryEntry directory:
                    addDirectory(directory);
                    break;
            }
        }

        public void RemoveEntry(BaseEntry entry)
        {
            switch (entry)
            {
                case FileEntry file:
                    removeFile(file);
                    break;
                case DirectoryEntry directory:
                    removeDirectory(directory);
                    break;
            }
        }

        public bool TryGetFile(long id, out FileEntry file)
        {
            lock (_filesLockObject)
            {
                return _files.TryGetValue(id, out file);
            }
        }

        public bool TryGetDirectory(long id, out DirectoryEntry directory)
        {
            lock (_directoriesLockObject)
            {
                return _directories.TryGetValue(id, out directory);
            }
        }

        public bool TryGetDirectoryFiles(long id, out ICollection<FileEntry> files)
        {
            lock (_directoriesLockObject)
                lock (_filesLockObject)
                {
                    var retv = _directoryFiles.TryGetValue(id, out var list);
                    files = list;
                    return retv;
                }
        }

        public bool TryGetNestedDirectories(long id, out ICollection<DirectoryEntry> directories)
        {
            lock (_directoriesLockObject)
            {
                var retv = _nestedDirectories.TryGetValue(id, out var list);
                directories = list;
                return retv;
            }
        }

        public bool TryGetParentDirectory(BaseEntry entry, out DirectoryEntry directory)
        {
            lock (_directoriesLockObject)
            {
                return _directories.TryGetValue(entry.DirectoryId, out directory);
            }
        }

        private void removeDirectory(DirectoryEntry directory)
        {
            lock (_directoriesLockObject)
            {
                _directories.Remove(directory.Id);
                _nestedDirectories[directory.DirectoryId].Remove(directory);
            }
        }

        private void removeFile(FileEntry file)
        {
            lock (_filesLockObject)
            {
                _files.Remove(file.Id);
                _directoryFiles[file.DirectoryId].Remove(file);
            }
        }

        private void addDirectory(DirectoryEntry directory)
        {
            lock (_directoriesLockObject)
            {
                _directories.Add(directory.Id, directory);

                if (!_nestedDirectories.ContainsKey(directory.DirectoryId))
                    _nestedDirectories[directory.DirectoryId] = new List<DirectoryEntry>();

                _nestedDirectories[directory.DirectoryId].Add(directory);
            }
        }

        private void addFile(FileEntry file)
        {
            lock (_filesLockObject)
            {
                _files.Add(file.Id, file);

                if (!_directoryFiles.ContainsKey(file.DirectoryId))
                    _directoryFiles[file.DirectoryId] = new List<FileEntry>();

                _directoryFiles[file.DirectoryId].Add(file);
            }
        }

        public void Dispose()
        {
            lock (_directoriesLockObject)
                lock (_filesLockObject)
                {
                    _directoryFiles.Clear();
                    _nestedDirectories.Clear();
                    _files.Clear();
                    _directories.Clear();
                }
        }
    }
}